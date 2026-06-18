using ScannerServerTray;
using ScannerServerTray.Services;
using ScannerShared;
using System.Net;

ApplicationConfiguration.Initialize();

var configService = new ConfigService();
var config = configService.Load();
var log = new LogService(config);
var tempFiles = new TempFileService(config, log);
var scannerService = new ScannerService(config, tempFiles, log);

using var tray = new NotifyIcon();
using var cts = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{config.Port}");
builder.Services.AddSingleton(config);
builder.Services.AddSingleton(log);
builder.Services.AddSingleton(tempFiles);
builder.Services.AddSingleton(scannerService);

var app = builder.Build();

app.MapGet("/api/status", (AppConfig cfg) =>
    Results.Ok(new StatusDto(true, cfg.ServerName, "0.1.0-mvp", DateTimeOffset.Now)));

app.MapGet("/api/scanners", async (ScannerService service) =>
{
    var scanners = await service.GetScannersAsync();
    return Results.Ok(scanners);
});

app.MapPost("/api/scan", async (ScanRequestDto request, ScannerService service, HttpContext ctx) =>
{
    var result = await service.ScanAsync(request, ctx.RequestAborted);
    return Results.Ok(result);
});

app.MapGet("/api/scans/{scanId}", (string scanId, TempFileService files) =>
{
    var file = files.Get(scanId);
    return file is null
        ? Results.NotFound(new ErrorDto("scan_not_found", "Arquivo temporario nao encontrado."))
        : Results.File(file.Path, file.ContentType, file.FileName);
});

app.MapDelete("/api/scans/{scanId}", (string scanId, TempFileService files) =>
    files.Delete(scanId)
        ? Results.NoContent()
        : Results.NotFound(new ErrorDto("scan_not_found", "Arquivo temporario nao encontrado.")));

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorDto(
            "server_error",
            "Falha ao processar a solicitacao no servidor.",
            "Veja o arquivo de log do servidor para detalhes."));
    });
});

var serverTask = app.RunAsync(cts.Token);

tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
tray.Text = $"PScanner Server - {config.ServerName}";
tray.Visible = true;
tray.ContextMenuStrip = BuildMenu(config, log, cts, tray);
tray.ShowBalloonTip(3000, "PScanner Server", $"Servidor ativo em {GetServerAddress(config.Port)}", ToolTipIcon.Info);

log.Info($"PScanner Server iniciado em http://0.0.0.0:{config.Port}");
Application.Run();

cts.Cancel();
try
{
    await serverTask;
}
catch (OperationCanceledException)
{
    // Encerramento normal.
}

static ContextMenuStrip BuildMenu(AppConfig config, LogService log, CancellationTokenSource cts, NotifyIcon tray)
{
    var menu = new ContextMenuStrip();
    menu.Items.Add("Abrir status", null, (_, _) =>
    {
        var url = $"{GetServerAddress(config.Port)}/api/status";
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { log.Error("Falha ao abrir status.", ex); }
    });
    menu.Items.Add("Copiar endereco do servidor", null, (_, _) =>
    {
        Clipboard.SetText(GetServerAddress(config.Port));
        tray.ShowBalloonTip(1500, "PScanner Server", "Endereco copiado.", ToolTipIcon.Info);
    });
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Sair", null, (_, _) =>
    {
        tray.Visible = false;
        cts.Cancel();
        Application.Exit();
    });
    return menu;
}

static string GetServerAddress(int port)
{
    var host = Dns.GetHostEntry(Dns.GetHostName());
    var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
    return $"http://{ip ?? IPAddress.Loopback}:{port}";
}
