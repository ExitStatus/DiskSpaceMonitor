using System;
using System.Collections.Generic;
using DiskSpaceMonitor.Widgets.Effects;

namespace DiskSpaceMonitor.Widgets.Circular
{
    /// <summary>
    /// Settings editor for the circular widget: contributes "Appearance", "Colours" and "Effects"
    /// tabs, and reports every change through the <c>onChanged</c> callback for live preview.
    /// </summary>
    public sealed class CircularConfigEditor : IWidgetConfigEditor
    {
        private readonly CircularAppearancePanel _appearance = new();
        private readonly CircularColoursPanel _colours = new();
        private readonly GlowEffectEditor _glow;
        private readonly IReadOnlyList<WidgetConfigTab> _tabs;

        public CircularConfigEditor(CircularConfig initial, Action onChanged)
        {
            _appearance.Load(initial);
            _colours.Load(initial);
            _glow = new GlowEffectEditor(initial.Glow, onChanged);
            _appearance.Changed += onChanged;
            _colours.Changed += onChanged;

            _tabs = new[]
            {
                new WidgetConfigTab("Appearance", _appearance),
                new WidgetConfigTab("Colours", _colours),
                new WidgetConfigTab("Effects", _glow.View),
            };
        }

        public IReadOnlyList<WidgetConfigTab> Tabs => _tabs;

        public IWidgetConfig CurrentConfig()
        {
            var c = new CircularConfig();
            _appearance.ApplyTo(c);
            _colours.ApplyTo(c);
            c.Glow = _glow.Current();
            return c;
        }
    }
}
