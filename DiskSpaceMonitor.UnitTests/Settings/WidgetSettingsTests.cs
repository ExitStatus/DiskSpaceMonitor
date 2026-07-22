using System.Text.Json.Nodes;
using DiskSpaceMonitor.Settings;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Settings
{
    [TestFixture]
    public class WidgetSettingsTests
    {
        [Test]
        public void Migrate_LegacySingleWidget_BecomesOneDriveEntry()
        {
            var settings = new WidgetSettings
            {
                DrivePath = "D:\\",
                Left = 100,
                Top = 200,
                Size = 240
            };

            settings.Migrate();

            settings.Drives.Should().HaveCount(1);
            var drive = settings.Drives[0];
            drive.DrivePath.Should().Be("D:\\");
            drive.Left.Should().Be(100);
            drive.Top.Should().Be(200);
            drive.Size.Should().Be(240);

            // Legacy fields are cleared so they aren't re-persisted.
            settings.DrivePath.Should().BeNull();
            settings.Left.Should().BeNull();
        }

        [Test]
        public void Migrate_AlreadyMultiDrive_LeavesDrivesUntouched()
        {
            var settings = new WidgetSettings
            {
                Drives = { new DriveWidgetConfig { DrivePath = "C:\\" } },
                DrivePath = "D:\\" // stray legacy value should be ignored
            };

            settings.Migrate();

            settings.Drives.Should().HaveCount(1);
            settings.Drives[0].DrivePath.Should().Be("C:\\");
        }

        [Test]
        public void Migrate_Empty_StaysEmpty()
        {
            var settings = new WidgetSettings();

            settings.Migrate();

            settings.Drives.Should().BeEmpty();
        }

        [Test]
        public void Migrate_LegacyGlobalAppearance_FoldsIntoSharedCircularConfig()
        {
            var settings = new WidgetSettings
            {
                Drives =
                {
                    new DriveWidgetConfig { DrivePath = "C:\\" },
                    new DriveWidgetConfig { DrivePath = "D:\\" },
                },
                BackgroundOpacity = 0.5,
                WidgetOpacity = 0.6,
                RingThickness = 22,
                LowThresholdPercent = 55,
                CriticalThresholdPercent = 25,
                BackgroundColor = "#111111",
                TrackColor = "#222222",
                HealthyColor = "#333333",
                WarningColor = "#444444",
                CriticalColor = "#555555",
                TextColor = "#666666",
            };

            settings.Migrate();

            settings.Style.Should().Be("Circular");
            settings.WidgetOpacity.Should().Be(0.6);   // top-level global, not nulled
            settings.StyleConfig.Should().NotBeNull();
            settings.StyleConfig!["RingThickness"]!.GetValue<double>().Should().Be(22);
            settings.StyleConfig!["CriticalThresholdPercent"]!.GetValue<double>().Should().Be(25);
            settings.StyleConfig!["BackgroundColor"]!.GetValue<string>().Should().Be("#111111");

            // Appearance legacy globals cleared so they aren't re-persisted.
            settings.BackgroundColor.Should().BeNull();
            settings.RingThickness.Should().BeNull();

            // Drives keep just drive + placement.
            settings.Drives.Should().HaveCount(2);
        }
    }
}
