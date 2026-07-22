using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Interop;
using DiskSpaceMonitor.Layout;
using DiskSpaceMonitor.Settings;
using DiskSpaceMonitor.Widgets;

namespace DiskSpaceMonitor.Views
{
    public partial class MainWindow : Window
    {
        private const double MinSize = 120;
        private const double MaxSize = 600;

        private readonly WidgetSettings _settings;
        private readonly DriveWidgetConfig _config;
        private readonly IDriveReader _driveReader;
        private readonly WidgetRegistry _registry;
        private readonly DispatcherTimer _diskTimer;
        private readonly DispatcherTimer _inputTimer;

        // The live widget view hosted in WidgetHost; rebuilt when the widget changes.
        private IWidgetView? _view;
        private string? _widgetId;

        private IntPtr _hwnd;
        private bool _clickThrough;
        private bool _editing;
        private bool _dragging;
        private bool _resizing;
        private Point _grabOffset;

        // Screen position of the corner opposite the one being dragged; fixed for
        // the duration of a resize so there is no feedback between moving the
        // window and moving the handle.
        private double _anchorX;
        private double _anchorY;

        /// <summary>The drive/placement this window is bound to.</summary>
        public DriveWidgetConfig Config => _config;

        public MainWindow(WidgetSettings settings, DriveWidgetConfig config, IDriveReader driveReader,
            WidgetRegistry registry)
        {
            InitializeComponent();

            _settings = settings;
            _config = config;
            _driveReader = driveReader;
            _registry = registry;

            Width = Height = Clamp(config.Size);

            _diskTimer = new DispatcherTimer { Interval = RefreshInterval() };
            _diskTimer.Tick += (_, _) => RefreshDisk();

            // Tracks the hovered widget + cursor while Ctrl is held. Only runs during
            // interaction (started/stopped by SetInteractive).
            _inputTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            _inputTimer.Tick += (_, _) => UpdateHover();

            Loaded += OnLoaded;
        }

        // --- Startup / placement ---------------------------------------------

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (double.IsNaN(_config.Left) || double.IsNaN(_config.Top))
            {
                // Fallback placement (App normally assigns this first).
                var wa = SystemParameters.WorkArea;
                Left = wa.Right - Width - 40;
                Top = wa.Bottom - Height - 40;
            }
            else
            {
                Left = _config.Left;
                Top = _config.Top;
            }

            // Pull a widget back on-screen if its saved spot is now off the desktop
            // (e.g. a monitor was disconnected or rearranged).
            if (WorkAreaBounds() is Rect vb)
            {
                var (cl, ct) = WidgetLayout.Constrain(Left, Top, Width, Height, vb);
                Left = cl;
                Top = ct;
            }

            ApplyWidget();
            _diskTimer.Start();
            // _inputTimer is started on demand by SetInteractive (Ctrl held).
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;

