using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// A hue/saturation/brightness colour chooser with gradient slider tracks and a live preview.
    /// Raises <see cref="LiveColorChanged"/> on every adjustment so the owning row (and the widget
    /// behind it) can update in real time; the caller reverts on Cancel.
    /// </summary>
    public partial class HsvColorPickerDialog : ThemedWindow
    {
        private double _h;   // 0..360
        private double _s;   // 0..1
        private double _v;   // 0..1
        private bool _suspend = true;

        /// <summary>Raised with the new colour as the user drags any slider.</summary>
        public event Action<Color>? LiveColorChanged;

        /// <summary>The colour currently described by the sliders.</summary>
        public Color SelectedColor { get; private set; }

        public HsvColorPickerDialog(Color initial)
        {
            InitializeComponent();

            SelectedColor = initial;
            (_h, _s, _v) = HsvColor.FromRgb(initial);

            HueSlider.Value = _h;
            SatSlider.Value = _s * 100;
            BrightSlider.Value = _v * 100;

            Recompute(raise: false);   // paint preview, gradients and readouts
            _suspend = false;          // now live
        }

        private void OnHueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suspend) return;
            _h = e.NewValue;
            Recompute();
        }

        private void OnSatChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suspend) return;
            _s = e.NewValue / 100.0;
            Recompute();
        }

        private void OnBrightChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suspend) return;
            _v = e.NewValue / 100.0;
            Recompute();
        }

        private void Recompute(bool raise = true)
        {
            var c = HsvColor.ToRgb(_h, _s, _v);
            SelectedColor = c;

            PreviewBrush.Color = c;
            HueValue.Text = $"{_h:0}";
            SatValue.Text = $"{_s * 100:0.0}";
            BrightValue.Text = $"{_v * 100:0.0}";

            // The saturation/brightness tracks depend on the other two components, so refresh them.
            SatSlider.Background = Horizontal(HsvColor.ToRgb(_h, 0, _v), HsvColor.ToRgb(_h, 1, _v));
            BrightSlider.Background = Horizontal(HsvColor.ToRgb(_h, _s, 0), HsvColor.ToRgb(_h, _s, 1));

            if (raise)
                LiveColorChanged?.Invoke(c);
        }

        private static LinearGradientBrush Horizontal(Color a, Color b) => new()
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            GradientStops = { new GradientStop(a, 0), new GradientStop(b, 1) },
        };

        private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
