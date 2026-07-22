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
                StyleConfig = new JsonObject { ["RingThickness"] = 24, ["TrackColor"] = "#123456" },
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
            loaded.StyleConfig.Should().NotBeNull();
            loaded.StyleConfig!["RingThickness"]!.GetValue<double>().Should().Be(24);
            loaded.StyleConfig!["TrackColor"]!.GetValue<string>().Should().Be("#123456");

            loaded.Drives.Should().HaveCount(2);
            loaded.Drives[0].DrivePath.Should().Be("C:\\");
            loaded.Drives[0].Size.Should().Be(220);
            loaded.Drives[1].DrivePath.Should().Be("D:\\");
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
            loaded.StyleConfig.Should().NotBeNull();
            loaded.StyleConfig!["RingThickness"]!.GetValue<double>().Should().Be(22);
            loaded.StyleConfig!["LowThresholdPercent"]!.GetValue<double>().Should().Be(55);
            loaded.StyleConfig!["BackgroundColor"]!.GetValue<string>().Should().Be("#111111");

            loaded.Drives.Should().HaveCount(2);

            // Legacy appearance globals are cleared so they aren't re-persisted.
            loaded.BackgroundColor.Should().BeNull();
            loaded.RingThickness.Should().BeNull();
        }
    }
}
