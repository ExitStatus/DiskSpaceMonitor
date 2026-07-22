namespace DiskSpaceMonitor.Settings
{
    /// <summary>Persisted placement and monitored drive for a single widget instance (window).</summary>
    public sealed class DriveWidgetConfig
    {
        /// <summary>Root path of the monitored drive, e.g. "C:\\".</summary>
        public string DrivePath { get; set; } = "";

        /// <summary>Window left in DIPs. NaN means "not yet placed".</summary>
        public double Left { get; set; } = double.NaN;

        /// <summary>Window top in DIPs. NaN means "not yet placed".</summary>
        public double Top { get; set; } = double.NaN;

        /// <summary>Widget is square; this is both width and height in DIPs.</summary>
        public double Size { get; set; } = 200;
    }
}
