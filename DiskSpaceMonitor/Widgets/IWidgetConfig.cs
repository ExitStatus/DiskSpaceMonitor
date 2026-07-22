namespace DiskSpaceMonitor.Widgets
{
    /// <summary>
    /// Marker for a widget's configuration POCO. Each widget defines its own
    /// concrete type; the settings/persistence layer never inspects it — the owning
    /// <see cref="IWidget"/> is the only thing that reads or writes it.
    /// </summary>
    public interface IWidgetConfig
    {
    }
}
