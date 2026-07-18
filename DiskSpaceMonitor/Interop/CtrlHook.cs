using System;
using System.Runtime.InteropServices;

namespace DiskSpaceMonitor.Interop
{
    /// <summary>
    /// A single, app-wide low-level keyboard hook that reports when the Ctrl key
    /// goes down or up. This replaces per-window polling so the app can sit idle
    /// (no timers running) whenever Ctrl isn't held.
    /// </summary>
    internal sealed class CtrlHook : IDisposable
    {
        // Kept in a field so the delegate isn't garbage-collected while hooked.
        private readonly NativeMethods.LowLevelKeyboardProc _proc;
        private IntPtr _hook;
        private bool _leftDown;
        private bool _rightDown;
        private bool _ctrlDown;

        /// <summary>Raised (on the UI thread) when the combined Ctrl state changes.</summary>
        public event Action<bool>? CtrlChanged;

        public CtrlHook()
        {
            _proc = HookProc;
            _hook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL, _proc, NativeMethods.GetModuleHandle(null), 0);
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // This runs from native code on every keystroke. An exception must NOT
            // escape into the native caller — that would crash the whole process.
            try
            {
                if (nCode >= 0)
                {
                    // vkCode is the first field of KBDLLHOOKSTRUCT. Derive up/down from the
                    // message, NOT GetAsyncKeyState — inside a low-level hook the key state
                    // isn't updated yet, so a query would miss the release.
                    int msg = wParam.ToInt32();
                    int vk = Marshal.ReadInt32(lParam);

                    bool? isDown = msg switch
                    {
                        NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN => true,
                        NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP => false,
                        _ => null
                    };

                    if (isDown is bool down)
                    {
                        if (vk is NativeMethods.VK_LCONTROL or NativeMethods.VK_CONTROL)
                            _leftDown = down;
                        if (vk is NativeMethods.VK_RCONTROL or NativeMethods.VK_CONTROL)
                            _rightDown = down;

                        bool ctrl = _leftDown || _rightDown;
                        if (ctrl != _ctrlDown)
                        {
                            _ctrlDown = ctrl;
                            CtrlChanged?.Invoke(ctrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.ErrorLog.Write("CtrlHook", ex);
            }

            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
