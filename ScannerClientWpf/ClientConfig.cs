namespace ScannerClientWpf;

public sealed class ClientConfig
{
    public string LastServerUrl { get; set; } = "http://127.0.0.1:5155";
    public int LastDiscoveryPort { get; set; } = 5155;
    public string LastScannerId { get; set; } = string.Empty;
    public string LastDestinationFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public int LastDpi { get; set; } = 200;
    public string LastColorMode { get; set; } = "Color";
    public string LastOutputFormat { get; set; } = "PDF";
}
