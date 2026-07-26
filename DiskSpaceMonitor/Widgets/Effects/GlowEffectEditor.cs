using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Effects
{
    /// <summary>
    /// Reusable "Effects" settings section: an outer-glow radius slider (0–10) and an outer-glow
    /// colour row. A widget's config editor drops <see cref="View"/> into an Effects tab and reads
    /// <see cref="Current"/> back into its config; every change raises the supplied callback so the
    /// glow previews live like any other setting.
    /// </summary>
    public sealed class GlowEffectEditor
    {
        private readonly Action _onChanged;
        private readonly Slider _radius;
        private readonly ColorRow _colorRow;
        private bool _ready;

        public GlowEffectEditor(GlowEffectConfig initial, Action onChanged)
        {
            _onChanged = onChanged;

            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 6) };

            _radius = BuildSlider(panel, initial.OuterGlowRadius);

            panel.Children.Add(new TextBlock
            {
                Text = "Outer glow colour",
                FontSize = 12,
                Opacity = 0.7,
                Margin = new Thickness(0, 16, 0, 4),
            });
            _colorRow = new ColorRow
            {
                Label = "Colour",
                Color = ColorUtil.Parse(initial.OuterGlowColor, Colors.White),
                ToolTip = "The colour of the halo. A dark glow behind light text is what makes it "
                    + "readable over a busy wallpaper.",
            };
            _colorRow.ColorChanged += _ => Raise();
            panel.Children.Add(_colorRow);

            View = panel;
            _ready = true;
        }

        /// <summary>The settings UI to host in an Effects tab.</summary>
        public FrameworkElement View { get; }

        /// <summary>The glow settings currently shown in the editor.</summary>
        public GlowEffectConfig Current() => new()
        {
            OuterGlowRadius = _radius.Value,
            OuterGlowColor = ColorUtil.ToHex(_colorRow.Color),
        };

        private void Raise()
        {
            if (_ready)
                _onChanged();
        }

        private Slider BuildSlider(StackPanel panel, double value)
        {
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 10,
                SmallChange = 1,
                LargeChange = 2,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Value = Math.Clamp(value, 0, 10),
            };
            var readout = SettingRow.Readout($"{slider.Value:0}");
            slider.ValueChanged += (_, e) => { readout.Text = $"{e.NewValue:0}"; Raise(); };
            panel.Children.Add(SettingRow.Build("Outer glow", slider, readout,
                tooltip: "How far a soft halo spreads behind the widget's text, to lift it off the "
                    + "wallpaper. 0 turns the glow off."));
            return slider;
        }
    }
}
