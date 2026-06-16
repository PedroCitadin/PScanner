# Troubleshooting

## Cliente não conecta

- Confira se o servidor está aberto na bandeja.
- Teste `http://IP:5155/api/status` no navegador do cliente.
- Verifique se a porta foi liberada no Firewall do Windows.
- Confirme se cliente e servidor estão na mesma rede local.

## Nenhum scanner aparece

- Confirme se o scanner funciona no aplicativo nativo do Windows.
- Reinstale ou atualize o driver WIA.
- Desconecte e reconecte o USB.
- Reinicie o servidor depois de instalar o driver.

## Scanner ocupado

- Feche outros aplicativos de digitalização.
- Aguarde o scanner concluir qualquer tarefa pendente.
- Reinicie o scanner se o driver travou.

## Falha ao salvar temporário

- Confira permissão de escrita na pasta `TempFolder` do servidor.
- Troque `TempFolder` em `config.json` para uma pasta local do usuário.

## UI parece parada

As operações de rede e digitalização usam `async/await`, mas alguns drivers WIA podem bloquear a chamada interna até o hardware responder. Aguarde alguns segundos e consulte o log do servidor.
