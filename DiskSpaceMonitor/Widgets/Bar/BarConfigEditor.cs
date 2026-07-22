using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Bar
{
    /// <summary>
    /// Settings editor for the bar-graph widget: an Appearance tab (unused-space transparency and
    /// the low/critical thresholds) and a Colours tab (label text, the unused-bar track, and the
    /// healthy/low/critical status colours).
    /// </summary>
    public sealed class BarConfigEditor : IWidgetConfigEditor
    {
        private readonly Action _onChanged;
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        private Slider _barWidth = null!;
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
        private bool _ready;

        public BarConfigEditor(BarConfig initial, Action onChanged)
        {
            _onChanged = onChanged;

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", BuildAppearance(initial)),
                new WidgetConfigTab("Colours", BuildColours(initial)),
            };
            _ready = true;
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig() => new BarConfig
        {
            BarWidthPercent = _barWidth.Value,
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
        };

        private void Raise()
        {
            if (_ready)
                _onChanged();
        }

        private FrameworkElement BuildAppearance(BarConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 6) };

            panel.Children.Add(new TextBlock { Text = "Bar width", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
            _barWidth = AddSlider(panel, min: 10, max: 100, value: initial.BarWidthPercent,
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

        private FrameworkElement BuildColours(BarConfig initial)
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
