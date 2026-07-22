using System.Collections.Generic;
using System.Linq;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// The set of availables. Constructed explicitly in the composition root
    /// (no reflection). Registration order is the dropdown order; an unknown or null id
    /// falls back to the default widget so a settings file naming a widget this build lacks
    /// degrades gracefully instead of crashing.
    /// </summary>
    public sealed class WidgetRegistry
    {
        /// <summary>Id of the widget used as the fallback and first-run default.</summary>
        public const string DefaultWidgetId = "Circular";

        private readonly List<IWidget> _all;
        private readonly Dictionary<string, IWidget> _byId;

        public WidgetRegistry(params IWidget[] factories)
        {
            _all = factories.ToList();
            _byId = _all.ToDictionary(f => f.Id);
        }

        /// <summary>All widgets in registration order (for the dropdown).</summary>
        public IReadOnlyList<IWidget> All => _all;

        /// <summary>True if a widget with this id is registered.</summary>
        public bool Contains(string id) => _byId.ContainsKey(id);

        /// <summary>The widget for this id, or the default widget if the id is null/unknown.</summary>
        public IWidget Get(string? id)
        {
            if (id != null && _byId.TryGetValue(id, out var factory))
                return factory;

            return _byId[DefaultWidgetId];
        }
    }
}
