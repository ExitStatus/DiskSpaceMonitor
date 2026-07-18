using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>Parse/format WPF colours as "#RRGGBB" hex strings.</summary>
    internal static class ColorUtil
    {
        public static Color Parse(string? hex, Color fallback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex) && ColorConverter.ConvertFromString(hex) is Color c)
                    return c;
            }
            catch
            {
                // Fall through to the fallback on any malformed value.
            }

            return fallback;
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
