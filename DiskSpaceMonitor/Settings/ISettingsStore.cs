namespace DiskSpaceMonitor.Settings
{
    /// <summary>Loads and persists <see cref="WidgetSettings"/>.</summary>
    public interface ISettingsStore
    {
        /// <summary>Load settings (migrating any legacy file), or defaults if none/unreadable.</summary>
        WidgetSettings Load();

        /// <summary>Persist the given settings (best-effort).</summary>
        void Save(WidgetSettings settings);
    }
}
