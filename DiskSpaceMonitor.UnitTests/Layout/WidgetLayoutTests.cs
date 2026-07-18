using System.Collections.Generic;
using System.Windows;
using DiskSpaceMonitor.Layout;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Layout
{
    [TestFixture]
    public class WidgetLayoutTests
    {
        private static IReadOnlyList<Rect> Others(params Rect[] rects) => rects;

        // --- Overlaps --------------------------------------------------------

        [Test]
        public void Overlaps_OverlappingRects_True()
        {
            WidgetLayout.Overlaps(new Rect(0, 0, 100, 100), new Rect(50, 50, 100, 100))
                .Should().BeTrue();
        }

        [Test]
        public void Overlaps_FlushEdges_False()
        {
            // Touching along an edge is not an overlap.
            WidgetLayout.Overlaps(new Rect(0, 0, 100, 100), new Rect(100, 0, 100, 100))
                .Should().BeFalse();
        }

        [Test]
        public void Overlaps_Separated_False()
        {
            WidgetLayout.Overlaps(new Rect(0, 0, 100, 100), new Rect(200, 0, 100, 100))
                .Should().BeFalse();
        }

        // --- Snapping (move) -------------------------------------------------

        [Test]
        public void SnapMove_LeftEdgeWithinThreshold_SnapsToAlign()
        {
            var others = Others(new Rect(300, 300, 100, 100));

            // Left edge 296 is 4px from the other's left (300) → snaps to 300.
            var (left, top) = WidgetLayout.SnapMove(296, 0, 100, 100, others);

            left.Should().Be(300);
            top.Should().Be(0); // nothing to snap vertically
        }

        [Test]
        public void SnapMove_RightEdgeNearOtherLeft_SnapsFlush()
        {
            var others = Others(new Rect(300, 0, 100, 100));

            // Our right edge (left+100) is near the other's left (300) → left snaps to 200.
            var (left, _) = WidgetLayout.SnapMove(203, 0, 100, 100, others);

            left.Should().Be(200); // 300 - width(100)
        }

        [Test]
        public void SnapMove_OutsideThreshold_NoSnap()
        {
            var others = Others(new Rect(300, 0, 100, 100));

            var (left, top) = WidgetLayout.SnapMove(280, 0, 100, 100, others);

            left.Should().Be(280);
            top.Should().Be(0);
        }

        // --- Collision (move) ------------------------------------------------

        [Test]
        public void ClampMoveX_MovingRightIntoNeighbour_StopsFlush()
        {
            var others = Others(new Rect(300, 0, 100, 100));

            // Same row, moving right from 100; desired 260 would overlap → clamp to 200.
            double result = WidgetLayout.ClampMoveX(100, 260, 0, 100, 100, others);

            result.Should().Be(200); // 300 - width
        }

        [Test]
        public void ClampMoveX_DifferentRow_NoClamp()
        {
            // Neighbour is far below → no shared rows → free movement.
            var others = Others(new Rect(300, 500, 100, 100));

            double result = WidgetLayout.ClampMoveX(100, 260, 0, 100, 100, others);

            result.Should().Be(260);
        }

        [Test]
        public void ClampMoveY_MovingDownIntoNeighbour_StopsFlush()
        {
            var others = Others(new Rect(0, 300, 100, 100));

            double result = WidgetLayout.ClampMoveY(100, 260, 0, 100, 100, others);

            result.Should().Be(200); // 300 - height
        }

        // --- Screen edge snapping --------------------------------------------

        [Test]
        public void SnapToScreen_NearLeftEdge_SnapsToScreenLeft()
        {
            var screen = new Rect(0, 0, 1920, 1080);

            var (left, top) = WidgetLayout.SnapToScreen(6, 500, 200, 200, screen);

            left.Should().Be(0);
            top.Should().Be(500);
        }

        [Test]
        public void SnapToScreen_NearBottomEdge_SnapsRightAndBottom()
        {
            var screen = new Rect(0, 0, 1920, 1080);

            // Right edge near 1920, bottom edge near 1080.
            var (left, top) = WidgetLayout.SnapToScreen(1716, 884, 200, 200, screen);

            left.Should().Be(1720); // 1920 - 200
            top.Should().Be(880);   // 1080 - 200
        }

        [Test]
        public void SnapToScreen_MiddleOfScreen_NoSnap()
        {
            var screen = new Rect(0, 0, 1920, 1080);

            var (left, top) = WidgetLayout.SnapToScreen(800, 400, 200, 200, screen);

            left.Should().Be(800);
            top.Should().Be(400);
        }

        // --- On-screen constraint --------------------------------------------

        [Test]
        public void Constrain_OffLeftTop_PullsInsideBounds()
        {
            var bounds = new Rect(0, 0, 1920, 1080);

            var (left, top) = WidgetLayout.Constrain(-50, -30, 200, 200, bounds);

            left.Should().Be(0);
            top.Should().Be(0);
        }

        [Test]
        public void Constrain_OffRightBottom_PullsInsideBounds()
        {
            var bounds = new Rect(0, 0, 1920, 1080);

            var (left, top) = WidgetLayout.Constrain(1900, 1000, 200, 200, bounds);

            left.Should().Be(1720); // 1920 - 200
            top.Should().Be(880);   // 1080 - 200
        }

        [Test]
        public void Constrain_AcrossVirtualDesktop_AllowsSecondMonitorPosition()
        {
            // A second monitor to the right: virtual bounds span both.
            var bounds = new Rect(0, 0, 3840, 1080);

            var (left, top) = WidgetLayout.Constrain(2000, 300, 200, 200, bounds);

            left.Should().Be(2000); // already valid on the second display
            top.Should().Be(300);
        }

        [Test]
        public void Constrain_NegativeOriginBounds_Respected()
        {
            // A monitor positioned to the left of primary (negative X).
            var bounds = new Rect(-1920, 0, 3840, 1080);

            var (left, _) = WidgetLayout.Constrain(-2000, 100, 200, 200, bounds);

            left.Should().Be(-1920);
        }

        // --- Snapping + clamping (resize) ------------------------------------

        [Test]
        public void SnapAndClampResize_GrowingIntoNeighbour_ClampsToGap()
        {
            // Anchored at top-left (0,0), dragging bottom-right, neighbour at x=250.
            var others = Others(new Rect(250, 0, 100, 100));

            double size = WidgetLayout.SnapAndClampResize(
                anchorX: 0, anchorY: 0, left: false, top: false,
                candidateSize: 400, others, minSize: 120, maxSize: 600);

            size.Should().Be(250); // can't cross the neighbour's left edge
        }

        [Test]
        public void SnapAndClampResize_NeighbourBehindAnchor_NoClamp()
        {
            // Neighbour is to the left of the anchor and we grow right → it never blocks.
            var others = Others(new Rect(-500, 0, 100, 100));

            double size = WidgetLayout.SnapAndClampResize(
                anchorX: 0, anchorY: 0, left: false, top: false,
                candidateSize: 300, others, minSize: 120, maxSize: 600);

            size.Should().Be(300);
        }

        [Test]
        public void SnapAndClampResize_ClampsToMinAndMax()
        {
            var none = Others();

            WidgetLayout.SnapAndClampResize(0, 0, false, false, 50, none, 120, 600)
                .Should().Be(120);
            WidgetLayout.SnapAndClampResize(0, 0, false, false, 5000, none, 120, 600)
                .Should().Be(600);
        }
    }
}
