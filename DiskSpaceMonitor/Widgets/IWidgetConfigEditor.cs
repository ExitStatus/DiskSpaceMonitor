using System.Collections.Generic;
using System.Windows;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>One configuration tab a widget contributes to the settings dialog.</summary>
    public sealed record WidgetConfigTab(string Header, FrameworkElement Content);

    /// <summary>
    /// The configuration UI a widget contributes to the settings dialog. A widget may split its
    /// parameters across several tabs; the dialog hosts them as top-level tabs beside "General".
    /// </summary>
    public interface IWidgetConfigEditor
    {
        /// <summary>The tabs this editor contributes (in order).</summary>
        IReadOnlyList<WidgetConfigTab> Tabs { get; }

        /// <summary>A snapshot of the currently edited configuration.</summary>
        IWidgetConfig CurrentConfig();
    }
}
