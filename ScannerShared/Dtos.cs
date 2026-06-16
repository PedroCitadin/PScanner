namespace ScannerShared;

public sealed record ScannerInfoDto(string Id, string Name, string Description);

public sealed record ScanRequestDto(
    string ScannerId,
    int Dpi = 200,
    string ColorMode = "Color",
    string PageSize = "A4");

public sealed record ScanResultDto(
    string ScanId,
    string FileName,
    string Format,
    int Width,
    int Height,
    string DownloadUrl);

public sealed record StatusDto(
    bool Online,
    string ServerName,
    string Version,
    DateTimeOffset DateTime);

public sealed record ErrorDto(string Code, string Message, string? Detail = null);
