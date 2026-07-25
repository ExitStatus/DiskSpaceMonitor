using System.Text.Json.Serialization;

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

        /// <summary>Side of a square widget in DIPs — both its width and its height. Widgets that
        /// size freely use <see cref="Width"/>/<see cref="Height"/> instead and fall back to this
        /// the first time they are shown.</summary>
        public double Size { get; set; } = 200;

        /// <summary>Window width in DIPs for a freely-sized widget; null until it has been sized.
        /// Omitted from the file rather than written null, so a square widget's entry stays as
        /// terse as it was.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Width { get; set; }

        /// <summary>Window height in DIPs for a freely-sized widget; null until it has been sized.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Height { get; set; }
    }
}
