using System.Text.Json.Serialization;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.BarGraph
{
    /// <summary>
    /// Which end of the axis 0% sits at, and therefore the direction the bars grow. The first two
    /// belong to the vertical bar graph and the last two to the horizontal one; each widget offers
    /// only its own pair and normalises anything else back to its default.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BarOrientation
    {
        /// <summary>0% at the bottom: bars grow upwards from the axis.</summary>
        BottomUp,

        /// <summary>0% at the top: bars hang downwards from the top of the plot.</summary>
        TopDown,

        /// <summary>0% at the left: bars grow rightwards from the axis.</summary>
        LeftToRight,

        /// <summary>0% at the right: bars grow leftwards from the right of the plot.</summary>
        RightToLeft,
    }

    /// <summary>How each bar's fill is outlined.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BarStyle
    {
        /// <summary>No outline: just the flat status colour.</summary>
        Plain,

        /// <summary>A single-colour outline of the configured size around the fill.</summary>
        Border,

        /// <summary>A bevel: the highlight along the top and left, the lowlight down the right
        /// and along the bottom, so the bar reads as raised.</summary>
        ThreeDBorder,
    }

    /// <summary>
    /// Configuration shared by both bar graph widgets: a bar per drive filled to its used %,
    /// coloured by free-space status (healthy/low/critical). The transparency of the unused part of
    /// each bar, the status colours, the track/text colours, the thresholds, the bar outline and the
    /// text glow are all configurable. The vertical and horizontal widgets persist their own copy
    /// under their own style id, so each keeps its own settings.
    /// </summary>
    public sealed class BarGraphConfig : IWidgetConfig
    {
        /// <summary>Which way round the axis runs, and so the direction the bars fill.</summary>
        public BarOrientation Orientation { get; set; } = BarOrientation.BottomUp;

        /// <summary>Thickness of each bar as a percent (10–100) of its slot.</summary>
        public double BarWidthPercent { get; set; } = 80;

        /// <summary>Opacity of the unused part of each bar (0 = hidden, 1 = solid).</summary>
        public double TrackOpacity { get; set; } = 0.2;

        /// <summary>Show the used space (humanized, e.g. "1.5 GB") against each bar.</summary>
        public bool ShowUsedSpace { get; set; }

        /// <summary>Show the total drive space (humanized) at the 100% end of each bar.</summary>
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

        // --- Bar outline (Effects tab). Only the fields the chosen style uses are read, but all of
        // them persist, so switching styles back and forth keeps each one's colours. ---

        /// <summary>How each bar's fill is outlined.</summary>
        public BarStyle BarStyle { get; set; } = BarStyle.Plain;

        /// <summary>Outline width in device-independent pixels (1–10), for Border and 3D Border.</summary>
        public double BorderSize { get; set; } = 2;

        /// <summary>Outline colour for the Border style.</summary>
        public string BorderColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the lit edge, drawn along the top and left.</summary>
        public string HighlightColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the shaded edge, drawn along the bottom and right.</summary>
        public string LowlightColor { get; set; } = "#000000";

        /// <summary>Reusable text outer-glow effect (radius + colour).</summary>
        public GlowEffectConfig Glow { get; set; } = new();
    }
}
