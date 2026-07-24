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

namespace DiskSpaceMonitor.Widgets.VerticalBar
{
    /// <summary>
    /// Draws a vertical bar per drive: the y-axis runs 0–100% (used space) and each bar fills from
    /// the 0% end to its used %. The unused part beyond the fill is a faint track. Bottom-up puts
    /// 0% at the bottom (bars grow up); top-down flips the axis so 0% is at the top and the bars
    /// hang down. Built from proportional Grid rows so the bars reflow smoothly when resized.
    /// </summary>
    public partial class VerticalBarGauge : UserControl
    {
        // Fixed design height; the width sizes to content so the graph hugs the bars. The Viewbox
        // scales it (and every label) to the actual window size.
        private const double DesignHeight = 200;
        private const double MaxBarWidth = 46;  // bar thickness at 100% width
        private const double MaxGap = 14;       // gap between bars at 100% width (both scale with width)
        private const double EdgePad = 14;      // room on the right for outer label overflow
        private const double CaptionBaseFont = 10;     // default size for the used/total captions
        private const double CaptionMinFont = 6;       // shrink down to here before rotating
        private const double CaptionRotatedFont = 8;   // size used once rotated

        private double _aspect = 1;

        // The outer-glow effect for this render, or null when off. Each text element is built through
        // GlowEffect.Wrap so the glow sits behind crisp glyphs rather than blurring the font.
        private Effect? _glow;

        // True when the axis is flipped (0% at the top, bars hanging down). Held for this render so
        // the builders below can mirror themselves without every one taking the orientation.
        private bool _topDown;

        // How this render outlines each bar's fill.
        private BarSkin _skin;

        /// <summary>Content width ÷ height after the last render; the window fits itself to this.</summary>
        internal double DesignAspect => _aspect;

        public VerticalBarGauge()
        {
            InitializeComponent();
        }

        internal void Render(IReadOnlyList<Bar> bars, Color track, double trackOpacity, Color text,
            double barWidth, BarOrientation orientation, BarSkin skin, Effect? glow)
        {
            _glow = glow;
            _topDown = orientation == BarOrientation.TopDown;
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

            // Fixed design height (so the star-sized bar rows have a height to divide) but the width
            // is left to size to content: as the bars get narrower the design – and therefore the
            // rendered graph – gets narrower too, hugging the bars instead of leaving space at the
            // edges. The plot column is Auto so the gridlines span exactly the bar group.
            Root.Height = DesignHeight;
            Root.Width = double.NaN;

            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // y-axis
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // plot (bars)
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(EdgePad) });  // right label overflow
            // Both annotation rows are Auto; which one holds the totals and which holds the drive
            // labels swaps with the orientation, so each stays on its own end of the axis.
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // annotations above
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // plot
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // annotations below

            // The totals sit at the 100% end and the drive labels at the 0% end, so bottom-up puts
            // totals on top and labels underneath, and top-down swaps them.
            int totalsRow = _topDown ? 2 : 0;
            int labelsRow = _topDown ? 0 : 2;

            byte trackAlpha = (byte)(Math.Clamp(trackOpacity, 0, 1) * 255);
            var trackBrush = new SolidColorBrush(Color.FromArgb(trackAlpha, track.R, track.G, track.B));

            // Bar thickness and the gap between bars both scale with the width setting, so a narrower
            // bar also sits closer to its neighbours. The bars are centred in the plot as a group.
            int n = bars.Count;
            double f = Math.Clamp(barWidth, 0.05, 1);
            double barThickness = f * MaxBarWidth;
            double gap = f * MaxGap;

            // One uniform font size for the used/total captions so they fit within the bar width. If
            // that would drop below the minimum, keep the minimum and rotate the text 90° CCW instead,
            // so a narrow bar still fits it. Every caption uses the one size.
            var captionTexts = new List<string>();
            foreach (var b in bars)
            {
                if (!string.IsNullOrEmpty(b.UsedLabel)) captionTexts.Add(b.UsedLabel);
                if (!string.IsNullOrEmpty(b.TotalLabel)) captionTexts.Add(b.TotalLabel);
            }
            double captionFont = CaptionBaseFont;
            bool rotateCaptions = false;
            if (captionTexts.Count > 0)
            {
                double maxWidth = captionTexts.Max(c => BarGraphParts.Measure(c, CaptionBaseFont).Width);
                double fitFont = maxWidth > 0 ? CaptionBaseFont * barThickness / maxWidth : CaptionBaseFont;
                fitFont = Math.Min(CaptionBaseFont, fitFont);
                rotateCaptions = fitFont < CaptionMinFont;
                captionFont = rotateCaptions ? CaptionRotatedFont : fitFont;
            }

