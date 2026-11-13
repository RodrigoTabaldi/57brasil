# Arquitetura

A solução segue monólito modular pragmático. `Brasil57.Domain` contém entidades e regras puras, `Brasil57.Infrastructure` concentra EF Core/Npgsql e `Brasil57.Api` expõe HTTP. `Brasil57.Application` e `Brasil57.Worker` estão preparados, mas ainda não têm casos de uso de produção.

## Estado das fases

1. Fundação: estrutura, Compose local, health checks e banco inicial criados; inicialização integrada em Docker pendente por ausência de daemon no ambiente de desenvolvimento.
2. Interface: homepage e tokens visuais criados; design system e auditoria visual completa pendentes.
3. Catálogo: leitura por API e páginas ligadas; edição administrativa e imagens pendentes.
4. Veículos: hierarquia, aplicações e filtro por versão/ano ligados; importação de dados técnicos pendente.
5. Carrinho: persistência anônima por cookie HttpOnly e valores calculados no servidor; autenticação e mesclagem pendentes.
6. Checkout: cotação de frete implementada e adaptador de pagamento hospedado testado de forma isolada. Criação de pedidos, reserva transacional, webhook, reconciliação e idempotência pendentes; compra permanece bloqueada.
7. Administração: login com ASP.NET Identity, papel Administrator, criação de categorias, marcas e produtos, dashboard básico e auditoria inicial. Edição, estoque, pedidos e importação pendentes.
8. Qualidade: 10 testes de quantidade, frete e pagamento; integração, E2E, acessibilidade e segurança completos pendentes.
9. Deploy: Compose local apenas; configuração TLS, backups externos e restauração pendentes.

Critério de promoção: concluir e verificar cada fluxo até a persistência, sem liberar compra antes de checkout, estoque, frete e pagamento confiáveis.





