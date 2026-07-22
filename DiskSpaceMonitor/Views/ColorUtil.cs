using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>Parse/format WPF colours as "#RRGGBB" hex strings.</summary>
    internal static class ColorUtil
    {
        public static Color Parse(string? hex, Color fallback)
            => TryParse(hex, out var c) ? c : fallback;

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
