using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Views;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Box
{
    /// <summary>
    /// The box widget view: one drive drawn as a rounded panel with its size, its used space and a
    /// bar filled to the used % and coloured by free-space status. Caches the last reading and
    /// config so either change re-renders.
    /// </summary>
    public sealed class BoxView : IWidgetView
    {
        private readonly BoxGauge _gauge = new();
        private BoxConfig _config = new();
        private DriveSpace? _drive;

        public FrameworkElement View => _gauge;

        /// <summary>A box is a wide shape holding two lines of text above a bar, so both dimensions
        /// are the user's to set: the panel fills whatever rectangle they drag.</summary>
        public bool ResizesFreely => true;

        public void Update(IReadOnlyList<DriveSpace> drives)
        {
            if (drives.Count == 0)
                return;

            _drive = drives[0];
            Render();
        }

        public void Apply(IWidgetConfig config)
        {
            _config = (BoxConfig)config;
            Render();
        }

        public void ApplyTypography(WidgetTypography typography) => _gauge.ApplyTypography(typography);

        private void Render()
        {
            if (_drive is not { } drive)
                return;

            double used = DiskGauge.UsedFraction(drive.UsedBytes, drive.TotalBytes);
            var level = DiskGauge.LevelForFree(1 - used,
                _config.LowThresholdPercent / 100.0, _config.CriticalThresholdPercent / 100.0);

            var fill = level switch
            {
                DiskFillLevel.Healthy => ColorUtil.Parse(_config.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
                DiskFillLevel.Warning => ColorUtil.Parse(_config.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
                _ => ColorUtil.Parse(_config.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
            };

            // Every transparency is blended into its part's alpha rather than set as an element
            // opacity, so a see-through panel shows the desktop instead of fading its own text.
            double border = _config.BorderOpacity;
            var boxSkin = new BarSkin(
                _config.BoxStyle,
                _config.BoxBorderSize,
                _config.CornerRadius,
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BoxBorderColor, Colors.White), border),
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BoxHighlightColor, Colors.White), border),
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BoxLowlightColor, Colors.Black), border));

            var barSkin = new BarSkin(
                _config.BarStyle,
                _config.BarBorderSize,
                _config.BarCornerRadius,
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BarBorderColor, Colors.White), border),
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BarHighlightColor, Colors.White), border),
                ColorUtil.WithOpacity(ColorUtil.Parse(_config.BarLowlightColor, Colors.Black), border));

            _gauge.Render(
                title: $"{drive.Name.TrimEnd('\\')} {ByteSize.Humanize(drive.TotalBytes)}",
                used: $"Used: {ByteSize.Humanize(drive.UsedBytes)}",
                usedFraction: used,
                fill: fill,
                text: ColorUtil.Parse(_config.TextColor, Colors.White),
                background: ColorUtil.WithOpacity(
                    ColorUtil.Parse(_config.BackgroundColor, Color.FromRgb(0x16, 0x1A, 0x20)),
                    _config.BackgroundOpacity),
                track: ColorUtil.WithOpacity(
                    ColorUtil.Parse(_config.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                    _config.TrackOpacity),
                barHeight: _config.BarHeightPercent / 100.0,
                boxSkin: boxSkin,
                barSkin: barSkin,
                glow: GlowEffect.Build(_config.Glow));
        }
    }
}
