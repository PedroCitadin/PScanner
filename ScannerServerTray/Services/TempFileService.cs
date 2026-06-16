namespace ScannerServerTray.Services;

public sealed record TempScanFile(string ScanId, string Path, string FileName, string ContentType);

public sealed class TempFileService
{
    private readonly AppConfig _config;
    private readonly LogService _log;

    public TempFileService(AppConfig config, LogService log)
    {
        _config = config;
        _log = log;
        Directory.CreateDirectory(_config.TempFolder);
    }

    public string CreatePath(string extension)
    {
        Directory.CreateDirectory(_config.TempFolder);
        return Path.Combine(_config.TempFolder, $"{Guid.NewGuid():N}.{extension.TrimStart('.')}");
    }

    public TempScanFile? Get(string scanId)
    {
        var file = Directory.EnumerateFiles(_config.TempFolder, $"{scanId}.*").FirstOrDefault();
        if (file is null) return null;

        var ext = Path.GetExtension(file).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
        return new TempScanFile(scanId, file, Path.GetFileName(file), contentType);
    }

    public bool Delete(string scanId)
    {
        var file = Get(scanId);
        if (file is null) return false;

        try
        {
            File.Delete(file.Path);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao remover scan {scanId}.", ex);
            return false;
        }
    }
}
