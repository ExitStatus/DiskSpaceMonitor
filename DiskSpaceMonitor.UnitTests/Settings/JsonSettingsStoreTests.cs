using System.IO;
using System.Text.Json.Nodes;
using DiskSpaceMonitor.Settings;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Settings
{
    [TestFixture]
    public class JsonSettingsStoreTests
    {
        private string _path = null!;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"dsm-{Path.GetRandomFileName()}", "settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            var dir = Path.GetDirectoryName(_path);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }

        [Test]
        public void Load_MissingFile_ReturnsDefaults()
        {
            var settings = new JsonSettingsStore(_path).Load();

            settings.Drives.Should().BeEmpty();
            settings.RefreshSeconds.Should().Be(5);
            settings.Style.Should().Be("Circular");
            settings.WidgetOpacity.Should().Be(1.0);
            settings.StyleConfig.Should().BeNull();
        }

        [Test]
        public void SaveThenLoad_RoundTripsDrivesIntervalAndGlobalWidget()
        {
            var store = new JsonSettingsStore(_path);
            var original = new WidgetSettings
            {
                RefreshSeconds = 30,
                Style = "Circular",
                WidgetOpacity = 0.8,
                StyleConfigs = { ["Circular"] = new JsonObject { ["RingThickness"] = 24, ["TrackColor"] = "#123456" } },
                Drives =
                {
                    new DriveWidgetConfig { DrivePath = "C:\\", Left = 10, Top = 20, Size = 220 },
                    new DriveWidgetConfig { DrivePath = "D:\\", Left = 30, Top = 40, Size = 180 },
                }
            };

            store.Save(original);
            var loaded = store.Load();

            loaded.RefreshSeconds.Should().Be(30);
            loaded.Style.Should().Be("Circular");
            loaded.WidgetOpacity.Should().Be(0.8);
            var cfg = loaded.GetStyleConfig("Circular");
            cfg.Should().NotBeNull();
            cfg!["RingThickness"]!.GetValue<double>().Should().Be(24);
            cfg!["TrackColor"]!.GetValue<string>().Should().Be("#123456");

            loaded.Drives.Should().HaveCount(2);
            loaded.Drives[0].DrivePath.Should().Be("C:\\");
            loaded.Drives[0].Size.Should().Be(220);
            loaded.Drives[1].DrivePath.Should().Be("D:\\");
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryStylesConfig()
        {
            var store = new JsonSettingsStore(_path);
            var original = new WidgetSettings
            {
                Style = "Bar",
                StyleConfigs =
                {
                    ["Circular"] = new JsonObject { ["RingThickness"] = 18 },
                    ["Bar"] = new JsonObject { ["BarWidthPercent"] = 55, ["ShowTotalSpace"] = true },
                },
            };

            store.Save(original);
            var loaded = store.Load();

            // The inactive style keeps its config too, so switching back never loses it.
            loaded.GetStyleConfig("Circular")!["RingThickness"]!.GetValue<double>().Should().Be(18);
            loaded.GetStyleConfig("Bar")!["BarWidthPercent"]!.GetValue<double>().Should().Be(55);
            loaded.GetStyleConfig("Bar")!["ShowTotalSpace"]!.GetValue<bool>().Should().BeTrue();
        }

        [Test]
        public void Load_LegacyV110SingleStyleConfig_IsMigratedToPerStyleMap()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """
                {
                  "Style": "Concentric",
                  "StyleConfig": { "RingThickness": 20, "TrackOpacity": 0.4 },
                  "Drives": [ { "DrivePath": "C:\\", "Size": 200 } ]
                }
                """);

            var loaded = new JsonSettingsStore(_path).Load();

            loaded.Style.Should().Be("Concentric");
            loaded.StyleConfig.Should().BeNull();   // legacy field folded away
            var concentric = loaded.GetStyleConfig("Concentric");
            concentric.Should().NotBeNull();
            concentric!["RingThickness"]!.GetValue<double>().Should().Be(20);
            concentric!["TrackOpacity"]!.GetValue<double>().Should().Be(0.4);
        }

        [Test]
        public void SaveThenLoad_RoundTripsSingleInstancePlacement()
        {
            var store = new JsonSettingsStore(_path);
            var original = new WidgetSettings
            {
                Style = "Concentric",
                SingleInstance = new DriveWidgetConfig { DrivePath = "", Left = 50, Top = 60, Size = 260 },
                Drives = { new DriveWidgetConfig { DrivePath = "C:\\", Left = 10, Top = 20, Size = 200 } },
            };

            store.Save(original);
            var loaded = store.Load();

            loaded.Style.Should().Be("Concentric");
            loaded.SingleInstance.Should().NotBeNull();
            loaded.SingleInstance!.Left.Should().Be(50);
            loaded.SingleInstance.Top.Should().Be(60);
            loaded.SingleInstance.Size.Should().Be(260);
        }

        [Test]
        public void Save_CreatesDirectoryIfMissing()
        {
            var store = new JsonSettingsStore(_path);

            store.Save(new WidgetSettings());

            File.Exists(_path).Should().BeTrue();
        }

        [Test]
        public void Load_LegacySingleWidgetFile_IsMigratedToDrivesList()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """
                { "Left": 100, "Top": 200, "Size": 240, "DrivePath": "E:\\" }
                """);

            var loaded = new JsonSettingsStore(_path).Load();

            loaded.Drives.Should().HaveCount(1);
            loaded.Drives[0].DrivePath.Should().Be("E:\\");
            loaded.Drives[0].Size.Should().Be(240);
            loaded.Style.Should().Be("Circular");
        }

        [Test]
        public void Load_LegacyV1File_FoldsGlobalAppearanceIntoTheSharedWidgetConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """
                {
                  "RefreshSeconds": 10,
                  "BackgroundOpacity": 0.5,
                  "WidgetOpacity": 0.6,
                  "RingThickness": 22,
                  "LowThresholdPercent": 55,
                  "CriticalThresholdPercent": 25,
                  "BackgroundColor": "#111111",
                  "TrackColor": "#222222",
                  "HealthyColor": "#333333",
                  "WarningColor": "#444444",
                  "CriticalColor": "#555555",
                  "TextColor": "#666666",
                  "Drives": [
                    { "DrivePath": "C:\\", "Size": 200 },
                    { "DrivePath": "D:\\", "Size": 180 }
                  ]
                }
                """);

            var loaded = new JsonSettingsStore(_path).Load();

            loaded.RefreshSeconds.Should().Be(10);
            loaded.Style.Should().Be("Circular");
            loaded.WidgetOpacity.Should().Be(0.6);   // top-level global, loaded directly
            var circular = loaded.GetStyleConfig("Circular");
            circular.Should().NotBeNull();
            circular!["RingThickness"]!.GetValue<double>().Should().Be(22);
            circular!["LowThresholdPercent"]!.GetValue<double>().Should().Be(55);
            circular!["BackgroundColor"]!.GetValue<string>().Should().Be("#111111");

            loaded.Drives.Should().HaveCount(2);

            // Legacy appearance globals are cleared so they aren't re-persisted.
            loaded.BackgroundColor.Should().BeNull();
            loaded.RingThickness.Should().BeNull();
        }
    }
}
