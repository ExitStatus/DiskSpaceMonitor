using System;
using System.Globalization;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>
    /// Formats a byte count as a human-readable size (B/KB/MB/GB/TB/PB, base 1024)
    /// with at most one decimal place, e.g. "512 B", "1.5 GB", "234.5 GB".
    /// </summary>
    public static class ByteSize
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

        public static string Humanize(long bytes)
        {
            double scaled = Math.Abs(bytes);
            var unit = 0;
            while (scaled >= 1024 && unit < Units.Length - 1)
            {
                scaled /= 1024;
                unit++;
            }

            var sign = bytes < 0 ? "-" : string.Empty;
            return $"{sign}{scaled.ToString("0.#", CultureInfo.InvariantCulture)} {Units[unit]}";
        }
    }
}
