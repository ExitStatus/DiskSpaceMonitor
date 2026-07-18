using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DiskSpaceMonitor.Diagnostics;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Interop;
using DiskSpaceMonitor.Settings;
using DiskSpaceMonitor.Startup;
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

        private ISettingsStore _store = null!;
        private IDriveReader _driveReader = null!;
        private IDriveCatalog _catalog = null!;
        private IAutoStartService _autoStart = null!;
        private WidgetSettings _settings = null!;
        private CtrlHook? _ctrlHook;

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
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

            var window = new MainWindow(_settings, cfg, _driveReader);
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
            // Remember the saved appearance so a Cancel can revert the live preview.
            var saved = AppearancePreview.FromSettings(_settings);

            var shown = _settings.Drives.Select(d => d.DrivePath).ToList();
            var dialog = new SettingsWindow(
                shown, _settings.RefreshSeconds, _autoStart.IsEnabled(),
                saved, _catalog, PreviewAppearance);
            dialog.ShowDialog();

            if (dialog.ExitRequested)
            {
                ExitApplication();
                return;
            }

            if (dialog.Applied)
            {
                _settings.RefreshSeconds = dialog.RefreshSeconds;
                StoreAppearance(dialog.Appearance);
                _autoStart.SetEnabled(dialog.AutoStart);
                foreach (var window in _windows)
                    window.ApplySettings();

                ApplyDriveSelection(dialog.SelectedDrivePaths, source.Width);
            }
            else
            {
                // Cancelled / closed: undo any live preview.
                PreviewAppearance(saved);
            }
        }

        /// <summary>Apply an appearance snapshot to every widget immediately (live preview).</summary>
        private void PreviewAppearance(AppearancePreview a)
        {
            foreach (var window in _windows)
                window.PreviewAppearance(a);
        }

        private void StoreAppearance(AppearancePreview a)
        {
            _settings.BackgroundOpacity = a.BackgroundOpacity;
            _settings.WidgetOpacity = a.WidgetOpacity;
            _settings.RingThickness = a.RingThickness;
            _settings.LowThresholdPercent = a.LowThresholdPercent;
            _settings.CriticalThresholdPercent = a.CriticalThresholdPercent;
            _settings.BackgroundColor = ColorUtil.ToHex(a.Background);
            _settings.TrackColor = ColorUtil.ToHex(a.Track);
            _settings.HealthyColor = ColorUtil.ToHex(a.Healthy);
            _settings.WarningColor = ColorUtil.ToHex(a.Warning);
            _settings.CriticalColor = ColorUtil.ToHex(a.Critical);
            _settings.TextColor = ColorUtil.ToHex(a.Text);
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
