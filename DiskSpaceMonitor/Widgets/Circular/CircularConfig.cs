using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>
    /// Per-widget configuration for the circular gauge widget. These are exactly the
    /// appearance values that were global before v1.1, minus overall widget opacity
    /// (which is now a generic per-widget property applied by the window).
    /// </summary>
    public sealed class CircularConfig : IWidgetConfig
    {
        /// <summary>Opacity of the dark backing disc behind the ring (0–1).</summary>
        public double BackgroundOpacity { get; set; } = 0.7;

        /// <summary>Stroke thickness of the ring (track + used arc), in design-space px.</summary>
        public double RingThickness { get; set; } = 16;

        /// <summary>Percent of free space below which the ring shows the "low" colour.</summary>
        public double LowThresholdPercent { get; set; } = 40;

        /// <summary>Percent of free space below which the ring shows the "critical" colour.</summary>
        public double CriticalThresholdPercent { get; set; } = 15;

        // --- Part colours (hex "#RRGGBB"). ---
        public string BackgroundColor { get; set; } = "#161A20";
        public string TrackColor { get; set; } = "#6E7686";
        public string HealthyColor { get; set; } = "#4CAF50";
        public string WarningColor { get; set; } = "#FFB300";
        public string CriticalColor { get; set; } = "#F44336";
        public string TextColor { get; set; } = "#FFFFFF";

        /// <summary>Reusable text outer-glow effect (radius + colour) behind the centre stats.</summary>
        public GlowEffectConfig Glow { get; set; } = new();
    }
}
