using DiskSpaceMonitor.Drives;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Drives
{
    [TestFixture]
    public class ByteSizeTests
    {
        [TestCase(0L, "0 B")]
        [TestCase(512L, "512 B")]
        [TestCase(1024L, "1 KB")]
        [TestCase(1536L, "1.5 KB")]
        [TestCase(1048576L, "1 MB")]
        [TestCase(1073741824L, "1 GB")]
        [TestCase(1610612736L, "1.5 GB")]
        public void Humanize_ScalesToBase1024WithOneDecimal(long bytes, string expected)
        {
            ByteSize.Humanize(bytes).Should().Be(expected);
        }

        [Test]
        public void Humanize_Negative_KeepsSign()
        {
            ByteSize.Humanize(-1536).Should().Be("-1.5 KB");
        }

        [Test]
        public void Humanize_UsesInvariantDecimalPoint()
        {
            // Always a '.' regardless of the current culture.
            ByteSize.Humanize(1536).Should().Contain(".");
        }
    }
}
