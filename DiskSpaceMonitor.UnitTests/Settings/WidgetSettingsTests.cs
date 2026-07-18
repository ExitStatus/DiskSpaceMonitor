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
    }
}
