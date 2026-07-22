using System;
using System.Collections.Generic;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>
    /// Settings editor for the circular widget: contributes an "Appearance" tab and a "Colours"
    /// tab, and reports every change through the <c>onChanged</c> callback for live preview.
    /// </summary>
    public sealed class CircularConfigEditor : IWidgetConfigEditor
    {
        private readonly CircularAppearancePanel _appearance = new();
        private readonly CircularColoursPanel _colours = new();
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        public CircularConfigEditor(CircularConfig initial, Action onChanged)
        {
            _appearance.Load(initial);
            _colours.Load(initial);
            _appearance.Changed += onChanged;
            _colours.Changed += onChanged;

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", _appearance),
                new WidgetConfigTab("Colours", _colours),
            };
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig()
        {
            var c = new CircularConfig();
            _appearance.ApplyTo(c);
            _colours.ApplyTo(c);
            return c;
        }
    }
}
