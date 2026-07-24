using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets.VerticalBar
{
    /// <summary>Registry entry for the vertical bar graph widget: one window showing a bar per drive.</summary>
    public sealed class VerticalBarWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string Id => "VerticalBar";

        public string DisplayName => "Vertical bar graph";

        public bool ShowsAllDrives => true;

        public IWidgetView CreateView() => new VerticalBarView();

        public IWidgetConfig DefaultConfig() => new VerticalBarConfig();

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return new VerticalBarConfig();

            try
            {
                return json.Deserialize<VerticalBarConfig>(Options) ?? new VerticalBarConfig();
            }
            catch (JsonException)
            {
                return new VerticalBarConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((VerticalBarConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
            => new VerticalBarConfigEditor((VerticalBarConfig)initial, onChanged);
    }
}
