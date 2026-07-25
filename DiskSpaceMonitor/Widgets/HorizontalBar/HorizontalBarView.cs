using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Views;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.HorizontalBar
{
    /// <summary>
    /// The horizontal bar graph view: renders every drive as a horizontal bar filled to its used %,
    /// coloured by free-space status. Caches the last readings and config so either change re-renders.
    /// </summary>
    public sealed class HorizontalBarView : IWidgetView
    {
        private readonly HorizontalBarGauge _gauge = new();
        private BarGraphConfig _config = new();
        private IReadOnlyList<DriveSpace> _drives = Array.Empty<DriveSpace>();

        public FrameworkElement View => _gauge;

        /// <summary>The chart fills whatever rectangle the user drags, so both dimensions are theirs
        /// to set: a wider window lengthens the bars, a taller one thickens them.</summary>
        public bool ResizesFreely => true;

        public void Update(IReadOnlyList<DriveSpace> drives)
        {
            _drives = drives;
            Render();
        }

        public void Apply(IWidgetConfig config)
        {
            _config = (BarGraphConfig)config;
            Render();
        }

        public void ApplyTypography(WidgetTypography typography) => _gauge.ApplyTypography(typography);

        private void Render()
        {
            double low = _config.LowThresholdPercent / 100.0;
            double critical = _config.CriticalThresholdPercent / 100.0;
            var healthy = ColorUtil.Parse(_config.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50));
            var warning = ColorUtil.Parse(_config.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00));
            var crit = ColorUtil.Parse(_config.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36));

            var bars = new List<Bar>(_drives.Count);
            foreach (var d in _drives)
            {
                double used = DiskGauge.UsedFraction(d.UsedBytes, d.TotalBytes);
                string letter = d.Name.TrimEnd('\\');

                var level = DiskGauge.LevelForFree(1 - used, low, critical);
                var fill = level switch
                {
                    DiskFillLevel.Healthy => healthy,
                    DiskFillLevel.Warning => warning,
                    _ => crit,
                };

                string usedLabel = _config.ShowUsedSpace ? ByteSize.Humanize(d.UsedBytes) : string.Empty;
                string totalLabel = _config.ShowTotalSpace ? ByteSize.Humanize(d.TotalBytes) : string.Empty;
                bars.Add(new Bar(letter, used, fill, usedLabel, totalLabel));
            }

            // The outline size and corner rounding are given at the gauge's reference size; it scales
            // them with its text so they keep their weight relative to the graph.
            var skin = new BarSkin(
                _config.BarStyle,
                _config.BorderSize,
                BarGraphParts.CornerRadius,
                ColorUtil.Parse(_config.BorderColor, Colors.White),
                ColorUtil.Parse(_config.HighlightColor, Colors.White),
                ColorUtil.Parse(_config.LowlightColor, Colors.Black));

            _gauge.Render(bars, ColorUtil.Parse(_config.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                _config.TrackOpacity, ColorUtil.Parse(_config.TextColor, Colors.White),
                _config.BarGapPercent / 100.0, _config.Orientation, skin, GlowEffect.Build(_config.Glow));
        }
    }
}
