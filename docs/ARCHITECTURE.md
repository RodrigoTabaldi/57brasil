# Arquitetura

A solução segue monólito modular pragmático. `Brasil57.Domain` contém entidades e regras puras, `Brasil57.Infrastructure` concentra EF Core/Npgsql e adaptadores, `Brasil57.Api` expõe HTTP e `Brasil57.Worker` processa notificações de pagamento quando o webhook estiver habilitado. A réplica PostgreSQL, o Redis e o RabbitMQ estão descritos em `docs/architecture/distributed-services.md`.

## Estado das fases

1. Fundação: estrutura, Compose local, health checks e banco inicial criados; inicialização integrada em Docker pendente por ausência de daemon no ambiente de desenvolvimento.
2. Interface: homepage e tokens visuais criados; design system e auditoria visual completa pendentes.
3. Catálogo: leitura por API e páginas ligadas; edição administrativa e imagens pendentes.
4. Veículos: hierarquia, aplicações e filtro por versão/ano ligados; importação de dados técnicos pendente.
5. Carrinho: persistência anônima por cookie HttpOnly e valores calculados no servidor; autenticação e mesclagem pendentes.
6. Checkout: cotação de frete implementada e adaptador de pagamento hospedado testado de forma isolada. O serviço interno de reserva transacional com chave idempotente foi testado contra PostgreSQL com concorrência. A API pode validar e persistir webhooks quando `Payment__EnableWebhook=true`; o worker separado consulta o provedor e confirma referência, valor e moeda antes de baixar estoque na mesma transação. O webhook fica desabilitado por padrão. Ainda faltam entrega/endereço persistidos, início de pagamento seguro, reconciliação periódica, tratamento operacional de reembolso/contestação e testes sandbox; compra permanece bloqueada.
7. Administração: login com ASP.NET Identity, papel Administrator, criação de categorias, marcas e produtos, dashboard básico e auditoria inicial. Edição, estoque, pedidos e importação pendentes.
8. Qualidade: 11 testes unitários, quatro testes de integração com PostgreSQL e script de carga de catálogo; E2E, acessibilidade, segurança e carga representativa pendentes.
9. Deploy: Compose local e template para uma VPS com Caddy TLS, duas APIs e migration única; backups externos, restauração e HA entre hosts pendentes.

Critério de promoção: concluir e verificar cada fluxo até a persistência, sem liberar compra antes de checkout, estoque, frete e pagamento confiáveis.





