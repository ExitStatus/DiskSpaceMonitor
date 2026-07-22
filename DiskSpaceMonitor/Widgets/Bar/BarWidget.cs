using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets.Bar
{
    /// <summary>Registry entry for the bar-graph widget: one window showing a bar per drive.</summary>
    public sealed class BarWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string Id => "Bar";

        public string DisplayName => "Bar graph";

        public bool ShowsAllDrives => true;

        public IWidgetView CreateView() => new BarView();

        public IWidgetConfig DefaultConfig() => new BarConfig();

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return new BarConfig();

            try
            {
                return json.Deserialize<BarConfig>(Options) ?? new BarConfig();
            }
            catch (JsonException)
            {
                return new BarConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((BarConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
            => new BarConfigEditor((BarConfig)initial, onChanged);
    }
}
