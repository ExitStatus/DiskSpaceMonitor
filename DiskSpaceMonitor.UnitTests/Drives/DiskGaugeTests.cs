using DiskSpaceMonitor.Drives;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Drives
{
    [TestFixture]
    public class DiskGaugeTests
    {
        [Test]
        public void UsedFraction_HalfFull_ReturnsHalf()
        {
            DiskGauge.UsedFraction(50, 100).Should().Be(0.5);
        }

        [Test]
        public void UsedFraction_ZeroTotal_ReturnsZero()
        {
            DiskGauge.UsedFraction(10, 0).Should().Be(0);
        }

        [Test]
        public void UsedFraction_OverfullReading_ClampsToOne()
        {
            DiskGauge.UsedFraction(150, 100).Should().Be(1);
        }

        [TestCase(0.60, DiskFillLevel.Healthy)]  // 60% free
        [TestCase(0.41, DiskFillLevel.Healthy)]
        [TestCase(0.40, DiskFillLevel.Warning)]  // boundary is exclusive
        [TestCase(0.20, DiskFillLevel.Warning)]
        [TestCase(0.15, DiskFillLevel.Critical)]
        [TestCase(0.05, DiskFillLevel.Critical)]
        public void LevelForFree_MapsThresholds(double freeFraction, DiskFillLevel expected)
        {
            DiskGauge.LevelForFree(freeFraction).Should().Be(expected);
        }

        [TestCase(0.60, DiskFillLevel.Healthy)]  // above low threshold (0.50)
        [TestCase(0.50, DiskFillLevel.Warning)]  // low boundary is exclusive
        [TestCase(0.30, DiskFillLevel.Warning)]
        [TestCase(0.25, DiskFillLevel.Critical)] // critical boundary is exclusive
        [TestCase(0.10, DiskFillLevel.Critical)]
        public void LevelForFree_CustomThresholds_MapsAccordingly(double freeFraction, DiskFillLevel expected)
        {
            DiskGauge.LevelForFree(freeFraction, lowThreshold: 0.50, criticalThreshold: 0.25)
                .Should().Be(expected);
        }
    }
}
