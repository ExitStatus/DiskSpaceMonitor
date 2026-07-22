using System.Windows;
using DiskSpaceMonitor.Drives;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// The live visual bound to a single widget window. Created by an
    /// <see cref="IWidget"/> and hosted by the widget window.
    /// </summary>
    public interface IWidgetView
    {
        /// <summary>The control that renders the widget. Created once; hosted by the window.</summary>
        FrameworkElement View { get; }

        /// <summary>Push a fresh drive reading and re-render.</summary>
        void Update(DriveSpace space);

        /// <summary>Apply configuration — used both for the initial load and for live preview.</summary>
        void Apply(IWidgetConfig config);
    }
}
