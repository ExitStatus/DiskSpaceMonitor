using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Views;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Bar
{
    /// <summary>
    /// The bar-graph view: renders every drive as a vertical bar filled to its used %, coloured by
    /// free-space status. Caches the last readings and config so either change re-renders.
    /// </summary>
    public sealed class BarView : IWidgetView
    {
        private readonly BarGauge _gauge = new();
        private BarConfig _config = new();
        private IReadOnlyList<DriveSpace> _drives = Array.Empty<DriveSpace>();

        public FrameworkElement View => _gauge;

        public double AspectRatio => _gauge.DesignAspect;

        public void Update(IReadOnlyList<DriveSpace> drives)
        {
            _drives = drives;
            Render();
        }

        public void Apply(IWidgetConfig config)
        {
            _config = (BarConfig)config;
            Render();
        }

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

            _gauge.Render(bars, ColorUtil.Parse(_config.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                _config.TrackOpacity, ColorUtil.Parse(_config.TextColor, Colors.White),
                _config.BarWidthPercent / 100.0, GlowEffect.Build(_config.Glow));
        }
    }
}
