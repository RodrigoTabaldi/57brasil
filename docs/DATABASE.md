# Banco de dados

As migrations `InitialCatalog`, `IdentityAdmin` e `AdminAudit` e `ShippingDimensions` em `backend/src/Brasil57.Infrastructure/Migrations` criam identidade, auditoria, categorias, marcas, produtos, estoque, carrinhos, veículos e aplicações. SKU e slug são únicos. Preço usa `numeric(12,2)` com constraint de não negatividade; estoque também tem constraint. A busca usa `ILIKE` e índice trigram no nome.

No Compose local, `RunMigrations=true` aplica migrations ao iniciar a API. Em produção, execute migrations em etapa controlada antes de atualizar containers. Não há seed de produto nem compatibilidade: esses dados precisam de fonte técnica verificada.

Para gerar novas migrations, instale temporariamente `Microsoft.EntityFrameworkCore.Design` no projeto API e use `dotnet ef migrations add Nome --project backend/src/Brasil57.Infrastructure --startup-project backend/src/Brasil57.Api`. O pacote de design foi removido do build padrão porque seu grafo de dependências apresentava avisos de segurança em `System.Security.Cryptography.Pkcs` no cache disponível nesta sessão.


