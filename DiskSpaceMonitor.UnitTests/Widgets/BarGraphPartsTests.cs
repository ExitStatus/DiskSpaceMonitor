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
    }
}
