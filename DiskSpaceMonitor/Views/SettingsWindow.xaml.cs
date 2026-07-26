using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiskSpaceMonitor.Drives;
using DiskSpaceMonitor.Widgets;

namespace DiskSpaceMonitor.Views
{
    public partial class SettingsWindow : ThemedWindow
    {
        private readonly List<CheckBox> _boxes = new();
        private readonly WidgetRegistry _registry;
        private readonly Action<string, IWidgetConfig, GlobalAppearance> _preview;
        private readonly Func<string, IWidgetConfig> _configFor;   // a style's saved config (default if none)
        private readonly Dictionary<string, IWidgetConfig> _sessionConfigs = new();  // in-dialog edits per style
        private readonly IReadOnlyList<string> _shownDrives;   // snapshot at open; per-drive editors use it

        private string _widgetId;
        private string _fontFamily;
        private IWidgetConfig _config;
        private IWidgetConfigEditor? _editor;
        private readonly List<TabItem> _widgetTabs = new();   // tabs contributed by the current widget
        private bool _ready;

        /// <summary>User pressed OK; results are valid.</summary>
        public bool Applied { get; private set; }

        /// <summary>User pressed Exit Application.</summary>
        public bool ExitRequested { get; private set; }

        public List<string> SelectedDrivePaths { get; } = new();

        /// <summary>Chosen refresh interval in seconds (valid once Applied).</summary>
        public int RefreshSeconds { get; private set; }

        /// <summary>Whether auto-start at login should be enabled (valid once Applied).</summary>
        public bool AutoStart { get; private set; }

        /// <summary>Chosen widget id (valid once Applied).</summary>
        public string SelectedWidget { get; private set; }

        /// <summary>Chosen widget config (valid once Applied).</summary>
        public IWidgetConfig SelectedConfig { get; private set; }

        /// <summary>Chosen app-wide appearance — opacity and typography (valid once Applied).</summary>
        public GlobalAppearance SelectedAppearance { get; private set; }

        private static readonly (string Label, int Seconds)[] IntervalPresets =
        {
            ("1 second", 1),
            ("2 seconds", 2),
            ("5 seconds", 5),
            ("10 seconds", 10),
            ("30 seconds", 30),
            ("1 minute", 60),
            ("5 minutes", 300),
        };

        public SettingsWindow(IReadOnlyList<string> shownPaths, int refreshSeconds, bool autoStart,
            string widgetId, IWidgetConfig config, GlobalAppearance appearance, IDriveCatalog catalog,
            WidgetRegistry registry, Action<string, IWidgetConfig, GlobalAppearance> preview,
            Func<string, IWidgetConfig> configFor)
        {
            InitializeComponent();
            _registry = registry;
            _preview = preview;
            _configFor = configFor;
            _shownDrives = shownPaths.ToList();
            _widgetId = widgetId;
            _config = config;
            _fontFamily = appearance.Typography.FamilyName;
            SelectedWidget = widgetId;
            SelectedConfig = config;
            SelectedAppearance = appearance;

            AutoStartCheck.IsChecked = autoStart;

            foreach (var drive in catalog.GetAvailableDrives())
                AddCheckBox(drive.Path, drive.Label, shownPaths.Contains(drive.Path));

            // Include any shown drive that isn't currently ready, so it isn't silently dropped.
            var listed = _boxes.Select(b => (string)b.Tag).ToHashSet();
            foreach (var path in shownPaths.Where(p => !string.IsNullOrEmpty(p) && !listed.Contains(p)))
                AddCheckBox(path, $"{path}   (offline)", isChecked: true);

            PopulateIntervals(refreshSeconds);

            foreach (var factory in registry.All)
                WidgetSelector.Items.Add(new ComboBoxItem { Content = factory.DisplayName, Tag = factory.Id });
            SelectComboByTag(WidgetSelector, widgetId);

            OpacitySlider.Value = Math.Clamp(appearance.Opacity, OpacitySlider.Minimum, OpacitySlider.Maximum);

            var type = appearance.Typography;
            MinFontSlider.Value = Math.Clamp(type.MinSize, MinFontSlider.Minimum, MinFontSlider.Maximum);
            MaxFontSlider.Value = Math.Clamp(type.MaxSize, MaxFontSlider.Minimum, MaxFontSlider.Maximum);
            UpdateFontPreview();
            UpdateFontValues();

            BuildWidgetTabs();
            UpdateGuards();
            _ready = true;
        }

        // --- Widget selection + config tabs ----------------------------------

        private void OnWidgetSelected(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || WidgetSelector.SelectedItem is not ComboBoxItem item)
                return;

            var id = (string)item.Tag;
            if (id == _widgetId)
                return;

            // Remember any edits to the style we're leaving, so switching back restores them.
            if (_editor != null)
                _sessionConfigs[_widgetId] = _editor.CurrentConfig();

