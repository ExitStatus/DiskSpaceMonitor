using System;
using System.Text.Json.Nodes;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// A registered widget: display metadata, a factory for the live view, a UI-free
    /// codec for its config, and a factory for its settings-dialog editor.
    /// </summary>
    public interface IWidget
    {
        /// <summary>Stable, persisted identifier (e.g. "Circular").</summary>
        string Id { get; }

        /// <summary>Human-readable label shown in the widget dropdown.</summary>
        string DisplayName { get; }

        /// <summary>Create a fresh live view for a widget window.</summary>
        IWidgetView CreateView();

        // --- Config codec (UI-free, so it is unit-testable) ---

        /// <summary>The default config for a newly created widget.</summary>
        IWidgetConfig DefaultConfig();

        /// <summary>Read config from a persisted blob; null or missing fields fall back to defaults.</summary>
        IWidgetConfig ReadConfig(JsonNode? json);

        /// <summary>Serialise config to a blob for persistence.</summary>
        JsonNode WriteConfig(IWidgetConfig config);

        // --- Settings-dialog contribution (WPF) ---

        /// <summary>Create the editor UI, seeded from <paramref name="initial"/>; invoke
        /// <paramref name="onChanged"/> whenever the user edits a value (for live preview).</summary>
        IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged);
    }
}
