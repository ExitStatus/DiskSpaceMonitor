using System;
using Microsoft.Win32;

namespace DiskSpaceMonitor.Startup
{
    /// <summary>
    /// Per-user auto-start via the standard
    /// HKCU\Software\Microsoft\Windows\CurrentVersion\Run key. No admin rights needed.
    /// </summary>
    public sealed class AutoStartService : IAutoStartService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "DiskSpaceMonitor";

        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
            }
            catch
            {
                return false;
            }
        }

        public void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                if (key is null)
                    return;

                if (enabled)
                {
                    // The launching executable (the apphost, e.g. DiskSpaceMonitor.exe).
                    var exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                        key.SetValue(ValueName, $"\"{exePath}\"");
                }
                else if (key.GetValue(ValueName) is not null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // Best-effort; a locked-down registry shouldn't crash the app.
            }
        }
    }
}
