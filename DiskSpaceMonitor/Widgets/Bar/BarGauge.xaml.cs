using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiskSpaceMonitor.Widgets.Bar
{
    /// <summary>
    /// One bar to draw: its axis label, used fraction [0,1], fill (status) colour, and an optional
    /// used-space caption shown on top of the bar (empty to hide it).
    /// </summary>
    internal readonly record struct Bar(string Letter, double UsedFraction, Color Fill,
        string UsedLabel, string TotalLabel);

    /// <summary>
    /// Draws a vertical bar per drive: the y-axis runs 0–100% (used space) and each bar fills from
    /// the bottom to its used %. The unused part above the fill is a faint track. Built from
    /// proportional Grid rows so the bars reflow smoothly when the window is resized.
    /// </summary>
    public partial class BarGauge : UserControl
    {
        private const double CornerRadius = 3;

        // Fixed design height; the width sizes to content so the graph hugs the bars. The Viewbox
        // scales it (and every label) to the actual window size.
        private const double DesignHeight = 200;
        private const double MaxBarWidth = 46;  // bar thickness at 100% width
        private const double MaxGap = 14;       // gap between bars at 100% width (both scale with width)
        private const double HeaderRoom = 16;   // space above the 100% line for used-space captions
        private const double EdgePad = 14;      // room on the right for outer label overflow
        private const double CaptionBaseFont = 10;     // default size for the used/total captions
        private const double CaptionMinFont = 6;       // shrink down to here before rotating
        private const double CaptionRotatedFont = 8;   // size used once rotated

        private double _aspect = 1;

        /// <summary>Content width ÷ height after the last render; the window fits itself to this.</summary>
        internal double DesignAspect => _aspect;

        public BarGauge()
        {
            InitializeComponent();
        }

        internal void Render(IReadOnlyList<Bar> bars, Color track, double trackOpacity, Color text, double barWidth)
        {
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
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // total-space header
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // plot
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // x-axis labels

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
                double maxWidth = captionTexts.Max(c => MeasureWidth(c, CaptionBaseFont));
                double fitFont = maxWidth > 0 ? CaptionBaseFont * barThickness / maxWidth : CaptionBaseFont;
                fitFont = Math.Min(CaptionBaseFont, fitFont);
                rotateCaptions = fitFont < CaptionMinFont;
                captionFont = rotateCaptions ? CaptionRotatedFont : fitFont;
            }

            // Reserve headroom above the 100% line for the used-space caption (which rides the top of
            // each fill). Not needed when the captions are rotated — those read up inside the bar.
            bool anyUsed = bars.Any(b => !string.IsNullOrEmpty(b.UsedLabel));
            Root.RowDefinitions[0].MinHeight = (anyUsed && !rotateCaptions) ? HeaderRoom : 0;

            // Total-space header, sitting just above the graph top, aligned above each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = BuildAlignedRow(n, barThickness, gap, i => BuildCaption(bars[i].TotalLabel, text, captionFont, rotateCaptions));
                totals.VerticalAlignment = VerticalAlignment.Bottom;
                totals.Margin = new Thickness(0, 0, 0, 2);
                Root.Children.Add(Place(totals, row: 0, col: 1));
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

            // X-axis labels (drive letter + used %), aligned under the bars.
            var labels = BuildAlignedRow(n, barThickness, gap, i => BuildXLabel(bars[i], text));
            labels.Margin = new Thickness(0, 4, 0, 0);
            Root.Children.Add(Place(labels, row: 2, col: 1));

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

        private static FrameworkElement BuildBar(Bar bar, Brush trackBrush, byte trackAlpha, Color text,
            double captionFont, bool rotate)
        {
            double used = Math.Clamp(bar.UsedFraction, 0, 1);

            var col = new Grid();
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - used, GridUnitType.Star) });
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(used, GridUnitType.Star) });

            if (trackAlpha > 0)
            {
                var track = new Rectangle
                {
                    Fill = trackBrush,
                    RadiusX = CornerRadius,
                    RadiusY = CornerRadius,
                };
                Grid.SetRowSpan(track, 2);
                col.Children.Add(track);
            }

            var fill = new Rectangle
            {
                Fill = new SolidColorBrush(bar.Fill),
                RadiusX = CornerRadius,
                RadiusY = CornerRadius,
            };
            Grid.SetRow(fill, 1);
            col.Children.Add(fill);

            // Used-space caption. Horizontal: rides just above the fill (unused row, bottom-aligned),
            // overflowing up into the headroom at high usage. Rotated: reads up from the bar's bottom.
            if (!string.IsNullOrEmpty(bar.UsedLabel))
            {
                var caption = BuildCaption(bar.UsedLabel, text, captionFont, rotate);
                caption.Margin = new Thickness(0, 0, 0, rotate ? 6 : 2);   // rotated: nudged up 4px
                Grid.SetRow(caption, 0);
                if (rotate)
                    Grid.SetRowSpan(caption, 2);
                col.Children.Add(caption);
            }

            return col;
        }

        private static FrameworkElement BuildYAxis(Color text)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 6, 0) };

            grid.Children.Add(AxisTick("100%", text, VerticalAlignment.Top));
            grid.Children.Add(AxisTick("50%", text, VerticalAlignment.Center));
            grid.Children.Add(AxisTick("0%", text, VerticalAlignment.Bottom));
            return grid;
        }

        private static TextBlock AxisTick(string label, Color text, VerticalAlignment v) => new()
        {
            Text = label,
            FontSize = 10,
            Opacity = 0.7,
            Foreground = new SolidColorBrush(text),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = v,
        };

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

        private static TextBlock BuildCaption(string label, Color text, double fontSize, bool rotate)
        {
            var tb = new TextBlock
            {
                Text = label,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(text),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Center,
            };
            if (rotate)
                tb.LayoutTransform = new RotateTransform(-90);   // 90° counter-clockwise
            return tb;
        }

        // Natural width of a caption at a given font size, used to pick a size that fits the bar.
        private static double MeasureWidth(string text, double fontSize)
        {
            var tb = new TextBlock { Text = text, FontSize = fontSize };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return tb.DesiredSize.Width;
        }

        private static FrameworkElement BuildXLabel(Bar bar, Color text)
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
        }
    }
}