            // Hide from Alt-Tab and never take focus.
            long ex = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            ex |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ex));

            // Drop to the very bottom of the Z-order and keep it there (see WndProc).
            NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

            HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);

            // Idle state: clicks pass straight through until Ctrl is pressed.
            SetClickThrough(true);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_WINDOWPOSCHANGING)
            {
                // Force the window to stay pinned behind everything else whenever the
                // Z-order would otherwise change.
                var pos = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
                pos.hwndInsertAfter = NativeMethods.HWND_BOTTOM;
                pos.flags |= NativeMethods.SWP_NOACTIVATE;
                Marshal.StructureToPtr(pos, lParam, false);
            }

            return IntPtr.Zero;
        }

        // --- Interaction state -----------------------------------------------

        /// <summary>
        /// Enter/leave interactive mode in response to the app-wide Ctrl notification.
        /// Interactive = live (not click-through) with the hover poll running.
        /// </summary>
        public void SetInteractive(bool interactive)
        {
            if (interactive)
            {
                SetClickThrough(false);
                if (!_inputTimer.IsEnabled)
                    _inputTimer.Start();
                UpdateHover();
            }
            else
            {
                // Don't drop out of a drag/resize just because Ctrl was released;
                // the gesture finalises on mouse-up (see the button-up handlers).
                if (_dragging || _resizing)
                    return;

                _inputTimer.Stop();
                SetClickThrough(true);
                _editing = false;
                EditOverlay.Visibility = Visibility.Collapsed;
                Cursor = Cursors.Arrow;
            }
        }

        private void UpdateHover()
        {
            // While dragging/resizing keep the overlay up; movement is driven by
            // mouse events, not this poll.
            if (_dragging || _resizing)
            {
                EditOverlay.Visibility = Visibility.Visible;
                return;
            }

            // Edit mode = cursor genuinely inside THIS window. Tested geometrically
            // (not via IsMouseOver) so it is reliable in the transparent corners and
            // correctly turns off when the cursor moves to another widget.
            _editing = IsCursorOverWindow();
            EditOverlay.Visibility = _editing ? Visibility.Visible : Visibility.Collapsed;
            Cursor = _editing ? Cursors.SizeAll : Cursors.Arrow;
        }

        private bool IsCursorOverWindow()
        {
            if (!NativeMethods.GetCursorPos(out var pt))
                return false;

            try
            {
                // GetCursorPos is in physical pixels; PointFromScreen returns DIPs
                // local to this window, matching Width/Height.
                var local = PointFromScreen(new Point(pt.X, pt.Y));
                return local.X >= 0 && local.Y >= 0 && local.X <= Width && local.Y <= Height;
            }
            catch (InvalidOperationException)
            {
                return false; // window not yet sourced
            }
        }

        private void SetClickThrough(bool value)
        {
            if (_hwnd == IntPtr.Zero || value == _clickThrough)
                return;

            _clickThrough = value;
            long ex = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            if (value)
                ex |= NativeMethods.WS_EX_TRANSPARENT;
            else
                ex &= ~NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ex));
        }

        /// <summary>Add or clear WS_EX_NOACTIVATE so the window can (temporarily) be activated.</summary>
        private void SetNoActivate(bool noActivate)
        {
            if (_hwnd == IntPtr.Zero)
                return;

            long ex = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            if (noActivate)
                ex |= NativeMethods.WS_EX_NOACTIVATE;
            else
                ex &= ~NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ex));
        }

        // The widget is a no-activate, always-behind tool window, so WPF's context
        // menu never gains the focus/mouse-capture it needs to dismiss on an outside
        // click or Escape — it would only close when an item was picked. Briefly make
        // the window activatable and foreground while the menu is up so it behaves
        // normally; the WndProc Z-order pin keeps it visually behind everything.
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            SetNoActivate(false);
            NativeMethods.SetForegroundWindow(_hwnd);
        }

        private void OnContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            SetNoActivate(true);

            // Activation may have nudged the Z-order; drop straight back to the bottom.
            NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }

        // --- Moving ----------------------------------------------------------

        private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Let the resize handles do their own thing.
            if (e.OriginalSource is Thumb)
                return;

            if (!NativeMethods.IsKeyDown(NativeMethods.VK_CONTROL))
                return;

            // Custom drag (instead of DragMove) so we can snap and block overlaps.
            _grabOffset = e.GetPosition(this);
            _dragging = true;
            CaptureMouse();
            e.Handled = true;
        }

        private void OnWindowMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging)
                return;

            double w = Width, h = Height;

            // Absolute mouse position in Left/Top coordinate space, valid even as
            // the window moves (local position is relative to the current origin).
            var local = e.GetPosition(this);
            double desiredLeft = Left + local.X - _grabOffset.X;
            double desiredTop = Top + local.Y - _grabOffset.Y;

            var others = App.Instance.OtherWidgetBounds(this);

            // Snap to other widgets, then to the current screen's edges.
            (desiredLeft, desiredTop) = WidgetLayout.SnapMove(desiredLeft, desiredTop, w, h, others);
            if (CurrentScreenWorkArea() is Rect screen)
                (desiredLeft, desiredTop) = WidgetLayout.SnapToScreen(desiredLeft, desiredTop, w, h, screen);

            double left0 = Left, top0 = Top;
            double newLeft = WidgetLayout.ClampMoveX(left0, desiredLeft, top0, w, h, others);
            double newTop = WidgetLayout.ClampMoveY(top0, desiredTop, newLeft, w, h, others);

            // Keep the whole window on-screen and out of the taskbar (constrained to
            // the union of every monitor's work area, so it can still cross displays).
            if (WorkAreaBounds() is Rect vb)
                (newLeft, newTop) = WidgetLayout.Constrain(newLeft, newTop, w, h, vb);

            // Safety net: never leave the window overlapping another.
            foreach (var o in others)
            {
                if (WidgetLayout.Overlaps(new Rect(newLeft, newTop, w, h), o))
                {
                    newLeft = left0;
                    newTop = top0;
                    break;
                }
            }

            Left = newLeft;
            Top = newTop;
        }

        // --- Screen geometry (in Left/Top DIP space) -------------------------

        /// <summary>Work area of the monitor this window is on, or null if unavailable.</summary>
        private Rect? CurrentScreenWorkArea()
        {
            if (_hwnd == IntPtr.Zero)
                return null;

            var hMon = NativeMethods.MonitorFromWindow(_hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (!NativeMethods.GetMonitorInfo(hMon, ref mi))
                return null;

            return ScreenRectToDip(mi.rcWork);
        }

        /// <summary>
        /// Bounding box of every monitor's work area (excludes taskbars), in DIP.
        /// Spans all displays so a widget can cross between them, but stops it being
        /// dragged behind the taskbar. Null if unavailable.
        /// </summary>
        private Rect? WorkAreaBounds()
        {
            var areas = new List<Rect>();

            NativeMethods.MonitorEnumProc callback = (IntPtr hMon, IntPtr _, ref NativeMethods.RECT _, IntPtr _) =>
            {
                var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
                if (NativeMethods.GetMonitorInfo(hMon, ref mi) && ScreenRectToDip(mi.rcWork) is Rect wr)
                    areas.Add(wr);
                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            if (areas.Count == 0)
                return null;

            double left = areas.Min(r => r.Left);
            double top = areas.Min(r => r.Top);
            double right = areas.Max(r => r.Right);
            double bottom = areas.Max(r => r.Bottom);
            return new Rect(left, top, right - left, bottom - top);
        }

        // Convert a physical-pixel screen rect into the window's Left/Top DIP space
        // via the window's own DPI transform (correct across monitors).
        private Rect? ScreenRectToDip(NativeMethods.RECT rc)
        {
            try
            {
                var tl = PointFromScreen(new Point(rc.left, rc.top));
                var br = PointFromScreen(new Point(rc.right, rc.bottom));
                return new Rect(Left + tl.X, Top + tl.Y, br.X - tl.X, br.Y - tl.Y);
            }
            catch (InvalidOperationException)
            {
                return null; // window not yet sourced
            }
        }

        private void OnWindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging)
                return;

            _dragging = false;
            ReleaseMouseCapture();
            SavePlacement();

            // If Ctrl was released mid-drag, return to the idle click-through state now.
            if (!NativeMethods.IsKeyDown(NativeMethods.VK_CONTROL))
                SetInteractive(false);
        }

        private void OnWindowLostMouseCapture(object sender, MouseEventArgs e) => _dragging = false;

        // --- Resizing --------------------------------------------------------

        private void OnResizeStarted(object sender, DragStartedEventArgs e)
        {
            var corner = (string)((Thumb)sender).Tag;
            bool left = corner.Contains("Left");
            bool top = corner.Contains("Top");

            _anchorX = left ? Left + Width : Left;
            _anchorY = top ? Top + Height : Top;
            _resizing = true;
        }

        private void OnResizeDelta(object sender, DragDeltaEventArgs e)
        {
            var corner = (string)((Thumb)sender).Tag;
            bool left = corner.Contains("Left");
            bool top = corner.Contains("Top");

            // Absolute mouse position in the same coordinate space as Left/Top.
            var local = Mouse.GetPosition(this);
            double mouseX = Left + local.X;
            double mouseY = Top + local.Y;

            // Distance from the fixed anchor, then keep it square via the longer side.
            double width = left ? _anchorX - mouseX : mouseX - _anchorX;
            double height = top ? _anchorY - mouseY : mouseY - _anchorY;
            double candidate = Math.Max(width, height);

            // Snap edges to other widgets and stop the square growing into them.
            var others = App.Instance.OtherWidgetBounds(this);
            double size = WidgetLayout.SnapAndClampResize(
                _anchorX, _anchorY, left, top, candidate, others, MinSize, MaxSize);

            Left = left ? _anchorX - size : _anchorX;
            Top = top ? _anchorY - size : _anchorY;
            Width = size;
            Height = size;
        }

        private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
        {
            _resizing = false;
            SavePlacement();

            if (!NativeMethods.IsKeyDown(NativeMethods.VK_CONTROL))
                SetInteractive(false);
        }

        // --- Commands --------------------------------------------------------

        private void OnExitClick(object sender, RoutedEventArgs e) => App.Instance.RemoveWidget(this);

        private void OnSettingsClick(object sender, RoutedEventArgs e) => App.Instance.OpenSettings(this);

        private void OnExitApplicationClick(object sender, RoutedEventArgs e) => App.Instance.ExitApplication();

        // --- Data ------------------------------------------------------------

        /// <summary>Push changed settings (interval + the global widget/config) into this instance.</summary>
        public void ApplySettings()
        {
            _diskTimer.Interval = RefreshInterval();
            ApplyWidget();
        }

        /// <summary>Apply the current global widget + config + opacity from settings.</summary>
        private void ApplyWidget()
        {
            var factory = _registry.Get(_settings.Style);
            ApplyWidget(_settings.Style, factory.ReadConfig(_settings.StyleConfig), _settings.WidgetOpacity);
        }

        /// <summary>
        /// Apply a widget + its config + overall opacity — for the initial load, live preview from
        /// the dialog, and applying saved changes. Rebuilds the hosted view when the widget changes.
        /// </summary>
        public void ApplyWidget(string widgetId, IWidgetConfig config, double widgetOpacity)
        {
            if (_view is null || widgetId != _widgetId)
            {
                _widgetId = widgetId;
                _view = _registry.Get(widgetId).CreateView();
                WidgetHost.Content = _view.View;
            }

            _view.Apply(config);

            // Fade the rendered visual only, NOT the window — otherwise the edit overlay
            // (resize thumbs + buttons) would fade with it and be hard to use.
            WidgetHost.Opacity = Math.Clamp(widgetOpacity, 0.2, 1.0);
            RefreshDisk();
        }

        private TimeSpan RefreshInterval()
            => TimeSpan.FromSeconds(Math.Clamp(_settings.RefreshSeconds, 1, 3600));

        private void RefreshDisk()
        {
            var space = _driveReader.Read(_config.DrivePath);
            if (space != null)
                _view?.Update(space);
        }

        // --- Persistence -----------------------------------------------------

        private void SavePlacement()
        {
            _config.Left = Left;
            _config.Top = Top;
            _config.Size = Width;
            App.Instance.SaveSettings();
        }

        private static double Clamp(double size) => Math.Clamp(size, MinSize, MaxSize);
    }
}
