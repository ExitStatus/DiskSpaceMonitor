using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiskSpaceMonitor.Widgets.BarGraph
{
    /// <summary>
    /// One bar to draw: its drive label, used fraction [0,1], fill (status) colour, and the optional
    /// used-space and total-space captions (empty to hide either).
    /// </summary>
    internal readonly record struct Bar(string Letter, double UsedFraction, Color Fill,
        string UsedLabel, string TotalLabel);

    /// <summary>
    /// How to outline a bar's fill: the chosen style, the outline width, the corner rounding, and
    /// the colours each style draws with (the unused ones are ignored). The gauge scales
    /// <paramref name="Size"/> and <paramref name="Corner"/> with its text, so an outline keeps its
    /// weight relative to the graph rather than turning into a hairline on a large widget.
    /// </summary>
    internal readonly record struct BarSkin(BarStyle Style, double Size, double Corner, Color Border,
        Color Highlight, Color Lowlight);

    /// <summary>
    /// Drawing primitives shared by both bar graph gauges. The two gauges lay their bars out along
    /// different axes, but a bar's fill and the text measuring are identical either way.
    /// </summary>
    internal static class BarGraphParts
    {
        /// <summary>Corner rounding applied to a bar's fill and track, before the gauge's scale.</summary>
        internal const double CornerRadius = 3;

        /// <summary>The bar's rounding at the gauge's current scale, never sharper than a hairline.</summary>
        internal static double Corner(double scale) => Math.Max(1, CornerRadius * scale);

        /// <summary>
        /// The used portion of a bar, outlined per the chosen bar style. Plain is a bare rounded
        /// block; Border rings it evenly; 3D Border lays a lit edge and a shaded edge over it so the
        /// bar reads as raised. The bevel is two overlaid Borders because a single one can only
        /// carry one brush, and the two halves need different colours. The bevel is anchored to the
        /// screen (top-left lit) rather than to the axis, so it stays consistent whichever way the
        /// bars grow.
        /// </summary>
        internal static FrameworkElement BuildFill(Color color, BarSkin skin)
        {
            var corners = new CornerRadius(skin.Corner);
            var fill = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = corners,
            };

            double size = Math.Max(0, skin.Size);
            if (size <= 0 || skin.Style == BarStyle.Plain)
                return fill;

            if (skin.Style == BarStyle.Border)
            {
                fill.BorderBrush = new SolidColorBrush(skin.Border);
                fill.BorderThickness = new Thickness(size);
                return fill;
            }

            var bevel = new Grid();
            bevel.Children.Add(fill);
            bevel.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(skin.Highlight),
                BorderThickness = new Thickness(size, size, 0, 0),   // top + left
                CornerRadius = corners,
            });
            bevel.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(skin.Lowlight),
                BorderThickness = new Thickness(0, 0, size, size),   // right + bottom
                CornerRadius = corners,
            });
            return bevel;
        }

        /// <summary>Natural size of a caption at a given font size, used to pick one that fits.</summary>
        internal static Size Measure(string text, double fontSize)
        {
            var tb = new TextBlock { Text = text, FontSize = fontSize };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return tb.DesiredSize;
        }
    }
}
