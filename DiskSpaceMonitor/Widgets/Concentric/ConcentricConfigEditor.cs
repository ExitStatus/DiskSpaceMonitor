using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>
    /// Settings editor for the concentric widget: an Appearance tab (ring thickness + label text
    /// colour) and a Colours tab with one colour row per shown drive (built dynamically, since the
    /// drive count varies). Colours for drives not currently shown are preserved.
    /// </summary>
    public sealed class ConcentricConfigEditor : IWidgetConfigEditor
    {
        private readonly Action _onChanged;
        private readonly Dictionary<string, string> _initialColors;   // preserves non-shown drives
        private readonly Dictionary<string, ColorRow> _driveRows = new();
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        private Slider _thickness = null!;
        private Slider _trackOpacity = null!;
        private Slider _lowThreshold = null!;
        private Slider _criticalThreshold = null!;
        private ColorRow _textRow = null!;
        private ColorRow _healthyRow = null!;
        private ColorRow _warningRow = null!;
        private ColorRow _criticalRow = null!;
        private bool _ready;

        public ConcentricConfigEditor(ConcentricConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
        {
            _onChanged = onChanged;
            _initialColors = new Dictionary<string, string>(initial.DriveColors);

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", BuildAppearance(initial)),
                new WidgetConfigTab("Colours", BuildColours(initial, shownDrives)),
            };
            _ready = true;
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig()
        {
            var colours = new Dictionary<string, string>(_initialColors);
            foreach (var (path, row) in _driveRows)
                colours[path] = ColorUtil.ToHex(row.Color);

            return new ConcentricConfig
            {
                RingThickness = _thickness.Value,
                TrackOpacity = _trackOpacity.Value,
                LowThresholdPercent = _lowThreshold.Value,
                CriticalThresholdPercent = _criticalThreshold.Value,
                TextColor = ColorUtil.ToHex(_textRow.Color),
                HealthyColor = ColorUtil.ToHex(_healthyRow.Color),
                WarningColor = ColorUtil.ToHex(_warningRow.Color),
                CriticalColor = ColorUtil.ToHex(_criticalRow.Color),
                DriveColors = colours,
            };
        }

        private void Raise()
        {
            if (_ready)
                _onChanged();
        }

        private FrameworkElement BuildAppearance(ConcentricConfig initial)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 16, 6, 6) };

            _thickness = new Slider
            {
                Minimum = 2,
                Maximum = 40,
                SmallChange = 1,
                LargeChange = 4,
                Value = Math.Clamp(initial.RingThickness, 2, 40),
            };
            var thicknessValue = SettingRow.Readout($"{_thickness.Value:0}");
            _thickness.ValueChanged += (_, e) => { thicknessValue.Text = $"{e.NewValue:0}"; Raise(); };
            panel.Children.Add(SettingRow.Build("Ring thickness", _thickness, thicknessValue,
                tooltip: "How wide each drive's ring is drawn. Thicker rings mean fewer fit in the "
                    + "widget, so the innermost ones get smaller."));

            _trackOpacity = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                SmallChange = 0.05,
                LargeChange = 0.1,
                Value = Math.Clamp(initial.TrackOpacity, 0, 1),
            };
            var trackValue = SettingRow.Readout($"{_trackOpacity.Value * 100:0}%");
            _trackOpacity.ValueChanged += (_, e) => { trackValue.Text = $"{e.NewValue * 100:0}%"; Raise(); };
            panel.Children.Add(SettingRow.Build("Unused space transparency", _trackOpacity, trackValue, topMargin: 12,
                tooltip: "How solid the unswept part of each ring is drawn. 0% hides it and leaves "
                    + "just the used arc."));

            _lowThreshold = AddPercentSlider(panel, "Low threshold", initial.LowThresholdPercent,
                "A drive with less free space than this turns its label chip the 'low' colour.");
            _criticalThreshold = AddPercentSlider(panel, "Critical threshold", initial.CriticalThresholdPercent,
                "A drive with less free space than this turns its label chip the 'critical' colour. "
                    + "It wins over the low threshold.");

            return SettingRow.Scope(panel);
        }

        /// <summary>Appends a captioned 1–90% slider (with a live "NN%" readout) and returns it.</summary>
        private Slider AddPercentSlider(StackPanel panel, string caption, double initial, string tooltip)
        {
            var slider = new Slider
            {
                Minimum = 1,
                Maximum = 90,
                SmallChange = 1,
                LargeChange = 5,
                Value = Math.Clamp(initial, 1, 90),
            };
            var value = SettingRow.Readout($"{slider.Value:0}%");
            slider.ValueChanged += (_, e) => { value.Text = $"{e.NewValue:0}%"; Raise(); };
            panel.Children.Add(SettingRow.Build(caption, slider, value, topMargin: 12, tooltip: tooltip));
            return slider;
        }

        private FrameworkElement BuildColours(ConcentricConfig initial, IReadOnlyList<string> shownDrives)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 12, 6, 6) };

            // Label text colour.
            panel.Children.Add(SubHeading("Label text", 0));
            _textRow = AddColorRow(panel, "Text", ColorUtil.Parse(initial.TextColor, Colors.White),
                "The colour of the text inside each label chip (\"C 90%\").");

            // Status colours for the label chips (independent of the drive set).
            panel.Children.Add(SubHeading("Chip status", 12));
            _healthyRow = AddColorRow(panel, "Healthy", ColorUtil.Parse(initial.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50)),
                "The chip colour for a drive with more free space than the low threshold.");
            _warningRow = AddColorRow(panel, "Low", ColorUtil.Parse(initial.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00)),
                "The chip colour for a drive below the low threshold.");
            _criticalRow = AddColorRow(panel, "Critical", ColorUtil.Parse(initial.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36)),
                "The chip colour for a drive below the critical threshold.");

            // Per-drive ring colours.
            panel.Children.Add(SubHeading("Drive ring colours", 12));
            if (shownDrives.Count == 0)
            {
                panel.Children.Add(new TextBlock { Text = "No drives selected.", Opacity = 0.6, FontSize = 12 });
            }
            else
            {
                for (int i = 0; i < shownDrives.Count; i++)
                {
                    var path = shownDrives[i];
                    var seed = ColorUtil.Parse(ConcentricPalette.ColorFor(initial, path, i), Colors.Gray);
                    var row = new ColorRow
                    {
                        Label = path.TrimEnd('\\'),
                        Color = seed,
                        ToolTip = $"The ring colour for {path.TrimEnd('\\')} — the used part of its "
                            + "arc. The status colours above apply to its label chip, not the ring.",
                    };
                    row.ColorChanged += _ => Raise();
                    _driveRows[path] = row;
                    panel.Children.Add(row);
                }
            }

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
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
