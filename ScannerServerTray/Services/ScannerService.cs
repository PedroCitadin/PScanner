using ScannerShared;

namespace ScannerServerTray.Services;

public sealed class ScannerService
{
    private const int WiaScannerDeviceType = 1;
    private const int WiaHorizontalResolution = 6147;
    private const int WiaVerticalResolution = 6148;
    private const int WiaHorizontalExtent = 6151;
    private const int WiaVerticalExtent = 6152;
    private const int WiaCurrentIntent = 6146;
    private const string WiaJpegFormat = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";

    private readonly AppConfig _config;
    private readonly TempFileService _tempFiles;
    private readonly LogService _log;

    public ScannerService(AppConfig config, TempFileService tempFiles, LogService log)
    {
        _config = config;
        _tempFiles = tempFiles;
        _log = log;
    }

    public Task<IReadOnlyList<ScannerInfoDto>> GetScannersAsync() => Task.Run<IReadOnlyList<ScannerInfoDto>>(() =>
    {
        try
        {
            dynamic manager = CreateDeviceManager();
            var scanners = new List<ScannerInfoDto>();

            foreach (dynamic info in manager.DeviceInfos)
            {
                if ((int)info.Type != WiaScannerDeviceType) continue;
                string id = info.DeviceID;
                var name = ReadProperty(info.Properties, "Name") ?? id;
                var description = ReadProperty(info.Properties, "Description") ?? name;
                scanners.Add(new ScannerInfoDto(id, name, description));
            }

            return scanners;
        }
        catch (Exception ex)
        {
            _log.Error("Falha ao listar scanners WIA.", ex);
            throw new InvalidOperationException("Nao foi possivel listar scanners WIA.", ex);
        }
    });

    public Task<ScanResultDto> ScanAsync(ScanRequestDto request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            dynamic manager = CreateDeviceManager();
            dynamic? selected = null;
            foreach (dynamic info in manager.DeviceInfos)
            {
                if ((int)info.Type == WiaScannerDeviceType && (string)info.DeviceID == request.ScannerId)
                {
                    selected = info;
                    break;
                }
            }

            if (selected is null)
                throw new InvalidOperationException("Scanner nao encontrado ou desconectado.");

            dynamic device = selected.Connect();
            dynamic item = device.Items[1];
            ConfigureItem(item, request);

            dynamic imageFile = item.Transfer(WiaJpegFormat);
            var scanId = Guid.NewGuid().ToString("N");
            var path = Path.Combine(_config.TempFolder, $"{scanId}.jpg");
            if (File.Exists(path)) File.Delete(path);
            imageFile.SaveFile(path);

            _log.Info($"Scan {scanId} salvo em {path}.");
            return new ScanResultDto(
                scanId,
                Path.GetFileName(path),
                "jpg",
                (int)imageFile.Width,
                (int)imageFile.Height,
                $"/api/scans/{scanId}");
        }
        catch (Exception ex)
        {
            _log.Error("Falha ao digitalizar.", ex);
            throw new InvalidOperationException("Falha ao digitalizar. Verifique conexao, driver WIA e se o scanner esta ocupado.", ex);
        }
    }, cancellationToken);

    private static object CreateDeviceManager()
    {
        var type = Type.GetTypeFromProgID("WIA.DeviceManager")
            ?? throw new InvalidOperationException("WIA nao esta disponivel neste Windows.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Nao foi possivel iniciar o WIA DeviceManager.");
    }

    private static void ConfigureItem(dynamic item, ScanRequestDto request)
    {
        SetProperty(item.Properties, WiaHorizontalResolution, request.Dpi);
        SetProperty(item.Properties, WiaVerticalResolution, request.Dpi);
        SetProperty(item.Properties, WiaCurrentIntent, request.ColorMode switch
        {
            "Grayscale" => 2,
            "BlackAndWhite" => 4,
            _ => 1
        });

        if (request.PageSize.Equals("A4", StringComparison.OrdinalIgnoreCase))
        {
            SetProperty(item.Properties, WiaHorizontalExtent, (int)(8.27 * request.Dpi));
            SetProperty(item.Properties, WiaVerticalExtent, (int)(11.69 * request.Dpi));
        }
    }

    private static string? ReadProperty(dynamic properties, string name)
    {
        foreach (dynamic property in properties)
        {
            string propertyName = property.Name;
            if (propertyName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return property.Value?.ToString();
        }

        return null;
    }

    private static void SetProperty(dynamic properties, int propertyId, object value)
    {
        foreach (dynamic property in properties)
        {
            if ((int)property.PropertyID == propertyId)
            {
                property.Value = value;
                return;
            }
        }
    }
}
