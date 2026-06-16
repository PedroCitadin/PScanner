namespace ScannerServerTray.Services;

public sealed class LogService
{
    private readonly string _logPath;
    private readonly object _gate = new();

    public LogService(AppConfig config)
    {
        Directory.CreateDirectory(config.TempFolder);
        _logPath = Path.Combine(config.TempFolder, "server.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", ex is null ? message : $"{message} {ex}");

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            File.AppendAllText(_logPath, $"{DateTimeOffset.Now:u} [{level}] {message}{Environment.NewLine}");
        }
    }
}
