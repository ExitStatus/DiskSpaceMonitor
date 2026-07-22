using System;
using System.Windows;
using System.Windows.Media;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>Shared arc geometry used by the ring-based widgets: a used-fraction arc drawn
    /// clockwise from 12 o'clock. Kept UI-free of any specific control so it can be reused.</summary>
    internal static class RingArc
    {
        /// <summary>
        /// Build an arc of <paramref name="fraction"/> of a turn [0,1] at <paramref name="radius"/>
        /// around <paramref name="center"/>, clockwise from 12 o'clock. Returns null when the
        /// fraction is ~0 (nothing to draw). Capped just below a full turn so the endpoints don't
        /// coincide and collapse the segment.
        /// </summary>
        public static PathGeometry? Build(Point center, double radius, double fraction)
        {
            if (fraction <= 0.0005)
                return null;

            double angleDeg = Math.Min(fraction, 0.9999) * 360.0;
            double angleRad = angleDeg * Math.PI / 180.0;

            var start = new Point(center.X, center.Y - radius);
            var end = new Point(
                center.X + radius * Math.Sin(angleRad),
                center.Y - radius * Math.Cos(angleRad));

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = angleDeg > 180,
                SweepDirection = SweepDirection.Clockwise
            });

            return new PathGeometry(new[] { figure });
        }
    }
}
