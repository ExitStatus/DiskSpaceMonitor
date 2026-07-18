using System.Windows.Media;
using DiskSpaceMonitor.Settings;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// A full snapshot of the live-previewable widget appearance: the two opacities
    /// and the six part colours. Passed from the settings dialog to the widgets so
    /// changes show immediately, and used to apply the saved appearance on startup.
    /// </summary>
    public sealed record AppearancePreview(
        double BackgroundOpacity,
        double WidgetOpacity,
        double RingThickness,
        double LowThresholdPercent,
        double CriticalThresholdPercent,
        Color Background,
        Color Track,
        Color Healthy,
        Color Warning,
        Color Critical,
        Color Text)
    {
        public static AppearancePreview FromSettings(WidgetSettings s) => new(
            s.BackgroundOpacity,
            s.WidgetOpacity,
            s.RingThickness,
            s.LowThresholdPercent,
            s.CriticalThresholdPercent,
            ColorUtil.Parse(s.BackgroundColor, Color.FromRgb(0x16, 0x1A, 0x20)),
            ColorUtil.Parse(s.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
            ColorUtil.Parse(s.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
            ColorUtil.Parse(s.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
            ColorUtil.Parse(s.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
            ColorUtil.Parse(s.TextColor, Colors.White));
    }
}
