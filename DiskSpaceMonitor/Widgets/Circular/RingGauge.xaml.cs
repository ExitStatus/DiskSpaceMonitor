using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DiskSpaceMonitor.Drives;

namespace DiskSpaceMonitor.Widgets.Circular
{
    public partial class RingGauge : UserControl
    {
        // Design-surface geometry (matches the 200x200 Grid in XAML).
        private const double Cx = 100;
        private const double Cy = 100;
        private const double Radius = 84; // 168px diameter track / 2

        // Configurable part colours (defaults match the original design).
        private Color _healthy = Color.FromRgb(0x4C, 0xAF, 0x50);
        private Color _warning = Color.FromRgb(0xFF, 0xB3, 0x00);
        private Color _critical = Color.FromRgb(0xF4, 0x43, 0x36);
        private Color _text = Colors.White;

        // Free-space thresholds (fractions) that decide the arc colour.
        private double _lowThreshold = DiskGauge.DefaultLowThreshold;
        private double _criticalThreshold = DiskGauge.DefaultCriticalThreshold;

        // The most recent fill level and free fraction, so the arc colour can be
        // re-evaluated when the palette or thresholds change without new drive data.
        private DiskFillLevel _level = DiskFillLevel.Healthy;
        private double _lastFreeFraction = 1.0;

        public RingGauge()
        {
            InitializeComponent();
        }

        /// <summary>Set the opacity of the dark backing disc (0 = invisible, 1 = solid).</summary>
        public void SetBackgroundOpacity(double opacity) => BackingDisc.Opacity = Math.Clamp(opacity, 0, 1);

        /// <summary>Set the ring stroke thickness (track + used arc stay equal so they align).</summary>
        public void SetThickness(double thickness)
        {
            double t = Math.Clamp(thickness, 2, 48);
            Track.StrokeThickness = t;
            UsedArc.StrokeThickness = t;
        }

        /// <summary>Set the free-space thresholds (as percentages) and re-colour the arc live.</summary>
        public void SetThresholds(double lowPercent, double criticalPercent)
        {
            _lowThreshold = Math.Clamp(lowPercent, 0, 100) / 100.0;
            _criticalThreshold = Math.Clamp(criticalPercent, 0, 100) / 100.0;

            _level = DiskGauge.LevelForFree(_lastFreeFraction, _lowThreshold, _criticalThreshold);
            UsedArc.Stroke = new SolidColorBrush(ColorFor(_level));
        }

        /// <summary>Set the colour of every part of the gauge.</summary>
        public void SetColors(Color background, Color track, Color healthy, Color warning, Color critical, Color text)
        {
            BackingDisc.Fill = new SolidColorBrush(background);
            Track.Stroke = new SolidColorBrush(track);
            _healthy = healthy;
            _warning = warning;
            _critical = critical;
            _text = text;

            ApplyTextColors();
            UsedArc.Stroke = new SolidColorBrush(ColorFor(_level));
        }

        /// <summary>Update the gauge from a drive reading.</summary>
        public void Update(DriveSpace space)
        {
            double usedFraction = DiskGauge.UsedFraction(space.UsedBytes, space.TotalBytes);
            long freeBytes = Math.Max(0, space.FreeBytes);

            DriveLabel.Text = $"{space.Name.TrimEnd('\\')} {ByteSize.Humanize(space.TotalBytes)}";
            FreeText.Text = ByteSize.Humanize(freeBytes);
            FreePercent.Text = $"{(1 - usedFraction) * 100:0}% Free";

            _lastFreeFraction = 1 - usedFraction;
            _level = DiskGauge.LevelForFree(_lastFreeFraction, _lowThreshold, _criticalThreshold);
            UsedArc.Stroke = new SolidColorBrush(ColorFor(_level));
            UpdateArc(usedFraction);
        }

        // The centre text keeps a subtle emphasis hierarchy off the one text colour.
        private void ApplyTextColors()
        {
            DriveLabel.Foreground = TextBrush(0.8);
            FreeText.Foreground = TextBrush(1.0);
            FreePercent.Foreground = TextBrush(1.0);
        }

        private SolidColorBrush TextBrush(double alpha)
            => new(Color.FromArgb((byte)(alpha * 255), _text.R, _text.G, _text.B));

        private void UpdateArc(double fraction)
        {
            if (fraction <= 0.0005)
            {
                UsedArc.Data = null;
                return;
            }

            // Cap just below a full turn so the arc's start and end points don't
            // coincide (which would collapse the ArcSegment).
            double angleDeg = Math.Min(fraction, 0.9999) * 360.0;
            double angleRad = angleDeg * Math.PI / 180.0;

            // Angle measured clockwise from 12 o'clock.
            var start = new Point(Cx, Cy - Radius);
            var end = new Point(
                Cx + Radius * Math.Sin(angleRad),
                Cy - Radius * Math.Cos(angleRad));

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(Radius, Radius),
                IsLargeArc = angleDeg > 180,
                SweepDirection = SweepDirection.Clockwise
            });

            UsedArc.Data = new PathGeometry(new[] { figure });
        }

        private Color ColorFor(DiskFillLevel level) => level switch
        {
            DiskFillLevel.Healthy => _healthy,
            DiskFillLevel.Warning => _warning,
            _ => _critical
        };
    }
}
