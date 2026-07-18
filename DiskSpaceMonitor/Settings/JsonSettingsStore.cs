using System;
using System.IO;
using System.Text.Json;

namespace DiskSpaceMonitor.Settings
{
    /// <summary>
    /// JSON-file <see cref="ISettingsStore"/>. Defaults to
    /// %AppData%\DiskSpaceMonitor\settings.json; the path is injectable for testing.
    /// </summary>
    public sealed class JsonSettingsStore : ISettingsStore
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        private readonly string _filePath;

        public JsonSettingsStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiskSpaceMonitor", "settings.json"))
        {
        }

        public JsonSettingsStore(string filePath) => _filePath = filePath;

        public WidgetSettings Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var loaded = JsonSerializer.Deserialize<WidgetSettings>(json);
                    if (loaded != null)
                    {
                        loaded.Migrate();
                        return loaded;
                    }
                }
            }
            catch
            {
                // Corrupt/unreadable settings fall back to defaults.
            }

            return new WidgetSettings();
        }

        public void Save(WidgetSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, Options));
            }
            catch
            {
                // Best-effort persistence; ignore IO failures.
            }
        }
    }
}
