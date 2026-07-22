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
using DiskSpaceMonitor.Widgets.Circular;
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
        private readonly WidgetRegistry _registry = new(new CircularWidget());

        private ISettingsStore _store = null!;
        private IDriveReader _driveReader = null!;
        private IDriveCatalog _catalog = null!;
        private IAutoStartService _autoStart = null!;
        private WidgetSettings _settings = null!;
        private CtrlHook? _ctrlHook;
        private DispatcherTimer? _trimTimer;

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

            foreach (var cfg in _settings.Drives.ToList())
                ShowWidget(cfg);

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

            var window = new MainWindow(_settings, cfg, _driveReader, _registry);
            _windows.Add(window);
            window.Show();
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

            // Snapshot the current widget/config/opacity so a Cancel can revert the live preview.
            string savedWidget = _settings.Style;
            IWidgetConfig savedConfig = factory.ReadConfig(_settings.StyleConfig);
            double savedOpacity = _settings.WidgetOpacity;

            var shown = _settings.Drives.Select(d => d.DrivePath).ToList();
            var dialog = new SettingsWindow(
                shown, _settings.RefreshSeconds, _autoStart.IsEnabled(),
                _settings.Style, factory.ReadConfig(_settings.StyleConfig), _settings.WidgetOpacity,
                _catalog, _registry, PreviewWidget);
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
                _settings.StyleConfig = _registry.Get(dialog.SelectedWidget).WriteConfig(dialog.SelectedConfig) as JsonObject;
                _settings.WidgetOpacity = dialog.WidgetOpacity;

                foreach (var window in _windows)
                    window.ApplySettings();

                ApplyDriveSelection(dialog.SelectedDrivePaths, source.Width);
            }
            else
            {
                // Cancelled / closed: undo the live preview on every instance.
                foreach (var window in _windows)
                    window.ApplyWidget(savedWidget, savedConfig, savedOpacity);
            }
        }

        /// <summary>Apply an edited widget/config/opacity to every instance immediately (live preview).</summary>
        private void PreviewWidget(string widgetId, IWidgetConfig config, double widgetOpacity)
        {
            foreach (var window in _windows)
                window.ApplyWidget(widgetId, config, widgetOpacity);
        }

        private void ApplyDriveSelection(IReadOnlyList<string> desired, double newWidgetSize)
        {
            if (desired.Count == 0)
                return; // never leave zero widgets

            // Close widgets for drives no longer selected.
            foreach (var window in _windows.ToList())
            {
                if (!desired.Contains(window.Config.DrivePath))
                {
                    _windows.Remove(window);
                    _settings.Drives.Remove(window.Config);
                    window.Close();
                }
            }

            // Open widgets for newly selected drives.
            var current = _settings.Drives.Select(d => d.DrivePath).ToHashSet();
            foreach (var path in desired)
            {
                if (current.Add(path))
                {
                    // New widgets inherit the size of the one that opened Settings.
                    var cfg = new DriveWidgetConfig { DrivePath = path, Size = newWidgetSize };
                    _settings.Drives.Add(cfg);
                    ShowWidget(cfg);
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
