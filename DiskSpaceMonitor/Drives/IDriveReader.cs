namespace DiskSpaceMonitor.Drives
{
    /// <summary>Reads the current capacity of a drive by its root path.</summary>
    public interface IDriveReader
    {
        /// <summary>
        /// Current space for the drive at <paramref name="drivePath"/> (e.g. "C:\"),
        /// or null if the drive is not ready or cannot be read.
        /// </summary>
        DriveSpace? Read(string drivePath);
    }
}
