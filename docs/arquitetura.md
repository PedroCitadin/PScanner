# Arquitetura

## Visão geral

O sistema tem dois executáveis Windows e uma biblioteca compartilhada.

```text
ScannerClientWpf  --HTTP REST-->  ScannerServerTray  --WIA-->  Scanner USB
```

## Servidor

`ScannerServerTray` é um aplicativo Windows Forms de bandeja. Ele inicia uma Minimal API ASP.NET Core ouvindo em `0.0.0.0` na porta configurada.

Serviços:

- `ConfigService`: lê e cria `config.json`.
- `LogService`: grava log simples em arquivo.
- `TempFileService`: gerencia imagens temporárias por `scanId`.
- `ScannerService`: lista scanners e executa digitalização via WIA.

## Cliente

`ScannerClientWpf` é um aplicativo WPF.

Serviços:

- `ApiClientService`: chama a API REST.
- `ClientConfigService`: salva preferências do usuário.
- `ImageEditService`: carrega, gira e salva imagens.
- `PdfExportService`: gera PDF multipágina simples com imagens JPEG.

## Persistência

Não há banco de dados no MVP.

- Servidor: `config.json` ao lado do executável e arquivos temporários na pasta configurada.
- Cliente: `%LOCALAPPDATA%\PScannerClient\config.json`.

## Segurança

O MVP não implementa autenticação. A separação de DTOs e o `ApiClientService` deixam espaço para adicionar API key/token em headers HTTP futuramente.
