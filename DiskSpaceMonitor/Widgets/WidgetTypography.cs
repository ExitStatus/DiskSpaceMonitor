using System;
using System.Windows.Media;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// The text settings every widget draws with: one font family, and the bounds each widget holds
    /// its <em>rendered</em> text within. Widgets size their text from the space they are given —
    /// the bar graphs scale it with the window, the ring gauges scale it with their Viewbox — so
    /// these bounds are what stop a small widget's labels becoming unreadable and a large one's
    /// filling the screen. They are app-wide rather than per-style: text that changed size when you
    /// switched styles would be surprising.
    /// </summary>
    public sealed class WidgetTypography
    {
        /// <summary>Smallest and largest size the user may choose, either end.</summary>
        public const double SmallestSize = 6;
        public const double LargestSize = 200;

        public const string DefaultFamily = "Segoe UI";
        public const double DefaultMinSize = 8;
        public const double DefaultMaxSize = 72;

        private FontFamily? _family;

        public WidgetTypography(string familyName, double minSize, double maxSize)
        {
            FamilyName = string.IsNullOrWhiteSpace(familyName) ? DefaultFamily : familyName;
            MinSize = Math.Clamp(minSize, SmallestSize, LargestSize);
            MaxSize = Math.Clamp(maxSize, MinSize, LargestSize);
        }

        /// <summary>Name of the font family, as it appears in the font chooser.</summary>
        public string FamilyName { get; }

        /// <summary>Rendered text is never smaller than this.</summary>
        public double MinSize { get; }

        /// <summary>Rendered text is never larger than this.</summary>
        public double MaxSize { get; }

        /// <summary>The family as WPF wants it. Built once and reused — every widget sets it on its
        /// root, and FontFamily inherits down the visual tree to each piece of text.</summary>
        public FontFamily Family => _family ??= new FontFamily(FamilyName);

        public static WidgetTypography Default { get; } =
            new(DefaultFamily, DefaultMinSize, DefaultMaxSize);

        /// <summary>Hold a rendered text size within the configured bounds.</summary>
        public double Clamp(double size) => Math.Clamp(size, MinSize, MaxSize);

        /// <summary>
        /// The design-space font size that renders as <paramref name="designSize"/> scaled by
        /// <paramref name="scale"/>, held within the bounds. For a widget drawn on a fixed design
        /// surface inside a Viewbox, the on-screen size is design × scale, so bounding what the user
        /// actually sees means dividing the bounded size back out by the same scale.
        /// </summary>
        public double DesignFont(double designSize, double scale)
            => scale > 0 ? Clamp(designSize * scale) / scale : designSize;
    }
}
