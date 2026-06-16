using ScannerShared;
using System.Net.Http;
using System.Net.Http.Json;

namespace ScannerClientWpf.Services;

public sealed class ApiClientService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public string ServerUrl { get; set; } = "http://127.0.0.1:5155";

    public async Task<StatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        return await _http.GetFromJsonAsync<StatusDto>(BuildUri("/api/status"), cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia do servidor.");
    }

    public async Task<IReadOnlyList<ScannerInfoDto>> GetScannersAsync(CancellationToken cancellationToken)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<ScannerInfoDto>>(BuildUri("/api/scanners"), cancellationToken)
            ?? Array.Empty<ScannerInfoDto>();
    }

    public async Task<ScanResultDto> ScanAsync(ScanRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(BuildUri("/api/scan"), request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScanResultDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia do servidor.");
    }

    public async Task<byte[]> DownloadScanAsync(string downloadUrl, CancellationToken cancellationToken)
    {
        return await _http.GetByteArrayAsync(BuildUri(downloadUrl), cancellationToken);
    }

    private Uri BuildUri(string pathOrUrl)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute))
            return absolute;

        var baseUrl = ServerUrl.TrimEnd('/') + "/";
        var relative = pathOrUrl.TrimStart('/');
        return new Uri(new Uri(baseUrl), relative);
    }
}
