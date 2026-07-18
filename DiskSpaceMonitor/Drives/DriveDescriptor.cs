namespace DiskSpaceMonitor.Drives
{
    /// <summary>
    /// A drive the user can choose to display, as shown in the settings dialog:
    /// its root <paramref name="Path"/> (e.g. "C:\") and a human label.
    /// </summary>
    public sealed record DriveDescriptor(string Path, string Label);
}
