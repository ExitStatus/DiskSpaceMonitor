using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets.Concentric
{
    /// <summary>Registry entry for the concentric-circles widget: one window showing every drive.</summary>
    public sealed class ConcentricWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string Id => "Concentric";

        public string DisplayName => "Concentric circles";

        public bool ShowsAllDrives => true;

        public IWidgetView CreateView() => new ConcentricView();

        public IWidgetConfig DefaultConfig() => new ConcentricConfig();

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return new ConcentricConfig();

            try
            {
                return json.Deserialize<ConcentricConfig>(Options) ?? new ConcentricConfig();
            }
            catch (JsonException)
            {
                return new ConcentricConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((ConcentricConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged,
            IReadOnlyList<string> shownDrives)
            => new ConcentricConfigEditor((ConcentricConfig)initial, onChanged, shownDrives);
    }
}
