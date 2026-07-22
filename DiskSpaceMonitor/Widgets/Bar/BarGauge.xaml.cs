using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        // Fixed design space; the Viewbox scales it (and every label) to the actual window size.
        private const double DesignHeight = 200;
        private const double SlotWidth = 60;    // design width allotted to each bar column
        private const double HeaderRoom = 16;   // space above the 100% line for used-space captions

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
                return;
            }

            // Render at a fixed design size so the star-sized bar rows have a height to divide;
            // the Viewbox scales this uniformly to the window.
            Root.Height = DesignHeight;
            Root.Width = 40 + bars.Count * SlotWidth;

            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // y-axis
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // total-space header
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // plot
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // x-axis labels

            byte trackAlpha = (byte)(Math.Clamp(trackOpacity, 0, 1) * 255);
            var trackBrush = new SolidColorBrush(Color.FromArgb(trackAlpha, track.R, track.G, track.B));

            // Reserve headroom for used-space captions (which ride the top of each fill) inside the
            // header row, so they don't clip at high usage without pushing the graph top down.
            bool anyUsed = bars.Any(b => !string.IsNullOrEmpty(b.UsedLabel));
            Root.RowDefinitions[0].MinHeight = anyUsed ? HeaderRoom : 0;

            // Total-space header, sitting just above the graph top, aligned above each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = new UniformGrid
                {
                    Rows = 1,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 2),
                };
                foreach (var b in bars)
                    totals.Children.Add(BuildCaption(b.TotalLabel, text));
                Root.Children.Add(Place(totals, row: 0, col: 1));
            }

            // Y-axis (100 / 50 / 0) aligned to the plot area.
            var yaxis = BuildYAxis(text);
            yaxis.Margin = new Thickness(0, 0, 6, 0);
            Root.Children.Add(Place(yaxis, row: 1, col: 0));

            // Plot: faint gridlines behind, then a bar per drive.
            var plot = new Grid();
            plot.Children.Add(BuildGridlines(text));

            var barRow = new UniformGrid { Rows = 1 };
            foreach (var b in bars)
                barRow.Children.Add(BuildBar(b, trackBrush, trackAlpha, text, barWidth));
            plot.Children.Add(barRow);

            Root.Children.Add(Place(plot, row: 1, col: 1));

            // X-axis labels (drive letter + used %), aligned under the bars.
            var labels = new UniformGrid { Rows = 1, Margin = new Thickness(0, 4, 0, 0) };
            foreach (var b in bars)
                labels.Children.Add(BuildXLabel(b, text));
            Root.Children.Add(Place(labels, row: 2, col: 1));
        }

        private static FrameworkElement Place(FrameworkElement e, int row, int col)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            return e;
        }

        private static FrameworkElement BuildBar(Bar bar, Brush trackBrush, byte trackAlpha, Color text, double barWidth)
        {
            double used = Math.Clamp(bar.UsedFraction, 0, 1);
            double f = Math.Clamp(barWidth, 0.05, 1);   // bar width as a fraction of its slot
            double side = (1 - f) / 2;

            var col = new Grid();
            col.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(side, GridUnitType.Star) });
            col.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(f, GridUnitType.Star) });
            col.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(side, GridUnitType.Star) });
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
                Grid.SetColumn(track, 1);
                Grid.SetRowSpan(track, 2);
                col.Children.Add(track);
            }

            var fill = new Rectangle
            {
                Fill = new SolidColorBrush(bar.Fill),
                RadiusX = CornerRadius,
                RadiusY = CornerRadius,
            };
            Grid.SetColumn(fill, 1);
            Grid.SetRow(fill, 1);
            col.Children.Add(fill);

            // Used-space caption riding just above the fill (in the unused row, bottom-aligned; it
            // overflows up into the headroom at high usage rather than clipping). It spans the whole
            // slot and is centred, so a narrow bar never clips the text.
            if (!string.IsNullOrEmpty(bar.UsedLabel))
            {
                var caption = new TextBlock
                {
                    Text = bar.UsedLabel,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(text),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 2),
                    TextAlignment = TextAlignment.Center,
                };
                Grid.SetRow(caption, 0);
                Grid.SetColumnSpan(caption, 3);
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

        private static TextBlock BuildCaption(string label, Color text) => new()
        {
            Text = label,
            FontSize = 10,
            Foreground = new SolidColorBrush(text),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        };

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
