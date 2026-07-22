using System;
using System.Windows;
using System.Windows.Controls;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>The "Appearance" tab for the circular widget: background opacity, ring thickness,
    /// and the low/critical free-space thresholds.</summary>
    public partial class CircularAppearancePanel : UserControl
    {
        private bool _ready;

        /// <summary>Raised when the user changes any value.</summary>
        public event Action? Changed;

        public CircularAppearancePanel()
        {
            InitializeComponent();
        }

        public void Load(CircularConfig c)
        {
            _ready = false;
            BackgroundSlider.Value = Clamp(c.BackgroundOpacity, BackgroundSlider);
            ThicknessSlider.Value = Clamp(c.RingThickness, ThicknessSlider);
            LowThresholdSlider.Value = Clamp(c.LowThresholdPercent, LowThresholdSlider);
            CriticalThresholdSlider.Value = Clamp(c.CriticalThresholdPercent, CriticalThresholdSlider);
            _ready = true;
        }

        public void ApplyTo(CircularConfig c)
        {
            c.BackgroundOpacity = BackgroundSlider.Value;
            c.RingThickness = ThicknessSlider.Value;
            c.LowThresholdPercent = LowThresholdSlider.Value;
            c.CriticalThresholdPercent = CriticalThresholdSlider.Value;
        }

        private static double Clamp(double value, Slider slider)
            => Math.Clamp(value, slider.Minimum, slider.Maximum);

        private void Raise()
        {
            if (_ready)
                Changed?.Invoke();
        }

        private void OnBackgroundChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BackgroundValue != null)
                BackgroundValue.Text = $"{e.NewValue * 100:0}%";
            Raise();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThicknessValue != null)
                ThicknessValue.Text = $"{e.NewValue:0}";
            Raise();
        }

        private void OnLowThresholdChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LowThresholdValue != null)
                LowThresholdValue.Text = $"{e.NewValue:0}%";

            // "Low" must never sit below "critical" — nudge critical down to match.
            if (CriticalThresholdSlider != null && CriticalThresholdSlider.Value > e.NewValue)
                CriticalThresholdSlider.Value = e.NewValue;

            Raise();
        }

        private void OnCriticalThresholdChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CriticalThresholdValue != null)
                CriticalThresholdValue.Text = $"{e.NewValue:0}%";

            // "Critical" must never sit above "low" — nudge low up to match.
            if (LowThresholdSlider != null && LowThresholdSlider.Value < e.NewValue)
                LowThresholdSlider.Value = e.NewValue;

            Raise();
        }
    }
}
