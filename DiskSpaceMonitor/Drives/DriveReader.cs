using System.IO;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>Default <see cref="IDriveReader"/> backed by <see cref="DriveInfo"/>.</summary>
    public sealed class DriveReader : IDriveReader
    {
        public DriveSpace? Read(string drivePath)
        {
            try
            {
                var drive = new DriveInfo(drivePath);
                if (!drive.IsReady)
                    return null;

                return new DriveSpace(drive.Name, drive.TotalSize, drive.TotalFreeSpace);
            }
            catch
            {
                // Drive temporarily unavailable (unmounted, permissions, etc.).
                return null;
            }
        }
    }
}
