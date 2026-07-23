using DiskSpaceMonitor.Widgets.Bar;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class BarWidgetTests
    {
        private readonly BarWidget _widget = new();

        [Test]
        public void Metadata_IsStableAndMultiDrive()
        {
            _widget.Id.Should().Be("Bar");
            _widget.DisplayName.Should().Be("Bar graph");
            _widget.ShowsAllDrives.Should().BeTrue();
        }

        [Test]
        public void DefaultConfig_HasExpectedDefaults()
        {
            var c = (BarConfig)_widget.DefaultConfig();

            c.BarWidthPercent.Should().Be(80);
            c.TrackOpacity.Should().Be(0.2);
            c.ShowUsedSpace.Should().BeFalse();
            c.ShowTotalSpace.Should().BeFalse();
            c.LowThresholdPercent.Should().Be(40);
            c.CriticalThresholdPercent.Should().Be(15);
            c.TrackColor.Should().Be("#6E7686");
            c.HealthyColor.Should().Be("#4CAF50");
            c.WarningColor.Should().Be("#FFB300");
            c.CriticalColor.Should().Be("#F44336");
            c.TextColor.Should().Be("#FFFFFF");
            c.Glow.OuterGlowRadius.Should().Be(0);
            c.Glow.OuterGlowColor.Should().Be("#FFFFFF");
        }

        [Test]
        public void WriteThenRead_RoundTripsValues()
        {
            var original = new BarConfig
            {
                BarWidthPercent = 55,
                TrackOpacity = 0.35,
                ShowUsedSpace = true,
                ShowTotalSpace = true,
                LowThresholdPercent = 30,
                CriticalThresholdPercent = 10,
                TrackColor = "#222222",
                HealthyColor = "#00FF00",
                WarningColor = "#FFFF00",
                CriticalColor = "#FF0000",
                TextColor = "#101010",
                Glow = new DiskSpaceMonitor.Widgets.Effects.GlowEffectConfig
                {
                    OuterGlowRadius = 7,
                    OuterGlowColor = "#00AAFF",
                },
            };

            var node = _widget.WriteConfig(original);
            var loaded = (BarConfig)_widget.ReadConfig(node);

            loaded.BarWidthPercent.Should().Be(55);
            loaded.TrackOpacity.Should().Be(0.35);
            loaded.ShowUsedSpace.Should().BeTrue();
            loaded.ShowTotalSpace.Should().BeTrue();
            loaded.LowThresholdPercent.Should().Be(30);
            loaded.CriticalThresholdPercent.Should().Be(10);
            loaded.TrackColor.Should().Be("#222222");
            loaded.HealthyColor.Should().Be("#00FF00");
            loaded.WarningColor.Should().Be("#FFFF00");
            loaded.CriticalColor.Should().Be("#FF0000");
            loaded.TextColor.Should().Be("#101010");
            loaded.Glow.OuterGlowRadius.Should().Be(7);
            loaded.Glow.OuterGlowColor.Should().Be("#00AAFF");
        }

        [Test]
        public void ReadConfig_Null_ReturnsDefaults()
        {
            var c = (BarConfig)_widget.ReadConfig(null);

            c.TrackOpacity.Should().Be(0.2);
            c.HealthyColor.Should().Be("#4CAF50");
        }
    }
}
