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
                Style = "VerticalBar",
                StyleConfigs =
                {
                    ["Circular"] = new JsonObject { ["RingThickness"] = 18 },
                    ["VerticalBar"] = new JsonObject { ["BarWidthPercent"] = 55, ["ShowTotalSpace"] = true },
                },
            };

            store.Save(original);
            var loaded = store.Load();

            // The inactive style keeps its config too, so switching back never loses it.
            loaded.GetStyleConfig("Circular")!["RingThickness"]!.GetValue<double>().Should().Be(18);
            loaded.GetStyleConfig("VerticalBar")!["BarWidthPercent"]!.GetValue<double>().Should().Be(55);
            loaded.GetStyleConfig("VerticalBar")!["ShowTotalSpace"]!.GetValue<bool>().Should().BeTrue();
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
        public void SaveThenLoad_RoundTripsEveryMultiDriveStylesPlacement()
        {
            var store = new JsonSettingsStore(_path);
            var original = new WidgetSettings
            {
                Style = "Concentric",
                SingleInstances =
                {
                    ["Concentric"] = new DriveWidgetConfig { DrivePath = "", Left = 50, Top = 60, Size = 260 },
                    // A freely-sized style stores a rectangle instead of the square size.
                    ["VerticalBar"] = new DriveWidgetConfig { DrivePath = "", Left = 70, Top = 80, Width = 420, Height = 300 },
                },
                Drives = { new DriveWidgetConfig { DrivePath = "C:\\", Left = 10, Top = 20, Size = 200 } },
            };

            store.Save(original);
            var loaded = store.Load();

            loaded.Style.Should().Be("Concentric");

            var concentric = loaded.SingleInstances["Concentric"];
            concentric.Left.Should().Be(50);
            concentric.Top.Should().Be(60);
            concentric.Size.Should().Be(260);
            concentric.Width.Should().BeNull();

            // The inactive style keeps its own rectangle, so switching back restores that shape.
            var vertical = loaded.SingleInstances["VerticalBar"];
            vertical.Left.Should().Be(70);
            vertical.Width.Should().Be(420);
            vertical.Height.Should().Be(300);
        }

        // Width/Height are null until a widget has been freely sized. They must be left out of the
        // file rather than written as a value, so a square widget's entry stays as terse as it was.
        [Test]
        public void Save_UnsizedWidget_OmitsTheRectangle()
        {
            new JsonSettingsStore(_path).Save(new WidgetSettings
            {
                Drives = { new DriveWidgetConfig { DrivePath = "C:\\", Left = 10, Top = 20, Size = 200 } },
            });

            File.ReadAllText(_path).Should().NotContain("\"Width\"").And.NotContain("\"Height\"");
        }

        [Test]
        public void SingleInstanceFor_UnknownStyle_CreatesAndKeepsOneRecord()
        {
            var settings = new WidgetSettings();

            var first = settings.SingleInstanceFor("VerticalBar");
            first.Left = 42;

            settings.SingleInstanceFor("VerticalBar").Left.Should().Be(42);
            settings.SingleInstanceFor("HorizontalBar").Should().NotBeSameAs(first);
        }

        [Test]
        public void Load_LegacySharedMultiDrivePlacement_MovesToTheActiveStyle()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """
                {
                  "Style": "VerticalBar",
                  "SingleInstance": { "DrivePath": "", "Left": 50, "Top": 60, "Size": 260 }
                }
                """);

            var loaded = new JsonSettingsStore(_path).Load();

            loaded.SingleInstance.Should().BeNull();   // legacy field folded away
            loaded.SingleInstances.Should().ContainKey("VerticalBar");
            loaded.SingleInstances["VerticalBar"].Left.Should().Be(50);
            loaded.SingleInstances["VerticalBar"].Size.Should().Be(260);
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
