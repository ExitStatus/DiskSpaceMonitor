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
        private readonly Action<string, IWidgetConfig, double> _preview;

        private string _widgetId;
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

        /// <summary>Chosen overall widget opacity (valid once Applied).</summary>
        public double WidgetOpacity { get; private set; }

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
            string widgetId, IWidgetConfig config, double widgetOpacity, IDriveCatalog catalog,
            WidgetRegistry registry, Action<string, IWidgetConfig, double> preview)
        {
            InitializeComponent();
            _registry = registry;
            _preview = preview;
            _widgetId = widgetId;
            _config = config;
            SelectedWidget = widgetId;
            SelectedConfig = config;
            WidgetOpacity = widgetOpacity;

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

            OpacitySlider.Value = Math.Clamp(widgetOpacity, OpacitySlider.Minimum, OpacitySlider.Maximum);

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

            _widgetId = id;
            _config = _registry.Get(id).DefaultConfig();
            BuildWidgetTabs();
            Preview();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValue != null)
                OpacityValue.Text = $"{e.NewValue * 100:0}%";
            Preview();
        }

        private void BuildWidgetTabs()
        {
            foreach (var tab in _widgetTabs)
                Tabs.Items.Remove(tab);
            _widgetTabs.Clear();

            _editor = _registry.Get(_widgetId).CreateEditor(_config, OnEditorChanged);
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
                _preview(_widgetId, _config, OpacitySlider.Value);
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
                FontSize = 13
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
            WidgetOpacity = OpacitySlider.Value;

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
