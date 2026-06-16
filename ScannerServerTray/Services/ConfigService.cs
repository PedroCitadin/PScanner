using System.Text.Json;

namespace ScannerServerTray.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "config.json");

    public AppConfig Load()
    {
        if (!File.Exists(_path))
        {
            var created = new AppConfig();
            Save(created);
            return created;
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
    }

    private void Save(AppConfig config)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(config, Options));
    }
}
