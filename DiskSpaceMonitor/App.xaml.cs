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
using DiskSpaceMonitor.Widgets.Box;
using DiskSpaceMonitor.Widgets.Circular;
using DiskSpaceMonitor.Widgets.Concentric;
using DiskSpaceMonitor.Widgets.HorizontalBar;
using DiskSpaceMonitor.Widgets.VerticalBar;
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
        private readonly WidgetRegistry _registry = new(new CircularWidget(), new ConcentricWidget(),
            new VerticalBarWidget(), new HorizontalBarWidget(), new BoxWidget());

        private ISettingsStore _store = null!;
        private IDriveReader _driveReader = null!;
        private IDriveCatalog _catalog = null!;
        private IAutoStartService _autoStart = null!;
        private WidgetSettings _settings = null!;
        private CtrlHook? _ctrlHook;
        private DispatcherTimer? _trimTimer;
        private bool _topologyShowsAll;      // what _windows is currently built for
        private string _topologyStyle = "";  // and whose saved placement they were built from

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

            RebuildWindows(_settings.Style);

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

        private void ShowWidget(DriveWidgetConfig cfg, string styleId)
        {
            SeedSize(cfg, styleId);

            // Give unplaced widgets the first free, non-overlapping spot.
            if (double.IsNaN(cfg.Left) || double.IsNaN(cfg.Top))
            {
                var spot = FindFreeSpot(cfg.Width ?? cfg.Size, cfg.Height ?? cfg.Size);
                cfg.Left = spot.X;
                cfg.Top = spot.Y;
            }

            var window = new MainWindow(_settings, cfg, _driveReader, _registry, showsAllDrives: false);
            _windows.Add(window);
            window.Show();
        }

        /// <summary>
        /// True when the live windows already suit <paramref name="styleId"/>: the same instancing,
        /// and — since each multi-drive style keeps its own window rectangle — built from that
        /// style's own saved placement.
        /// </summary>
        private bool TopologySuits(string styleId)
        {
            bool showsAll = _registry.Get(styleId).ShowsAllDrives;
            return showsAll == _topologyShowsAll && (!showsAll || _topologyStyle == styleId);
        }

        /// <summary>
        /// Tear down and recreate the widget windows for a style: one window per drive (single-drive
        /// widget), or a single window fed every drive (multi-drive widget) placed at that style's
        /// own saved rectangle. Safe because the app uses OnExplicitShutdown, so closing all windows
        /// never exits.
        /// </summary>
        private void RebuildWindows(string styleId)
        {
            bool showsAllDrives = _registry.Get(styleId).ShowsAllDrives;

            foreach (var window in _windows.ToList())
                window.Close();
            _windows.Clear();

            if (showsAllDrives)
            {
                // Each multi-drive style keeps its own window rectangle, so switching between them
                // restores the frame that style was last given rather than inheriting the last one's.
                var single = _settings.SingleInstanceFor(styleId);
                SeedSize(single, styleId);
                if (double.IsNaN(single.Left) || double.IsNaN(single.Top))
                {
                    var spot = FindFreeSpot(single.Width ?? single.Size, single.Height ?? single.Size);
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
                    ShowWidget(cfg, styleId);
            }

            _topologyShowsAll = showsAllDrives;
            _topologyStyle = styleId;
        }

        /// <summary>
        /// Give a window that has never been sized the rectangle its style asks for. Only a style
        /// that sizes each direction separately offers one — a wide shape opening as a square would
        /// have to be dragged into shape on first use. A size the user has dragged is already
        /// recorded, so this never overrides their choice.
        /// </summary>
        private void SeedSize(DriveWidgetConfig cfg, string styleId)
        {
            if (cfg.Width is null && cfg.Height is null &&
                _registry.Get(styleId).DefaultWindowSize is { } size)
            {
                cfg.Width = size.Width;
                cfg.Height = size.Height;
            }
        }

        /// <summary>Screen bounds of every widget except <paramref name="self"/>.</summary>
        public IReadOnlyList<Rect> OtherWidgetBounds(MainWindow self) =>
            _windows
                .Where(w => w != self && w.IsLoaded)
                .Select(w => new Rect(w.Left, w.Top, w.Width, w.Height))
                .ToList();

        /// <summary>First bottom-right-anchored slot that doesn't overlap an existing widget.</summary>
        private Point FindFreeSpot(double width, double height)
        {
            var wa = SystemParameters.WorkArea;
            var taken = _windows
                .Where(w => w.IsLoaded)
                .Select(w => new Rect(w.Left, w.Top, w.Width, w.Height))
                .ToList();

            const double margin = 40;
            const double gap = 12;

            for (double top = wa.Bottom - height - margin; top >= wa.Top; top -= height + gap)
            {
                for (double left = wa.Right - width - margin; left >= wa.Left; left -= width + gap)
                {
                    var candidate = new Rect(left, top, width, height);
                    if (!taken.Any(t => Layout.WidgetLayout.Overlaps(candidate, t)))
                        return new Point(left, top);
                }
            }

            return new Point(wa.Right - width - margin, wa.Bottom - height - margin);
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
            GlobalAppearance savedAppearance = _settings.Appearance();

            // Size for any drive added while the dialog is open. Per-drive widgets are square, so a
            // freely-sized source (a bar graph stretched wide) contributes its shorter side rather
            // than handing a new gauge that whole span.
            double newWidgetSize = Math.Min(source.Width, source.Height);

            // Each widget's initial config comes from its own stored blob (default if none yet), so
            // switching styles in the dialog restores that style's saved settings.
            IWidgetConfig ConfigFor(string id) => _registry.Get(id).ReadConfig(_settings.GetStyleConfig(id));

            var shown = _settings.Drives.Select(d => d.DrivePath).ToList();
            var dialog = new SettingsWindow(
                shown, _settings.RefreshSeconds, _autoStart.IsEnabled(),
                _settings.Style, ConfigFor(_settings.Style), savedAppearance,
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
                var chosen = dialog.SelectedAppearance;
                _settings.WidgetOpacity = chosen.Opacity;
                _settings.FontFamily = chosen.Typography.FamilyName;
                _settings.MinFontSize = chosen.Typography.MinSize;
                _settings.MaxFontSize = chosen.Typography.MaxSize;

                // Match the windows to the chosen widget before reconciling drives — including a
                // swap between two multi-drive styles, which each have their own saved rectangle.
                if (!TopologySuits(_settings.Style))
                    RebuildWindows(_settings.Style);

                ApplyDriveSelection(dialog.SelectedDrivePaths, newWidgetSize);

                foreach (var window in _windows)
                    window.ApplySettings();

                _store.Save(_settings);
            }
            else
            {
                // Cancelled / closed: restore the saved topology + widget on every window.
                if (!TopologySuits(savedWidget))
                    RebuildWindows(savedWidget);
                foreach (var window in _windows)
                    window.ApplyWidget(savedWidget, savedConfig, savedAppearance);
            }
        }

        /// <summary>Apply an edited widget/config/appearance to the live windows immediately (live
        /// preview). Rebuilds the windows first if they don't suit the previewed widget. Touches no
        /// setting Cancel would need to revert — at most it opens the previewed style's placement
        /// record, which is created on first use anyway and holds nothing the user chose.</summary>
        private void PreviewWidget(string widgetId, IWidgetConfig config, GlobalAppearance global)
        {
            if (!TopologySuits(widgetId))
                RebuildWindows(widgetId);

            foreach (var window in _windows)
                window.ApplyWidget(widgetId, config, global);
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
                        ShowWidget(cfg, _settings.Style);
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
