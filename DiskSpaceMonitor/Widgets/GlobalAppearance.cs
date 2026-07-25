namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// The appearance settings shared by every widget, whatever style is showing: how solid the
    /// rendered widget is, and the text it draws with. Passed around as one value so adding another
    /// app-wide setting doesn't mean threading a new parameter through the preview path again.
    /// </summary>
    /// <param name="Opacity">Overall opacity of the rendered widget (0.2–1).</param>
    /// <param name="Typography">The font and text size bounds every widget honours.</param>
    public sealed record GlobalAppearance(double Opacity, WidgetTypography Typography)
    {
        public static GlobalAppearance Default { get; } = new(1.0, WidgetTypography.Default);
    }
}
