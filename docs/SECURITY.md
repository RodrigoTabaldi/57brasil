# Segurança

Aplicado: ProblemDetails, limite global de 120 requisições/minuto por IP, cookie de carrinho HttpOnly/SameSite Strict, Secure fora de desenvolvimento, verificação de Origin em escritas, consultas EF parametrizadas, limites de quantidade e ausência de preços enviados pelo navegador.

Identity, papel de administrador e auditoria de criação estão implementados. Chaves de sessão persistem em volume próprio. Ainda obrigatório antes da produção: autorização por recurso, fluxo de recuperação de senha e clientes, política de CORS, cabeçalhos de segurança, testes de concorrência de estoque, webhooks autenticados, auditoria de todas as operações administrativas, revisão de LGPD e testes de penetração. Remova a senha de bootstrap do ambiente após criar o primeiro administrador. O carrinho é uma sessão anônima e não dá acesso a dados pessoais ou pedidos.

# Administração

O painel administrativo exige senha e TOTP (ou um código de recuperação de uso único).
Na primeira entrada, o proprietário cadastra a chave no aplicativo autenticador e guarda
os códigos de recuperação. A recuperação de senha usa o fluxo de e-mail da conta e depende
de SMTP configurado; ela não desativa o segundo fator. Em caso de perda do autenticador e
de todos os códigos, a recuperação exige intervenção operacional com verificação de
identidade fora da aplicação.

`BOOTSTRAP_ADMIN_EMAIL` e `BOOTSTRAP_ADMIN_PASSWORD` só devem existir na primeira execução
de `--migrate`. Remova ambas as variáveis do ambiente após a criação da conta; migrações
posteriores rejeitam a senha de bootstrap ainda configurada. Não há endpoint público para
criar outro administrador.

O painel lista contas e pedidos apenas após MFA. O pedido agora admite endereço de entrega,
mas o fluxo atual ainda não o coleta; pedidos sem esse dado mostram “Não informado”.
O resumo de acessos conta visualizações por dia, sem armazenar IP, cookie ou
identificador do visitante; ele não representa visitantes únicos.