            // Total-space header, hugging the 100% end of the plot, aligned with each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = BuildAlignedRow(n, barThickness, gap, i => BuildCaption(bars[i].TotalLabel, text, captionFont, rotateCaptions));
                totals.VerticalAlignment = _topDown ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                totals.Margin = _topDown ? new Thickness(0, 2, 0, 0) : new Thickness(0, 0, 0, 2);
                Root.Children.Add(Place(totals, totalsRow, col: 1));
            }

            // Y-axis (100 / 50 / 0) aligned to the plot area.
            var yaxis = BuildYAxis(text);
            yaxis.Margin = new Thickness(0, 0, 6, 0);
            Root.Children.Add(Place(yaxis, row: 1, col: 0));

            // Plot: faint gridlines behind (full width), then the centred group of bars.
            var plot = new Grid();
            plot.Children.Add(BuildGridlines(text));

            var barRow = BuildAlignedRow(n, barThickness, gap, i => BuildBar(bars[i], trackBrush, trackAlpha, text, captionFont, rotateCaptions));
            barRow.VerticalAlignment = VerticalAlignment.Stretch;
            plot.Children.Add(barRow);

            Root.Children.Add(Place(plot, row: 1, col: 1));

            // Drive labels (letter + used %), aligned with the bars at the 0% end of the axis.
            var labels = BuildAlignedRow(n, barThickness, gap, i => BuildXLabel(bars[i], text));
            labels.Margin = _topDown ? new Thickness(0, 0, 0, 4) : new Thickness(0, 4, 0, 0);
            Root.Children.Add(Place(labels, labelsRow, col: 1));

            // Record the content's aspect (its natural width over the fixed design height) so the
            // window can size itself to it. Measuring picks up the auto-sized y-axis and bar group.
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

        // A horizontally-centred grid of fixed-width bar columns separated by fixed-width gaps, with
        // one child per bar. Sharing this layout keeps the bars, their captions and their labels
        // aligned regardless of the bar width.
        private static Grid BuildAlignedRow(int count, double barThickness, double gap, Func<int, FrameworkElement> makeChild)
        {
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(barThickness) });
            }

            for (int i = 0; i < count; i++)
            {
                var child = makeChild(i);
                Grid.SetColumn(child, i * 2);   // bar columns sit at even indices (gaps are odd)
                grid.Children.Add(child);
            }

            return grid;
        }

        private FrameworkElement BuildBar(Bar bar, Brush trackBrush, byte trackAlpha, Color text,
            double captionFont, bool rotate)
        {
            double used = Math.Clamp(bar.UsedFraction, 0, 1);

            // The fill takes the row at the 0% end (bottom, or top when flipped) and the unused
            // remainder takes the other, so the bar grows away from the axis origin either way.
            int fillRow = _topDown ? 0 : 1;
            var col = new Grid();
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_topDown ? used : 1 - used, GridUnitType.Star) });
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_topDown ? 1 - used : used, GridUnitType.Star) });

            if (trackAlpha > 0)
            {
                var track = new Rectangle
                {
                    Fill = trackBrush,
                    RadiusX = BarGraphParts.CornerRadius,
                    RadiusY = BarGraphParts.CornerRadius,
                };
                Grid.SetRowSpan(track, 2);
                col.Children.Add(track);
            }

            var fill = BarGraphParts.BuildFill(bar.Fill, _skin);
            Grid.SetRow(fill, fillRow);
            col.Children.Add(fill);

            // Used-space caption. Horizontal: rides just past the fill's leading edge (the unused row,
            // aligned to the shared border), overflowing into the reserved room at high usage.
            // Rotated: reads up from inside the fill, at the 0% end of the bar.
            if (!string.IsNullOrEmpty(bar.UsedLabel))
            {
                var caption = BuildCaption(bar.UsedLabel, text, captionFont, rotate);
                double nudge = rotate ? 6 : 2;   // rotated: nudged a further 4px clear of the end
                caption.Margin = _topDown ? new Thickness(0, nudge, 0, 0) : new Thickness(0, 0, 0, nudge);

                // Horizontal captions sit in the unused row so they ride the fill's leading edge;
                // rotated ones span the whole bar and their alignment parks them at the 0% end.
                Grid.SetRow(caption, rotate || !_topDown ? 0 : 1);
                if (rotate)
                    Grid.SetRowSpan(caption, 2);
                col.Children.Add(caption);
            }

            return col;
        }

        private FrameworkElement BuildYAxis(Color text)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 6, 0) };

            // 0% sits at the axis origin: the bottom normally, the top when flipped.
            grid.Children.Add(AxisTick(_topDown ? "0%" : "100%", text, VerticalAlignment.Top));
            grid.Children.Add(AxisTick("50%", text, VerticalAlignment.Center));
            grid.Children.Add(AxisTick(_topDown ? "100%" : "0%", text, VerticalAlignment.Bottom));
            return grid;
        }

        private FrameworkElement AxisTick(string label, Color text, VerticalAlignment v)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = label,
                FontSize = 10,
                Opacity = 0.7,
                Foreground = new SolidColorBrush(text),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = v,
            }, _glow);

        private static FrameworkElement BuildGridlines(Color text)
        {
            var grid = new Grid();
            grid.Children.Add(Gridline(text, VerticalAlignment.Top));
            grid.Children.Add(Gridline(text, VerticalAlignment.Center));
            grid.Children.Add(Gridline(text, VerticalAlignment.Bottom));
            return grid;
        }

        private static Rectangle Gridline(Color text, VerticalAlignment v) => new()
        {
            Height = 1,
            Fill = new SolidColorBrush(Color.FromArgb(38, text.R, text.G, text.B)), // ~15% opacity
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = v,
        };

        private FrameworkElement BuildCaption(string label, Color text, double fontSize, bool rotate)
            => GlowEffect.Wrap(() =>
            {
                var tb = new TextBlock
                {
                    Text = label,
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(text),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = _topDown ? VerticalAlignment.Top : VerticalAlignment.Bottom,
                    TextAlignment = TextAlignment.Center,
                };
                if (rotate)
                    tb.LayoutTransform = new RotateTransform(-90);   // 90° counter-clockwise
                return tb;
            }, _glow);

        private FrameworkElement BuildXLabel(Bar bar, Color text)
            => GlowEffect.Wrap(() =>
            {
                var tb = new TextBlock
                {
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(text),
                    FontSize = 11,
                };
                tb.Inlines.Add(new Run(bar.Letter) { FontWeight = FontWeights.SemiBold });
                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(new Run($"{Math.Clamp(bar.UsedFraction, 0, 1) * 100:0}%") { FontSize = 10 });
                return tb;
            }, _glow);
    }
}
