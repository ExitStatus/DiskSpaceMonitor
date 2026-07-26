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
    /// 0% is at the right and the bars grow left. This is the vertical graph transposed, and sizes
    /// the same way: the bars fill the window in both directions, so widening it lengthens them and
    /// heightening it thickens them.
    /// </summary>
    public partial class HorizontalBarGauge : UserControl
    {
        // The size at which the text below renders at its literal font size. The graph scales its
        // text by whichever axis is proportionally smaller against this reference, so stretching in
        // one direction alone resizes the bars and leaves the text alone; only growing in both
        // directions makes the text bigger.
        private const double RefWidth = 320;
        private const double RefHeight = 200;

        private const double AxisFont = 10;            // value-axis ticks
        private const double AxisGap = 4;              // space between the plot and the value axis
        private const double LetterFont = 11;          // the drive letter
        private const double CaptionBaseFont = 10;     // default size for the used/total captions
        private const double LineHeightFactor = 1.4;   // text line height as a multiple of the font size

        // Smallest and largest text scale, so a widget squeezed to the minimum still reads and a
        // full-screen one doesn't turn into a poster.
        private const double MinScale = 0.4;
        private const double MaxScale = 6;

        // The outer-glow effect for this render, or null when off. Each text element is built through
        // GlowEffect.Wrap so the glow sits behind crisp glyphs rather than blurring the font.
        private Effect? _glow;

        // True when the axis is flipped (0% at the right, bars growing left). Held for this render so
        // the builders below can mirror themselves without every one taking the orientation.
        private bool _rightToLeft;

        // How this render outlines each bar's fill.
        private BarSkin _skin;

        // Fraction of each slot given over to the gap, held for this render so the bar builder can
        // inset itself without every helper taking it.
        private double _gap;

        // The last render's inputs and the scale they were drawn at, so a resize can redraw them at
        // the new text scale. Null until the first render.
        private RenderArgs? _last;
        private double _scale = 1;

        // App-wide font and size bounds. Defaults until the host pushes the user's choice.
        private WidgetTypography _type = WidgetTypography.Default;

        /// <summary>Everything <see cref="Render"/> was given, kept so a resize can replay it.</summary>
        private readonly record struct RenderArgs(IReadOnlyList<Bar> Bars, Color Track, double TrackOpacity,
            Color Text, double Gap, BarOrientation Orientation, BarSkin Skin, Effect? Glow);

        public HorizontalBarGauge()
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
            _rightToLeft = args.Orientation == BarOrientation.RightToLeft;
            _scale = CurrentScale;
            _skin = args.Skin with
            {
                Size = args.Skin.Size * _scale,
                Corner = BarGraphParts.Corner(args.Skin.Corner, _scale),
            };

            Root.Children.Clear();
            Root.ColumnDefinitions.Clear();
            Root.RowDefinitions.Clear();

            var bars = args.Bars;
            if (bars.Count == 0)
                return;

            double axisFont = BarGraphParts.Font(AxisFont, _scale, _type);
            double axisGap = AxisGap * _scale;

            // Both annotation columns are Auto; which one holds the totals and which holds the drive
            // labels swaps with the orientation, so each stays on its own end of the axis. The plot
            // takes everything left over, so the bars always span the width the user gave the widget.
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                  // annotations left
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // plot
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                  // annotations right
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });  // plot (bars)
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                       // value axis

            // The totals sit at the 100% end and the drive labels at the 0% end, so left-to-right
            // puts labels on the left and totals on the right, and right-to-left swaps them.
            int totalsCol = _rightToLeft ? 0 : 2;
            int labelsCol = _rightToLeft ? 2 : 0;

            byte trackAlpha = (byte)(Math.Clamp(args.TrackOpacity, 0, 1) * 255);
            var trackBrush = new SolidColorBrush(Color.FromArgb(trackAlpha, args.Track.R, args.Track.G, args.Track.B));

            // Each drive gets an equal share of the plot's height; the gap setting says how much of
            // that share is space rather than bar. The bars therefore always fill the window, and
            // heightening it thickens them.
            int n = bars.Count;
            double gap = _gap = Math.Clamp(args.Gap, 0, 0.5);

            // The slot one bar sits in, so the captions can be sized to fit it. The axis row is
            // Auto-height, so estimate it from a tick — the same text it will lay out.
            double axisRow = BarGraphParts.Measure("100%", axisFont).Height + axisGap;
            double slot = Math.Max(0, ActualHeight - axisRow) / n;

            // One uniform font size for the used/total captions. Unlike the vertical graph — where a
            // caption is constrained by the bar's width and rotates when it can't fit — a horizontal
            // bar has the whole axis to write along, so the limit is how much room it has vertically.
            // That is the whole slot, not just the bar: a caption sits beside its bar, so it may use
            // the gap around it and only shrinks once the slot itself gets tight. Never rotate —
            // rotated text would need even more of the same scarce dimension. It stops shrinking at
            // the readable floor and crowds its slot instead of vanishing.
            double captionFont = Math.Clamp(slot / LineHeightFactor,
                _type.MinSize, BarGraphParts.Font(CaptionBaseFont, _scale, _type));

            // Value axis (0 / 50 / 100) under the plot, aligned to the plot's edges.
            var axis = BuildValueAxis(args.Text, axisFont);
            axis.Margin = new Thickness(0, axisGap, 0, 0);
            Root.Children.Add(Place(axis, row: 1, col: 1));

            // Plot: faint gridlines behind (full height), then the bars spanning it.
            var plot = new Grid();
            plot.Children.Add(BuildGridlines(args.Text));

            var barColumn = BuildSlotColumn(n, gap,
                i => BuildBar(bars[i], trackBrush, trackAlpha, args.Text, captionFont));
            plot.Children.Add(barColumn);

            Root.Children.Add(Place(plot, row: 0, col: 1));

            // Total-space captions, hugging the 100% end of the plot, aligned with each bar.
            if (bars.Any(b => !string.IsNullOrEmpty(b.TotalLabel)))
            {
                var totals = BuildSlotColumn(n, gap,
                    i => BuildCaption(bars[i].TotalLabel, args.Text, captionFont, HorizontalAlignment.Center));
                totals.Margin = _rightToLeft
                    ? new Thickness(0, 0, 4 * _scale, 0)
                    : new Thickness(4 * _scale, 0, 0, 0);
                Root.Children.Add(Place(totals, row: 0, totalsCol));
            }

            // Drive labels (letter + used %), aligned with the bars at the 0% end of the axis.
            var labels = BuildSlotColumn(n, gap, i => BuildDriveLabel(bars[i], args.Text));
            labels.Margin = _rightToLeft
                ? new Thickness(6 * _scale, 0, 0, 0)
                : new Thickness(0, 0, 6 * _scale, 0);
            Root.Children.Add(Place(labels, row: 0, labelsCol));
        }

        private static FrameworkElement Place(FrameworkElement e, int row, int col)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            return e;
        }

        // One equal, proportional slot per drive spanning the plot's height, with the bar taking all
        // of its slot bar the gap and half a gap sitting either side of it — so the spacing stays
        // even including at the two ends, and everything grows with the window. Every column of the
        // chart (bars, totals and drive labels) is built on this same grid, so they stay aligned.
        // Only the bar visuals are inset to the bar itself: captions and labels beside a thin bar
        // keep the full slot and never clip.
        private static Grid BuildSlotColumn(int count, double gap, Func<int, FrameworkElement> makeChild)
        {
            var grid = new Grid();
            for (int i = 0; i < count; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < count; i++)
            {
                var child = makeChild(i);
                Grid.SetRow(child, i);
                grid.Children.Add(child);
            }

            return grid;
        }

        // The bar's own band within its slot: the gap is split above and below it, so neighbouring
        // bars are a whole gap apart and the two outer edges get half of one.
        private static Grid BarBand(double gap, FrameworkElement content)
        {
            var band = new Grid();
            band.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap / 2, GridUnitType.Star) });
            band.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - gap, GridUnitType.Star) });
            band.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap / 2, GridUnitType.Star) });

            Grid.SetRow(content, 1);
            band.Children.Add(content);
            return band;
        }

        private FrameworkElement BuildBar(Bar bar, Brush trackBrush, byte trackAlpha, Color text,
            double captionFont)
        {
            double used = Math.Clamp(bar.UsedFraction, 0, 1);

            // The fill takes the column at the 0% end (left, or right when flipped) and the unused
            // remainder takes the other, so the bar grows away from the axis origin either way.
            int fillCol = _rightToLeft ? 1 : 0;
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? 1 - used : used, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? used : 1 - used, GridUnitType.Star) });

            if (trackAlpha > 0)
            {
                var track = new Rectangle
                {
                    Fill = trackBrush,
                    RadiusX = _skin.Corner,
                    RadiusY = _skin.Corner,
                };
                Grid.SetColumnSpan(track, 2);
                row.Children.Add(track);
            }

            var fill = BarGraphParts.BuildFill(bar.Fill, _skin);
            Grid.SetColumn(fill, fillCol);
            row.Children.Add(fill);

            // Used-space caption, riding just past the fill's leading edge in the unused column so it
            // stays legible against the faint track rather than the fill. It sits outside the bar's
            // band so a thin bar doesn't squash it.
            var cell = new Grid();
            cell.Children.Add(BarBand(_gap, row));

            if (!string.IsNullOrEmpty(bar.UsedLabel))
            {
                var split = new Grid();
                split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? 1 - used : used, GridUnitType.Star) });
                split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rightToLeft ? used : 1 - used, GridUnitType.Star) });

                var caption = BuildCaption(bar.UsedLabel, text, captionFont,
                    _rightToLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                caption.Margin = _rightToLeft
                    ? new Thickness(0, 0, 3 * _scale, 0)
                    : new Thickness(3 * _scale, 0, 0, 0);
                Grid.SetColumn(caption, _rightToLeft ? 0 : 1);
                split.Children.Add(caption);
                cell.Children.Add(split);
            }

            return cell;
        }

        // 0 / 50 / 100 across the bottom, anchored to the plot's edges so the end labels sit inside
        // the plot rather than overhanging it. 0% sits at the axis origin: left, or right when flipped.
        private FrameworkElement BuildValueAxis(Color text, double fontSize)
        {
            var grid = new Grid();
            grid.Children.Add(AxisTick(_rightToLeft ? "100%" : "0%", text, fontSize, HorizontalAlignment.Left));
            grid.Children.Add(AxisTick("50%", text, fontSize, HorizontalAlignment.Center));
            grid.Children.Add(AxisTick(_rightToLeft ? "0%" : "100%", text, fontSize, HorizontalAlignment.Right));
            return grid;
        }

        private FrameworkElement AxisTick(string label, Color text, double fontSize, HorizontalAlignment h)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = label,
                FontSize = fontSize,
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

        // The drive letter and its used %, on one line so the label stays no taller than its slot.
        private FrameworkElement BuildDriveLabel(Bar bar, Color text)
            => GlowEffect.Wrap(() =>
            {
                var tb = new TextBlock
                {
                    HorizontalAlignment = _rightToLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(text),
                    FontSize = BarGraphParts.Font(LetterFont, _scale, _type),
                };
                tb.Inlines.Add(new Run(bar.Letter) { FontWeight = FontWeights.SemiBold });
                tb.Inlines.Add(new Run($" {Math.Clamp(bar.UsedFraction, 0, 1) * 100:0}%")
                {
                    FontSize = BarGraphParts.Font(AxisFont, _scale, _type),
                });
                return tb;
            }, _glow);
    }
}
