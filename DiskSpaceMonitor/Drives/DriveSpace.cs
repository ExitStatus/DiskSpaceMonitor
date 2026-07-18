using System;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>
    /// A point-in-time capacity reading for one drive: its display <paramref name="Name"/>
    /// (e.g. "C:\") and total/free figures in bytes.
    /// </summary>
    public sealed record DriveSpace(string Name, long TotalBytes, long FreeBytes)
    {
        /// <summary>Used space, never negative.</summary>
        public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    }
}
