using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.BarGraph
{
    /// <summary>
    /// Settings editor shared by both bar graph widgets: an Appearance tab (orientation, the gap
    /// between bars, unused-space transparency, the caption toggles and the low/critical thresholds),
    /// a Colours tab (label text, the unused-bar track, and the healthy/low/critical status colours),
    /// and an Effects tab (the bar outline style and the reusable text outer glow). The hosting
    /// widget supplies the orientation choices, which is all that differs between the vertical and
    /// horizontal graphs — the graph's size is dragged on the widget itself, not set here.
    /// </summary>
    public sealed class BarGraphConfigEditor : IWidgetConfigEditor
    {
        private readonly Action _onChanged;
        private readonly (string Label, BarOrientation Value)[] _orientations;
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        private ComboBox _orientation = null!;
        private ComboBox _barStyle = null!;
        private Slider _borderSize = null!;
        private ColorRow _borderRow = null!;
        private ColorRow _highlightRow = null!;
        private ColorRow _lowlightRow = null!;
        private FrameworkElement _borderSizePanel = null!;
        private FrameworkElement _borderColourPanel = null!;
        private FrameworkElement _bevelColourPanel = null!;
        private Slider _barGap = null!;
        private Slider _trackOpacity = null!;
        private CheckBox _showUsedSpace = null!;
        private CheckBox _showTotalSpace = null!;
        private Slider _lowThreshold = null!;
        private Slider _criticalThreshold = null!;
        private ColorRow _textRow = null!;
        private ColorRow _trackRow = null!;
        private ColorRow _healthyRow = null!;
        private ColorRow _warningRow = null!;
        private ColorRow _criticalRow = null!;
        private GlowEffectEditor _glow = null!;
        private bool _ready;

        /// <param name="orientations">The orientation choices to offer, in dropdown order.</param>
        public BarGraphConfigEditor(BarGraphConfig initial, Action onChanged,
            (string Label, BarOrientation Value)[] orientations)
        {
            _onChanged = onChanged;
            _orientations = orientations;
            _glow = new GlowEffectEditor(initial.Glow, Raise);

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", BuildAppearance(initial)),
                new WidgetConfigTab("Colours", BuildColours(initial)),
                new WidgetConfigTab("Effects", BuildEffects(initial)),
            };
            _ready = true;
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig() => new BarGraphConfig
        {
            Orientation = Selected<BarOrientation>(_orientation),
            BarGapPercent = _barGap.Value,
            TrackOpacity = _trackOpacity.Value,
            ShowUsedSpace = _showUsedSpace.IsChecked == true,
            ShowTotalSpace = _showTotalSpace.IsChecked == true,
            LowThresholdPercent = _lowThreshold.Value,
            CriticalThresholdPercent = _criticalThreshold.Value,
            TextColor = ColorUtil.ToHex(_textRow.Color),
            TrackColor = ColorUtil.ToHex(_trackRow.Color),
            HealthyColor = ColorUtil.ToHex(_healthyRow.Color),
            WarningColor = ColorUtil.ToHex(_warningRow.Color),
            CriticalColor = ColorUtil.ToHex(_criticalRow.Color),
            BarStyle = Selected<BarStyle>(_barStyle),
            BorderSize = _borderSize.Value,
            BorderColor = ColorUtil.ToHex(_borderRow.Color),
            HighlightColor = ColorUtil.ToHex(_highlightRow.Color),
            LowlightColor = ColorUtil.ToHex(_lowlightRow.Color),
            Glow = _glow.Current(),
        };

        private void Raise()
        {
            if (_ready)
                _onChanged();
        }

        private FrameworkElement BuildAppearance(BarGraphConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 6) };

            panel.Children.Add(new TextBlock { Text = "Orientation", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
            _orientation = AddCombo(panel, _orientations, initial.Orientation);

            panel.Children.Add(new TextBlock { Text = "Gap between bars", FontSize = 13, Margin = new Thickness(0, 16, 0, 4) });
            _barGap = AddSlider(panel, min: 0, max: 50, value: initial.BarGapPercent,
                small: 5, large: 10, format: v => $"{v:0}%", topMargin: 0, addCaption: false);

            panel.Children.Add(new TextBlock { Text = "Unused space transparency", FontSize = 13, Margin = new Thickness(0, 16, 0, 4) });
            _trackOpacity = AddSlider(panel, min: 0, max: 1, value: initial.TrackOpacity,
                small: 0.05, large: 0.1, format: v => $"{v * 100:0}%", topMargin: 0, addCaption: false);

            _showUsedSpace = AddCheckBox(panel, "Show used space", initial.ShowUsedSpace, topMargin: 16);
            _showTotalSpace = AddCheckBox(panel, "Show total space", initial.ShowTotalSpace, topMargin: 8);

            _lowThreshold = AddPercentSlider(panel, "Bar turns 'low' when free space drops below", initial.LowThresholdPercent);
            _criticalThreshold = AddPercentSlider(panel, "Bar turns 'critical' when free space drops below", initial.CriticalThresholdPercent);

            return panel;
        }

        // Effects: how each bar's fill is outlined, then the reusable text outer glow. Only the rows
        // the chosen bar style actually uses are shown, but every row keeps its value, so flipping
        // between Border and 3D Border doesn't lose the other's colours.
        private FrameworkElement BuildEffects(BarGraphConfig initial)
        {
            var panel = new StackPanel();

            var style = new StackPanel { Margin = new Thickness(6, 16, 6, 0) };
            style.Children.Add(new TextBlock { Text = "Bar style", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
            _barStyle = AddCombo(style, new[]
            {
                ("Plain", BarStyle.Plain),
                ("Border", BarStyle.Border),
                ("3D Border", BarStyle.ThreeDBorder),
            }, initial.BarStyle);

            var sizePanel = new StackPanel();
            sizePanel.Children.Add(new TextBlock
            {
                Text = "Border size",
                FontSize = 12,
                Opacity = 0.7,
                Margin = new Thickness(0, 16, 0, 4),
            });
            _borderSize = AddSlider(sizePanel, min: 1, max: 10, value: initial.BorderSize,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 0, addCaption: false);
            _borderSize.TickFrequency = 1;
            _borderSize.IsSnapToTickEnabled = true;
            style.Children.Add(sizePanel);
            _borderSizePanel = sizePanel;

            var borderColours = new StackPanel();
            borderColours.Children.Add(SubHeading("Border colour", 16));
            _borderRow = AddColorRow(borderColours, "Border", ColorUtil.Parse(initial.BorderColor, Colors.White));
            style.Children.Add(borderColours);
            _borderColourPanel = borderColours;

            var bevelColours = new StackPanel();
            bevelColours.Children.Add(SubHeading("Bevel colours", 16));
            _highlightRow = AddColorRow(bevelColours, "Highlight", ColorUtil.Parse(initial.HighlightColor, Colors.White));
            _lowlightRow = AddColorRow(bevelColours, "Lowlight", ColorUtil.Parse(initial.LowlightColor, Colors.Black));
            style.Children.Add(bevelColours);
            _bevelColourPanel = bevelColours;

            _barStyle.SelectionChanged += (_, _) => UpdateBarStyleRows();
            UpdateBarStyleRows();

            panel.Children.Add(style);
            panel.Children.Add(_glow.View);

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        }

        // Show only the rows the selected bar style uses.
        private void UpdateBarStyleRows()
        {
            var style = Selected<BarStyle>(_barStyle);
            _borderSizePanel.Visibility = style == BarStyle.Plain ? Visibility.Collapsed : Visibility.Visible;
            _borderColourPanel.Visibility = style == BarStyle.Border ? Visibility.Visible : Visibility.Collapsed;
            _bevelColourPanel.Visibility = style == BarStyle.ThreeDBorder ? Visibility.Visible : Visibility.Collapsed;
        }

        private FrameworkElement BuildColours(BarGraphConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 12, 6, 6) };

            panel.Children.Add(SubHeading("Labels", 0));
            _textRow = AddColorRow(panel, "Text", ColorUtil.Parse(initial.TextColor, Colors.White));

            panel.Children.Add(SubHeading("Unused space", 12));
            _trackRow = AddColorRow(panel, "Track", ColorUtil.Parse(initial.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)));

            panel.Children.Add(SubHeading("Bar status", 12));
            _healthyRow = AddColorRow(panel, "Healthy", ColorUtil.Parse(initial.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)));
            _warningRow = AddColorRow(panel, "Low", ColorUtil.Parse(initial.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)));
            _criticalRow = AddColorRow(panel, "Critical", ColorUtil.Parse(initial.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)));

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        }

        // A labelled-value dropdown, selecting the item matching <paramref name="initial"/> (falling
        // back to the first) and previewing on every change.
        private ComboBox AddCombo<T>(StackPanel panel, (string Label, T Value)[] options, T initial)
            where T : struct, Enum
        {
            var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var (label, value) in options)
            {
                var item = new ComboBoxItem { Content = label, Tag = value };
                combo.Items.Add(item);
                if (value.Equals(initial))
                    combo.SelectedItem = item;
            }

            combo.SelectedItem ??= combo.Items[0];
            combo.SelectionChanged += (_, _) => Raise();
            panel.Children.Add(combo);
            return combo;
        }

        private static T Selected<T>(ComboBox combo) => (T)((ComboBoxItem)combo.SelectedItem).Tag;

        private Slider AddPercentSlider(StackPanel panel, string caption, double initial)
        {
            panel.Children.Add(new TextBlock { Text = caption, FontSize = 13, Margin = new Thickness(0, 16, 0, 4) });
            return AddSlider(panel, min: 1, max: 90, value: initial, small: 1, large: 5,
                format: v => $"{v:0}%", topMargin: 0, addCaption: false);
        }

        // Adds a slider row (slider + right-aligned readout) and returns the slider.
        private Slider AddSlider(StackPanel panel, double min, double max, double value,
            double small, double large, Func<double, string> format, double topMargin, bool addCaption)
        {
            var grid = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                SmallChange = small,
                LargeChange = large,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Clamp(value, min, max),
            };
            var readout = new TextBlock
            {
                Width = 44,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Text = format(slider.Value),
            };
            slider.ValueChanged += (_, e) => { readout.Text = format(e.NewValue); Raise(); };
            Grid.SetColumn(slider, 0);
            Grid.SetColumn(readout, 1);
            grid.Children.Add(slider);
            grid.Children.Add(readout);
            panel.Children.Add(grid);
            return slider;
        }

        private CheckBox AddCheckBox(StackPanel panel, string label, bool isChecked, double topMargin)
        {
            var check = new CheckBox
            {
                Content = label,
                FontSize = 13,
                Margin = new Thickness(0, topMargin, 0, 0),
                IsChecked = isChecked,
            };
            check.Checked += (_, _) => Raise();
            check.Unchecked += (_, _) => Raise();
            panel.Children.Add(check);
            return check;
        }

        private ColorRow AddColorRow(StackPanel panel, string label, Color color)
        {
            var row = new ColorRow { Label = label, Color = color };
            row.ColorChanged += _ => Raise();
            panel.Children.Add(row);
            return row;
        }

        private static TextBlock SubHeading(string text, double topMargin) => new()
        {
            Text = text,
            FontSize = 12,
            Opacity = 0.7,
            Margin = new Thickness(0, topMargin, 0, 4),
        };
    }
}
