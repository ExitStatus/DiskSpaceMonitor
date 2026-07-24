using System.Text.Json.Nodes;
using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.BarGraph;
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
            c.BarWidthPercent.Should().Be(80);
            c.BarStyle.Should().Be(BarStyle.Plain);
            c.TrackColor.Should().Be("#6E7686");
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
            var node = JsonNode.Parse("""{ "BarWidthPercent": 50 }""");

            var c = (BarGraphConfig)_widget.ReadConfig(node);

            c.Orientation.Should().Be(BarOrientation.LeftToRight);
            c.BarWidthPercent.Should().Be(50);
        }

        // The two graphs share one config type, so a blob can name an orientation belonging to the
        // other axis. Each widget falls back to its own default rather than trying to draw it.
        [Test]
        public void ReadConfig_VerticalOrientation_FallsBackToLeftToRight()
        {
            var node = JsonNode.Parse("""{ "Orientation": "TopDown" }""");

            ((BarGraphConfig)_widget.ReadConfig(node)).Orientation.Should().Be(BarOrientation.LeftToRight);
        }

        // The horizontal graph grows downwards, so its window must keep its width and derive its
        // height — the reverse of every other widget. If this flips, thinning the bars would rescale
        // the content to fill the old frame instead of shortening the window.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void HorizontalView_LetsItsHeightFollowTheContent()
        {
            ((IWidgetView)new HorizontalBarView()).HeightFollowsContent.Should().BeTrue();
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void VerticalView_KeepsHeightAsTheUserDimension()
        {
            ((IWidgetView)new VerticalBarView()).HeightFollowsContent.Should().BeFalse();
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
