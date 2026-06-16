# Instalação manual

## Requisitos

- Windows 10 ou superior.
- Scanner instalado e funcionando no Windows do servidor.
- Driver WIA do scanner.
- .NET 8 Desktop Runtime, se publicar dependente de framework.
- Rede local entre cliente e servidor.

## Servidor

1. Publique o projeto `ScannerServerTray`.
2. Copie a pasta `publish` para o PC com scanner USB.
3. Execute `ScannerServerTray.exe`.
4. Edite `config.json` se quiser trocar porta, pasta temporária ou nome amigável.
5. Libere a porta no Firewall do Windows.

Exemplo de firewall:

```powershell
New-NetFirewallRule -DisplayName "SharedScanner 5155" -Direction Inbound -Protocol TCP -LocalPort 5155 -Action Allow
```

## Cliente

1. Publique o projeto `ScannerClientWpf`.
2. Copie a pasta `publish` para o PC cliente.
3. Execute `ScannerClientWpf.exe`.
4. Informe a URL do servidor.
5. Teste conexão, atualize scanners e digitalize.

## Atualização

1. Feche o cliente e o servidor.
2. Substitua os arquivos publicados.
3. Preserve `config.json` se quiser manter as configurações.

## Remoção

1. Feche os aplicativos.
2. Apague as pastas publicadas.
3. Opcionalmente remova:

```text
%LOCALAPPDATA%\SharedScanner
%LOCALAPPDATA%\SharedScannerClient
```
