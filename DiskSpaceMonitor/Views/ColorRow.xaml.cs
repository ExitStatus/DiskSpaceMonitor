using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>A compact colour editor: label + swatch + R/G/B sliders.</summary>
    public partial class ColorRow : UserControl
    {
        private bool _suspend;

        /// <summary>Raised (only on user edits) with the new colour.</summary>
        public event Action<Color>? ColorChanged;

        public ColorRow()
        {
            InitializeComponent();
        }

        /// <summary>The row's caption (settable from XAML).</summary>
        public string Label
        {
            set => LabelText.Text = value;
        }

        public Color Color
        {
            get => Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);
            set
            {
                _suspend = true;
                RSlider.Value = value.R;
                GSlider.Value = value.G;
                BSlider.Value = value.B;
                _suspend = false;
                UpdateSwatch();
            }
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SwatchBrush == null)
                return; // during InitializeComponent

            UpdateSwatch();
            if (!_suspend)
                ColorChanged?.Invoke(Color);
        }

        private void UpdateSwatch() => SwatchBrush.Color = Color;
    }
}
