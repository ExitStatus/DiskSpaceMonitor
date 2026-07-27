using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Box
{
    /// <summary>
    /// Draws one drive as a rounded panel holding three rows: the drive and its total size, the
    /// used space, and a bar filled to the used %. The panel fills the window in both directions
    /// and its rows are proportional, so the bar keeps its share of the height however the box is
    /// dragged; only the text is scaled, and only when the box has grown both ways.
    /// </summary>
    public partial class BoxGauge : UserControl
    {
        // The size at which the text below renders at its literal font size. The box scales its
        // text by whichever axis is proportionally smaller against this reference, so stretching in
        // one direction alone leaves the text alone. The reference is the height a box opens at and
        // a little under its width, so the height is what binds at the default size: widening a
        // fresh box lengthens its bar and leaves the labels exactly where they were.
        private const double RefWidth = 220;
        private const double RefHeight = 110;

        private const double TitleFont = 14;   // "C: 931 GB"
        private const double UsedFont = 12;    // "Used: 712 GB"
        private const double Inset = 8;        // space between the panel's outline and its content

        // Smallest and largest text scale, so a box squeezed to the minimum still reads and a
        // full-screen one doesn't turn into a poster.
        private const double MinScale = 0.4;
        private const double MaxScale = 6;

        // The last render's inputs and the scale they were drawn at, so a resize can redraw them at
        // the new text scale. Null until the first render.
        private RenderArgs? _last;
        private double _scale = 1;

        // App-wide font and size bounds. Defaults until the host pushes the user's choice.
        private WidgetTypography _type = WidgetTypography.Default;

        /// <summary>Everything <see cref="Render"/> was given, kept so a resize can replay it.
        /// The colours arrive with their opacity already blended into their alpha.</summary>
        private readonly record struct RenderArgs(string Title, string Used, double UsedFraction,
            Color Fill, Color Text, Color Background, Color Track, double BarHeight,
            BarSkin BoxSkin, BarSkin BarSkin, Effect? Glow);

        public BoxGauge()
        {
            InitializeComponent();

            // Only the text scale depends on the window size — the panel and its rows stretch on
            // their own — so a resize only needs a redraw when the scale has actually moved.
            SizeChanged += (_, _) =>
            {
                if (_last is not null && Math.Abs(CurrentScale - _scale) > 0.005)
                    Build();
            };
        }

        /// <summary>Text scale for the current window: the smaller of the two axis ratios, so text
        /// only grows when the box has grown in both directions.</summary>
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

        internal void Render(string title, string used, double usedFraction, Color fill, Color text,
            Color background, Color track, double barHeight, BarSkin boxSkin, BarSkin barSkin,
            Effect? glow)
        {
            _last = new RenderArgs(title, used, usedFraction, fill, text, background, track,
                barHeight, boxSkin, barSkin, glow);
            Build();
        }

        private void Build()
        {
            if (_last is not { } args)
                return;

            _scale = CurrentScale;
            var boxSkin = Scaled(args.BoxSkin);
            var barSkin = Scaled(args.BarSkin);

            // Keep the content clear of the outline: a bevel is drawn over the fill's edges, so
            // text run right to the border would sit underneath it.
            double outline = boxSkin.Style == BarStyle.Plain ? 0 : boxSkin.Size;
            var content = new Grid { Margin = new Thickness(outline + Inset * _scale) };

            // Three proportional rows: the bar takes exactly its configured share of the height and
            // the two text rows split what is left, so nothing needs recomputing on a stretch.
            double bar = Math.Clamp(args.BarHeight, 0.05, 0.5);
            double line = (1 - bar) / 2;
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(line, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(line, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(bar, GridUnitType.Star) });

            content.Children.Add(Place(BuildText(args.Title, TitleFont, FontWeights.SemiBold, args), 0));
            content.Children.Add(Place(BuildText(args.Used, UsedFont, FontWeights.Normal, args), 1));
            content.Children.Add(Place(BuildBar(args, barSkin), 2));

            Root.Children.Clear();
            Root.Children.Add(BarGraphParts.BuildFill(args.Background, boxSkin, content));
        }

        // The outline width and corner rounding are given at the reference size; scaling them with
        // the text keeps their weight relative to the box.
        private BarSkin Scaled(BarSkin skin) => skin with
        {
            Size = skin.Size * _scale,
            Corner = BarGraphParts.Corner(skin.Corner, _scale),
        };

        private static FrameworkElement Place(FrameworkElement e, int row)
        {
            Grid.SetRow(e, row);
            return e;
        }

        // Centred in the box both ways: the two lines sit on its centre line above the bar however
        // wide it is dragged.
        private FrameworkElement BuildText(string text, double baseFont, FontWeight weight, RenderArgs args)
            => GlowEffect.Wrap(() => new TextBlock
            {
                Text = text,
                FontSize = BarGraphParts.Font(baseFont, _scale, _type),
                FontWeight = weight,
                Foreground = new SolidColorBrush(args.Text),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }, args.Glow);

        // The fill takes the used share of the width and the unused remainder takes the rest, with
        // the faint track running the whole way behind both.
        private static FrameworkElement BuildBar(RenderArgs args, BarSkin skin)
        {
            double used = Math.Clamp(args.UsedFraction, 0, 1);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(used, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - used, GridUnitType.Star) });

            if (args.Track.A > 0)
            {
                var track = new Rectangle
                {
                    Fill = new SolidColorBrush(args.Track),
                    RadiusX = skin.Corner,
                    RadiusY = skin.Corner,
                };
                Grid.SetColumnSpan(track, 2);
                grid.Children.Add(track);
            }

            grid.Children.Add(BarGraphParts.BuildFill(args.Fill, skin));
            return grid;
        }
    }
}
