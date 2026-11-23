# Testes no Render e Supabase gratuitos

Esta implantação reúne site, API e proxy em um serviço gratuito do Render. O PostgreSQL fica no Supabase. É destinada a testes, não a operação comercial contínua.

## Limitações

- O serviço pode dormir por inatividade e demorar para iniciar. As 750 horas gratuitas mensais são compartilhadas no workspace; confira o consumo no painel.
- Site, API e proxy compartilham apenas 512 MB de RAM e 0,1 CPU. Ainda não há medição que confirme que a aplicação cabe nesse limite; valide consumo, inicialização e carga no Render.
- O disco é temporário. As chaves de Data Protection são perdidas em reinicializações ou deploys: sessões e tokens podem deixar de funcionar. Não use contas reais sem armazenamento persistente e protegido das chaves.
- O proxy não confia em IPs fornecidos pelo cliente. Como o Render está à frente dele, a limitação da API pode agrupar visitantes pelo IP do proxy. Teste antes de apresentar a aplicação.
- Não há worker, réplica, Redis ou RabbitMQ. O cache é em memória e o webhook de pagamento deve ficar desligado. O checkout continua indisponível.
- O Supabase gratuito pode pausar por inatividade e não inclui backup automático. Configure backups externos e teste a restauração antes de guardar dados importantes.
- Não publique preços, estoque, ofertas ou compatibilidades sem fonte verificada.

## 1. Criar e proteger o banco

Crie um projeto dedicado em https://supabase.com/. Guarde a senha do banco em um gerenciador de senhas. A aplicação usa PostgreSQL diretamente e não usa a Data API: desabilite a Data API nas configurações do Supabase **antes de criar as tabelas**. Caso precise habilitá-la futuramente, revise schemas expostos, permissões e RLS; as tabelas desta aplicação não foram projetadas para exposição direta.

Em **Connect**, escolha **Session pooler** e copie host, porta, usuário e nome do banco exibidos no painel. Use os valores exatos; o usuário normalmente inclui a referência do projeto. A porta é 5432. Essa modalidade suporta IPv4 e preserva a semântica de sessão. Não use a URL `postgresql://` diretamente como string Npgsql.

Monte a string Npgsql abaixo substituindo os valores. Use senha sem ponto e vírgula ou aspas para facilitar; caso contrário, escape-a conforme as regras do Npgsql. A string completa é secreta.

```text
Host=HOST_DO_POOLER;Port=5432;Database=postgres;Username=USUARIO_DO_POOLER;Password=SENHA_DO_BANCO;SSL Mode=VerifyFull;Maximum Pool Size=5;Timeout=15
```

Verifique a cadeia de certificados no computador e no Render. Se o projeto exigir uma CA própria, configure-a com segurança. Não desative a verificação para contornar falhas.

## 2. Aplicar migrations antes do deploy

Use banco vazio e dedicado. Em banco existente, faça backup e revise migrations. Em PowerShell, na raiz do projeto, com SDK .NET 10 instalado:

```powershell
$env:ConnectionStrings__Default = Read-Host 'Cole a connection string Npgsql do Supabase (segredo)'
dotnet run --project backend/src/Brasil57.Api/Brasil57.Api.csproj -- --migrate
Remove-Item Env:ConnectionStrings__Default
```

Não compartilhe a string nem a salve no Git. O valor solicitado pode ficar visível no terminal. Confirme que a migration terminou sem erro. Para criar o primeiro administrador, configure temporariamente `BootstrapAdmin__Email` e `BootstrapAdmin__Password` no mesmo ambiente antes do comando; remova ambos depois. Não configure essas credenciais permanentemente no Render. Execute migrations futuras explicitamente antes do deploy correspondente, após backup e revisão; o serviço não migra automaticamente.

## 3. Criar serviço no Render

1. Envie o repositório ao provedor Git e crie um **Blueprint** no Render a partir dele. `render.yaml` define um web service gratuito usando `Dockerfile.render`.
2. Quando solicitado, configure `ConnectionStrings__Default` com a string Npgsql no painel de variáveis secretas. `Payment__EnableWebhook` deve permanecer `false`.
3. Faça o deploy. O Render fornece HTTPS e uma URL `onrender.com`.
4. Abra `/health/live` e `/health/ready` nessa URL. O segundo verifica o banco e migrations pendentes. Teste navegação, cadastro e login com contas de teste, inclusive depois de reiniciar. Não divulgue segredos dos logs.
5. Após os testes, adicione o domínio no Render e configure o DNS conforme o painel. Preserve registros de e-mail.

Se um processo falhar, todos são encerrados para que a plataforma reinicie o container. O proxy encaminha `/api/*` e `/health/*` à API e as demais rotas ao Next.js. Ele informa HTTPS à API porque o Render termina TLS antes do container. Não exponha este container diretamente em HTTP fora do Render.

Referências: [conexão ao Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres), [segurança da Data API](https://supabase.com/docs/guides/api/securing-your-api) e [limites do Render](https://render.com/hosting/docker).
