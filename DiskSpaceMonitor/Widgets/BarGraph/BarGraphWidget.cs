using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets.BarGraph
{
    /// <summary>
    /// Shared registry entry for the bar graph widgets. Both show every drive in one window and
    /// share a config shape and settings editor; a subclass supplies its id, display name, live
    /// view, and the orientations it offers. Each subclass persists its own config blob under its
    /// own id, so the two graphs keep independent settings.
    /// </summary>
    public abstract class BarGraphWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public abstract string Id { get; }

        public abstract string DisplayName { get; }

        public bool ShowsAllDrives => true;

        public abstract IWidgetView CreateView();

        /// <summary>The orientation used when none is stored, or a stored one this widget can't draw.</summary>
        protected abstract BarOrientation DefaultOrientation { get; }

        /// <summary>The orientation choices this widget offers, in dropdown order.</summary>
        protected abstract (string Label, BarOrientation Value)[] Orientations { get; }

        /// <summary>What to call the bar-size slider — bars are sized across their own axis.</summary>
        protected abstract string BarSizeLabel { get; }

        public IWidgetConfig DefaultConfig() => new BarGraphConfig { Orientation = DefaultOrientation };

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return DefaultConfig();

            try
            {
                return Normalise(json.Deserialize<BarGraphConfig>(Options) ?? new BarGraphConfig());
            }
            catch (JsonException)
            {
                return DefaultConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((BarGraphConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
            => new BarGraphConfigEditor((BarGraphConfig)initial, onChanged, Orientations, BarSizeLabel);

        // Both widgets share one config type, and so one Orientation property covering all four
        // directions. A blob written by the other graph (or hand-edited) can therefore name an
        // orientation this one can't draw, so fall back to its own default rather than render
        // sideways.
        private BarGraphConfig Normalise(BarGraphConfig config)
        {
            if (!Orientations.Any(o => o.Value == config.Orientation))
                config.Orientation = DefaultOrientation;

            return config;
        }
    }
}
