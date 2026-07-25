using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Circular
{
    public partial class RingGauge : UserControl
    {
        // Design-surface geometry (matches the 200x200 Grid in XAML).
        private const double DesignSize = 200;
        private const double Cx = 100;
        private const double Cy = 100;
        private const double Radius = 84; // 168px diameter track / 2

        // Centre-text sizes on the design surface (matching the XAML), kept here so the size bounds
        // can be applied against them. What the user sees is these times the Viewbox scale.
        private const double LabelFont = 20;
        private const double FreeFont = 23;
        private const double PercentFont = 19;

        // Gaps above the second and third lines, on the same design surface. The third is negative:
        // it tightens the free figure and its percentage into one block.
        private const double FreeGap = 2;
        private const double PercentGap = -3;

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

        // The current outer-glow layer (behind the centre text), if any, so it can be swapped out.
        private FrameworkElement? _glowLayer;

        // App-wide font and size bounds. Defaults until the host pushes the user's choice.
        private WidgetTypography _type = WidgetTypography.Default;

        public RingGauge()
        {
            InitializeComponent();

            // The Viewbox scales the whole gauge with the window, text included, so the rendered
            // text size only settles once the control has been laid out — and changes on every
            // resize. Re-apply the bounds whenever it does.
            SizeChanged += (_, _) => ApplyFontSizes();
        }

        /// <summary>Apply the app-wide font and text size bounds. The family goes on the control so
        /// the centre text inherits it; the bounds are applied against the current Viewbox scale.</summary>
        public void ApplyTypography(WidgetTypography typography)
        {
            _type = typography;
            FontFamily = typography.Family;
            ApplyFontSizes();
        }

        // The Viewbox is Uniform over a square design surface in a square window, so its scale is
        // simply the window size over the design size.
        private void ApplyFontSizes()
        {
            double scale = ActualWidth > 0 ? ActualWidth / DesignSize : 1;

            double free = _type.DesignFont(FreeFont, scale);
            double percent = _type.DesignFont(PercentFont, scale);

            DriveLabel.FontSize = _type.DesignFont(LabelFont, scale);
            FreeText.FontSize = free;
            FreePercent.FontSize = percent;

            // The gaps between the three lines are design-space constants tuned against the design
            // font sizes, so they have to come in by whatever proportion the bounds took off the
            // text. Left alone, the negative gap would drag the last two lines through each other
            // as soon as a maximum size started biting.
            FreeText.Margin = new Thickness(0, FreeGap * free / FreeFont, 0, 0);
            FreePercent.Margin = new Thickness(0, PercentGap * percent / PercentFont, 0, 0);
        }

        /// <summary>Set (or clear, with null) the outer glow rendered behind the centre text.</summary>
        public void SetGlow(Effect? glow)
        {
            if (_glowLayer != null)
                CenterHost.Children.Remove(_glowLayer);

            _glowLayer = GlowEffect.BehindVisual(CenterStack, glow);
            CenterHost.Children.Insert(0, _glowLayer);   // behind the crisp text
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
            => UsedArc.Data = RingArc.Build(new Point(Cx, Cy), Radius, fraction);

        private Color ColorFor(DiskFillLevel level) => level switch
        {
            DiskFillLevel.Healthy => _healthy,
            DiskFillLevel.Warning => _warning,
            _ => _critical
        };
    }
}
