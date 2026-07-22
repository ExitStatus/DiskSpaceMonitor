using System;
using System.Windows.Controls;
using System.Windows.Media;
using DiskSpaceMonitor.Views;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>The "Colours" tab for the circular widget: the six part colours as RGB rows.</summary>
    public partial class CircularColoursPanel : UserControl
    {
        private bool _ready;

        /// <summary>Raised when the user changes any colour.</summary>
        public event Action? Changed;

        public CircularColoursPanel()
        {
            InitializeComponent();

            foreach (var row in new[] { BackgroundRow, TrackRow, HealthyRow, WarningRow, CriticalRow, TextRow })
                row.ColorChanged += _ => Raise();
        }

        public void Load(CircularConfig c)
        {
            _ready = false;
            BackgroundRow.Color = ColorUtil.Parse(c.BackgroundColor, Color.FromRgb(0x16, 0x1A, 0x20));
            TrackRow.Color = ColorUtil.Parse(c.TrackColor, Color.FromRgb(0x6E, 0x76, 0x86));
            HealthyRow.Color = ColorUtil.Parse(c.HealthyColor, Color.FromRgb(0x4C, 0xAF, 0x50));
            WarningRow.Color = ColorUtil.Parse(c.WarningColor, Color.FromRgb(0xFF, 0xB3, 0x00));
            CriticalRow.Color = ColorUtil.Parse(c.CriticalColor, Color.FromRgb(0xF4, 0x43, 0x36));
            TextRow.Color = ColorUtil.Parse(c.TextColor, Colors.White);
            _ready = true;
        }

        public void ApplyTo(CircularConfig c)
        {
            c.BackgroundColor = ColorUtil.ToHex(BackgroundRow.Color);
            c.TrackColor = ColorUtil.ToHex(TrackRow.Color);
            c.HealthyColor = ColorUtil.ToHex(HealthyRow.Color);
            c.WarningColor = ColorUtil.ToHex(WarningRow.Color);
            c.CriticalColor = ColorUtil.ToHex(CriticalRow.Color);
            c.TextColor = ColorUtil.ToHex(TextRow.Color);
        }

        private void Raise()
        {
            if (_ready)
                Changed?.Invoke();
        }
    }
}
