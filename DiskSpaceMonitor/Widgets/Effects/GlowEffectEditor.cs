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

            panel.Children.Add(new TextBlock { Text = "Outer glow", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
            _radius = BuildSlider(panel, initial.OuterGlowRadius);

            panel.Children.Add(new TextBlock
            {
                Text = "Outer glow colour",
                FontSize = 12,
                Opacity = 0.7,
                Margin = new Thickness(0, 16, 0, 4),
            });
            _colorRow = new ColorRow { Label = "Colour", Color = ColorUtil.Parse(initial.OuterGlowColor, Colors.White) };
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
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 10,
                SmallChange = 1,
                LargeChange = 2,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Clamp(value, 0, 10),
            };
            var readout = new TextBlock
            {
                Width = 44,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{slider.Value:0}",
            };
            slider.ValueChanged += (_, e) => { readout.Text = $"{e.NewValue:0}"; Raise(); };
            Grid.SetColumn(slider, 0);
            Grid.SetColumn(readout, 1);
            grid.Children.Add(slider);
            grid.Children.Add(readout);
            panel.Children.Add(grid);
            return slider;
        }
    }
}
