using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>Default <see cref="IDriveCatalog"/> backed by <see cref="DriveInfo"/>.</summary>
    public sealed class DriveCatalog : IDriveCatalog
    {
        public string BootDrivePath => Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";

        public IReadOnlyList<DriveDescriptor> GetAvailableDrives() =>
            DriveInfo.GetDrives()
                .Where(IsSelectable)
                .Select(d => new DriveDescriptor(d.Name, Describe(d)))
                .ToList();

        private static bool IsSelectable(DriveInfo drive)
        {
            try
            {
                return drive.IsReady && drive.DriveType is DriveType.Fixed
                    or DriveType.Removable or DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

        private static string Describe(DriveInfo drive)
        {
            string label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? drive.DriveType.ToString()
                : drive.VolumeLabel;
            return $"{drive.Name}   {label} — {ByteSize.Humanize(drive.TotalSize)}";
        }
    }
}
