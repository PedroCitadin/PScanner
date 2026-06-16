namespace ScannerServerTray;

public sealed class AppConfig
{
    public int Port { get; set; } = 5155;
    public string TempFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SharedScanner",
        "Temp");
    public string ServerName { get; set; } = Environment.MachineName;
}
