using System.Text.Json.Nodes;
using DiskSpaceMonitor.Widgets.Circular;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class CircularWidgetTests
    {
        private readonly CircularWidget _factory = new();

        [Test]
        public void Metadata_IsStable()
        {
            _factory.Id.Should().Be("Circular");
            _factory.DisplayName.Should().Be("Circular gauge");
        }

        [Test]
        public void DefaultConfig_HasExpectedDefaults()
        {
            var c = (CircularConfig)_factory.DefaultConfig();

            c.BackgroundOpacity.Should().Be(0.7);
            c.RingThickness.Should().Be(16);
            c.LowThresholdPercent.Should().Be(40);
            c.CriticalThresholdPercent.Should().Be(15);
            c.BackgroundColor.Should().Be("#161A20");
            c.TextColor.Should().Be("#FFFFFF");
            c.Glow.OuterGlowRadius.Should().Be(0);
            c.Glow.OuterGlowColor.Should().Be("#FFFFFF");
        }

        [Test]
        public void WriteThenRead_RoundTripsValues()
        {
            var original = new CircularConfig
            {
                RingThickness = 22,
                LowThresholdPercent = 55,
                TrackColor = "#123456",
                Glow = new DiskSpaceMonitor.Widgets.Effects.GlowEffectConfig
                {
                    OuterGlowRadius = 6,
                    OuterGlowColor = "#FF8800",
                },
            };

            var node = _factory.WriteConfig(original);
            var loaded = (CircularConfig)_factory.ReadConfig(node);

            loaded.RingThickness.Should().Be(22);
            loaded.LowThresholdPercent.Should().Be(55);
            loaded.TrackColor.Should().Be("#123456");
            loaded.BackgroundColor.Should().Be("#161A20"); // untouched default preserved
            loaded.Glow.OuterGlowRadius.Should().Be(6);
            loaded.Glow.OuterGlowColor.Should().Be("#FF8800");
        }

        [Test]
        public void ReadConfig_Null_ReturnsDefaults()
        {
            var c = (CircularConfig)_factory.ReadConfig(null);

            c.RingThickness.Should().Be(16);
        }

        [Test]
        public void ReadConfig_MissingFields_KeepDefaults()
        {
            var partial = new JsonObject { ["RingThickness"] = 30 };

            var c = (CircularConfig)_factory.ReadConfig(partial);

            c.RingThickness.Should().Be(30);
            c.BackgroundOpacity.Should().Be(0.7);   // missing -> default
            c.HealthyColor.Should().Be("#4CAF50");
        }
    }
}
