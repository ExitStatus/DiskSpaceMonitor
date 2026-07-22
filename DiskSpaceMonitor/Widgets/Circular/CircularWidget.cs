using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>Registry entry for the circular gauge widget.</summary>
    public sealed class CircularWidget : IWidget
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string Id => "Circular";

        public string DisplayName => "Circular gauge";

        public IWidgetView CreateView() => new CircularView();

        public IWidgetConfig DefaultConfig() => new CircularConfig();

        public IWidgetConfig ReadConfig(JsonNode? json)
        {
            if (json is null)
                return new CircularConfig();

            // Missing fields keep their POCO defaults; a corrupt blob falls back to defaults.
            try
            {
                return json.Deserialize<CircularConfig>(Options) ?? new CircularConfig();
            }
            catch (JsonException)
            {
                return new CircularConfig();
            }
        }

        public JsonNode WriteConfig(IWidgetConfig config)
            => JsonSerializer.SerializeToNode((CircularConfig)config, Options)!;

        public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged)
            => new CircularConfigEditor((CircularConfig)initial, onChanged);
    }
}
