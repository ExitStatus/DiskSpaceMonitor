using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// A compact colour editor: label + swatch + editable "#RRGGBB" text box + a pipette button
    /// that opens the HSB picker. The hex box supports copy/paste; the picker previews live.
    /// </summary>
    public partial class ColorRow : UserControl
    {
        private Color _color = Colors.Gray;
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

        /// <summary>
        /// The current colour. Setting it programmatically updates the UI without raising
        /// <see cref="ColorChanged"/> (so callers can seed a row silently).
        /// </summary>
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                SyncUi();
            }
        }

        // Reflect _color into the swatch and hex box without re-triggering change events.
        private void SyncUi()
        {
            bool prev = _suspend;
            _suspend = true;
            HexBox.Text = ColorUtil.ToHex(_color);
            _suspend = prev;

            if (SwatchBrush != null)
                SwatchBrush.Color = _color;
        }

        private void OnHexChanged(object sender, TextChangedEventArgs e)
        {
            if (_suspend || SwatchBrush == null)
                return;

            if (ColorUtil.TryParse(HexBox.Text, out var c))
            {
                _color = c;
                SwatchBrush.Color = c;
                ColorChanged?.Invoke(c);
            }
        }

        private void OnPipetteClick(object sender, RoutedEventArgs e)
        {
            var original = _color;

            var dialog = new HsvColorPickerDialog(_color) { Owner = Window.GetWindow(this) };
            dialog.LiveColorChanged += Apply;

            bool committed = dialog.ShowDialog() == true;
            if (!committed)
                Apply(original);   // revert the live preview
        }

        // Apply a colour chosen in the picker: update the UI and raise the live change.
        private void Apply(Color c)
        {
            _color = c;
            SyncUi();
            ColorChanged?.Invoke(c);
        }
    }
}
