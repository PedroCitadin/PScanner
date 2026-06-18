param(
    [ValidateSet("Client", "Server", "Both")]
    [string]$Mode,

    [string]$InstallRoot = "$env:ProgramFiles\PScanner",

    [switch]$NoStartServer
)

$ErrorActionPreference = "Stop"

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Quote-Arg([string]$value) {
    return '"' + $value.Replace('"', '\"') + '"'
}

if (-not (Test-IsAdmin)) {
    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", (Quote-Arg $PSCommandPath)
    )

    if ($Mode) {
        $arguments += @("-Mode", $Mode)
    }

    if ($InstallRoot) {
        $arguments += @("-InstallRoot", (Quote-Arg $InstallRoot))
    }

    if ($NoStartServer) {
        $arguments += "-NoStartServer"
    }

    Start-Process powershell.exe -Verb RunAs -ArgumentList ($arguments -join " ")
    exit
}

function Resolve-InstallMode {
    if ($Mode) {
        return $Mode
    }

    Write-Host ""
    Write-Host "PScanner Installer"
    Write-Host "1 - Instalar somente o Client"
    Write-Host "2 - Instalar somente o Server"
    Write-Host "3 - Instalar Client e Server"
    Write-Host ""

    do {
        $choice = Read-Host "Escolha uma opcao (1/2/3)"
    } until ($choice -in @("1", "2", "3"))

    switch ($choice) {
        "1" { "Client" }
        "2" { "Server" }
        default { "Both" }
    }
}

function New-Shortcut {
    param(
        [string]$Path,
        [string]$Target,
        [string]$WorkingDirectory,
        [string]$Description,
        [string]$IconPath
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($Path)
    $shortcut.TargetPath = $Target
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description
    $shortcut.IconLocation = $IconPath
    $shortcut.Save()
}

function Copy-App {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path $Source)) {
        throw "Payload nao encontrado: $Source"
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item -Path (Join-Path $Source "*") -Destination $Destination -Recurse -Force
}

$modeToInstall = Resolve-InstallMode
$scriptRoot = Split-Path -Parent $PSCommandPath
$payloadRoot = Join-Path $scriptRoot "Payload"
$programsFolder = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\PScanner"
$desktopFolder = [Environment]::GetFolderPath("CommonDesktopDirectory")

New-Item -ItemType Directory -Force -Path $programsFolder | Out-Null

if ($modeToInstall -in @("Client", "Both")) {
    $clientSource = Join-Path $payloadRoot "Client"
    $clientDestination = Join-Path $InstallRoot "Client"
    Copy-App -Source $clientSource -Destination $clientDestination

    $clientExe = Join-Path $clientDestination "PScanner.Client.exe"
    if (-not (Test-Path $clientExe)) {
        throw "Executavel do client nao encontrado: $clientExe"
    }

    New-Shortcut `
        -Path (Join-Path $desktopFolder "PScanner Client.lnk") `
        -Target $clientExe `
        -WorkingDirectory $clientDestination `
        -Description "PScanner Client" `
        -IconPath $clientExe

    New-Shortcut `
        -Path (Join-Path $programsFolder "PScanner Client.lnk") `
        -Target $clientExe `
        -WorkingDirectory $clientDestination `
        -Description "PScanner Client" `
        -IconPath $clientExe

    Write-Host "Client instalado em: $clientDestination"
}

if ($modeToInstall -in @("Server", "Both")) {
    $serverSource = Join-Path $payloadRoot "Server"
    $serverDestination = Join-Path $InstallRoot "Server"
    Copy-App -Source $serverSource -Destination $serverDestination

    $serverExe = Join-Path $serverDestination "PScanner.Server.exe"
    if (-not (Test-Path $serverExe)) {
        throw "Executavel do server nao encontrado: $serverExe"
    }

    New-Shortcut `
        -Path (Join-Path $programsFolder "PScanner Server.lnk") `
        -Target $serverExe `
        -WorkingDirectory $serverDestination `
        -Description "PScanner Server" `
        -IconPath $serverExe

    $runKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
    New-ItemProperty -Path $runKey -Name "PScanner Server" -Value ('"' + $serverExe + '"') -PropertyType String -Force | Out-Null

    if (-not $NoStartServer) {
        Start-Process -FilePath $serverExe -WorkingDirectory $serverDestination
    }

    Write-Host "Server instalado em: $serverDestination"
    Write-Host "Server configurado para iniciar com o Windows."
}

Write-Host ""
Write-Host "Instalacao do PScanner concluida."
Read-Host "Pressione Enter para sair"
