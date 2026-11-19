# Auditoria de segurança — 18/11/2026

Escopo: API, storefront, Identity, carrinho, frete, reservas, pagamentos, worker, EF Core, Compose e Caddy. Não há upload de arquivos. Checkout e cobrança permanecem indisponíveis até integração e testes reais.

| Severidade | Evidência | Situação |
| --- | --- | --- |
| Critical | O bootstrap promovia qualquer conta existente com o e-mail configurado a `Administrator`, inclusive uma criada pelo registro público antes da migração. | Corrigido: conta existente sem papel administrativo bloqueia a migração; administrador existente mantém idempotência. |
| High | Login administrativo não tinha limite específico por IP e podia criar sessão de cliente no endpoint administrativo. | Corrigido: limite de conta e verificação de papel antes de criar sessão. |
| Medium | Cotação de frete permitia até 120 chamadas por minuto por IP, com custo para o provedor externo. | Corrigido: limite de 10 chamadas por minuto por IP. |
| Medium | Proxy não enviava HSTS, proteção de MIME, enquadramento e política de referenciador; respostas privadas não declaravam `no-store`. | Corrigido no Caddy de produção. |
| Medium | Token de redefinição de senha trafegava na query string de `/conta`; histórico e logs de acesso podiam retê-lo. | Corrigido para novos links: fragmento, limpeza imediata da URL e validade de uma hora. Links antigos em query string ainda são aceitos durante a transição. |
| Low | Requisições não tinham limite explícito pequeno no Kestrel. | Corrigido: 64 KiB por requisição. |

Controles existentes: cookies HttpOnly, SameSite Strict e Secure em produção; verificação de Origin nas escritas; consultas EF parametrizadas; autorização por papel em `/api/v1/admin`; autorização por usuário no carrinho; assinatura de webhook Mercado Pago; idempotência no carrinho, reservas e notificações. CORS não é habilitado e a aplicação usa mesma origem. Não foi encontrado segredo real versionado nos arquivos inspecionados.

`dotnet build` da API, 11 testes unitários e `caddy validate` passaram. `npm audit --omit=dev` e `dotnet list package --vulnerable --include-transitive` não encontraram avisos nas fontes consultadas. Não houve teste de integração completo nem teste de penetração nesta etapa.

Antes de produção: validar autenticação, reserva e estoque com banco descartável sob concorrência; aplicar CSP compatível com Next.js após teste no navegador; revisar LGPD, retenção de dados e logs; testar falhas de Redis, RabbitMQ e pagamentos. Remova a senha de bootstrap do ambiente após criar o administrador.
