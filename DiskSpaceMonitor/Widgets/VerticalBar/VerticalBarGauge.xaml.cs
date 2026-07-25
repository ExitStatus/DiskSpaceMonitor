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
    /// hang down. Built from proportional Grid cells so the bars fill the window in both
    /// directions: widen it and the bars widen, heighten it and they lengthen.
    /// </summary>
    public partial class VerticalBarGauge : UserControl
    {
        // The size at which the text below renders at its literal font size. The graph scales its
        // text by whichever axis is proportionally smaller against this reference, so stretching in
        // one direction alone resizes the bars and leaves the text alone; only growing in both
        // directions makes the text bigger.
        private const double RefWidth = 220;
        private const double RefHeight = 200;

        private const double EdgePad = 14;             // room on the right for outer label overflow
        private const double AxisFont = 10;            // y-axis ticks
        private const double AxisGap = 6;              // space between the y-axis and the plot
        private const double LetterFont = 11;          // the drive letter
        private const double CaptionBaseFont = 10;     // default size for the used/total captions
        private const double CaptionRotatedFont = 8;   // size used once rotated

        // Smallest and largest text scale, so a widget squeezed to the minimum still reads and a
        // full-screen one doesn't turn into a poster.
        private const double MinScale = 0.4;
        private const double MaxScale = 6;

        // The outer-glow effect for this render, or null when off. Each text element is built through
        // GlowEffect.Wrap so the glow sits behind crisp glyphs rather than blurring the font.
        private Effect? _glow;

        // True when the axis is flipped (0% at the top, bars hanging down). Held for this render so
        // the builders below can mirror themselves without every one taking the orientation.
        private bool _topDown;

        // How this render outlines each bar's fill.
        private BarSkin _skin;

        // The last render's inputs and the scale they were drawn at, so a resize can redraw them at
        // the new text scale. Null until the first render.
        private RenderArgs? _last;
        private double _scale = 1;

        // App-wide font and size bounds. Defaults until the host pushes the user's choice.
        private WidgetTypography _type = WidgetTypography.Default;

        /// <summary>Everything <see cref="Render"/> was given, kept so a resize can replay it.</summary>
        private readonly record struct RenderArgs(IReadOnlyList<Bar> Bars, Color Track, double TrackOpacity,
            Color Text, double Gap, BarOrientation Orientation, BarSkin Skin, Effect? Glow);

        public VerticalBarGauge()
        {
            InitializeComponent();

            // Only the text scale depends on the window size — the bars stretch on their own — so a
            // resize only needs a redraw when the scale has actually moved.
            SizeChanged += (_, _) =>
            {
                if (_last is not null && Math.Abs(CurrentScale - _scale) > 0.005)
                    Build();
            };
        }

        /// <summary>Text scale for the current window: the smaller of the two axis ratios, so text
        /// only grows when the graph has grown in both directions.</summary>
        private double CurrentScale => Math.Clamp(
            Math.Min(ActualWidth / RefWidth, ActualHeight / RefHeight), MinScale, MaxScale);

        /// <summary>Apply the app-wide font and text size bounds. The family goes on the control, so
        /// every piece of text below inherits it; the bounds are applied as each one is sized.</summary>
        internal void ApplyTypography(WidgetTypography typography)
        {
            _type = typography;
            FontFamily = typography.Family;
            Build();
        }

        internal void Render(IReadOnlyList<Bar> bars, Color track, double trackOpacity, Color text,
            double gap, BarOrientation orientation, BarSkin skin, Effect? glow)
        {
            _last = new RenderArgs(bars, track, trackOpacity, text, gap, orientation, skin, glow);
            Build();
        }

        private void Build()
        {
            if (_last is not { } args)
                return;

            _glow = args.Glow;
            _topDown = args.Orientation == BarOrientation.TopDown;
            _scale = CurrentScale;
            _skin = args.Skin with
            {
                Size = args.Skin.Size * _scale,
                Corner = BarGraphParts.Corner(_scale),
            };

            Root.Children.Clear();
            Root.ColumnDefinitions.Clear();
            Root.RowDefinitions.Clear();

            var bars = args.Bars;
            if (bars.Count == 0)
                return;

            double axisFont = BarGraphParts.Font(AxisFont, _scale, _type);
            double edgePad = EdgePad * _scale;
            double axisGap = AxisGap * _scale;

            // The y-axis column is Auto (its ticks), the plot takes everything left over, so the
            // bars always span the width the user gave the widget.
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                  // y-axis
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // plot
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(edgePad) });          // label overflow
            // Both annotation rows are Auto; which one holds the totals and which holds the drive
            // labels swaps with the orientation, so each stays on its own end of the axis.
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // annotations above
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // plot
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // annotations below

            // The totals sit at the 100% end and the drive labels at the 0% end, so bottom-up puts
            // totals on top and labels underneath, and top-down swaps them.
            int totalsRow = _topDown ? 2 : 0;
            int labelsRow = _topDown ? 0 : 2;

            byte trackAlpha = (byte)(Math.Clamp(args.TrackOpacity, 0, 1) * 255);
            var trackBrush = new SolidColorBrush(Color.FromArgb(trackAlpha, args.Track.R, args.Track.G, args.Track.B));

            // Each drive gets an equal share of the plot; the gap setting says how much of that share
            // is space rather than bar. The bars therefore always fill the window, and widening it
            // widens them.
            int n = bars.Count;
            double gap = Math.Clamp(args.Gap, 0, 0.5);

            // What one bar will actually be, so the captions can be sized to fit it. The y-axis is
            // Auto-width, so estimate it from the widest tick — the same text it will lay out.
            double yAxisWidth = BarGraphParts.Measure("100%", axisFont).Width + axisGap;
            double plotWidth = Math.Max(0, ActualWidth - yAxisWidth - edgePad);
            double barThickness = plotWidth * (1 - gap) / n;

            // One uniform font size for the used/total captions so they fit within the bar width. If
            // fitting the width would take it below the readable floor, rotate the text 90° CCW
            // instead — reading up the bar, it has the bar's whole length to use rather than its
            // width. Every caption uses the one size.
            var captionTexts = new List<string>();
            foreach (var b in bars)
            {
                if (!string.IsNullOrEmpty(b.UsedLabel)) captionTexts.Add(b.UsedLabel);
                if (!string.IsNullOrEmpty(b.TotalLabel)) captionTexts.Add(b.TotalLabel);
            }

            double baseFont = BarGraphParts.Font(CaptionBaseFont, _scale, _type);
            double captionFont = baseFont;
            bool rotateCaptions = false;
            if (captionTexts.Count > 0)
            {
                double maxWidth = captionTexts.Max(c => BarGraphParts.Measure(c, baseFont).Width);
                double fitFont = maxWidth > 0 ? baseFont * barThickness / maxWidth : baseFont;
                fitFont = Math.Min(baseFont, fitFont);
                rotateCaptions = fitFont < _type.MinSize;
                captionFont = rotateCaptions ? BarGraphParts.Font(CaptionRotatedFont, _scale, _type) : fitFont;
            }

            // Total-space header, hugging the 100% end of the plot, aligned with each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = BuildAlignedRow(n, gap, i => BuildCaption(bars[i].TotalLabel, args.Text, captionFont, rotateCaptions));
                totals.VerticalAlignment = _topDown ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                totals.Margin = _topDown ? new Thickness(0, 2 * _scale, 0, 0) : new Thickness(0, 0, 0, 2 * _scale);
                Root.Children.Add(Place(totals, totalsRow, col: 1));
            }

            // Y-axis (100 / 50 / 0) aligned to the plot area.
            var yaxis = BuildYAxis(args.Text, axisFont);
            yaxis.Margin = new Thickness(0, 0, axisGap, 0);
            Root.Children.Add(Place(yaxis, row: 1, col: 0));

            // Plot: faint gridlines behind (full width), then the bars spanning it.
            var plot = new Grid();
            plot.Children.Add(BuildGridlines(args.Text));

            var barRow = BuildAlignedRow(n, gap, i => BuildBar(bars[i], trackBrush, trackAlpha, args.Text, captionFont, rotateCaptions));
            barRow.VerticalAlignment = VerticalAlignment.Stretch;
            plot.Children.Add(barRow);

            Root.Children.Add(Place(plot, row: 1, col: 1));

            // Drive labels (letter + used %), aligned with the bars at the 0% end of the axis.
            var labels = BuildAlignedRow(n, gap, i => BuildXLabel(bars[i], args.Text));
            labels.Margin = _topDown ? new Thickness(0, 0, 0, 4 * _scale) : new Thickness(0, 4 * _scale, 0, 0);
            Root.Children.Add(Place(labels, labelsRow, col: 1));
        }

        private static FrameworkElement Place(FrameworkElement e, int row, int col)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            return e;
        }

        // One equal, proportional slot per drive spanning the plot, with the bar taking all of its
        // slot but the gap and half a gap sitting either side of it — so the spacing stays even
        // including at the two ends, and everything grows with the window. Sharing this layout keeps
        // the bars, their captions and their labels aligned.
        private static Grid BuildAlignedRow(int count, double gap, Func<int, FrameworkElement> makeChild)
        {
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            for (int i = 0; i < count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < count; i++)
            {
                var slot = new Grid();
                slot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap / 2, GridUnitType.Star) });
                slot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - gap, GridUnitType.Star) });
                slot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap / 2, GridUnitType.Star) });

                var child = makeChild(i);
                Grid.SetColumn(child, 1);
                slot.Children.Add(child);

                Grid.SetColumn(slot, i);
                grid.Children.Add(slot);
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
                    RadiusX = _skin.Corner,
                    RadiusY = _skin.Corner,
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
                double nudge = (rotate ? 6 : 2) * _scale;   // rotated: nudged a further 4px clear of the end
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

        private FrameworkElement BuildYAxis(Color text, double fontSize)
        {
            var grid = new Grid();

            // 0% sits at the axis origin: the bottom normally, the top when flipped.
            grid.Children.Add(AxisTick(_topDown ? "0%" : "100%", text, fontSize, VerticalAlignment.Top));
            grid.Children.Add(AxisTick("50%", text, fontSize, VerticalAlignment.Center));
            grid.Children.Add(AxisTick(_topDown ? "100%" : "0%", text, fontSize, VerticalAlignment.Bottom));
            return grid;
        }

        private FrameworkElement AxisTick(string label, Color text, double fontSize, VerticalAlignment v)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = label,
                FontSize = fontSize,
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
                    FontSize = BarGraphParts.Font(LetterFont, _scale, _type),
                };
                tb.Inlines.Add(new Run(bar.Letter) { FontWeight = FontWeights.SemiBold });
                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(new Run($"{Math.Clamp(bar.UsedFraction, 0, 1) * 100:0}%")
                {
                    FontSize = BarGraphParts.Font(AxisFont, _scale, _type),
                });
                return tb;
            }, _glow);
    }
}
