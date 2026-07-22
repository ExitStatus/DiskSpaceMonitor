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
        private ColorRow _textRow = null!;
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
                TextColor = ColorUtil.ToHex(_textRow.Color),
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

            panel.Children.Add(new TextBlock { Text = "Ring thickness", FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _thickness = new Slider
            {
                Minimum = 2,
                Maximum = 40,
                SmallChange = 1,
                LargeChange = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Clamp(initial.RingThickness, 2, 40),
            };
            var thicknessValue = new TextBlock
            {
                Width = 44,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{_thickness.Value:0}",
            };
            _thickness.ValueChanged += (_, e) => { thicknessValue.Text = $"{e.NewValue:0}"; Raise(); };
            Grid.SetColumn(_thickness, 0);
            Grid.SetColumn(thicknessValue, 1);
            grid.Children.Add(_thickness);
            grid.Children.Add(thicknessValue);
            panel.Children.Add(grid);

            panel.Children.Add(new TextBlock { Text = "Unused space transparency", FontSize = 13, Margin = new Thickness(0, 16, 0, 4) });
            var trackGrid = new Grid();
            trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _trackOpacity = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                SmallChange = 0.05,
                LargeChange = 0.1,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Clamp(initial.TrackOpacity, 0, 1),
            };
            var trackValue = new TextBlock
            {
                Width = 44,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{_trackOpacity.Value * 100:0}%",
            };
            _trackOpacity.ValueChanged += (_, e) => { trackValue.Text = $"{e.NewValue * 100:0}%"; Raise(); };
            Grid.SetColumn(_trackOpacity, 0);
            Grid.SetColumn(trackValue, 1);
            trackGrid.Children.Add(_trackOpacity);
            trackGrid.Children.Add(trackValue);
            panel.Children.Add(trackGrid);

            panel.Children.Add(new TextBlock { Text = "Label text", FontSize = 13, Margin = new Thickness(0, 16, 0, 4) });
            _textRow = new ColorRow { Label = "Text", Color = ColorUtil.Parse(initial.TextColor, Colors.White) };
            _textRow.ColorChanged += _ => Raise();
            panel.Children.Add(_textRow);

            return panel;
        }

        private FrameworkElement BuildColours(ConcentricConfig initial, IReadOnlyList<string> shownDrives)
        {
            var panel = new StackPanel { Margin = new Thickness(6, 12, 6, 6) };

            if (shownDrives.Count == 0)
            {
                panel.Children.Add(new TextBlock { Text = "No drives selected.", Opacity = 0.6, FontSize = 12 });
            }
            else
            {
                panel.Children.Add(BuildRgbHeader());
                for (int i = 0; i < shownDrives.Count; i++)
                {
                    var path = shownDrives[i];
                    var seed = ColorUtil.Parse(ConcentricPalette.ColorFor(initial, path, i), Colors.Gray);
                    var row = new ColorRow { Label = path.TrimEnd('\\'), Color = seed };
                    row.ColorChanged += _ => Raise();
                    _driveRows[path] = row;
                    panel.Children.Add(row);
                }
            }

            return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        }

        // Column headers aligned with the R/G/B sliders in each ColorRow (90 / 26 / * / * / *).
        private static Grid BuildRgbHeader()
        {
            var header = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void Head(int col, string text)
            {
                var tb = new TextBlock { Text = text, TextAlignment = TextAlignment.Center, FontSize = 11, Opacity = 0.7 };
                Grid.SetColumn(tb, col);
                header.Children.Add(tb);
            }

            Head(2, "Red");
            Head(3, "Green");
            Head(4, "Blue");
            return header;
        }
    }
}
