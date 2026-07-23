using System.Collections.Generic;
using System.Windows;
using DiskSpaceMonitor.Drives;

namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// The live visual bound to a widget window. Created by an <see cref="IWidget"/> and
    /// hosted by the widget window.
    /// </summary>
    public interface IWidgetView
    {
        /// <summary>The control that renders the widget. Created once; hosted by the window.</summary>
        FrameworkElement View { get; }

        /// <summary>Push fresh drive readings and re-render. The list is in display order and is
        /// never empty; single-drive views use the first element, multi-drive views use them all.</summary>
        void Update(IReadOnlyList<DriveSpace> drives);

        /// <summary>Apply configuration — used both for the initial load and for live preview.</summary>
        void Apply(IWidgetConfig config);

        /// <summary>Natural width ÷ height of the content; the window sizes itself to match so it
        /// hugs the widget. 1 = square (the default for widgets that don't care).</summary>
        double AspectRatio => 1;
    }
}
