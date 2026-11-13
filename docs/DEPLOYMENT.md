# Implantação

O `compose.yaml` é somente para desenvolvimento local em HTTP (`localhost:8088`). Não usá-lo como configuração de produção.

Antes de publicar: concluir checkout e administração; executar migrations em etapa controlada; Caddy HTTPS com domínio real; segredos e credenciais de provedores; backups automáticos com cópia externa e restauração testada; health checks dos containers; limites de recursos; política de atualização e rollback; páginas legais revisadas.

Não foi possível testar o stack Docker nesta sessão porque o daemon Docker não estava ativo.

