using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>
    /// One ring to draw: its label, used fraction [0,1], the ring/arc colour, and the colour of
    /// its label chip (the drive's status colour, which may differ from the ring colour).
    /// </summary>
    internal readonly record struct Ring(string Label, double UsedFraction, Color Color, Color ChipColor);

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
        private const double MinSep = 2;       // minimum gap between label chips

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

            int n = rings.Count;
            double t = Math.Clamp(thickness, 2, 48);
            double Radius(int i) => InnerHole + t / 2 + i * (t + Gap);

            double outward = t / 2 + 3;                     // push chips just beyond the ring
            double baseRadius = Radius(n - 1) + t / 2;
            byte trackAlpha = (byte)(Math.Clamp(trackOpacity, 0, 1) * 255);

            // Build + measure each chip, capturing its ring radius and its "leading edge" angle
            // (the point on the ring where the used-arc ends).
            var chip = new Border[n];
            var size = new Size[n];
            var rad = new double[n];
            var angle = new double[n];

            for (int i = 0; i < n; i++)
            {
                double frac = Math.Min(Math.Max(rings[i].UsedFraction, 0.0), 0.9999);
                angle[i] = frac * 2 * Math.PI;
                rad[i] = Radius(i) + outward;

                chip[i] = BuildChip(rings[i].Label, rings[i].ChipColor, textColor);
                chip[i].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                size[i] = chip[i].DesiredSize;
            }

            // Nudge overlapping chips apart along their rings so labels stay readable.
            ResolveChipAngles(rad, size, angle);

            // Final positions + tight content bounds (relative to centre 0,0), so the canvas
            // ends up exactly big enough for the rings AND the (possibly nudged) labels.
            var pos = new Point[n];
            double minX = -baseRadius, minY = -baseRadius, maxX = baseRadius, maxY = baseRadius;

            for (int i = 0; i < n; i++)
            {
                double x = rad[i] * Math.Sin(angle[i]);
                double y = rad[i] * -Math.Cos(angle[i]);
                pos[i] = new Point(x, y);

                minX = Math.Min(minX, x - size[i].Width / 2);
                maxX = Math.Max(maxX, x + size[i].Width / 2);
                minY = Math.Min(minY, y - size[i].Height / 2);
                maxY = Math.Max(maxY, y + size[i].Height / 2);
            }

            double cx = -minX + Pad, cy = -minY + Pad;
            Surface.Width = (maxX - minX) + 2 * Pad;
            Surface.Height = (maxY - minY) + 2 * Pad;

            // Tracks + used arcs, centred at (cx, cy).
            for (int i = 0; i < n; i++)
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

            // Chips on top, at their (possibly nudged) positions.
            for (int i = 0; i < n; i++)
            {
                Canvas.SetLeft(chip[i], cx + pos[i].X - size[i].Width / 2);
                Canvas.SetTop(chip[i], cy + pos[i].Y - size[i].Height / 2);
                Surface.Children.Add(chip[i]);
            }
        }

        /// <summary>
        /// Nudges chip angles (in place) so no two label chips sit closer than
        /// <see cref="MinSep"/> pixels. Each chip starts at its arc's leading edge; overlapping
        /// pairs are rotated apart along their rings (the lower-angle chip rotates back, the
        /// higher-angle chip rotates forward) until every pair clears. The arcs themselves are
        /// unaffected — only the labels move.
        /// </summary>
        private static void ResolveChipAngles(double[] rad, Size[] size, double[] angle)
        {
            int n = angle.Length;
            if (n < 2)
                return;

            const int maxIter = 1200;
            const double step = 0.008;   // radians per overlapping pair, per pass

            for (int iter = 0; iter < maxIter; iter++)
            {
                bool moved = false;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        double xi = rad[i] * Math.Sin(angle[i]);
                        double yi = rad[i] * -Math.Cos(angle[i]);
                        double xj = rad[j] * Math.Sin(angle[j]);
                        double yj = rad[j] * -Math.Cos(angle[j]);

                        double overlapX = (size[i].Width + size[j].Width) / 2 + MinSep - Math.Abs(xi - xj);
                        double overlapY = (size[i].Height + size[j].Height) / 2 + MinSep - Math.Abs(yi - yj);

                        if (overlapX > 0 && overlapY > 0)   // AABBs are within MinSep on both axes
                        {
                            bool iFirst = angle[i] < angle[j] || (angle[i] == angle[j] && i < j);
                            if (iFirst) { angle[i] -= step; angle[j] += step; }
                            else { angle[i] += step; angle[j] -= step; }
                            moved = true;
                        }
                    }
                }
                if (!moved)
                    break;
            }
        }

        private static Border BuildChip(string label, Color chipColor, Color textColor) => new()
        {
            Background = new SolidColorBrush(chipColor),
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
