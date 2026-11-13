# Segurança

Aplicado: ProblemDetails, limite global de 120 requisições/minuto por IP, cookie de carrinho HttpOnly/SameSite Strict, Secure fora de desenvolvimento, verificação de Origin em escritas, consultas EF parametrizadas, limites de quantidade e ausência de preços enviados pelo navegador.

Identity, papel de administrador e auditoria de criação estão implementados. Chaves de sessão persistem em volume próprio. Ainda obrigatório antes da produção: autorização por recurso, fluxo de recuperação de senha e clientes, política de CORS, cabeçalhos de segurança, testes de concorrência de estoque, webhooks autenticados, auditoria de todas as operações administrativas, revisão de LGPD e testes de penetração. Remova a senha de bootstrap do ambiente após criar o primeiro administrador. O carrinho é uma sessão anônima e não dá acesso a dados pessoais ou pedidos.

