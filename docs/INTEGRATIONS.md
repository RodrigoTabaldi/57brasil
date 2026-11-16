# Integrações comerciais

A cotação de frete usa Melhor Envio quando `SHIPPING_TOKEN`, `SHIPPING_ORIGIN_CEP` (8 dígitos) e `SHIPPING_USER_AGENT` (nome da aplicação e e-mail técnico) estiverem configurados. `SHIPPING_ENVIRONMENT` deve ser `sandbox` ou `production`. O endpoint `/api/v1/checkout/quote` usa somente peso, dimensões, preço e quantidade do banco, e retorna `custom_price` e `custom_delivery_time` do provedor. Se faltar configuração, responde 503; nenhum valor é inventado.

O adaptador de Checkout Pro do Mercado Pago está implementado e testado com respostas controladas, mas não está ligado à criação de pedidos nem ao webhook de produção. Não configure cobrança real até concluir pedido, reserva transacional, idempotência, conciliação, testes sandbox e revisão do fluxo fiscal. O adaptador exige access token, chave secreta de webhook e URL HTTPS pública; não armazena cartão.

A referência atual do Mercado Pago exige `X-Idempotency-Key` na **Orders API** de Checkout Pro, enquanto a integração existente usa a **Preferences API**. Não presumir idempotência de criação de preferência por retry. Antes de ligar o checkout, escolher e testar um fluxo com chave persistente e reconciliação de chamadas com resultado incerto; confirmar pagamento por consulta autenticada ao provedor e comparar pedido, valor e estado. Fontes oficiais: [Orders API](https://www.mercadopago.com.br/developers/pt/reference/online-payments/checkout-pro-orders/create-order/post), [Preferences API](https://www.mercadopago.com.br/developers/pt/reference/online-payments/checkout-pro-preferences/create-preference/post) e [webhooks](https://www.mercadopago.com.br/developers/pt/docs/your-integrations/notifications/webhooks).

Fontes: https://docs.melhorenvio.com.br/reference/calculo-de-fretes-por-produtos e https://www.mercadopago.com.br/developers/pt/docs/checkout-pro-preferences/payment-notifications. As credenciais são distintas entre sandbox e produção.

