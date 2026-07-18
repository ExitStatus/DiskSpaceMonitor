using System;
using System.Collections.Generic;
using System.Windows;

namespace DiskSpaceMonitor.Layout
{
    /// <summary>
    /// Geometry helpers for keeping widget windows from overlapping and for snapping
    /// their edges to one another while moving and resizing. All values are in the
    /// same (screen, DIP) coordinate space as Window.Left/Top/Width/Height.
    /// </summary>
    public static class WidgetLayout
    {
        /// <summary>How close (px) an edge must be to snap.</summary>
        public const double SnapThreshold = 10;

        private const double Eps = 0.5;

        /// <summary>True if two rectangles overlap (touching edges do not count).</summary>
        public static bool Overlaps(Rect a, Rect b) =>
            a.Left < b.Right - Eps && a.Right > b.Left + Eps &&
            a.Top < b.Bottom - Eps && a.Bottom > b.Top + Eps;

        // --- Moving ----------------------------------------------------------

        /// <summary>Snap a moving rect's left/top so an edge lines up with another window.</summary>
        public static (double left, double top) SnapMove(
            double left, double top, double w, double h, IReadOnlyList<Rect> others)
        {
            double snappedLeft = SnapAxis(left, w, others, horizontal: true);
            double snappedTop = SnapAxis(top, h, others, horizontal: false);
            return (snappedLeft, snappedTop);
        }

        private static double SnapAxis(double lo, double size, IReadOnlyList<Rect> others, bool horizontal)
        {
            double best = lo;
            double bestDist = SnapThreshold;

            foreach (var o in others)
            {
                double near = horizontal ? o.Left : o.Top;
                double far = horizontal ? o.Right : o.Bottom;

                // Align this window's low edge, or its high edge, to either edge of o.
                Consider(near);
                Consider(far);
                Consider(near - size);
                Consider(far - size);
            }

            return best;

            void Consider(double candidate)
            {
                double d = Math.Abs(candidate - lo);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = candidate;
                }
            }
        }

        /// <summary>Clamp horizontal movement so the window slides flush against, but never into, others.</summary>
        public static double ClampMoveX(double startLeft, double desiredLeft,
            double top, double w, double h, IReadOnlyList<Rect> others)
        {
            double result = desiredLeft;
            foreach (var o in others)
            {
                bool sharesRows = top < o.Bottom - Eps && top + h > o.Top + Eps;
                if (!sharesRows)
                    continue;

                if (desiredLeft > startLeft && startLeft + w <= o.Left + Eps)
                    result = Math.Min(result, o.Left - w);
                else if (desiredLeft < startLeft && startLeft >= o.Right - Eps)
                    result = Math.Max(result, o.Right);
            }

            return result;
        }

        /// <summary>Clamp vertical movement so the window slides flush against, but never into, others.</summary>
        public static double ClampMoveY(double startTop, double desiredTop,
            double left, double w, double h, IReadOnlyList<Rect> others)
        {
            double result = desiredTop;
            foreach (var o in others)
            {
                bool sharesColumns = left < o.Right - Eps && left + w > o.Left + Eps;
                if (!sharesColumns)
                    continue;

                if (desiredTop > startTop && startTop + h <= o.Top + Eps)
                    result = Math.Min(result, o.Top - h);
                else if (desiredTop < startTop && startTop >= o.Bottom - Eps)
                    result = Math.Max(result, o.Bottom);
            }

            return result;
        }

        // --- Screen edges ----------------------------------------------------

        /// <summary>Snap the window's edges to the edges of a screen work area.</summary>
        public static (double left, double top) SnapToScreen(
            double left, double top, double w, double h, Rect screen)
        {
            double bestLeft = left, dxBest = SnapThreshold;
            double bestTop = top, dyBest = SnapThreshold;

            ConsiderX(screen.Left);          // left edge to screen left
            ConsiderX(screen.Right - w);     // right edge to screen right
            ConsiderY(screen.Top);           // top edge to screen top
            ConsiderY(screen.Bottom - h);    // bottom edge to screen bottom

            return (bestLeft, bestTop);

            void ConsiderX(double c)
            {
                double d = Math.Abs(c - left);
                if (d < dxBest) { dxBest = d; bestLeft = c; }
            }

            void ConsiderY(double c)
            {
                double d = Math.Abs(c - top);
                if (d < dyBest) { dyBest = d; bestTop = c; }
            }
        }

        /// <summary>Clamp the window so it stays fully within <paramref name="bounds"/>.</summary>
        public static (double left, double top) Constrain(
            double left, double top, double w, double h, Rect bounds)
        {
            double maxLeft = bounds.Right - w;
            double maxTop = bounds.Bottom - h;

            // If the window is larger than the bounds, pin to the top-left corner.
            double l = maxLeft >= bounds.Left ? Math.Clamp(left, bounds.Left, maxLeft) : bounds.Left;
            double t = maxTop >= bounds.Top ? Math.Clamp(top, bounds.Top, maxTop) : bounds.Top;
            return (l, t);
        }

        // --- Resizing --------------------------------------------------------

        /// <summary>
        /// Snap and clamp a square resize. The window is anchored at (anchorX, anchorY)
        /// (the corner opposite the one being dragged) and grows by <paramref name="candidateSize"/>
        /// in the direction implied by <paramref name="left"/>/<paramref name="top"/>.
        /// </summary>
        public static double SnapAndClampResize(double anchorX, double anchorY, bool left, bool top,
            double candidateSize, IReadOnlyList<Rect> others, double minSize, double maxSize)
        {
            int sgnX = left ? -1 : 1;
            int sgnY = top ? -1 : 1;

            // Snap the moving edges to nearby other-window edges.
            double size = candidateSize;
            double bestDist = SnapThreshold;
            foreach (var o in others)
            {
                ConsiderSnap(sgnX > 0 ? o.Left - anchorX : anchorX - o.Left);
                ConsiderSnap(sgnX > 0 ? o.Right - anchorX : anchorX - o.Right);
                ConsiderSnap(sgnY > 0 ? o.Top - anchorY : anchorY - o.Top);
                ConsiderSnap(sgnY > 0 ? o.Bottom - anchorY : anchorY - o.Bottom);
            }

            // Clamp so the growing square never overlaps another window.
            foreach (var o in others)
            {
                double dx = AxisGap(anchorX, sgnX, o.Left, o.Right);
                double dy = AxisGap(anchorY, sgnY, o.Top, o.Bottom);
                if (double.IsNaN(dx) || double.IsNaN(dy))
                    continue; // obstacle not in the growth quadrant

                size = Math.Min(size, Math.Max(dx, dy));
            }

            return Math.Clamp(size, minSize, maxSize);

            void ConsiderSnap(double s)
            {
                if (s < minSize || s > maxSize)
                    return;
                double d = Math.Abs(s - candidateSize);
                if (d < bestDist)
                {
                    bestDist = d;
                    size = s;
                }
            }
        }

        /// <summary>
        /// Size at which a corner-anchored edge growing along <paramref name="sgn"/> first
        /// reaches obstacle interval [lo, hi]. NaN if it never can (obstacle is behind).
        /// </summary>
        private static double AxisGap(double anchor, int sgn, double lo, double hi)
        {
            if (sgn > 0)
            {
                if (hi <= anchor)
                    return double.NaN;
                return Math.Max(0, lo - anchor);
            }

            if (lo >= anchor)
                return double.NaN;
            return Math.Max(0, anchor - hi);
        }
    }
}
