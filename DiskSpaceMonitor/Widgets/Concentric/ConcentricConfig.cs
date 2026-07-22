using System.Collections.Generic;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>
    /// Configuration for the concentric-circles widget: a uniform ring thickness, a colour per
    /// drive (keyed on the drive path, e.g. "C:\\"), and the label text colour.
    /// </summary>
    public sealed class ConcentricConfig : IWidgetConfig
    {
        /// <summary>Stroke thickness of every ring, in design-space px.</summary>
        public double RingThickness { get; set; } = 14;

        /// <summary>Opacity of the faint "unused" track behind each used arc (0 = hidden, 1 = solid).</summary>
        public double TrackOpacity { get; set; } = 0.2;

        /// <summary>Colour per drive path (hex "#RRGGBB"). Drives without an entry use the palette.</summary>
        public Dictionary<string, string> DriveColors { get; set; } = new();

        /// <summary>Colour of the per-ring label chips.</summary>
        public string TextColor { get; set; } = "#FFFFFF";
    }
}
