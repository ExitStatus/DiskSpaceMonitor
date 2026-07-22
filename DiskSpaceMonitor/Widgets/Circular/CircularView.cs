using System.Windows;
using System.Windows.Media;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>
    /// The circular gauge widget: wraps the existing <see cref="RingGauge"/> control and
    /// applies a <see cref="CircularConfig"/> to it. This is where the body of the old
    /// MainWindow.PreviewAppearance lives (minus overall opacity, now host-applied).
    /// </summary>
    public sealed class CircularView : IWidgetView
    {
        private readonly RingGauge _gauge = new();

        public FrameworkElement View => _gauge;

        public void Update(DriveSpace space) => _gauge.Update(space);

        public void Apply(IWidgetConfig config)
        {
            var c = (CircularConfig)config;

            _gauge.SetBackgroundOpacity(c.BackgroundOpacity);
            _gauge.SetThickness(c.RingThickness);
            _gauge.SetColors(
                ColorUtil.Parse(c.BackgroundColor, Color.FromRgb(0x16, 0x1A, 0x20)),
                ColorUtil.Parse(c.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                ColorUtil.Parse(c.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
                ColorUtil.Parse(c.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
                ColorUtil.Parse(c.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
                ColorUtil.Parse(c.TextColor, Colors.White));
            // Thresholds last: they recompute the fill level off the latest reading.
            _gauge.SetThresholds(c.LowThresholdPercent, c.CriticalThresholdPercent);
        }
    }
}
