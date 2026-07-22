using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DiskSpaceMonitor.Settings
{
    /// <summary>Persisted state: which drives are shown, how often they refresh, and the single
    /// widget style + its configuration shared by every instance.</summary>
    public sealed class WidgetSettings
    {
        /// <summary>One entry per displayed drive instance.</summary>
        public List<DriveWidgetConfig> Drives { get; set; } = new();

        /// <summary>How often the disk figures refresh, in seconds.</summary>
        public int RefreshSeconds { get; set; } = 5;

        /// <summary>Id of the widget style shown by every instance, e.g. "Circular".</summary>
        public string Style { get; set; } = "Circular";

        /// <summary>Overall opacity of the rendered widget (0.2–1). Shared by every instance.</summary>
        public double WidgetOpacity { get; set; } = 1.0;

        /// <summary>Opaque configuration blob per widget style, keyed by widget id. Every style
        /// keeps its own configuration, so switching styles or restarting never loses it.</summary>
        public Dictionary<string, JsonObject> StyleConfigs { get; set; } = new();

        /// <summary>The stored config for a style, or null if it has none yet.</summary>
        public JsonObject? GetStyleConfig(string styleId)
            => StyleConfigs.TryGetValue(styleId, out var cfg) ? cfg : null;

        /// <summary>Store (or, when null, clear) a style's config.</summary>
        public void SetStyleConfig(string styleId, JsonObject? config)
        {
            if (config != null)
                StyleConfigs[styleId] = config;
            else
                StyleConfigs.Remove(styleId);
        }

        /// <summary>Placement/size of the single window used by a multi-drive widget (e.g. Concentric).
        /// Null until first used. Kept separate from <see cref="Drives"/> so per-drive gauge positions
        /// are preserved when switching widgets.</summary>
        public DriveWidgetConfig? SingleInstance { get; set; }

        // --- Legacy single-style config (v1.1.0, one blob for the active style). Migrated into
        //     StyleConfigs on load. ---
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonObject? StyleConfig { get; set; }

        // --- Legacy single-widget fields (pre multi-drive). Migrated on load. ---
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Left { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Top { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Size { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DrivePath { get; set; }

        // --- Legacy global appearance (pre v1.1). Folded into StyleConfig on load. WidgetOpacity
        //     is not legacy: it was global before and stays a top-level field, so it loads directly. ---
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? BackgroundOpacity { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? RingThickness { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? LowThresholdPercent { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? CriticalThresholdPercent { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BackgroundColor { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TrackColor { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HealthyColor { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WarningColor { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CriticalColor { get; set; }
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextColor { get; set; }

        /// <summary>Fold legacy files into the current shape: pre-multi-drive single widget into
        /// <see cref="Drives"/>, and pre-v1.1 global appearance into the shared Circular config.</summary>
        public void Migrate()
        {
            // 1. Pre-multi-drive single widget -> Drives list.
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

            // 2. Pre-v1.1 global appearance -> the shared Circular config. A pre-v1.1 file always
            //    wrote every appearance field, so any being present marks it as legacy.
            bool legacyAppearance = BackgroundColor != null || BackgroundOpacity.HasValue;
            if (legacyAppearance && StyleConfig == null && !StyleConfigs.ContainsKey("Circular"))
            {
                Style = "Circular";
                StyleConfigs["Circular"] = BuildLegacyCircularBlob();
                // WidgetOpacity was already a top-level global; it loaded directly.
            }

            // Drop the legacy appearance globals so they are never re-persisted.
            BackgroundOpacity = RingThickness = null;
            LowThresholdPercent = CriticalThresholdPercent = null;
            BackgroundColor = TrackColor = HealthyColor = null;
            WarningColor = CriticalColor = TextColor = null;

            // 3. v1.1.0 single StyleConfig -> the per-style StyleConfigs map (keyed by the active
            //    style), then drop the legacy field so it is never re-persisted.
            if (StyleConfig != null && !StyleConfigs.ContainsKey(Style))
                StyleConfigs[Style] = StyleConfig;
            StyleConfig = null;
        }

        // Keys match CircularConfig property names; values come from the legacy globals with the
        // same defaults CircularConfig uses.
        private JsonObject BuildLegacyCircularBlob() => new()
        {
            ["BackgroundOpacity"] = BackgroundOpacity ?? 0.7,
            ["RingThickness"] = RingThickness ?? 16,
            ["LowThresholdPercent"] = LowThresholdPercent ?? 40,
            ["CriticalThresholdPercent"] = CriticalThresholdPercent ?? 15,
            ["BackgroundColor"] = BackgroundColor ?? "#161A20",
            ["TrackColor"] = TrackColor ?? "#6E7686",
            ["HealthyColor"] = HealthyColor ?? "#4CAF50",
            ["WarningColor"] = WarningColor ?? "#FFB300",
            ["CriticalColor"] = CriticalColor ?? "#F44336",
            ["TextColor"] = TextColor ?? "#FFFFFF",
        };
    }
}
