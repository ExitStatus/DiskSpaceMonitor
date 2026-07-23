namespace DiskSpaceMonitor.Widgets.Effects
{
    /// <summary>
    /// Reusable text-glow settings shared by widgets: an outer-glow radius (0–10, 0 = off) and its
    /// colour. Meant to be embedded as a nested block inside a widget's own config so any widget can
    /// offer the same "Effects" options and round-trip them through its existing JSON codec.
    /// </summary>
    public sealed class GlowEffectConfig
    {
        /// <summary>Outer-glow radius, 0–10 (0 disables the glow).</summary>
        public double OuterGlowRadius { get; set; }

        /// <summary>Outer-glow colour (hex "#RRGGBB").</summary>
        public string OuterGlowColor { get; set; } = "#FFFFFF";
    }
}
