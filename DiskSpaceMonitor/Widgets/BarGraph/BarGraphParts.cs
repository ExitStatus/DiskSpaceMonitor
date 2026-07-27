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
    /// How to outline a filled shape — a bar, or the box widget's whole panel: the chosen style,
    /// the outline width, the corner rounding, and the colours each style draws with (the unused
    /// ones are ignored). The widget scales <paramref name="Size"/> and <paramref name="Corner"/>
    /// with its text, so an outline keeps its weight relative to the widget rather than turning
    /// into a hairline on a large one.
    /// </summary>
    internal readonly record struct BarSkin(BarStyle Style, double Size, double Corner, Color Border,
        Color Highlight, Color Lowlight);

    /// <summary>
    /// Drawing primitives shared by both bar graph gauges. The two gauges lay their bars out along
    /// different axes, but a bar's fill and the text measuring are identical either way.
    /// </summary>
    internal static class BarGraphParts
    {
        /// <summary>
        /// The user's corner rounding at the gauge's current scale. Zero stays zero — square corners
        /// are a deliberate choice — but any rounding at all keeps at least a hairline, so shrinking
        /// a graph rounds the bars less rather than squaring them off.
        /// </summary>
        internal static double Corner(double radius, double scale)
            => radius <= 0 ? 0 : Math.Max(1, radius * scale);

        /// <summary>
        /// A font size at the gauge's current scale, held within the user's size bounds. A graph
        /// squeezed along one axis scales its text down with it, and past a point the labels stop
        /// being readable at all — better to let text crowd its bar than render what nobody can make
        /// out. The upper bound does the same at the other end.
        /// </summary>
        internal static double Font(double baseSize, double scale, WidgetTypography typography)
            => typography.Clamp(baseSize * scale);

        /// <summary>
        /// A filled, rounded block outlined per the chosen style — the used portion of a bar, or
        /// the box widget's panel. Plain is bare; Border rings it evenly; 3D Border lays a lit edge
        /// and a shaded edge over it so the block reads as raised. The bevel is two overlaid Borders
        /// because a single one can only carry one brush, and the two halves need different colours.
        /// The bevel is anchored to the screen (top-left lit) rather than to the axis, so it stays
        /// consistent whichever way the bars grow.
        /// <para><paramref name="content"/> rides inside the fill, so a caller wanting a panel gets
        /// the same three styles the bars use. It sits under the bevel's edges, so pad it clear of
        /// them.</para>
        /// </summary>
        internal static FrameworkElement BuildFill(Color color, BarSkin skin, UIElement? content = null)
        {
            var corners = new CornerRadius(skin.Corner);
            var fill = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = corners,
                Child = content,
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
