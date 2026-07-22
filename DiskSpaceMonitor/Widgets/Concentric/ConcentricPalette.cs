namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>Default ring colours and the rule for choosing a drive's colour. Pure and testable;
    /// used by both the view (to render) and the editor (to seed rows).</summary>
    internal static class ConcentricPalette
    {
        /// <summary>Vivid defaults cycled by ring position when a drive has no configured colour.</summary>
        public static readonly string[] Default =
        {
            "#E91E63", // pink
            "#9C27B0", // purple
            "#2196F3", // blue
            "#00BCD4", // cyan
            "#4CAF50", // green
            "#FF9800", // orange
            "#F44336", // red
            "#3F51B5", // indigo
        };

        /// <summary>The configured colour for a drive, or the palette default for its ring index.</summary>
        public static string ColorFor(ConcentricConfig config, string drivePath, int index)
            => config.DriveColors.TryGetValue(drivePath, out var hex) && !string.IsNullOrWhiteSpace(hex)
                ? hex
                : Default[index % Default.Length];
    }
}
