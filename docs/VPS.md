# Implantação em VPS única

## CI e deploy automático

O workflow `Verify` testa backend e frontend em pushes e pull requests. Após os dois jobs passarem em um push para `main`, o job de deploy conecta à VPS usando uma chave SSH restrita e publica apenas o commit que ainda estiver na ponta de `main`. O script da VPS constrói as imagens, cria um dump local do PostgreSQL, executa migrations e verifica a disponibilidade HTTPS. Consulte os logs em GitHub Actions se uma etapa falhar. O deploy pode interromper o serviço enquanto os containers são recriados; falhas após migrations exigem análise e recuperação manual do banco, pois não há rollback automático. O dump local não protege contra perda da VPS.

O script instalado em `/root/deploy-vps-ci.sh` é separado do checkout por segurança. Mudanças nesse script exigem atualização controlada dessa cópia na VPS; não são aplicadas pelo próprio deploy.

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

# Implantação em VPS de 8 GB

`compose.vps.yaml` é uma configuração inicial para um único host: Caddy, um frontend, uma API e PostgreSQL. Ela usa cache em memória e leitura no banco principal; não inicia réplica, Redis, RabbitMQ ou worker. O webhook de pagamento fica desabilitado. Não habilite checkout até concluir e testar o fluxo comercial. Esta configuração não oferece alta disponibilidade; a capacidade deve ser medida com dados e tráfego representativos.

Na VPS, instale Docker Engine e Compose, aponte o domínio para o IP e libere as portas 80 e 443. Copie `.env.production.example` para `.env.production`, configure domínio, URL pública, e-mail ACME e senha PostgreSQL forte; os segredos de réplica, Redis e RabbitMQ não são usados neste arquivo. Guarde o arquivo fora do Git e restrinja suas permissões com `chmod 600 .env.production`. Se necessário, informe as credenciais iniciais do administrador somente para a primeira migration.

Na raiz do projeto, use sempre o mesmo nome de projeto Compose e o arquivo de variáveis explícito:

```sh
docker compose --project-name brasil57-vps --env-file .env.production -f compose.vps.yaml config --quiet
docker compose --project-name brasil57-vps --env-file .env.production -f compose.vps.yaml build
docker compose --project-name brasil57-vps --env-file .env.production -f compose.vps.yaml up -d db
docker compose --project-name brasil57-vps --env-file .env.production -f compose.vps.yaml run --rm migrate
docker compose --project-name brasil57-vps --env-file .env.production -f compose.vps.yaml up -d --no-build
```

Execute cada comando somente após o anterior terminar com sucesso. Depois da criação inicial do administrador, remova ambas as variáveis `BOOTSTRAP_ADMIN_*` do arquivo antes de outra migration. Verifique `https://SEU_DOMINIO/health/ready`, o catálogo, login administrativo, logs e consumo de memória. Atualizações exigem revisão da migration, cópia recuperável do banco, teste de restauração e plano de rollback. Preserve os volumes PostgreSQL, chaves Data Protection e Caddy; não use `down -v` em dados que precisam ser mantidos.
