using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.HorizontalBar
{
    /// <summary>
    /// Draws a horizontal bar per drive: the value axis runs 0–100% (used space) across the window
    /// and each bar fills from the 0% end to its used %. The unused part beyond the fill is a faint
    /// track. Left-to-right puts 0% at the left (bars grow right); right-to-left flips the axis so
    /// 0% is at the right and the bars grow left. This is the vertical graph transposed: the value
    /// axis is horizontal and the bars stack down the window, so the design width is fixed and the
    /// height sizes to the bars.
    /// </summary>
    public partial class HorizontalBarGauge : UserControl
    {
        // This graph grows downwards, so the view reports HeightFollowsContent: the window keeps the
        // width the user gave it and derives its height. That makes the Viewbox's scale
        // windowWidth ÷ designWidth, and the design width is fixed (a fixed plot plus label columns
        // whose text doesn't change size), so the scale is fixed too — text and gaps hold their size
        // on screen. The design height is then free to shrink with the bars, taking the window's
        // height with it.
        private const double PlotWidth = 240;       // design width of the 0–100% axis
        private const double MaxBarThickness = 34;  // bar thickness at 100%
        private const double BarGap = 10;           // space between bars — fixed, so it never changes
        private const double AxisFont = 10;
        private const double AxisGap = 4;           // space between the plot and the value axis
        private const double CaptionBaseFont = 10;  // default size for the used/total captions
        private const double CaptionMinFont = 6;    // never shrink a caption below this
        private const double LineHeightFactor = 1.4;// text line height as a multiple of the font size

        private double _aspect = 1;

        // The outer-glow effect for this render, or null when off. Each text element is built through
        // GlowEffect.Wrap so the glow sits behind crisp glyphs rather than blurring the font.
        private Effect? _glow;

        // True when the axis is flipped (0% at the right, bars growing left). Held for this render so
        // the builders below can mirror themselves without every one taking the orientation.
        private bool _rightToLeft;

        // How this render outlines each bar's fill.
        private BarSkin _skin;

        /// <summary>Content width ÷ height after the last render; the window fits itself to this.</summary>
        internal double DesignAspect => _aspect;

        public HorizontalBarGauge()
        {
            InitializeComponent();
        }

        internal void Render(IReadOnlyList<Bar> bars, Color track, double trackOpacity, Color text,
            double barSize, BarOrientation orientation, BarSkin skin, Effect? glow)
        {
            _glow = glow;
            _rightToLeft = orientation == BarOrientation.RightToLeft;
            _skin = skin;
            Root.Children.Clear();
            Root.ColumnDefinitions.Clear();
            Root.RowDefinitions.Clear();

            if (bars.Count == 0)
            {
                Root.Width = Root.Height = 0;
                _aspect = 1;
                return;
            }

            // Both dimensions size to content: the width from the fixed plot plus the label columns,
            // the height from the bars. Only the height actually moves — which is the point, since
            // the window's height follows it.
            Root.Width = double.NaN;
            Root.Height = double.NaN;

            // Both annotation columns are Auto; which one holds the totals and which holds the drive
            // labels swaps with the orientation, so each stays on its own end of the axis.
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                // annotations left
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PlotWidth) });      // plot
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                // annotations right
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                // plot (bars)
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                // value axis

            // The totals sit at the 100% end and the drive labels at the 0% end, so left-to-right
            // puts labels on the left and totals on the right, and right-to-left swaps them.
            int totalsCol = _rightToLeft ? 0 : 2;
            int labelsCol = _rightToLeft ? 2 : 0;

            byte trackAlpha = (byte)(Math.Clamp(trackOpacity, 0, 1) * 255);
            var trackBrush = new SolidColorBrush(Color.FromArgb(trackAlpha, track.R, track.G, track.B));

            // Only the bars themselves scale with the thickness setting; the gap between them is
            // fixed, so thinning the bars closes the graph up rather than spreading it out. Each
            // drive gets a slot of bar + gap, and the design height is simply the sum of them.
            int n = bars.Count;
            double barThickness = Math.Clamp(barSize, 0.05, 1) * MaxBarThickness;
            double slot = barThickness + BarGap;

            // One uniform font size for the used/total captions. Unlike the vertical graph — where a
            // caption is constrained by the bar's width and rotates when it can't fit — a horizontal
            // bar has the whole axis to write along, so the limit is how much room it has vertically.
            // That is the whole slot, not just the bar: a caption sits beside its bar, so it may use
            // the gap around it and only shrinks once the slot itself gets tight. Never rotate —
            // rotated text would need even more of the same scarce dimension.
            double captionFont = Math.Clamp(slot / LineHeightFactor, CaptionMinFont, CaptionBaseFont);

            // Value axis (0 / 50 / 100) under the plot, aligned to the plot's edges.
            var axis = BuildValueAxis(text);
            axis.Margin = new Thickness(0, AxisGap, 0, 0);
            Root.Children.Add(Place(axis, row: 1, col: 1));

            // Plot: faint gridlines behind (full height), then the group of bars.
            var plot = new Grid();
            plot.Children.Add(BuildGridlines(text));

            var barColumn = BuildSlotColumn(n, slot,
                i => BuildBar(bars[i], trackBrush, trackAlpha, text, captionFont, barThickness));
            plot.Children.Add(barColumn);

            Root.Children.Add(Place(plot, row: 0, col: 1));

            // Total-space captions, hugging the 100% end of the plot, aligned with each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = BuildSlotColumn(n, slot,
                    i => BuildCaption(bars[i].TotalLabel, text, captionFont, HorizontalAlignment.Center));
                totals.Margin = _rightToLeft ? new Thickness(0, 0, 4, 0) : new Thickness(4, 0, 0, 0);
                Root.Children.Add(Place(totals, row: 0, totalsCol));
            }

            // Drive labels (letter + used %), aligned with the bars at the 0% end of the axis.
            var labels = BuildSlotColumn(n, slot, i => BuildDriveLabel(bars[i], text));
            labels.Margin = _rightToLeft ? new Thickness(6, 0, 0, 0) : new Thickness(0, 0, 6, 0);
            Root.Children.Add(Place(labels, row: 0, labelsCol));

            // Record the content's aspect (the fixed design width over its natural height) so the
            // window can size itself to it. Measuring picks up the auto-sized annotation columns.
            Root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double w = Root.DesiredSize.Width, h = Root.DesiredSize.Height;
            _aspect = (w > 0 && h > 0) ? w / h : 1;
        }

        private static FrameworkElement Place(FrameworkElement e, int row, int col)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            return e;
        }

        // One equal slot per drive — a bar plus its gap — sized in design pixels. Every column of
        // the chart (bars, totals and drive labels) is built on this same grid, so they stay aligned
        // whatever the bar thickness, and each child gets the whole slot to sit in. The bar visuals
        // are the only thing inset to the bar's own thickness, so captions and labels beside a thin
        // bar keep the full slot height and never clip.
        private static Grid BuildSlotColumn(int count, double slot, Func<int, FrameworkElement> makeChild)
        {
            var grid = new Grid();
            for (int i = 0; i < count; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(slot) });

            for (int i = 0; i < count; i++)
            {
                var child = makeChild(i);
                Grid.SetRow(child, i);
                grid.Children.Add(child);
            }

            return grid;
        }

        private FrameworkElement BuildBar(Bar bar, Brush trackBrush, byte trackAlpha, Color text,
            double captionFont, double thickness)
        {
            double used = Math.Clamp(bar.UsedFraction, 0, 1);

            // The fill takes the column at the 0% end (left, or right when flipped) and the unused
            // remainder takes the other, so the bar grows away from the axis origin either way.
            int fillCol = _rightToLeft ? 1 : 0;
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? 1 - used : used, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? used : 1 - used, GridUnitType.Star) });

            // The bar visuals are the bar's own thickness, centred in the slot; the gap around them
            // is the rest of the slot and stays the same however thin the bars get.
            if (trackAlpha > 0)
            {
                var track = new Rectangle
                {
                    Fill = trackBrush,
                    RadiusX = BarGraphParts.CornerRadius,
                    RadiusY = BarGraphParts.CornerRadius,
                    Height = thickness,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumnSpan(track, 2);
                row.Children.Add(track);
            }

            var fill = BarGraphParts.BuildFill(bar.Fill, _skin);
            fill.Height = thickness;
            fill.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(fill, fillCol);
            row.Children.Add(fill);

            // Used-space caption, riding just past the fill's leading edge in the unused column so it
            // stays legible against the faint track rather than the fill.
            if (!string.IsNullOrEmpty(bar.UsedLabel))
            {
                var caption = BuildCaption(bar.UsedLabel, text, captionFont,
                    _rightToLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                caption.Margin = _rightToLeft ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
                Grid.SetColumn(caption, _rightToLeft ? 0 : 1);
                row.Children.Add(caption);
            }

            return row;
        }

        // 0 / 50 / 100 across the bottom, anchored to the plot's edges so the end labels sit inside
        // the plot rather than overhanging it. 0% sits at the axis origin: left, or right when flipped.
        private FrameworkElement BuildValueAxis(Color text)
        {
            var grid = new Grid();
            grid.Children.Add(AxisTick(_rightToLeft ? "100%" : "0%", text, HorizontalAlignment.Left));
            grid.Children.Add(AxisTick("50%", text, HorizontalAlignment.Center));
            grid.Children.Add(AxisTick(_rightToLeft ? "0%" : "100%", text, HorizontalAlignment.Right));
            return grid;
        }

        private FrameworkElement AxisTick(string label, Color text, HorizontalAlignment h)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = label,
                FontSize = AxisFont,
                Opacity = 0.7,
                Foreground = new SolidColorBrush(text),
                HorizontalAlignment = h,
                VerticalAlignment = VerticalAlignment.Top,
            }, _glow);

        private static FrameworkElement BuildGridlines(Color text)
        {
            var grid = new Grid();
            grid.Children.Add(Gridline(text, HorizontalAlignment.Left));
            grid.Children.Add(Gridline(text, HorizontalAlignment.Center));
            grid.Children.Add(Gridline(text, HorizontalAlignment.Right));
            return grid;
        }

        private static Rectangle Gridline(Color text, HorizontalAlignment h) => new()
        {
            Width = 1,
            Fill = new SolidColorBrush(Color.FromArgb(38, text.R, text.G, text.B)), // ~15% opacity
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = h,
        };

        private FrameworkElement BuildCaption(string label, Color text, double fontSize, HorizontalAlignment h)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = label,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(text),
                HorizontalAlignment = h,
                VerticalAlignment = VerticalAlignment.Center,
            }, _glow);

        // The drive letter and its used %, on one line so the label stays no taller than its bar.
        private FrameworkElement BuildDriveLabel(Bar bar, Color text)
            => GlowEffect.Wrap(() =>
            {
                var tb = new TextBlock
                {
                    HorizontalAlignment = _rightToLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(text),
                    FontSize = 11,
                };
                tb.Inlines.Add(new Run(bar.Letter) { FontWeight = FontWeights.SemiBold });
                tb.Inlines.Add(new Run($" {Math.Clamp(bar.UsedFraction, 0, 1) * 100:0}%") { FontSize = 10 });
                return tb;
            }, _glow);
    }
}
