using System.IO;
using System.Text.Json;

namespace ScannerClientWpf.Services;

public sealed class ClientConfigService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PScannerClient");
    private string PathName => Path.Combine(_folder, "config.json");

    public ClientConfig Load()
    {
        Directory.CreateDirectory(_folder);
        if (!File.Exists(PathName))
        {
            var config = new ClientConfig();
            Save(config);
            return config;
        }

        return JsonSerializer.Deserialize<ClientConfig>(File.ReadAllText(PathName), Options) ?? new ClientConfig();
    }

    public void Save(ClientConfig config)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(PathName, JsonSerializer.Serialize(config, Options));
    }
}
