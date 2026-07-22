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
            c.LowThresholdPercent.Should().Be(40);
            c.CriticalThresholdPercent.Should().Be(15);
            c.HealthyColor.Should().Be("#4CAF50");
            c.WarningColor.Should().Be("#FFB300");
            c.CriticalColor.Should().Be("#F44336");
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
                LowThresholdPercent = 33,
                CriticalThresholdPercent = 8,
                HealthyColor = "#00FF00",
                WarningColor = "#FFFF00",
                CriticalColor = "#FF0000",
                DriveColors = { ["C:\\"] = "#AABBCC", ["D:\\"] = "#112233" },
            };

            var node = _widget.WriteConfig(original);
            var loaded = (ConcentricConfig)_widget.ReadConfig(node);

            loaded.RingThickness.Should().Be(20);
            loaded.TrackOpacity.Should().Be(0.45);
            loaded.TextColor.Should().Be("#101010");
            loaded.LowThresholdPercent.Should().Be(33);
            loaded.CriticalThresholdPercent.Should().Be(8);
            loaded.HealthyColor.Should().Be("#00FF00");
            loaded.WarningColor.Should().Be("#FFFF00");
            loaded.CriticalColor.Should().Be("#FF0000");
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
