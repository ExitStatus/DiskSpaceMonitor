using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>One ring to draw: its label, used fraction [0,1], and colour.</summary>
    internal readonly record struct Ring(string Label, double UsedFraction, Color Color);

    /// <summary>
    /// Draws concentric used-space arcs (innermost = first drive, outward), each with a small
    /// label chip at the arc's leading edge. Reuses <see cref="RingArc"/> for the arc geometry.
    /// The canvas is sized to the tight bounds of the rings AND labels so nothing is clipped.
    /// </summary>
    public partial class ConcentricGauge : UserControl
    {
        private const double InnerHole = 18;   // radius of the empty centre
        private const double Gap = 5;          // space between rings
        private const double Pad = 2;          // hairline so nothing touches the very edge

        public ConcentricGauge()
        {
            InitializeComponent();
        }

        internal void Render(IReadOnlyList<Ring> rings, double thickness, Color textColor, double trackOpacity)
        {
            Surface.Children.Clear();

            if (rings.Count == 0)
            {
                Surface.Width = Surface.Height = 1;
                return;
            }

            double t = Math.Clamp(thickness, 2, 48);
            double Radius(int i) => InnerHole + t / 2 + i * (t + Gap);

            double outward = t / 2 + 3;                     // push chips just beyond the ring
            double baseRadius = Radius(rings.Count - 1) + t / 2;
            byte trackAlpha = (byte)(Math.Clamp(trackOpacity, 0, 1) * 255);

            // Pass 1: build + measure the chips and accumulate the content bounds (relative to
            // centre 0,0), so the canvas ends up exactly big enough for the rings AND the labels.
            var chips = new List<(Border Chip, double X, double Y, Size Size)>();
            double minX = -baseRadius, minY = -baseRadius, maxX = baseRadius, maxY = baseRadius;

            for (int i = 0; i < rings.Count; i++)
            {
                double r = Radius(i);
                double frac = Math.Min(Math.Max(rings[i].UsedFraction, 0.0), 0.9999);
                double ang = frac * 2 * Math.PI;
                double x = (r + outward) * Math.Sin(ang);
                double y = (r + outward) * -Math.Cos(ang);

                var chip = BuildChip(rings[i].Label, rings[i].Color, textColor);
                chip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var sz = chip.DesiredSize;

                minX = Math.Min(minX, x - sz.Width / 2);
                maxX = Math.Max(maxX, x + sz.Width / 2);
                minY = Math.Min(minY, y - sz.Height / 2);
                maxY = Math.Max(maxY, y + sz.Height / 2);

                chips.Add((chip, x, y, sz));
            }

            double cx = -minX + Pad, cy = -minY + Pad;
            Surface.Width = (maxX - minX) + 2 * Pad;
            Surface.Height = (maxY - minY) + 2 * Pad;

            // Pass 2: tracks + used arcs, centred at (cx, cy).
            for (int i = 0; i < rings.Count; i++)
            {
                double r = Radius(i);
                var color = rings[i].Color;

                if (trackAlpha > 0)
                {
                    var track = new Ellipse
                    {
                        Width = 2 * r,
                        Height = 2 * r,
                        Stroke = new SolidColorBrush(Color.FromArgb(trackAlpha, color.R, color.G, color.B)),
                        StrokeThickness = t
                    };
                    Canvas.SetLeft(track, cx - r);
                    Canvas.SetTop(track, cy - r);
                    Surface.Children.Add(track);
                }

                var arc = new Path
                {
                    Data = RingArc.Build(new Point(cx, cy), r, rings[i].UsedFraction),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = t,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                Surface.Children.Add(arc);
            }

            // Chips on top.
            foreach (var (chip, x, y, sz) in chips)
            {
                Canvas.SetLeft(chip, cx + x - sz.Width / 2);
                Canvas.SetTop(chip, cy + y - sz.Height / 2);
                Surface.Children.Add(chip);
            }
        }

        private static Border BuildChip(string label, Color ringColor, Color textColor) => new()
        {
            Background = new SolidColorBrush(ringColor),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(textColor),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            }
        };
    }
}
