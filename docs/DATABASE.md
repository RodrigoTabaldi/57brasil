# Banco de dados

As migrations `InitialCatalog`, `IdentityAdmin` e `AdminAudit` e `ShippingDimensions` em `backend/src/Brasil57.Infrastructure/Migrations` criam identidade, auditoria, categorias, marcas, produtos, estoque, carrinhos, veículos e aplicações. SKU e slug são únicos. Preço usa `numeric(12,2)` com constraint de não negatividade; estoque também tem constraint. A busca usa `ILIKE` e índice trigram no nome.

Nos arquivos Compose, o serviço único `migrate` aplica migrations antes de iniciar as instâncias HTTP. As migrations `OrderReservations`, `OrderPaymentConfirmation` e `PaymentInbox` adicionam pedido e linhas com snapshots de preço, chave idempotente única, identificação de pagamento e notificações persistidas. O serviço interno `OrderReservationService` usa atualização condicional de `Reserved` numa transação. A confirmação de pagamento consultado no provedor converte `Reserved` em baixa de `OnHand` na mesma transação. O worker usa lease para processar notificações e repetição com backoff após falha. Não existe endpoint de criação de pedido habilitado. Em produção, revise e execute migrations de forma controlada antes de atualizar containers. Não há seed de produto nem compatibilidade: esses dados precisam de fonte técnica verificada.

Os testes de integração requerem PostgreSQL descartável em `BRASIL57_TEST_DATABASE` e rodam com `dotnet test backend/tests/Brasil57.IntegrationTests/Brasil57.IntegrationTests.csproj`. Eles executam migrations e deixam dados no banco informado; nunca use banco de produção. Verificam reserva concorrente, replay, cancelamento, confirmação com valor/moeda incorretos e corretos, consumo da inbox pelo worker com provedor falso, rejeição/deduplicação do webhook e filtros/facetas do catálogo executados no banco.

`ProductSourceReferences` acrescenta referências textuais aos documentos físicos de preço, estoque e aplicação veicular. A API exige as referências relevantes para publicar uma oferta e registra um retrato delas na auditoria administrativa. Ajustes de estoque exigem referência do documento da operação. Como produtos publicados antes dessa migration não possuem evidência registrada, a migration os torna rascunhos visíveis no catálogo, sem anunciar preço ou estoque. Revise e registre as fontes antes de republicá-los.

Para gerar novas migrations, use `dotnet ef migrations add Nome --project backend/src/Brasil57.Infrastructure --startup-project backend/src/Brasil57.Api`. O pacote `Microsoft.EntityFrameworkCore.Design` já está no projeto API com `PrivateAssets="all"`.


