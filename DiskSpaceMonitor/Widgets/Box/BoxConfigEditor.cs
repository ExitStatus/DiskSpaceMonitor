using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;
using DiskSpaceMonitor.Widgets.BarGraph;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Box
{
    /// <summary>
    /// Settings editor for the box widget: an Appearance tab (the box's and the bar's corner
    /// rounding, the bar's share of the height, the background/border/unused-space transparencies
    /// and the low/critical thresholds), a Colours tab (text, background, the unused-bar track and
    /// the healthy/low/critical status colours), and one effects tab each for the panel and the bar
    /// — a style, its width and its colours — since the two are outlined independently and putting
    /// both on one tab left it scrolling. The text's outer glow sits with the box, which holds it.
    /// The box's size is dragged on the widget itself, not set here.
    /// </summary>
    public sealed class BoxConfigEditor : IWidgetConfigEditor
    {
        private readonly Action _onChanged;
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        private Slider _cornerRadius = null!;
        private Slider _barCornerRadius = null!;
        private Slider _barHeight = null!;
        private Slider _backgroundOpacity = null!;
        private Slider _borderOpacity = null!;
        private Slider _trackOpacity = null!;
        private Slider _lowThreshold = null!;
        private Slider _criticalThreshold = null!;
        private ColorRow _textRow = null!;
        private ColorRow _backgroundRow = null!;
        private ColorRow _trackRow = null!;
        private ColorRow _healthyRow = null!;
        private ColorRow _warningRow = null!;
        private ColorRow _criticalRow = null!;
        private ComboBox _boxStyle = null!;
        private ComboBox _barStyle = null!;
        private Slider _boxBorderSize = null!;
        private Slider _barBorderSize = null!;
        private ColorRow _boxBorderRow = null!;
        private ColorRow _boxHighlightRow = null!;
        private ColorRow _boxLowlightRow = null!;
        private ColorRow _barBorderRow = null!;
        private ColorRow _barHighlightRow = null!;
        private ColorRow _barLowlightRow = null!;
        private FrameworkElement _boxSizePanel = null!;
        private FrameworkElement _boxBorderPanel = null!;
        private FrameworkElement _boxBevelPanel = null!;
        private FrameworkElement _barSizePanel = null!;
        private FrameworkElement _barBorderPanel = null!;
        private FrameworkElement _barBevelPanel = null!;
        private readonly GlowEffectEditor _glow;
        private readonly bool _ready;

        public BoxConfigEditor(BoxConfig initial, Action onChanged)
        {
            _onChanged = onChanged;
            _glow = new GlowEffectEditor(initial.Glow, Raise);

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", BuildAppearance(initial)),
                new WidgetConfigTab("Colours", BuildColours(initial)),
                new WidgetConfigTab("Box effects", BuildBoxEffects(initial)),
                new WidgetConfigTab("Bar effects", BuildBarEffects(initial)),
            };
            _ready = true;
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig() => new BoxConfig
        {
            CornerRadius = _cornerRadius.Value,
            BarCornerRadius = _barCornerRadius.Value,
            BarHeightPercent = _barHeight.Value,
            BackgroundOpacity = _backgroundOpacity.Value,
            BorderOpacity = _borderOpacity.Value,
            TrackOpacity = _trackOpacity.Value,
            LowThresholdPercent = _lowThreshold.Value,
            CriticalThresholdPercent = _criticalThreshold.Value,
            TextColor = ColorUtil.ToHex(_textRow.Color),
            BackgroundColor = ColorUtil.ToHex(_backgroundRow.Color),
            TrackColor = ColorUtil.ToHex(_trackRow.Color),
            HealthyColor = ColorUtil.ToHex(_healthyRow.Color),
            WarningColor = ColorUtil.ToHex(_warningRow.Color),
            CriticalColor = ColorUtil.ToHex(_criticalRow.Color),
            BoxStyle = Selected<BarStyle>(_boxStyle),
            BarStyle = Selected<BarStyle>(_barStyle),
            BoxBorderSize = _boxBorderSize.Value,
            BarBorderSize = _barBorderSize.Value,
            BoxBorderColor = ColorUtil.ToHex(_boxBorderRow.Color),
            BoxHighlightColor = ColorUtil.ToHex(_boxHighlightRow.Color),
            BoxLowlightColor = ColorUtil.ToHex(_boxLowlightRow.Color),
            BarBorderColor = ColorUtil.ToHex(_barBorderRow.Color),
            BarHighlightColor = ColorUtil.ToHex(_barHighlightRow.Color),
            BarLowlightColor = ColorUtil.ToHex(_barLowlightRow.Color),
            Glow = _glow.Current(),
        };

        private void Raise()
        {
            if (_ready)
                _onChanged();
        }

        private FrameworkElement BuildAppearance(BoxConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 6) };

            // Two sections: the shape of the box, then how solidly it is painted. Each is its own
            // caption scope, so the short captions above the rule aren't pushed out by the long
            // ones below it — the sections line up within themselves, not with each other.
            var shape = SettingRow.Scope(new StackPanel());
            var paint = SettingRow.Scope(new StackPanel());

            _cornerRadius = AddSlider(shape, "Corner radius", min: 0, max: 30, value: initial.CornerRadius,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 0,
                tooltip: "How rounded the box's corners are. 0 gives a square-cornered panel.");
            Snap(_cornerRadius);

            _barCornerRadius = AddSlider(shape, "Bar corner radius", min: 0, max: 20, value: initial.BarCornerRadius,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 12,
                tooltip: "How rounded the ends of the bar inside the box are — set apart from the "
                    + "box's own corners, so a square panel can hold a pill-shaped bar.");
            Snap(_barCornerRadius);

            _barHeight = AddSlider(shape, "Bar height", min: 5, max: 50, value: initial.BarHeightPercent,
                small: 1, large: 5, format: v => $"{v:0}%", topMargin: 12,
                tooltip: "How much of the box's height the bar takes. The two text rows share what "
                    + "is left, so this stays true whatever size the box is dragged to.");

            _backgroundOpacity = AddSlider(paint, "Background transparency", min: 0, max: 1,
                value: initial.BackgroundOpacity, small: 0.05, large: 0.1, format: v => $"{v * 100:0}%",
                topMargin: 0,
                tooltip: "How solid the box's fill is. 0% leaves the text and bar floating on the "
                    + "desktop with no panel behind them.");

            _borderOpacity = AddSlider(paint, "Border transparency", min: 0, max: 1,
                value: initial.BorderOpacity, small: 0.05, large: 0.1, format: v => $"{v * 100:0}%",
                topMargin: 12,
                tooltip: "How solid the box's and the bar's outlines are drawn, when a border or "
                    + "bevel is chosen on the Effects tab.");

            _trackOpacity = AddSlider(paint, "Unused space transparency", min: 0, max: 1,
                value: initial.TrackOpacity, small: 0.05, large: 0.1, format: v => $"{v * 100:0}%",
                topMargin: 12,
                tooltip: "How solid the free part of the bar is drawn, beyond the fill. 0% hides it "
                    + "and leaves the fill alone in the row.");

            _lowThreshold = AddPercentSlider(paint, "Low threshold", initial.LowThresholdPercent,
                tooltip: "A drive with less free space than this turns the 'low' colour.");
            _criticalThreshold = AddPercentSlider(paint, "Critical threshold", initial.CriticalThresholdPercent,
                tooltip: "A drive with less free space than this turns the 'critical' colour. "
                    + "It wins over the low threshold.");

            panel.Children.Add(shape);
            panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
            panel.Children.Add(paint);

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            };
        }

        private FrameworkElement BuildColours(BoxConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 12, 6, 6) };

            panel.Children.Add(SubHeading("Box", 0));
            _textRow = AddColorRow(panel, "Text", ColorUtil.Parse(initial.TextColor, Colors.White),
                "The colour of both lines of text in the box.");
            _backgroundRow = AddColorRow(panel, "Background",
                ColorUtil.Parse(initial.BackgroundColor, Color.FromRgb(0x16, 0x1A, 0x20)),
                "The colour of the panel behind the text and the bar.");

            panel.Children.Add(SubHeading("Unused space", 12));
            _trackRow = AddColorRow(panel, "Track",
                ColorUtil.Parse(initial.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86)),
                "The colour of the free part of the bar, beyond the fill.");

            panel.Children.Add(SubHeading("Bar status", 12));
            _healthyRow = AddColorRow(panel, "Healthy",
                ColorUtil.Parse(initial.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
                "The bar colour for a drive with more free space than the low threshold.");
            _warningRow = AddColorRow(panel, "Low",
                ColorUtil.Parse(initial.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
                "The bar colour for a drive below the low threshold.");
            _criticalRow = AddColorRow(panel, "Critical",
                ColorUtil.Parse(initial.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
                "The bar colour for a drive below the critical threshold.");

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        }

        private static readonly (string Label, BarStyle Value)[] OutlineStyles =
        {
            ("Plain", BarStyle.Plain),
            ("Border", BarStyle.Border),
            ("3D Border", BarStyle.ThreeDBorder),
        };

        // How the panel itself is outlined, then the reusable text outer glow — the box is what
        // holds the text, so the glow belongs here. Only the rows the chosen style actually uses are
        // shown, but every row keeps its value, so flipping between Border and 3D Border doesn't
        // lose the other's colours.
        private FrameworkElement BuildBoxEffects(BoxConfig initial)
        {
            var panel = new StackPanel();
            var style = new StackPanel { Margin = new Thickness(6, 16, 6, 0) };

            _boxStyle = AddCombo(style, "Box style", OutlineStyles, initial.BoxStyle, topMargin: 0,
                tooltip: "How the box's panel is outlined: Plain leaves it bare, Border rings it "
                    + "evenly, 3D Border bevels it so it reads as raised.");

            _boxSizePanel = SizeRow(style, initial.BoxBorderSize, "box", out _boxBorderSize);
            _boxBorderPanel = BorderColours(style, initial.BoxBorderColor,
                "The colour of the outline drawn around the box.", out _boxBorderRow);
            _boxBevelPanel = BevelColours(style, initial.BoxHighlightColor, initial.BoxLowlightColor,
                "box", out _boxHighlightRow, out _boxLowlightRow);

            _boxStyle.SelectionChanged += (_, _) => UpdateBoxRows();
            UpdateBoxRows();

            panel.Children.Add(style);
            panel.Children.Add(_glow.View);

            // One scope for the whole tab, so the glow's row lines up with the style rows above it.
            return SettingRow.Scope(new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            });
        }

        // The same three choices for the bar inside the box, kept on their own tab: with both sets
        // of colours on one, the tab scrolled.
        private FrameworkElement BuildBarEffects(BoxConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 0) };

            _barStyle = AddCombo(panel, "Bar style", OutlineStyles, initial.BarStyle, topMargin: 0,
                tooltip: "How the bar inside the box is outlined, set apart from the panel's own "
                    + "outline: Plain leaves it bare, Border rings it evenly, 3D Border bevels it.");

            _barSizePanel = SizeRow(panel, initial.BarBorderSize, "bar", out _barBorderSize);
            _barBorderPanel = BorderColours(panel, initial.BarBorderColor,
                "The colour of the outline drawn around the bar's fill.", out _barBorderRow);
            _barBevelPanel = BevelColours(panel, initial.BarHighlightColor, initial.BarLowlightColor,
                "bar", out _barHighlightRow, out _barLowlightRow);

            _barStyle.SelectionChanged += (_, _) => UpdateBarRows();
            UpdateBarRows();

            return SettingRow.Scope(new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            });
        }

        // The outline width, in its own panel so it can be hidden along with the colours when the
        // Plain style has nothing to size.
        private FrameworkElement SizeRow(StackPanel parent, double initial, string what, out Slider slider)
        {
            var section = new StackPanel();
            slider = AddSlider(section, "Border size", min: 1, max: 10, value: initial,
                small: 1, large: 2, format: v => $"{v:0}", topMargin: 12,
                tooltip: $"How thick the {what}'s outline or bevel is. It scales with the widget, so "
                    + "it keeps its weight as you resize the box.");
            Snap(slider);
            parent.Children.Add(section);
            return section;
        }

        private FrameworkElement BorderColours(StackPanel parent, string initial, string tooltip,
            out ColorRow row)
        {
            var section = new StackPanel();
            section.Children.Add(SubHeading("Border colour", 16));
            row = AddColorRow(section, "Border", ColorUtil.Parse(initial, Colors.White), tooltip);
            parent.Children.Add(section);
            return section;
        }

        private FrameworkElement BevelColours(StackPanel parent, string highlight, string lowlight,
            string what, out ColorRow highlightRow, out ColorRow lowlightRow)
        {
            var section = new StackPanel();
            section.Children.Add(SubHeading("Bevel colours", 16));
            highlightRow = AddColorRow(section, "Highlight", ColorUtil.Parse(highlight, Colors.White),
                $"The lit edge of the bevel, drawn along the top and left of the {what}.");
            lowlightRow = AddColorRow(section, "Lowlight", ColorUtil.Parse(lowlight, Colors.Black),
                $"The shaded edge of the bevel, drawn down the right and along the bottom of the {what}.");
            parent.Children.Add(section);
            return section;
        }

        // Show only the rows the selected style uses: Plain has nothing to size or colour, Border
        // takes one colour, and the bevel takes two.
        private void UpdateBoxRows()
        {
            var style = Selected<BarStyle>(_boxStyle);
            _boxSizePanel.Visibility = Show(style != BarStyle.Plain);
            _boxBorderPanel.Visibility = Show(style == BarStyle.Border);
            _boxBevelPanel.Visibility = Show(style == BarStyle.ThreeDBorder);
        }

        private void UpdateBarRows()
        {
            var style = Selected<BarStyle>(_barStyle);
            _barSizePanel.Visibility = Show(style != BarStyle.Plain);
            _barBorderPanel.Visibility = Show(style == BarStyle.Border);
            _barBevelPanel.Visibility = Show(style == BarStyle.ThreeDBorder);
        }

        private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

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

        // Whole pixels only, for the sliders whose readout has no decimals to show a fraction in.
        private static void Snap(Slider slider)
        {
            slider.TickFrequency = 1;
            slider.IsSnapToTickEnabled = true;
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
