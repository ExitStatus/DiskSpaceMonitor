using System.Windows.Media;
using DiskSpaceMonitor.Views;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Views
{
    [TestFixture]
    public class HsvColorTests
    {
        [Test]
        public void FromRgb_PrimaryColours_HaveExpectedHue()
        {
            HsvColor.FromRgb(Color.FromRgb(255, 0, 0)).Should().Be((0.0, 1.0, 1.0));
            HsvColor.FromRgb(Color.FromRgb(0, 255, 0)).Should().Be((120.0, 1.0, 1.0));
            HsvColor.FromRgb(Color.FromRgb(0, 0, 255)).Should().Be((240.0, 1.0, 1.0));
        }

        [Test]
        public void FromRgb_Greys_HaveZeroSaturation()
        {
            var (h, s, v) = HsvColor.FromRgb(Color.FromRgb(128, 128, 128));
            h.Should().Be(0);
            s.Should().Be(0);
            v.Should().BeApproximately(128 / 255.0, 1e-9);
        }

        [Test]
        public void ToRgb_KnownValues_ProduceExpectedColours()
        {
            HsvColor.ToRgb(0, 1, 1).Should().Be(Color.FromRgb(255, 0, 0));
            HsvColor.ToRgb(120, 1, 1).Should().Be(Color.FromRgb(0, 255, 0));
            HsvColor.ToRgb(240, 1, 0.5).Should().Be(Color.FromRgb(0, 0, 128));
            HsvColor.ToRgb(0, 0, 0).Should().Be(Color.FromRgb(0, 0, 0));
        }

        [Test]
        [TestCase((byte)76, (byte)175, (byte)80)]    // healthy green
        [TestCase((byte)255, (byte)179, (byte)0)]    // amber
        [TestCase((byte)244, (byte)67, (byte)54)]    // red
        [TestCase((byte)22, (byte)26, (byte)32)]     // near-black
        [TestCase((byte)110, (byte)118, (byte)134)]  // grey-blue
        [TestCase((byte)255, (byte)255, (byte)255)]  // white
        public void RoundTrip_RgbToHsvAndBack_IsLossless(byte r, byte g, byte b)
        {
            var original = Color.FromRgb(r, g, b);
            var (h, s, v) = HsvColor.FromRgb(original);

            HsvColor.ToRgb(h, s, v).Should().Be(original);
        }
    }
}
