using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using DiskSpaceMonitor.Diagnostics;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Interop;
using DiskSpaceMonitor.Settings;
using DiskSpaceMonitor.Startup;
using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.Bar;
using DiskSpaceMonitor.Widgets.Circular;
using DiskSpaceMonitor.Widgets.Concentric;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor
{
    /// <summary>
    /// Composition root and window/lifecycle manager: wires up the services, opens
    /// one widget per configured drive, and coordinates add/remove, settings and exit.
    /// </summary>
    public partial class App : Application
    {
        private readonly List<MainWindow> _windows = new();
        private readonly WidgetRegistry _registry = new(new CircularWidget(), new ConcentricWidget(), new BarWidget());

        private ISettingsStore _store = null!;
        private IDriveReader _driveReader = null!;
        private IDriveCatalog _catalog = null!;
        private IAutoStartService _autoStart = null!;
        private WidgetSettings _settings = null!;
        private CtrlHook? _ctrlHook;
        private DispatcherTimer? _trimTimer;
        private bool _topologyShowsAll;   // what _windows is currently built for

        /// <summary>The running application instance.</summary>
        public static App Instance => (App)Current;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Log unexpected failures instead of letting the widget vanish silently.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            _store = new JsonSettingsStore();
            _driveReader = new DriveReader();
            _catalog = new DriveCatalog();
            _autoStart = new AutoStartService();

            _settings = _store.Load();

            // First run (or nothing configured): show the boot drive.
            if (_settings.Drives.Count == 0)
                _settings.Drives.Add(new DriveWidgetConfig { DrivePath = _catalog.BootDrivePath });

            RebuildWindows(_registry.Get(_settings.Style).ShowsAllDrives);

            _store.Save(_settings);

            // Drive edit mode from a single Ctrl notification instead of per-window
            // polling, so nothing is running while Ctrl is up.
            _ctrlHook = new CtrlHook();
            _ctrlHook.CtrlChanged += OnCtrlChanged;

            StartWorkingSetTrimming();
        }

        /// <summary>
        /// The widget is idle almost all the time, so hand the transient startup
        /// memory (JIT, XAML parse) back to the OS once the first frame has settled,
        /// then keep the reported working set low on a slow cadence.
        /// </summary>
        private void StartWorkingSetTrimming()
        {
            // Trim once after the initial render/layout has drained.
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(NativeMethods.TrimWorkingSet));

            _trimTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _trimTimer.Tick += (_, _) => NativeMethods.TrimWorkingSet();
            _trimTimer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trimTimer?.Stop();
            _ctrlHook?.Dispose();
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Record it and keep the widget running rather than tearing down the app.
            ErrorLog.Write("Dispatcher", e.Exception);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
            => ErrorLog.Write("AppDomain", e.ExceptionObject as Exception);

        private void OnCtrlChanged(bool ctrlDown)
        {
            foreach (var window in _windows)
                window.SetInteractive(ctrlDown);
        }

        private void ShowWidget(DriveWidgetConfig cfg)
        {
            // Give unplaced widgets the first free, non-overlapping spot.
            if (double.IsNaN(cfg.Left) || double.IsNaN(cfg.Top))
            {
                var spot = FindFreeSpot(cfg.Size);
                cfg.Left = spot.X;
                cfg.Top = spot.Y;
            }

            var window = new MainWindow(_settings, cfg, _driveReader, _registry, showsAllDrives: false);
            _windows.Add(window);
            window.Show();
        }

        /// <summary>
        /// Tear down and recreate the widget windows for the given topology: one window per drive
        /// (single-drive widget), or a single window fed every drive (multi-drive widget). Safe
        /// because the app uses OnExplicitShutdown, so closing all windows never exits.
        /// </summary>
        private void RebuildWindows(bool showsAllDrives)
        {
            foreach (var window in _windows.ToList())
                window.Close();
            _windows.Clear();

            if (showsAllDrives)
            {
                _settings.SingleInstance ??= new DriveWidgetConfig { DrivePath = "", Size = 240 };
                var single = _settings.SingleInstance;
                if (double.IsNaN(single.Left) || double.IsNaN(single.Top))
                {
                    var spot = FindFreeSpot(single.Size);
                    single.Left = spot.X;
                    single.Top = spot.Y;
                }

                var window = new MainWindow(_settings, single, _driveReader, _registry, showsAllDrives: true);
                _windows.Add(window);
                window.Show();
            }
            else
            {
                foreach (var cfg in _settings.Drives.ToList())
                    ShowWidget(cfg);
            }

            _topologyShowsAll = showsAllDrives;
        }

        /// <summary>Screen bounds of every widget except <paramref name="self"/>.</summary>
        public IReadOnlyList<Rect> OtherWidgetBounds(MainWindow self) =>
            _windows
                .Where(w => w != self && w.IsLoaded)
                .Select(w => new Rect(w.Left, w.Top, w.Width, w.Height))
                .ToList();

        /// <summary>First bottom-right-anchored slot that doesn't overlap an existing widget.</summary>
        private Point FindFreeSpot(double size)
        {
            var wa = SystemParameters.WorkArea;
            var taken = _windows
                .Where(w => w.IsLoaded)
                .Select(w => new Rect(w.Left, w.Top, w.Width, w.Height))
                .ToList();

            const double margin = 40;
            const double gap = 12;

            for (double top = wa.Bottom - size - margin; top >= wa.Top; top -= size + gap)
            {
                for (double left = wa.Right - size - margin; left >= wa.Left; left -= size + gap)
                {
                    var candidate = new Rect(left, top, size, size);
                    if (!taken.Any(t => Layout.WidgetLayout.Overlaps(candidate, t)))
                        return new Point(left, top);
                }
            }

            return new Point(wa.Right - size - margin, wa.Bottom - size - margin);
        }

        /// <summary>Hide a single drive's widget (the ✕ button). Keeps at least one.</summary>
        public void RemoveWidget(MainWindow window)
        {
            if (_windows.Count <= 1)
                return;

            _windows.Remove(window);
            _settings.Drives.Remove(window.Config);
            _store.Save(_settings);
            window.Close();
        }

        public void OpenSettings(MainWindow source)
        {
            var factory = _registry.Get(_settings.Style);

            // Snapshot for cancel-revert. Capture the size now — a preview rebuild may close 'source'.
            string savedWidget = _settings.Style;
            IWidgetConfig savedConfig = factory.ReadConfig(_settings.GetStyleConfig(_settings.Style));
            double savedOpacity = _settings.WidgetOpacity;
            double newWidgetSize = source.Width;

            // Each widget's initial config comes from its own stored blob (default if none yet), so
            // switching styles in the dialog restores that style's saved settings.
            IWidgetConfig ConfigFor(string id) => _registry.Get(id).ReadConfig(_settings.GetStyleConfig(id));

            var shown = _settings.Drives.Select(d => d.DrivePath).ToList();
            var dialog = new SettingsWindow(
                shown, _settings.RefreshSeconds, _autoStart.IsEnabled(),
                _settings.Style, ConfigFor(_settings.Style), _settings.WidgetOpacity,
                _catalog, _registry, PreviewWidget, ConfigFor);
            dialog.ShowDialog();

            if (dialog.ExitRequested)
            {
                ExitApplication();
                return;
            }

            if (dialog.Applied)
            {
                _settings.RefreshSeconds = dialog.RefreshSeconds;
                _autoStart.SetEnabled(dialog.AutoStart);

                _settings.Style = dialog.SelectedWidget;
                _settings.SetStyleConfig(dialog.SelectedWidget,
                    _registry.Get(dialog.SelectedWidget).WriteConfig(dialog.SelectedConfig) as JsonObject);
                _settings.WidgetOpacity = dialog.WidgetOpacity;

                // Match the window topology to the chosen widget before reconciling drives.
                bool targetShowsAll = _registry.Get(_settings.Style).ShowsAllDrives;
                if (targetShowsAll != _topologyShowsAll)
                    RebuildWindows(targetShowsAll);

                ApplyDriveSelection(dialog.SelectedDrivePaths, newWidgetSize);

                foreach (var window in _windows)
                    window.ApplySettings();

                _store.Save(_settings);
            }
            else
            {
                // Cancelled / closed: restore the saved topology + widget on every window.
                bool savedShowsAll = _registry.Get(savedWidget).ShowsAllDrives;
                if (savedShowsAll != _topologyShowsAll)
                    RebuildWindows(savedShowsAll);
                foreach (var window in _windows)
                    window.ApplyWidget(savedWidget, savedConfig, savedOpacity);
            }
        }

        /// <summary>Apply an edited widget/config/opacity to the live windows immediately (live
        /// preview). Rebuilds the window topology first if the previewed widget's instancing differs.
        /// Mutates only windows — never <c>_settings</c> — so Cancel can revert cleanly.</summary>
        private void PreviewWidget(string widgetId, IWidgetConfig config, double widgetOpacity)
        {
            bool showsAll = _registry.Get(widgetId).ShowsAllDrives;
            if (showsAll != _topologyShowsAll)
                RebuildWindows(showsAll);

            foreach (var window in _windows)
                window.ApplyWidget(widgetId, config, widgetOpacity);
        }

        private void ApplyDriveSelection(IReadOnlyList<string> desired, double newWidgetSize)
        {
            if (desired.Count == 0)
                return; // never leave zero drives

            if (_topologyShowsAll)
            {
                // Single multi-drive window: reconcile the drive list; the window re-reads it.
                _settings.Drives.RemoveAll(d => !desired.Contains(d.DrivePath));
                var have = _settings.Drives.Select(d => d.DrivePath).ToHashSet();
                foreach (var path in desired)
                    if (have.Add(path))
                        _settings.Drives.Add(new DriveWidgetConfig { DrivePath = path, Size = newWidgetSize });

                foreach (var window in _windows)
                    window.RefreshNow();
            }
            else
            {
                // One window per drive: close removed, open added.
                foreach (var window in _windows.ToList())
                {
                    if (!desired.Contains(window.Config.DrivePath))
                    {
                        _windows.Remove(window);
                        _settings.Drives.Remove(window.Config);
                        window.Close();
                    }
                }

                var current = _settings.Drives.Select(d => d.DrivePath).ToHashSet();
                foreach (var path in desired)
                {
                    if (current.Add(path))
                    {
                        var cfg = new DriveWidgetConfig { DrivePath = path, Size = newWidgetSize };
                        _settings.Drives.Add(cfg);
                        ShowWidget(cfg);
                    }
                }
            }

            _store.Save(_settings);
        }

        /// <summary>Persist the current settings (called by widgets after move/resize).</summary>
        public void SaveSettings() => _store.Save(_settings);

        public void ExitApplication()
        {
            _store.Save(_settings);
            Shutdown();
        }
    }
}
