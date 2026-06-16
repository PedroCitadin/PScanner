# Endpoints

Base URL padrão:

```text
http://SERVIDOR:5155
```

## GET /api/status

Retorna status do servidor.

Resposta:

```json
{
  "online": true,
  "serverName": "PC-SERVIDOR",
  "version": "0.1.0-mvp",
  "dateTime": "2026-06-16T10:00:00-03:00"
}
```

## GET /api/scanners

Lista scanners WIA detectados.

Resposta:

```json
[
  {
    "id": "DEVICE-ID",
    "name": "Scanner",
    "description": "Scanner WIA"
  }
]
```

## POST /api/scan

Executa digitalização.

Request:

```json
{
  "scannerId": "DEVICE-ID",
  "dpi": 200,
  "colorMode": "Color",
  "pageSize": "A4"
}
```

`colorMode` aceita:

- `Color`
- `Grayscale`
- `BlackAndWhite`

Resposta:

```json
{
  "scanId": "abc123",
  "fileName": "abc123.jpg",
  "format": "jpg",
  "width": 1654,
  "height": 2338,
  "downloadUrl": "/api/scans/abc123"
}
```

## GET /api/scans/{scanId}

Baixa a imagem temporária.

## DELETE /api/scans/{scanId}

Remove a imagem temporária.

Resposta de sucesso:

```http
204 No Content
```

## Erros

Formato padrão:

```json
{
  "code": "server_error",
  "message": "Falha ao processar a solicitacao no servidor.",
  "detail": "Veja o arquivo de log do servidor para detalhes."
}
```
