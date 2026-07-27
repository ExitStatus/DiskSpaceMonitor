using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Box
{
    /// <summary>
    /// Configuration for the box widget: one rounded panel per drive holding the drive and its
    /// size, the used space, and a bar filled to the used %. The panel and the bar inside it are
    /// styled separately — each has its own corner rounding and outline — so a bevelled box can
    /// hold a plain bar or the other way round. The status colours and thresholds match the other
    /// graphs, so a drive reads the same whichever style is showing.
    /// </summary>
    public sealed class BoxConfig : IWidgetConfig
    {
        // --- The panel ---

        /// <summary>How rounded the box's corners are (0–30), in pixels at the widget's reference
        /// size; it scales with the box, like the outline width.</summary>
        public double CornerRadius { get; set; } = 8;

        /// <summary>Opacity of the box's fill (0 = invisible, 1 = solid).</summary>
        public double BackgroundOpacity { get; set; } = 0.7;

        /// <summary>Opacity of the box's outline or bevel (0 = invisible, 1 = solid).</summary>
        public double BorderOpacity { get; set; } = 1.0;

        // --- The bar ---

        /// <summary>How rounded the bar's ends are (0–20), scaled with the box like the box's own
        /// rounding. 0 gives square corners.</summary>
        public double BarCornerRadius { get; set; } = 3;

        /// <summary>The bar's share of the box's height as a percent (5–50). A proportion rather
        /// than a pixel size, so the bar keeps its place when the box is stretched; the two text
        /// rows split whatever is left.</summary>
        public double BarHeightPercent { get; set; } = 18;

        /// <summary>Opacity of the unused part of the bar (0 = hidden, 1 = solid).</summary>
        public double TrackOpacity { get; set; } = 0.2;

        // --- Thresholds ---

        /// <summary>Percent of free space below which the bar shows the "low" colour.</summary>
        public double LowThresholdPercent { get; set; } = 40;

        /// <summary>Percent of free space below which the bar shows the "critical" colour.</summary>
        public double CriticalThresholdPercent { get; set; } = 15;

        // --- Part colours (hex "#RRGGBB"). ---
        public string TextColor { get; set; } = "#FFFFFF";
        public string BackgroundColor { get; set; } = "#161A20";
        public string TrackColor { get; set; } = "#6E7686";
        public string HealthyColor { get; set; } = "#4CAF50";
        public string WarningColor { get; set; } = "#FFB300";
        public string CriticalColor { get; set; } = "#F44336";

        // --- Outlines (the Box effects and Bar effects tabs). The panel and the bar are outlined
        // independently, so each has its own style, width and colours. Only the fields the chosen
        // styles use are read, but all of them persist, so switching styles back and forth keeps
        // each one's colours. ---

        /// <summary>How the box's panel is outlined.</summary>
        public BarStyle BoxStyle { get; set; } = BarStyle.Plain;

        /// <summary>How the bar's fill is outlined.</summary>
        public BarStyle BarStyle { get; set; } = BarStyle.Plain;

        /// <summary>Width of the box's outline or bevel in pixels (1–10).</summary>
        public double BoxBorderSize { get; set; } = 2;

        /// <summary>Width of the bar's outline or bevel in pixels (1–10). Set apart from the box's,
        /// so a heavy panel can hold a finely outlined bar.</summary>
        public double BarBorderSize { get; set; } = 2;

        /// <summary>Outline colour for the box's Border style.</summary>
        public string BoxBorderColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the box's lit edge, drawn along its top and left.</summary>
        public string BoxHighlightColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the box's shaded edge, drawn down its right and along its bottom.</summary>
        public string BoxLowlightColor { get; set; } = "#000000";

        /// <summary>Outline colour for the bar's Border style.</summary>
        public string BarBorderColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the bar's lit edge, drawn along its top and left.</summary>
        public string BarHighlightColor { get; set; } = "#FFFFFF";

        /// <summary>3D Border: the bar's shaded edge, drawn down its right and along its bottom.</summary>
        public string BarLowlightColor { get; set; } = "#000000";

        /// <summary>Reusable text outer-glow effect (radius + colour) behind the two text rows.</summary>
        public GlowEffectConfig Glow { get; set; } = new();
    }
}
