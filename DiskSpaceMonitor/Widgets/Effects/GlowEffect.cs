using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Effects
{
    /// <summary>
    /// The reusable outer glow for widget text. <see cref="Build"/> makes the glow <see cref="Effect"/>;
    /// <see cref="Wrap"/> composes it so the glow sits strictly <em>behind</em> the text without
    /// touching the glyphs themselves. Any widget that wants an alpha-blended halo around its text
    /// builds each text element through <see cref="Wrap"/>.
    /// </summary>
    public static class GlowEffect
    {
        // Each slider step (0–10) maps to this much blur radius; 10 gives a broad, soft halo.
        private const double BlurPerStep = 3.5;

        // A single drop-shadow's alpha falls off quickly, so the glow reads faint. Stacking several
        // identical blurred copies makes their alpha accumulate into a denser, more intense halo.
        private const int GlowLayers = 4;

        /// <summary>
        /// A frozen glow effect for the given settings, or <c>null</c> when the radius is zero
        /// (glow off). Shareable across many elements.
        /// </summary>
        public static Effect? Build(GlowEffectConfig config)
            => Build(config.OuterGlowRadius, ColorUtil.Parse(config.OuterGlowColor, Colors.White));

        /// <summary>A frozen glow effect for a radius (0–10) and colour, or null when radius ≤ 0.</summary>
        public static Effect? Build(double radius, Color color)
        {
            if (radius <= 0)
                return null;

            // ShadowDepth 0 centres the shadow on the element, so it reads as an even outer glow;
            // the effect's own alpha falloff does the blending.
            var effect = new DropShadowEffect
            {
                Color = color,
                ShadowDepth = 0,
                BlurRadius = Math.Clamp(radius, 0, 10) * BlurPerStep,
                Opacity = 1,
            };
            effect.Freeze();
            return effect;
        }

        /// <summary>
        /// Builds a text element with the glow applied <em>behind</em> it: a blurred copy carries the
        /// effect and a crisp, effect-free copy is drawn on top, so the glyphs stay sharp (the effect
        /// never rasterizes the visible text) and the glow only shows in the area around the text.
        /// When <paramref name="glow"/> is null the factory's element is returned unchanged.
        /// </summary>
        /// <param name="makeText">
        /// Creates a fresh text element each call — invoked twice (glow layer + crisp layer), so it
        /// must not return the same instance twice.
        /// </param>
        public static FrameworkElement Wrap(Func<FrameworkElement> makeText, Effect? glow)
        {
            if (glow is null)
                return makeText();

            var layered = new Grid();

            // Several stacked blurred copies; their alpha accumulates into an intense halo.
            for (int i = 0; i < GlowLayers; i++)
            {
                var behind = makeText();
                behind.Effect = glow;   // the crisp copy on top hides each layer's own glyphs
                layered.Children.Add(behind);
            }

            layered.Children.Add(makeText());   // pristine, vector-sharp text on top
            return layered;
        }

        /// <summary>
        /// Builds an intense glow that sits <em>behind</em> an existing on-screen visual — for text
        /// that lives in XAML and updates in place, rather than being rebuilt through <see cref="Wrap"/>.
        /// Returns a stack of blurred, live mirrors of <paramref name="source"/> (via a
        /// <see cref="VisualBrush"/>, so they track its content automatically); place the result in the
        /// same cell as, and behind, the source so its crisp glyphs stay untouched on top. The result
        /// contributes no size of its own, so it never disturbs the source's layout. A null glow
        /// yields a collapsed placeholder.
        /// </summary>
        public static FrameworkElement BehindVisual(Visual source, Effect? glow)
        {
            var layered = new Grid { IsHitTestVisible = false };
            if (glow is null)
            {
                layered.Visibility = Visibility.Collapsed;
                return layered;
            }

            // Each mirror only paints where the source has content, so the drop shadow's halo follows
            // the text's silhouette; stacking several accumulates alpha into a dense glow.
            for (int i = 0; i < GlowLayers; i++)
            {
                layered.Children.Add(new Rectangle
                {
                    Effect = glow,
                    Fill = new VisualBrush(source) { Stretch = Stretch.None },
                });
            }

            return layered;
        }
    }
}
