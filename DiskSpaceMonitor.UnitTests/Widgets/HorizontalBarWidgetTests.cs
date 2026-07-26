using System.Text.Json.Nodes;
using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Circular;
using DiskSpaceMonitor.Widgets.Concentric;
using DiskSpaceMonitor.Widgets.HorizontalBar;
using DiskSpaceMonitor.Widgets.VerticalBar;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class HorizontalBarWidgetTests
    {
        private readonly HorizontalBarWidget _widget = new();

        [Test]
        public void Metadata_IsStableAndMultiDrive()
        {
            _widget.Id.Should().Be("HorizontalBar");
            _widget.DisplayName.Should().Be("Horizontal bar graph");
            _widget.ShowsAllDrives.Should().BeTrue();
        }

        [Test]
        public void DefaultConfig_StartsLeftToRight_AndSharesTheBarGraphDefaults()
        {
            var c = (BarGraphConfig)_widget.DefaultConfig();

            c.Orientation.Should().Be(BarOrientation.LeftToRight);
            c.BarGapPercent.Should().Be(20);
            c.BarCornerRadius.Should().Be(3);
            c.BarStyle.Should().Be(BarStyle.Plain);
            c.TrackColor.Should().Be("#6E7686");
        }

        [Test]
        public void WriteThenRead_RoundTripsTheCornerRadius()
        {
            var node = _widget.WriteConfig(new BarGraphConfig { BarCornerRadius = 0 });

            ((BarGraphConfig)_widget.ReadConfig(node)).BarCornerRadius.Should().Be(0);
        }

        [Test]
        public void WriteThenRead_RoundTripsRightToLeft()
        {
            var node = _widget.WriteConfig(new BarGraphConfig { Orientation = BarOrientation.RightToLeft });

            node["Orientation"]!.GetValue<string>().Should().Be("RightToLeft");
            ((BarGraphConfig)_widget.ReadConfig(node)).Orientation.Should().Be(BarOrientation.RightToLeft);
        }

        [Test]
        public void ReadConfig_OmittedOrientation_DefaultsToLeftToRight()
        {
            var node = JsonNode.Parse("""{ "BarGapPercent": 50 }""");

            var c = (BarGraphConfig)_widget.ReadConfig(node);

            c.Orientation.Should().Be(BarOrientation.LeftToRight);
            c.BarGapPercent.Should().Be(50);
        }

        // The two graphs share one config type, so a blob can name an orientation belonging to the
        // other axis. Each widget falls back to its own default rather than trying to draw it.
        [Test]
        public void ReadConfig_VerticalOrientation_FallsBackToLeftToRight()
        {
            var node = JsonNode.Parse("""{ "Orientation": "TopDown" }""");

            ((BarGraphConfig)_widget.ReadConfig(node)).Orientation.Should().Be(BarOrientation.LeftToRight);
        }

        // Both graphs fill whatever rectangle the user drags, so their windows must offer both
        // dimensions (side handles as well as corners). If this flips they would be forced square
        // and a stretch in one direction would rescale the whole chart instead of the bars.
        // The cast is needed because ResizesFreely is a default interface member.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void BothBarGraphs_SizeEachDirectionSeparately()
        {
            ((IWidgetView)new HorizontalBarView()).ResizesFreely.Should().BeTrue();
            ((IWidgetView)new VerticalBarView()).ResizesFreely.Should().BeTrue();
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void RoundWidgets_StaySquare()
        {
            ((IWidgetView)new CircularView()).ResizesFreely.Should().BeFalse();
            ((IWidgetView)new ConcentricView()).ResizesFreely.Should().BeFalse();
        }

        [Test]
        public void VerticalWidget_ReadingHorizontalOrientation_FallsBackToBottomUp()
        {
            var node = JsonNode.Parse("""{ "Orientation": "RightToLeft" }""");

            ((BarGraphConfig)new VerticalBarWidget().ReadConfig(node)).Orientation
                .Should().Be(BarOrientation.BottomUp);
        }
    }
}
