using System;
using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>Parse/format WPF colours as "#RRGGBB" hex strings.</summary>
    internal static class ColorUtil
    {
        public static Color Parse(string? hex, Color fallback)
            => TryParse(hex, out var c) ? c : fallback;

        /// <summary>The same colour carrying an opacity (0–1) as its alpha. Widgets blend their
        /// parts this way rather than setting Opacity on the element, so a transparent part shows
        /// the desktop through it instead of fading whatever it contains.</summary>
        public static Color WithOpacity(Color color, double opacity)
            => Color.FromArgb((byte)(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);

        /// <summary>Parse a "#RRGGBB" (or any WPF colour string); false if malformed.</summary>
        public static bool TryParse(string? hex, out Color color)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex) && ColorConverter.ConvertFromString(hex) is Color c)
                {
                    color = c;
                    return true;
                }
            }
            catch
            {
                // Fall through to false on any malformed value.
            }

            color = Colors.Black;
            return false;
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
