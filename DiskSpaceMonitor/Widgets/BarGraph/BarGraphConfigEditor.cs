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
    /// between bars, the bars' corner rounding, unused-space transparency, the axis and caption
    /// toggles and the low/critical thresholds),
    /// a Colours tab (label text, the unused-bar track, and the healthy/low/critical status colours),
    /// and an Effects tab (the bar outline style and the reusable text outer glow). The hosting
    /// widget supplies the orientation choices and the axis toggle's caption, which is all that
    /// differs between the vertical and horizontal graphs — the graph's size is dragged on the
    /// widget itself, not set here.
    /// </summary>
    public sealed class BarGraphConfigEditor : IWidgetConfigEditor
    {
        private readonly Action _onChanged;
        private readonly (string Label, BarOrientation Value)[] _orientations;
        private readonly string _axisLabel;
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
        private Slider _cornerRadius = null!;
        private Slider _trackOpacity = null!;
        private CheckBox _showAxis = null!;
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
        /// <param name="axisLabel">Caption for the axis toggle, naming the direction this graph's
        /// 0–100% ticks run in.</param>
        public BarGraphConfigEditor(BarGraphConfig initial, Action onChanged,
            (string Label, BarOrientation Value)[] orientations, string axisLabel)
        {
            _onChanged = onChanged;
            _orientations = orientations;
            _axisLabel = axisLabel;
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
            BarCornerRadius = _cornerRadius.Value,
            TrackOpacity = _trackOpacity.Value,
            ShowAxis = _showAxis.IsChecked == true,
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

            // Two sections: what the graph shows, then how it is drawn. Each is its own caption
            // scope, so the short captions above the rule aren't pushed out by the long ones below
            // it — the sections line up within themselves, not with each other.
            var shown = SettingRow.Scope(new StackPanel());
            var drawn = SettingRow.Scope(new StackPanel());

            _orientation = AddCombo(shown, "Orientation", _orientations, initial.Orientation, topMargin: 0,
                tooltip: "Which end of the axis 0% sits at, and so the direction the bars fill.");

            _showAxis = AddCheckBox(shown, _axisLabel, initial.ShowAxis, topMargin: 16,
                tooltip: "Show the 0–100% scale beside the plot. Hiding it gives the room to the bars; "
                    + "the faint gridlines stay either way.");
            _showUsedSpace = AddCheckBox(shown, "Show used space", initial.ShowUsedSpace, topMargin: 8,
                tooltip: "Write how much of each drive is in use (e.g. \"400 GB\") against its bar.");
            _showTotalSpace = AddCheckBox(shown, "Show total space", initial.ShowTotalSpace, topMargin: 8,
                tooltip: "Write each drive's total size at the 100% end of its bar.");

            _barGap = AddSlider(drawn, "Gap between bars", min: 0, max: 50, value: initial.BarGapPercent,
                small: 5, large: 10, format: v => $"{v:0}%", topMargin: 0,
                tooltip: "How much of each bar's share of the graph is space rather than bar. The bars "
                    + "always fill the window, so this divides it up — it doesn't resize the graph.");

            _cornerRadius = AddSlider(drawn, "Corner radius", min: 0, max: 20, value: initial.BarCornerRadius,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 12,
                tooltip: "How rounded the ends of each bar are. 0 gives square corners; a large value "
                    + "rounds a bar as far as its thickness allows.");
            _cornerRadius.TickFrequency = 1;
            _cornerRadius.IsSnapToTickEnabled = true;

            _trackOpacity = AddSlider(drawn, "Unused space transparency", min: 0, max: 1, value: initial.TrackOpacity,
                small: 0.05, large: 0.1, format: v => $"{v * 100:0}%", topMargin: 12,
                tooltip: "How solid the free part of each bar is drawn, beyond the fill. 0% hides it "
                    + "and leaves the fill floating on the desktop.");

            _lowThreshold = AddPercentSlider(drawn, "Low threshold", initial.LowThresholdPercent,
                tooltip: "A drive with less free space than this turns the 'low' colour.");
            _criticalThreshold = AddPercentSlider(drawn, "Critical threshold", initial.CriticalThresholdPercent,
                tooltip: "A drive with less free space than this turns the 'critical' colour. "
                    + "It wins over the low threshold.");

            panel.Children.Add(shown);
            panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
            panel.Children.Add(drawn);

            // Scrollable like the other two tabs: this is the longest of them, and the dialog is a
            // fixed size, so without this the last settings sit below the bottom edge unreachable.
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            };
        }

        // Effects: how each bar's fill is outlined, then the reusable text outer glow. Only the rows
        // the chosen bar style actually uses are shown, but every row keeps its value, so flipping
        // between Border and 3D Border doesn't lose the other's colours.
        private FrameworkElement BuildEffects(BarGraphConfig initial)
        {
            var panel = new StackPanel();

            var style = new StackPanel { Margin = new Thickness(6, 16, 6, 0) };
            _barStyle = AddCombo(style, "Bar style", new[]
            {
                ("Plain", BarStyle.Plain),
                ("Border", BarStyle.Border),
                ("3D Border", BarStyle.ThreeDBorder),
            }, initial.BarStyle, topMargin: 0,
                tooltip: "How each bar's fill is outlined: Plain leaves it bare, Border rings it evenly, "
                    + "3D Border bevels it so it reads as raised.");

            var sizePanel = new StackPanel();
            _borderSize = AddSlider(sizePanel, "Border size", min: 1, max: 10, value: initial.BorderSize,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 12,
                tooltip: "How thick the outline or bevel is. It scales with the graph, so it keeps its "
                    + "weight as you resize the widget.");
            _borderSize.TickFrequency = 1;
            _borderSize.IsSnapToTickEnabled = true;
            style.Children.Add(sizePanel);
            _borderSizePanel = sizePanel;

            var borderColours = new StackPanel();
            borderColours.Children.Add(SubHeading("Border colour", 16));
            _borderRow = AddColorRow(borderColours, "Border", ColorUtil.Parse(initial.BorderColor, Colors.White),
                "The colour of the outline drawn around each bar's fill.");
            style.Children.Add(borderColours);
            _borderColourPanel = borderColours;

            var bevelColours = new StackPanel();
            bevelColours.Children.Add(SubHeading("Bevel colours", 16));
            _highlightRow = AddColorRow(bevelColours, "Highlight", ColorUtil.Parse(initial.HighlightColor, Colors.White),
                "The lit edge of the bevel, drawn along the top and left of each bar.");
            _lowlightRow = AddColorRow(bevelColours, "Lowlight", ColorUtil.Parse(initial.LowlightColor, Colors.Black),
                "The shaded edge of the bevel, drawn down the right and along the bottom of each bar.");
            style.Children.Add(bevelColours);
            _bevelColourPanel = bevelColours;

            _barStyle.SelectionChanged += (_, _) => UpdateBarStyleRows();
            UpdateBarStyleRows();

            panel.Children.Add(style);
            panel.Children.Add(_glow.View);

            // One scope for the whole tab, so the glow's row lines up with the bar style rows above it.
            return SettingRow.Scope(new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            });
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
            _textRow = AddColorRow(panel, "Text", ColorUtil.Parse(initial.TextColor, Colors.White),
                "The colour of every piece of text on the graph: drive labels, axis ticks and captions.");

            panel.Children.Add(SubHeading("Unused space", 12));
            _trackRow = AddColorRow(panel, "Track", ColorUtil.Parse(initial.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                "The colour of the free part of each bar, beyond the fill.");

            panel.Children.Add(SubHeading("Bar status", 12));
            _healthyRow = AddColorRow(panel, "Healthy", ColorUtil.Parse(initial.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
                "The bar colour for a drive with more free space than the low threshold.");
            _warningRow = AddColorRow(panel, "Low", ColorUtil.Parse(initial.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
                "The bar colour for a drive below the low threshold.");
            _criticalRow = AddColorRow(panel, "Critical", ColorUtil.Parse(initial.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
                "The bar colour for a drive below the critical threshold.");

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        }

        // A captioned dropdown, selecting the item matching <paramref name="initial"/> (falling
        // back to the first) and previewing on every change.
        private ComboBox AddCombo<T>(StackPanel panel, string label, (string Label, T Value)[] options,
            T initial, double topMargin, string tooltip)
            where T : struct, Enum
        {
            var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var (text, value) in options)
            {
                var item = new ComboBoxItem { Content = text, Tag = value };
                combo.Items.Add(item);
                if (value.Equals(initial))
                    combo.SelectedItem = item;
            }

            combo.SelectedItem ??= combo.Items[0];
            combo.SelectionChanged += (_, _) => Raise();
            panel.Children.Add(SettingRow.Build(label, combo, topMargin: topMargin, tooltip: tooltip));
            return combo;
        }

        private static T Selected<T>(ComboBox combo) => (T)((ComboBoxItem)combo.SelectedItem).Tag;

        private Slider AddPercentSlider(StackPanel panel, string caption, double initial, string tooltip)
            => AddSlider(panel, caption, min: 1, max: 90, value: initial, small: 1, large: 5,
                format: v => $"{v:0}%", topMargin: 12, tooltip: tooltip);

        // Adds a captioned slider row (caption + slider + right-aligned readout) and returns the slider.
        private Slider AddSlider(StackPanel panel, string label, double min, double max, double value,
            double small, double large, Func<double, string> format, double topMargin, string tooltip)
        {
            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                SmallChange = small,
                LargeChange = large,
                Value = Math.Clamp(value, min, max),
            };
            var readout = SettingRow.Readout(format(slider.Value));
            slider.ValueChanged += (_, e) => { readout.Text = format(e.NewValue); Raise(); };
            panel.Children.Add(SettingRow.Build(label, slider, readout, topMargin, tooltip));
            return slider;
        }

        // The checkbox carries its own caption, so it is indented to start where the labelled
        // controls do instead of hanging off the panel's left edge.
        private CheckBox AddCheckBox(StackPanel panel, string label, bool isChecked, double topMargin,
            string tooltip)
        {
            var check = new CheckBox
            {
                Content = label,
                FontSize = 13,
                IsChecked = isChecked,
            };
            check.Checked += (_, _) => Raise();
            check.Unchecked += (_, _) => Raise();
            panel.Children.Add(SettingRow.Indented(check, topMargin, tooltip));
            return check;
        }

        private ColorRow AddColorRow(StackPanel panel, string label, Color color, string tooltip)
        {
            var row = new ColorRow { Label = label, Color = color, ToolTip = tooltip };
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
