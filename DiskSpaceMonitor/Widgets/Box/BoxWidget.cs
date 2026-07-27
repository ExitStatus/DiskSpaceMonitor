using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace DiskSpaceMonitor.Widgets.Box
{
    /// <summary>Registry entry for the box widget.</summary>
    public sealed class BoxWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string Id => "Box";

        public string DisplayName => "Box";

        public bool ShowsAllDrives => false;

        /// <summary>A box holds two lines of text above a bar, so it wants to open wide and short
        /// rather than as the square every other per-drive style starts at.</summary>
        public Size? DefaultWindowSize => new(260, 110);

        public IWidgetView CreateView() => new BoxView();

        public IWidgetConfig DefaultConfig() => new BoxConfig();

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return new BoxConfig();

            // Missing fields keep their POCO defaults; a corrupt blob falls back to defaults.
            try
            {
                return json.Deserialize<BoxConfig>(Options) ?? new BoxConfig();
            }
            catch (JsonException)
            {
                return new BoxConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((BoxConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
            => new BoxConfigEditor((BoxConfig)initial, onChanged);   // single-drive: shownDrives unused
    }
}
