using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>
    /// The concentric-circles view: renders every drive as a ring (innermost = first drive).
    /// Caches the last readings and config so either changing re-renders.
    /// </summary>
    public sealed class ConcentricView : IWidgetView
    {
        private readonly ConcentricGauge _gauge = new();
        private ConcentricConfig _config = new();
        private IReadOnlyList<DriveSpace> _drives = Array.Empty<DriveSpace>();

        public FrameworkElement View => _gauge;

        public void Update(IReadOnlyList<DriveSpace> drives)
        {
            _drives = drives;
            Render();
        }

        public void Apply(IWidgetConfig config)
        {
            _config = (ConcentricConfig)config;
            Render();
        }

        private void Render()
        {
            double low = _config.LowThresholdPercent / 100.0;
            double critical = _config.CriticalThresholdPercent / 100.0;
            var healthy = ColorUtil.Parse(_config.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50));
            var warning = ColorUtil.Parse(_config.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00));
            var crit = ColorUtil.Parse(_config.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36));

            var rings = new List<Ring>(_drives.Count);
            for (int i = 0; i < _drives.Count; i++)
            {
                var d = _drives[i];
                double used = DiskGauge.UsedFraction(d.UsedBytes, d.TotalBytes);
                string letter = d.Name.TrimEnd('\\');
                var ringColour = ColorUtil.Parse(ConcentricPalette.ColorFor(_config, d.Name, i), Colors.Gray);

                var level = DiskGauge.LevelForFree(1 - used, low, critical);
                var chipColour = level switch
                {
                    DiskFillLevel.Healthy => healthy,
                    DiskFillLevel.Warning => warning,
                    _ => crit,
                };

                rings.Add(new Ring($"{letter} {used * 100:0}%", used, ringColour, chipColour));
            }

            _gauge.Render(rings, _config.RingThickness, ColorUtil.Parse(_config.TextColor, Colors.White),
                _config.TrackOpacity);
        }
    }
}
