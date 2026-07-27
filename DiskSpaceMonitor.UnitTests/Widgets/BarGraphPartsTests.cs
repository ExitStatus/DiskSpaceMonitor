using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Widgets.BarGraph;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class BarGraphPartsTests
    {
        // The rounding is given at the graph's reference size and scaled with it, so a bar keeps its
        // shape as the widget is dragged rather than looking blockier the bigger it gets.
        [Test]
        public void Corner_ScalesWithTheGraph()
        {
            BarGraphParts.Corner(6, 1).Should().Be(6);
            BarGraphParts.Corner(6, 2.5).Should().Be(15);
        }

        // Square corners are a deliberate choice, so zero has to survive the scaling untouched.
        [Test]
        public void Corner_Zero_StaysSquareAtEverySize()
        {
            BarGraphParts.Corner(0, 0.4).Should().Be(0);
            BarGraphParts.Corner(0, 6).Should().Be(0);
        }

        // Any rounding at all keeps at least a hairline: shrinking a graph should round the bars less,
        // not silently square them off at the point the scaled radius drops below a pixel.
        [Test]
        public void Corner_SmallRadiusOnASmallGraph_KeepsAHairline()
        {
            BarGraphParts.Corner(1, 0.4).Should().Be(1);
        }

        // The bar graphs pass no content: the fill is a bare block, and must stay one.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void BuildFill_WithoutContent_IsABareBlock()
        {
            var fill = (Border)BarGraphParts.BuildFill(Colors.Red, Skin(BarStyle.Border));

            fill.Child.Should().BeNull();
            fill.BorderThickness.Should().Be(new Thickness(2));
        }

        // The box widget outlines a whole panel with the same three styles, so a caller must be
        // able to put content inside the fill — under the bevel's edges in the 3D case.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        [TestCase(BarStyle.Plain)]
        [TestCase(BarStyle.Border)]
        [TestCase(BarStyle.ThreeDBorder)]
        public void BuildFill_WithContent_HostsItInsideTheFill(BarStyle style)
        {
            var content = new TextBlock { Text = "C: 931 GB" };

            var built = BarGraphParts.BuildFill(Colors.Red, Skin(style), content);

            FirstBorder(built).Child.Should().BeSameAs(content);
        }

        // Plain and Border return the fill itself; the bevel wraps it in a Grid with the two edge
        // borders over the top, so the fill is that Grid's first child.
        private static Border FirstBorder(FrameworkElement built)
            => built as Border ?? (Border)((Grid)built).Children[0];

        private static BarSkin Skin(BarStyle style)
            => new(style, 2, 4, Colors.White, Colors.White, Colors.Black);
    }
}
