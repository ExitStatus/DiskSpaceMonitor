using System.Linq;
using System.Text.Json.Nodes;
using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Box;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class BoxWidgetTests
    {
        private readonly BoxWidget _widget = new();

        [Test]
        public void Metadata_IsStableAndPerDrive()
        {
            _widget.Id.Should().Be("Box");
            _widget.DisplayName.Should().Be("Box");
            _widget.ShowsAllDrives.Should().BeFalse();
        }

        // A box holds two lines above a bar, so it has to open wide rather than as the square every
        // other per-drive style starts at — otherwise every drive needs dragging into shape by hand.
        [Test]
        public void DefaultWindowSize_OpensWideRatherThanSquare()
        {
            var size = _widget.DefaultWindowSize;

            size.Should().NotBeNull();
            size!.Value.Width.Should().BeGreaterThan(size.Value.Height);
        }

        [Test]
        public void DefaultConfig_MatchesTheOtherGraphsWhereTheyOverlap()
        {
            var c = (BoxConfig)_widget.DefaultConfig();

            c.CornerRadius.Should().Be(8);
            c.BarCornerRadius.Should().Be(3);
            c.BarHeightPercent.Should().Be(18);
            c.BackgroundOpacity.Should().Be(0.7);
            c.BorderOpacity.Should().Be(1.0);
            c.TrackOpacity.Should().Be(0.2);
            c.LowThresholdPercent.Should().Be(40);
            c.CriticalThresholdPercent.Should().Be(15);
            c.TrackColor.Should().Be("#6E7686");
            c.BoxStyle.Should().Be(BarStyle.Plain);
            c.BarStyle.Should().Be(BarStyle.Plain);
            c.BoxBorderSize.Should().Be(2);
            c.BarBorderSize.Should().Be(2);
        }

        // The panel and the bar are outlined independently — that is why they have a tab each — so
        // their widths have to be stored apart rather than sharing one.
        [Test]
        public void WriteThenRead_KeepsTheBoxAndBarOutlineWidthsApart()
        {
            var node = _widget.WriteConfig(new BoxConfig { BoxBorderSize = 6, BarBorderSize = 1 });

            var read = (BoxConfig)_widget.ReadConfig(node);
            read.BoxBorderSize.Should().Be(6);
            read.BarBorderSize.Should().Be(1);
        }

        // Both sets of outline colours on one Effects tab made it scroll, so the panel's settings
        // and the bar's each get their own.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void Editor_GivesThePanelAndTheBarATabEach()
        {
            var editor = _widget.CreateEditor(_widget.DefaultConfig(), () => { }, System.Array.Empty<string>());

            editor.Tabs.Select(t => t.Header).Should()
                .Equal("Appearance", "Colours", "Box effects", "Bar effects");
        }

        // The panel and the bar are styled apart, so both outlines have to survive a round-trip
        // independently — and as names, so a hand-edited settings file stays readable.
        [Test]
        public void WriteThenRead_RoundTripsBothOutlineStylesByName()
        {
            var node = _widget.WriteConfig(new BoxConfig
            {
                BoxStyle = BarStyle.ThreeDBorder,
                BarStyle = BarStyle.Border,
            });

            node["BoxStyle"]!.GetValue<string>().Should().Be("ThreeDBorder");
            node["BarStyle"]!.GetValue<string>().Should().Be("Border");

            var read = (BoxConfig)_widget.ReadConfig(node);
            read.BoxStyle.Should().Be(BarStyle.ThreeDBorder);
            read.BarStyle.Should().Be(BarStyle.Border);
        }

        [Test]
        public void WriteThenRead_RoundTripsTheShapeSettings()
        {
            var node = _widget.WriteConfig(new BoxConfig
            {
                CornerRadius = 0,
                BarCornerRadius = 20,
                BarHeightPercent = 45,
                BorderOpacity = 0.35,
            });

            var read = (BoxConfig)_widget.ReadConfig(node);
            read.CornerRadius.Should().Be(0);
            read.BarCornerRadius.Should().Be(20);
            read.BarHeightPercent.Should().Be(45);
            read.BorderOpacity.Should().Be(0.35);
        }

        [Test]
        public void ReadConfig_Null_ReturnsDefaults()
        {
            ((BoxConfig)_widget.ReadConfig(null)).CornerRadius.Should().Be(8);
        }

        // A blob written before a setting existed simply omits it, which is how every style
        // migrates: the stored values load and the new one takes its default.
        [Test]
        public void ReadConfig_PartialBlob_KeepsTheOtherDefaults()
        {
            var node = JsonNode.Parse("""{ "BarHeightPercent": 30, "TextColor": "#00FF00" }""");

            var c = (BoxConfig)_widget.ReadConfig(node);
            c.BarHeightPercent.Should().Be(30);
            c.TextColor.Should().Be("#00FF00");
            c.CornerRadius.Should().Be(8);
            c.BackgroundColor.Should().Be("#161A20");
        }

        [Test]
        public void ReadConfig_CorruptBlob_FallsBackToDefaults()
        {
            var node = JsonNode.Parse("""{ "CornerRadius": "not a number" }""");

            ((BoxConfig)_widget.ReadConfig(node)).CornerRadius.Should().Be(8);
        }

        // The box is a wide shape, so its window must offer both dimensions (side handles as well
        // as corners). If this flips it would be forced square.
        // The cast is needed because ResizesFreely is a default interface member.
        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void BoxView_SizesEachDirectionSeparately()
        {
            ((IWidgetView)new BoxView()).ResizesFreely.Should().BeTrue();
        }
    }
}
