using System;
using System.IO;

namespace DiskSpaceMonitor.Diagnostics
{
    /// <summary>
    /// Minimal, never-throwing error log at %AppData%\DiskSpaceMonitor\error.log.
    /// Used to capture unexpected exceptions so an otherwise-silent failure can be
    /// diagnosed rather than the app just vanishing.
    /// </summary>
    internal static class ErrorLog
    {
        private static readonly object Gate = new();

        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiskSpaceMonitor", "error.log");

        public static void Write(string context, Exception? ex)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}";
                lock (Gate)
                    File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never itself bring down the app.
            }
        }
    }
}