            _widgetId = id;
            _config = _sessionConfigs.TryGetValue(id, out var cached) ? cached : _configFor(id);
            BuildWidgetTabs();
            Preview();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValue != null)
                OpacityValue.Text = $"{e.NewValue * 100:0}%";
            Preview();
        }

        // --- Typography (global) ---------------------------------------------

        private void OnChooseFont(object sender, RoutedEventArgs e)
        {
            string original = _fontFamily;
            var dialog = new FontPickerDialog(_fontFamily) { Owner = this };

            // Preview each font as it's picked, so the choice is judged on the widget itself.
            dialog.LivePreview += family =>
            {
                _fontFamily = family;
                UpdateFontPreview();
                Preview();
            };

            if (dialog.ShowDialog() == true)
                _fontFamily = dialog.SelectedFamily;
            else
                _fontFamily = original;   // cancelled: put back what it was, preview and all

            UpdateFontPreview();
            Preview();
        }

        private void OnMinFontChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // The two bounds can't cross; push the other one along rather than refusing the drag.
            if (MaxFontSlider != null && e.NewValue > MaxFontSlider.Value)
                MaxFontSlider.Value = e.NewValue;

            UpdateFontValues();
            Preview();
        }

        private void OnMaxFontChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinFontSlider != null && e.NewValue < MinFontSlider.Value)
                MinFontSlider.Value = e.NewValue;

            UpdateFontValues();
            Preview();
        }

        private void UpdateFontPreview()
        {
            FontPreview.Text = _fontFamily;
            FontPreview.FontFamily = new System.Windows.Media.FontFamily(_fontFamily);
        }

        private void UpdateFontValues()
        {
            if (MinFontValue != null)
                MinFontValue.Text = $"{MinFontSlider.Value:0} pt";
            if (MaxFontValue != null)
                MaxFontValue.Text = $"{MaxFontSlider.Value:0} pt";
        }

        /// <summary>The app-wide appearance as the dialog currently has it.</summary>
        private GlobalAppearance CurrentAppearance() => new(
            OpacitySlider.Value,
            new WidgetTypography(_fontFamily, MinFontSlider.Value, MaxFontSlider.Value));

        private void BuildWidgetTabs()
        {
            foreach (var tab in _widgetTabs)
                Tabs.Items.Remove(tab);
            _widgetTabs.Clear();

            _editor = _registry.Get(_widgetId).CreateEditor(_config, OnEditorChanged, _shownDrives);
            foreach (var tab in _editor.Tabs)
            {
                var item = new TabItem { Header = tab.Header, Content = tab.Content };
                Tabs.Items.Add(item);
                _widgetTabs.Add(item);
            }
        }

        private void OnEditorChanged()
        {
            if (_editor == null)
                return;

            _config = _editor.CurrentConfig();
            Preview();
        }

        private void Preview()
        {
            if (_ready)
                _preview(_widgetId, _config, CurrentAppearance());
        }

        private static void SelectComboByTag(ComboBox combo, string tag)
        {
            foreach (var item in combo.Items.OfType<ComboBoxItem>())
            {
                if ((string)item.Tag == tag)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        // --- Drives + interval (global) --------------------------------------

        private void AddCheckBox(string path, string label, bool isChecked)
        {
            var box = new CheckBox
            {
                Content = label,
                Tag = path,
                IsChecked = isChecked,
                Margin = new Thickness(0, 5, 0, 5),
                FontSize = 13,
                ToolTip = $"Show {path} in the widgets. The Circular style gives it a window of its "
                    + "own; the other styles add it to their single window.",
            };
            box.Checked += (_, _) => UpdateGuards();
            box.Unchecked += (_, _) => UpdateGuards();

            _boxes.Add(box);
            DriveList.Children.Add(box);
        }

        private void PopulateIntervals(int currentSeconds)
        {
            var presets = IntervalPresets.ToList();
            if (presets.All(p => p.Seconds != currentSeconds))
                presets.Add(($"{currentSeconds} seconds", currentSeconds));

            foreach (var (label, seconds) in presets.OrderBy(p => p.Seconds))
            {
                var item = new ComboBoxItem { Content = label, Tag = seconds };
                IntervalCombo.Items.Add(item);
                if (seconds == currentSeconds)
                    IntervalCombo.SelectedItem = item;
            }
        }

        /// <summary>Enforce "at least one drive": lock the last remaining checkbox.</summary>
        private void UpdateGuards()
        {
            var checkedBoxes = _boxes.Where(b => b.IsChecked == true).ToList();

            foreach (var box in _boxes)
                box.IsEnabled = true;

            if (checkedBoxes.Count <= 1)
            {
                foreach (var box in checkedBoxes)
                    box.IsEnabled = false; // can't uncheck the only one left
            }

            OkButton.IsEnabled = checkedBoxes.Count >= 1;
        }

        // --- Buttons ---------------------------------------------------------

        private void OnOk(object sender, RoutedEventArgs e)
        {
            SelectedDrivePaths.Clear();
            SelectedDrivePaths.AddRange(_boxes.Where(b => b.IsChecked == true).Select(b => (string)b.Tag));
            RefreshSeconds = (int)((ComboBoxItem)IntervalCombo.SelectedItem).Tag;
            AutoStart = AutoStartCheck.IsChecked == true;

            SelectedWidget = _widgetId;
            SelectedConfig = _editor != null ? _editor.CurrentConfig() : _config;
            SelectedAppearance = CurrentAppearance();

            Applied = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e) => Close();

        private void OnExitApplication(object sender, RoutedEventArgs e)
        {
            ExitRequested = true;
            Close();
        }
    }
}
