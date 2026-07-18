namespace DiskSpaceMonitor.Drives
{
    /// <summary>How full a drive is, used to colour the gauge (green → amber → red).</summary>
    public enum DiskFillLevel
    {
        Healthy,
        Warning,
        Critical
    }
}
