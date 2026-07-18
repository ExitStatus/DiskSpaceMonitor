using System.IO;
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
            var store = new JsonSettingsStore(_path);

            var settings = store.Load();

            settings.Drives.Should().BeEmpty();
            settings.RefreshSeconds.Should().Be(5);
            settings.BackgroundOpacity.Should().Be(0.7);
            settings.WidgetOpacity.Should().Be(1.0);
            settings.RingThickness.Should().Be(16);
            settings.LowThresholdPercent.Should().Be(40);
            settings.CriticalThresholdPercent.Should().Be(15);
            settings.BackgroundColor.Should().Be("#161A20");
            settings.TrackColor.Should().Be("#6E7686");
            settings.HealthyColor.Should().Be("#4CAF50");
            settings.TextColor.Should().Be("#FFFFFF");
        }

        [Test]
        public void SaveThenLoad_RoundTripsDrivesAndInterval()
        {
            var store = new JsonSettingsStore(_path);
            var original = new WidgetSettings
            {
                RefreshSeconds = 30,
                BackgroundOpacity = 0.35,
                WidgetOpacity = 0.8,
                RingThickness = 24,
                LowThresholdPercent = 50,
                CriticalThresholdPercent = 20,
                TrackColor = "#123456",
                CriticalColor = "#ABCDEF",
                Drives =
                {
                    new DriveWidgetConfig { DrivePath = "C:\\", Left = 10, Top = 20, Size = 220 },
                    new DriveWidgetConfig { DrivePath = "D:\\", Left = 30, Top = 40, Size = 180 },
                }
            };

            store.Save(original);
            var loaded = store.Load();

            loaded.RefreshSeconds.Should().Be(30);
            loaded.BackgroundOpacity.Should().Be(0.35);
            loaded.WidgetOpacity.Should().Be(0.8);
            loaded.RingThickness.Should().Be(24);
            loaded.LowThresholdPercent.Should().Be(50);
            loaded.CriticalThresholdPercent.Should().Be(20);
            loaded.TrackColor.Should().Be("#123456");
            loaded.CriticalColor.Should().Be("#ABCDEF");
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
        public void Load_LegacyFile_IsMigratedToDrivesList()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """
                { "Left": 100, "Top": 200, "Size": 240, "DrivePath": "E:\\" }
                """);

            var loaded = new JsonSettingsStore(_path).Load();

            loaded.Drives.Should().HaveCount(1);
            loaded.Drives[0].DrivePath.Should().Be("E:\\");
            loaded.Drives[0].Size.Should().Be(240);
        }
    }
}
