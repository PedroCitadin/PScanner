param(
    [string]$Configuration = "Release",
    [string]$PackageRoot = "$PSScriptRoot\PackageReady",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$clientProject = Join-Path $repoRoot "ScannerClientWpf\ScannerClientWpf.csproj"
$serverProject = Join-Path $repoRoot "ScannerServerTray\ScannerServerTray.csproj"
$clientPublish = Join-Path $repoRoot "ScannerClientWpf\bin\$Configuration\net8.0-windows10.0.17763.0\publish"
$serverPublish = Join-Path $repoRoot "ScannerServerTray\bin\$Configuration\net8.0-windows10.0.17763.0\publish"
$payloadRoot = Join-Path $PackageRoot "Payload"

$publishArgs = @("-c", $Configuration)
if ($NoRestore) {
    $publishArgs += "--no-restore"
}

dotnet publish $clientProject @publishArgs
dotnet publish $serverProject @publishArgs

if (Test-Path $PackageRoot) {
    Remove-Item $PackageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path (Join-Path $payloadRoot "Client") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $payloadRoot "Server") | Out-Null

Copy-Item -Path (Join-Path $clientPublish "*") -Destination (Join-Path $payloadRoot "Client") -Recurse -Force
Copy-Item -Path (Join-Path $serverPublish "*") -Destination (Join-Path $payloadRoot "Server") -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Install-PScanner.ps1") -Destination $PackageRoot -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Install-PScanner.bat") -Destination $PackageRoot -Force

$zipPath = Join-Path $PSScriptRoot "PScannerInstaller.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $PackageRoot "*") -DestinationPath $zipPath -Force

Write-Host "Pacote criado em: $PackageRoot"
Write-Host "ZIP criado em: $zipPath"
