# Segurança

Aplicado: ProblemDetails, limite global de 120 requisições/minuto por IP por instância, cookie de autenticação HttpOnly/SameSite Strict, Secure fora de desenvolvimento, verificação de Origin em escritas, consultas EF parametrizadas, limites de quantidade e ausência de preços enviados pelo navegador.

Identity, papel de administrador e auditoria de criação estão implementados. O carrinho exige autenticação e é isolado por usuário. Chaves de sessão persistem em volume próprio. A recuperação de senha depende de SMTP configurado. Antes da operação comercial, ainda são necessários checkout e pagamento integrados, testes de ponta a ponta e de concorrência, auditoria das operações administrativas, revisão de LGPD e testes de segurança. Remova a senha de bootstrap do ambiente após criar o primeiro administrador.

