using System.Collections.Generic;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>Enumerates the drives available to display and identifies the boot drive.</summary>
    public interface IDriveCatalog
    {
        /// <summary>Root path of the drive Windows is installed on, e.g. "C:\".</summary>
        string BootDrivePath { get; }

        /// <summary>Ready, real drives (fixed, removable, network) the user can choose to display.</summary>
        IReadOnlyList<DriveDescriptor> GetAvailableDrives();
    }
}
