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

        /// <summary>Apply the app-wide font and text size bounds. Every view honours these, so it is
        /// required rather than optional: text that ignored them would be the one thing on screen
        /// not matching the rest. Called before <see cref="Apply"/> on load and on every change.</summary>
        void ApplyTypography(WidgetTypography typography);

        /// <summary>
        /// How the user sizes this widget. False (the default) keeps the window square: one size
        /// drives both dimensions. True lets the user set the width and the height independently —
        /// with a handle on each side as well as each corner — and the content stretches to fill
        /// whatever rectangle they choose, as both bar graphs do.
        /// </summary>
        bool ResizesFreely => false;
    }
}
