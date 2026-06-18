# PScanner MVP

MVP de compartilhamento de scanner em rede local para Windows. Um computador servidor com scanner USB executa o PScanner Server na bandeja e hospeda uma API HTTP. Um computador cliente executa o PScanner Client, chama a API, baixa as imagens e salva o resultado final como PDF, PNG ou JPG.

## Projetos

- `ScannerShared`: DTOs usados por cliente e servidor.
- `ScannerServerTray`: tray app Windows com ASP.NET Core Minimal API e acesso WIA.
- `ScannerClientWpf`: cliente WPF com preview, páginas múltiplas, rotação, remoção, reordenação e exportação.

## Como compilar

Instale o Visual Studio 2022 com:

- .NET 8 SDK.
- Workload "Desenvolvimento para desktop com .NET".
- Workload "ASP.NET e desenvolvimento Web".

Abra `SharedScanner.sln` e compile em `Release` ou use:

```powershell
dotnet build .\SharedScanner.sln -c Release
```

## Como rodar o servidor

1. Conecte o scanner USB no PC servidor.
2. Execute `PScanner.Server`.
3. Na primeira execução, o arquivo `config.json` é criado ao lado do executável.
4. O ícone aparecerá na bandeja do Windows com as opções:
   - Abrir status
   - Copiar endereço do servidor
   - Sair

Configuração padrão:

```json
{
  "Port": 5155,
  "TempFolder": "%LOCALAPPDATA%\\PScanner\\Temp",
  "ServerName": "NOME-DO-PC"
}
```

O servidor escuta em `http://0.0.0.0:5155`, ou seja, aceita conexões da rede local quando o firewall permitir.

## Como descobrir o IP do servidor

No PC servidor, use o menu "Copiar endereço do servidor" no tray app ou rode:

```powershell
ipconfig
```

Procure o IPv4 da placa de rede local, por exemplo `192.168.0.25`. O endereço do servidor será:

```text
http://192.168.0.25:5155
```

## Como liberar a porta no Firewall do Windows

Execute o PowerShell como administrador:

```powershell
New-NetFirewallRule -DisplayName "PScanner 5155" -Direction Inbound -Protocol TCP -LocalPort 5155 -Action Allow
```

## Como rodar o cliente

1. Execute `PScanner.Client`.
2. Informe a URL do servidor, por exemplo `http://192.168.0.25:5155`.
3. Clique em "Testar conexão".
4. Clique em "Atualizar scanners".
5. Selecione scanner, DPI, modo de cor e formato final.
6. Clique em "Digitalizar".
7. Confira o preview, gire/remova/reordene páginas se necessário.
8. Clique em "Salvar".

Preferências do cliente são salvas em:

```text
%LOCALAPPDATA%\PScannerClient\config.json
```

## Testar API pelo navegador/Postman

Status:

```http
GET http://SERVIDOR:5155/api/status
```

Scanners:

```http
GET http://SERVIDOR:5155/api/scanners
```

Digitalizar:

```http
POST http://SERVIDOR:5155/api/scan
Content-Type: application/json

{
  "scannerId": "ID-DO-SCANNER",
  "dpi": 200,
  "colorMode": "Color",
  "pageSize": "A4"
}
```

Download:

```http
GET http://SERVIDOR:5155/api/scans/{scanId}
```

Excluir temporário:

```http
DELETE http://SERVIDOR:5155/api/scans/{scanId}
```

## Gerar instalador

Use:

```powershell
.\Installer\Build-InstallerPackage.ps1 -NoRestore
```

O pacote fica em `Installer\Package` e o ZIP em `Installer\PScannerInstaller.zip`. Execute `Install-PScanner.bat` e escolha Client, Server ou ambos. O Client cria atalho na area de trabalho; o Server cria atalho no menu Iniciar e inicia com o Windows.

## Publicar executáveis Windows

Servidor:

```powershell
dotnet publish .\ScannerServerTray\ScannerServerTray.csproj -c Release -r win-x64 --self-contained true
```

Cliente:

```powershell
dotnet publish .\ScannerClientWpf\ScannerClientWpf.csproj -c Release -r win-x64 --self-contained true
```

Os executáveis ficam em `bin\Release\net8.0-windows10.0.17763.0\win-x64\publish` quando publicado com runtime `win-x64`, ou em `bin\Release\net8.0-windows10.0.17763.0\publish` na publicação dependente do .NET instalado.

## Limitações do MVP

- Sem autenticação. Use apenas em rede local confiável.
- Digitalização inicial usa WIA e salva temporariamente em JPG.
- Configuração de scanner é básica: DPI, cor e A4.
- Alguns scanners expõem propriedades WIA diferentes; drivers antigos podem exigir ajustes.
- Não há descoberta automática de servidores na rede.
- Não há instalador MSI/serviço Windows.

## Próximos passos

- Adicionar API key/token.
- Adicionar descoberta via UDP/mDNS.
- Suportar alimentador automático e duplex.
- Configurar área de recorte e tamanhos de página adicionais.
- Criar instalador.
- Adicionar limpeza periódica de arquivos temporários.
