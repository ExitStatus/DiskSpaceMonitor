using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DiskSpaceMonitor.Settings
{
    /// <summary>Persisted state for all widgets: which drives are shown and how often they refresh.</summary>
    public sealed class WidgetSettings
    {
        /// <summary>One entry per displayed drive widget.</summary>
        public List<DriveWidgetConfig> Drives { get; set; } = new();

        /// <summary>How often the disk figures refresh, in seconds.</summary>
        public int RefreshSeconds { get; set; } = 5;

        /// <summary>Opacity of the dark backing disc behind the ring (0 = invisible, 1 = solid).</summary>
        public double BackgroundOpacity { get; set; } = 0.7;

        /// <summary>Opacity of the whole widget window (0.2–1).</summary>
        public double WidgetOpacity { get; set; } = 1.0;

        /// <summary>Stroke thickness of the ring (track + used arc), in design-space px.</summary>
        public double RingThickness { get; set; } = 16;

        /// <summary>Percent of free space below which the ring shows the "low" colour.</summary>
        public double LowThresholdPercent { get; set; } = 40;

        /// <summary>Percent of free space below which the ring shows the "critical" colour.</summary>
        public double CriticalThresholdPercent { get; set; } = 15;

        // --- Part colours (hex "#RRGGBB"). ---
        /// <summary>Dark backing disc.</summary>
        public string BackgroundColor { get; set; } = "#161A20";
        /// <summary>Grey track ring behind the used arc.</summary>
        public string TrackColor { get; set; } = "#6E7686";
        /// <summary>Used arc when there is plenty of free space.</summary>
        public string HealthyColor { get; set; } = "#4CAF50";
        /// <summary>Used arc when free space is getting low.</summary>
        public string WarningColor { get; set; } = "#FFB300";
        /// <summary>Used arc when free space is critically low.</summary>
        public string CriticalColor { get; set; } = "#F44336";
        /// <summary>Centre text.</summary>
        public string TextColor { get; set; } = "#FFFFFF";

        // --- Legacy single-widget fields (pre multi-drive). Migrated on load. ---
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Left { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Top { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Size { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DrivePath { get; set; }

        /// <summary>Fold a pre-multi-drive settings file into the <see cref="Drives"/> list.</summary>
        public void Migrate()
        {
            if (Drives.Count == 0 && (DrivePath != null || Left.HasValue))
            {
                Drives.Add(new DriveWidgetConfig
                {
                    DrivePath = DrivePath ?? "",
                    Left = Left ?? double.NaN,
                    Top = Top ?? double.NaN,
                    Size = Size ?? 200
                });
            }

            Left = Top = Size = null;
            DrivePath = null;
        }
    }
}
