using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.VerticalBar;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class VerticalBarWidgetTests
    {
        private readonly VerticalBarWidget _widget = new();

        [Test]
        public void Metadata_IsStableAndMultiDrive()
        {
            _widget.Id.Should().Be("VerticalBar");
            _widget.DisplayName.Should().Be("Vertical bar graph");
            _widget.ShowsAllDrives.Should().BeTrue();
        }

        [Test]
        public void DefaultConfig_HasExpectedDefaults()
        {
            var c = (BarGraphConfig)_widget.DefaultConfig();

            c.Orientation.Should().Be(BarOrientation.BottomUp);
            c.BarStyle.Should().Be(BarStyle.Plain);
            c.BorderSize.Should().Be(2);
            c.BorderColor.Should().Be("#FFFFFF");
            c.HighlightColor.Should().Be("#FFFFFF");
            c.LowlightColor.Should().Be("#000000");
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
            var original = new BarGraphConfig
            {
                Orientation = BarOrientation.TopDown,
                BarStyle = BarStyle.ThreeDBorder,
                BorderSize = 4,
                BorderColor = "#ABCDEF",
                HighlightColor = "#C0FFEE",
                LowlightColor = "#123456",
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
            var loaded = (BarGraphConfig)_widget.ReadConfig(node);

            loaded.Orientation.Should().Be(BarOrientation.TopDown);
            loaded.BarStyle.Should().Be(BarStyle.ThreeDBorder);
            loaded.BorderSize.Should().Be(4);
            loaded.BorderColor.Should().Be("#ABCDEF");
            loaded.HighlightColor.Should().Be("#C0FFEE");
            loaded.LowlightColor.Should().Be("#123456");
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
            var c = (BarGraphConfig)_widget.ReadConfig(null);

            c.TrackOpacity.Should().Be(0.2);
            c.HealthyColor.Should().Be("#4CAF50");
        }

        [Test]
        public void WriteConfig_PersistsOrientationByName()
        {
            var node = _widget.WriteConfig(new BarGraphConfig { Orientation = BarOrientation.TopDown });

            node["Orientation"]!.GetValue<string>().Should().Be("TopDown");
        }

        [Test]
        public void ReadConfig_OmittedOrientation_DefaultsToBottomUp()
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse("""{ "BarWidthPercent": 50 }""");

            var c = (BarGraphConfig)_widget.ReadConfig(node);

            c.Orientation.Should().Be(BarOrientation.BottomUp);
            c.BarWidthPercent.Should().Be(50);
        }

        [Test]
        public void WriteConfig_PersistsBarStyleByName()
        {
            var node = _widget.WriteConfig(new BarGraphConfig { BarStyle = BarStyle.ThreeDBorder });

            node["BarStyle"]!.GetValue<string>().Should().Be("ThreeDBorder");
        }

        [Test]
        public void ReadConfig_PreOutlineSettings_DefaultToPlain()
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse("""{ "BarWidthPercent": 50, "TrackOpacity": 0.5 }""");

            var c = (BarGraphConfig)_widget.ReadConfig(node);

            c.BarStyle.Should().Be(BarStyle.Plain);
            c.BorderSize.Should().Be(2);
            c.LowlightColor.Should().Be("#000000");
        }
    }
}
