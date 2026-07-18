using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiskSpaceMonitor.Drives;

namespace DiskSpaceMonitor.Views
{
    public partial class SettingsWindow : ThemedWindow
    {
        private readonly List<CheckBox> _boxes = new();

        // Apply the full appearance snapshot live to the widgets.
        private readonly Action<AppearancePreview>? _preview;
        private bool _ready;

        /// <summary>User pressed OK; SelectedDrivePaths holds the chosen drives.</summary>
        public bool Applied { get; private set; }

        /// <summary>User pressed Exit Application.</summary>
        public bool ExitRequested { get; private set; }

        public List<string> SelectedDrivePaths { get; } = new();

        /// <summary>Chosen refresh interval in seconds (valid once Applied).</summary>
        public int RefreshSeconds { get; private set; }

        /// <summary>Chosen appearance — opacities + part colours (valid once Applied).</summary>
        public AppearancePreview Appearance { get; private set; } = null!;

        /// <summary>Whether auto-start at login should be enabled (valid once Applied).</summary>
        public bool AutoStart { get; private set; }

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
            AppearancePreview appearance, IDriveCatalog catalog, Action<AppearancePreview> preview)
        {
            InitializeComponent();
            _preview = preview;
            AutoStartCheck.IsChecked = autoStart;

            foreach (var drive in catalog.GetAvailableDrives())
                AddCheckBox(drive.Path, drive.Label, shownPaths.Contains(drive.Path));

            // Include any shown drive that isn't currently ready, so it isn't
            // silently dropped when the dialog is applied.
            var listed = _boxes.Select(b => (string)b.Tag).ToHashSet();
            foreach (var path in shownPaths.Where(p => !string.IsNullOrEmpty(p) && !listed.Contains(p)))
                AddCheckBox(path, $"{path}   (offline)", isChecked: true);

            PopulateIntervals(refreshSeconds);

            BackgroundSlider.Value = System.Math.Clamp(appearance.BackgroundOpacity, BackgroundSlider.Minimum, BackgroundSlider.Maximum);
            OpacitySlider.Value = System.Math.Clamp(appearance.WidgetOpacity, OpacitySlider.Minimum, OpacitySlider.Maximum);
            ThicknessSlider.Value = System.Math.Clamp(appearance.RingThickness, ThicknessSlider.Minimum, ThicknessSlider.Maximum);
            LowThresholdSlider.Value = System.Math.Clamp(appearance.LowThresholdPercent, LowThresholdSlider.Minimum, LowThresholdSlider.Maximum);
            CriticalThresholdSlider.Value = System.Math.Clamp(appearance.CriticalThresholdPercent, CriticalThresholdSlider.Minimum, CriticalThresholdSlider.Maximum);

            BackgroundRow.Color = appearance.Background;
            TrackRow.Color = appearance.Track;
            HealthyRow.Color = appearance.Healthy;
            WarningRow.Color = appearance.Warning;
            CriticalRow.Color = appearance.Critical;
            TextRow.Color = appearance.Text;

            foreach (var row in new[] { BackgroundRow, TrackRow, HealthyRow, WarningRow, CriticalRow, TextRow })
                row.ColorChanged += _ => Preview();

            UpdateGuards();

            // Live preview only takes effect after the initial values are set.
            _ready = true;
        }

        private void OnBackgroundChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BackgroundValue != null)
                BackgroundValue.Text = $"{e.NewValue * 100:0}%";
            Preview();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValue != null)
                OpacityValue.Text = $"{e.NewValue * 100:0}%";
            Preview();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThicknessValue != null)
                ThicknessValue.Text = $"{e.NewValue:0}";
            Preview();
        }

        private void OnLowThresholdChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LowThresholdValue != null)
                LowThresholdValue.Text = $"{e.NewValue:0}%";

            // "Low" must never sit below "critical" — nudge critical down to match.
            if (CriticalThresholdSlider != null && CriticalThresholdSlider.Value > e.NewValue)
                CriticalThresholdSlider.Value = e.NewValue;

            Preview();
        }

        private void OnCriticalThresholdChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CriticalThresholdValue != null)
                CriticalThresholdValue.Text = $"{e.NewValue:0}%";

            // "Critical" must never sit above "low" — nudge low up to match.
            if (LowThresholdSlider != null && LowThresholdSlider.Value < e.NewValue)
                LowThresholdSlider.Value = e.NewValue;

            Preview();
        }

        private AppearancePreview CurrentAppearance() => new(
            BackgroundSlider.Value, OpacitySlider.Value, ThicknessSlider.Value,
            LowThresholdSlider.Value, CriticalThresholdSlider.Value,
            BackgroundRow.Color, TrackRow.Color, HealthyRow.Color,
            WarningRow.Color, CriticalRow.Color, TextRow.Color);

        private void Preview()
        {
            if (_ready)
                _preview?.Invoke(CurrentAppearance());
        }

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

        private void OnOk(object sender, RoutedEventArgs e)
        {
            SelectedDrivePaths.Clear();
            SelectedDrivePaths.AddRange(_boxes.Where(b => b.IsChecked == true).Select(b => (string)b.Tag));
            RefreshSeconds = (int)((ComboBoxItem)IntervalCombo.SelectedItem).Tag;
            Appearance = CurrentAppearance();
            AutoStart = AutoStartCheck.IsChecked == true;
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
