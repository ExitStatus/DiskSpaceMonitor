using DiskSpaceMonitor.Widgets.Concentric;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class ConcentricWidgetTests
    {
        private readonly ConcentricWidget _widget = new();

        [Test]
        public void Metadata_IsStableAndMultiDrive()
        {
            _widget.Id.Should().Be("Concentric");
            _widget.DisplayName.Should().Be("Concentric circles");
            _widget.ShowsAllDrives.Should().BeTrue();
        }

        [Test]
        public void DefaultConfig_HasExpectedDefaults()
        {
            var c = (ConcentricConfig)_widget.DefaultConfig();

            c.RingThickness.Should().Be(14);
            c.TrackOpacity.Should().Be(0.2);
            c.TextColor.Should().Be("#FFFFFF");
            c.DriveColors.Should().BeEmpty();
        }

        [Test]
        public void WriteThenRead_RoundTripsValues()
        {
            var original = new ConcentricConfig
            {
                RingThickness = 20,
                TrackOpacity = 0.45,
                TextColor = "#101010",
                DriveColors = { ["C:\\"] = "#AABBCC", ["D:\\"] = "#112233" },
            };

            var node = _widget.WriteConfig(original);
            var loaded = (ConcentricConfig)_widget.ReadConfig(node);

            loaded.RingThickness.Should().Be(20);
            loaded.TrackOpacity.Should().Be(0.45);
            loaded.TextColor.Should().Be("#101010");
            loaded.DriveColors.Should().HaveCount(2);
            loaded.DriveColors["C:\\"].Should().Be("#AABBCC");
            loaded.DriveColors["D:\\"].Should().Be("#112233");
        }

        [Test]
        public void ReadConfig_Null_ReturnsDefaults()
        {
            var c = (ConcentricConfig)_widget.ReadConfig(null);

            c.RingThickness.Should().Be(14);
            c.DriveColors.Should().BeEmpty();
        }

        [Test]
        public void Palette_UsesConfiguredColourOrFallsBackByIndex()
        {
            var config = new ConcentricConfig { DriveColors = { ["C:\\"] = "#123456" } };

            ConcentricPalette.ColorFor(config, "C:\\", 0).Should().Be("#123456");   // configured
            ConcentricPalette.ColorFor(config, "D:\\", 1)
                .Should().Be(ConcentricPalette.Default[1]);                          // palette default
            ConcentricPalette.ColorFor(config, "Z:\\", ConcentricPalette.Default.Length)
                .Should().Be(ConcentricPalette.Default[0]);                          // cycles
        }
    }
}
