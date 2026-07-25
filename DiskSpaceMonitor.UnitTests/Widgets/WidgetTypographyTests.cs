using DiskSpaceMonitor.Widgets;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class WidgetTypographyTests
    {
        [Test]
        public void Default_IsTheStockFontAndAWideRange()
        {
            var t = WidgetTypography.Default;

            t.FamilyName.Should().Be("Segoe UI");
            t.MinSize.Should().Be(8);
            t.MaxSize.Should().Be(72);
        }

        [Test]
        public void Clamp_HoldsTextWithinTheBounds()
        {
            var t = new WidgetTypography("Arial", 10, 20);

            t.Clamp(4).Should().Be(10);
            t.Clamp(15).Should().Be(15);
            t.Clamp(400).Should().Be(20);
        }

        // A widget on a fixed design surface is scaled by a Viewbox, so bounding what the user sees
        // means dividing the bounded size back out: at 4x, a 20pt ceiling is a 5pt design font.
        [Test]
        public void DesignFont_BoundsTheRenderedSizeNotTheDesignSize()
        {
            var t = new WidgetTypography("Arial", 10, 20);

            t.DesignFont(23, 4).Should().Be(5);          // 92 rendered -> capped at 20 -> 20/4
            t.DesignFont(23, 1).Should().Be(20);         // 23 rendered -> capped at 20
            t.DesignFont(12, 1).Should().Be(12);         // inside the bounds, left alone
            t.DesignFont(4, 1).Should().Be(10);          // below the floor -> raised
        }

        [Test]
        public void DesignFont_ZeroScale_FallsBackToTheDesignSize()
        {
            // Before the first layout pass there is no scale to divide by; don't produce infinity.
            new WidgetTypography("Arial", 10, 20).DesignFont(23, 0).Should().Be(23);
        }

        [Test]
        public void Constructor_CrossedBounds_KeepsMaximumAtLeastTheMinimum()
        {
            var t = new WidgetTypography("Arial", 30, 12);

            t.MinSize.Should().Be(30);
            t.MaxSize.Should().Be(30);
        }

        [Test]
        public void Constructor_SizesOutsideWhatCanBeChosen_AreBroughtBackIn()
        {
            var tiny = new WidgetTypography("Arial", 1, 5);
            tiny.MinSize.Should().Be(WidgetTypography.SmallestSize);

            var huge = new WidgetTypography("Arial", 8, 9999);
            huge.MaxSize.Should().Be(WidgetTypography.LargestSize);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Constructor_NoFamilyName_FallsBackToTheDefault(string? name)
        {
            new WidgetTypography(name!, 8, 72).FamilyName.Should().Be(WidgetTypography.DefaultFamily);
        }
    }
}
