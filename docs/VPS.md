# Implantação em VPS única

Use uma VPS Linux com Docker Engine e plugin Compose, domínio apontando para o IP público e portas TCP 80/443 e UDP 443 liberadas. O Compose de produção reserva até cerca de 6 GiB de memória entre os serviços; dimensione a VPS com folga para o sistema, builds e picos. Somente Caddy deve ter portas públicas. Não publique PostgreSQL, Redis ou RabbitMQ.

Na VPS, clone o repositório e prepare a configuração:

```bash
cp .env.production.example .env.production
chmod 600 .env.production
```

Substitua domínio, email e as quatro senhas por valores reais e distintos. `PUBLIC_SITE_URL` deve ser `https://` seguido exatamente de `SITE_DOMAIN`, sem barra final. Configure `BOOTSTRAP_ADMIN_EMAIL` e `BOOTSTRAP_ADMIN_PASSWORD` somente para criar o primeiro administrador. O checkout permanece indisponível.

Para a primeira implantação e as atualizações:

```bash
bash scripts/deploy-vps.sh
```

O script valida a configuração, constrói as imagens, faz `pg_dump` compactado em `backups/` se o banco estiver em execução, aplica migrations em um container separado e sobe o stack. Se uma etapa falhar, interrompe a sequência. Após o primeiro bootstrap, retire a senha de administrador de `.env.production`.

Verifique `https://SEU_DOMINIO/health/ready` e os logs com `docker compose --env-file .env.production -f compose.production.yaml logs --tail=100`. Não use `docker compose down -v` no servidor.

Os backups locais do script protegem uma atualização, mas não substituem backup automático com cópia fora da VPS, retenção, monitoramento e teste periódico de restauração. Migrations não são revertidas automaticamente; planeje o rollback da aplicação e do banco antes de atualizar. Consulte [implantação](DEPLOYMENT.md) para limitações do stack e integrações opcionais.
