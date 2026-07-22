namespace DiskSpaceMonitor.Widgets.Bar
{
    /// <summary>
    /// Configuration for the bar-graph widget: a bar per drive filled to its used %, coloured by
    /// free-space status (healthy/low/critical). The transparency of the unused part of each bar,
    /// the status colours, the track/text colours, and the thresholds are all configurable.
    /// </summary>
    public sealed class BarConfig : IWidgetConfig
    {
        /// <summary>Width of each bar as a percent (10–100) of its column slot.</summary>
        public double BarWidthPercent { get; set; } = 80;

        /// <summary>Opacity of the unused part of each bar (0 = hidden, 1 = solid).</summary>
        public double TrackOpacity { get; set; } = 0.2;

        /// <summary>Show the used space (humanized, e.g. "1.5 GB") on top of each bar.</summary>
        public bool ShowUsedSpace { get; set; }

        /// <summary>Show the total drive space (humanized) as a header above each bar.</summary>
        public bool ShowTotalSpace { get; set; }

        /// <summary>Percent of free space below which a bar shows the "low" colour.</summary>
        public double LowThresholdPercent { get; set; } = 40;

        /// <summary>Percent of free space below which a bar shows the "critical" colour.</summary>
        public double CriticalThresholdPercent { get; set; } = 15;

        // --- Part colours (hex "#RRGGBB"). ---
        public string TrackColor { get; set; } = "#6E7686";
        public string HealthyColor { get; set; } = "#4CAF50";
        public string WarningColor { get; set; } = "#FFB300";
        public string CriticalColor { get; set; } = "#F44336";
        public string TextColor { get; set; } = "#FFFFFF";
    }
}
