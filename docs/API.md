# API v1

- `GET /health/live`, `GET /health/ready`
- `GET /api/v1/categories`, `/brands`, `/products`, `/products/{slug}`
- `GET /api/v1/products` aceita `q`, `category`, `brand`, `vehicleVersion`, `year`, `minPrice`, `maxPrice`, `availableOnly` e `page`; os filtros e a paginação são executados no PostgreSQL.
- `GET /api/v1/products/facets` aceita os mesmos filtros (sem `page`) e retorna contagens limitadas a 50 categorias e 50 marcas do conjunto filtrado.
- `GET /api/v1/products/{slug}/applications`
- `GET /api/v1/vehicles/makes`, `/models?makeId=`, `/versions?modelId=`
- `POST /api/v1/checkout/quote` com `{ "cep": "01001000" }` (requer Melhor Envio configurado)
- `GET /api/v1/cart`
- `POST /api/v1/cart/items` com `{ "productId": "uuid", "quantity": 1 }` e cabeçalho `Idempotency-Key: <uuid>`
- `PATCH /api/v1/cart/items/{itemId}` com `{ "quantity": 1 }`
- `DELETE /api/v1/cart/items/{itemId}`
- `POST /api/v1/payments/mercado-pago/webhooks` somente com `Payment__EnableWebhook=true`; valida assinatura e grava na inbox para o worker. Não aceita valores de pagamento enviados no corpo como confirmação.

O carrinho pertence ao usuário autenticado e persiste no PostgreSQL. A mesma `Idempotency-Key` com produto e quantidade iguais retorna sucesso sem adicionar novamente; reutilizá-la com dados diferentes retorna 409. Escritas exigem `Origin` igual ao host da requisição. Preços e disponibilidade são sempre obtidos do banco.

Clientes: `POST /api/v1/account/register`, `/login`, `/logout`, `/forgot-password`, `/reset-password`; `GET/PATCH /api/v1/account/me`. Login e cadastro criam cookie HttpOnly. Recuperação por e-mail depende de `Smtp__Host`, `Smtp__From`, `Smtp__User`, `Smtp__Password` e `Storefront__Url` configurados no processo da API.

A área administrativa exige Administrator e MFA no servidor. POST /api/v1/admin/session recebe e-mail, senha e código TOTP ou de recuperação. GET/POST /api/v1/admin/mfa/setup cadastram o autenticador na primeira entrada. Após MFA, GET /api/v1/admin/dashboard, /overview, /products?q=&page=, /categories, /orders?page= e /accounts?page=; POST /products, /categories e /brands; PATCH /products/{id} e /categories/{id}. POST /api/v1/visits conta acessos diários sem identificar visitantes. Ainda não há checkout.

Pedidos para consulta ao fornecedor: `POST /api/v1/supplier-requests` exige cliente autenticado, `Origin` igual ao host e `Idempotency-Key` de 16 a 100 caracteres ASCII (`A-Z`, `a-z`, `0-9`, `-`, `_`). Corpo: `productId` (UUID de produto visível ou publicado), `quantity` (1–99), `vehicle`, `customerName`, `contact`, `postalCode` (8 dígitos), `street`, `number`, `district`, `city`, `state` (UF); `complement` e `notes` são opcionais. Retorna `publicId` e estado `AwaitingSupplier`. A mesma chave e o mesmo corpo retornam o pedido existente; corpo diferente retorna 409. `GET /api/v1/supplier-requests` e `GET /api/v1/supplier-requests/{id}` mostram apenas pedidos do cliente autenticado. O botão `COMPRAR` de qualquer produto abre a coleta de endereço, sem iniciar checkout. Em produto publicado, o pedido guarda também o preço anunciado e sua fonte como snapshot, quando a referência de preço está validada.

O administrador consulta `GET /api/v1/admin/supplier-requests`, `GET /api/v1/admin/supplier-requests/{id}` e baixa `GET /api/v1/admin/supplier-requests/export.csv` para envio manual. `PATCH /api/v1/admin/supplier-requests/{id}/answer` recebe `available` e `source`; se disponível, exige também `exactPart`, `unitPrice`, `leadDays`, `weightKg`, `heightCm`, `widthCm` e `lengthCm`. A resposta move o pedido para `Quoted` ou `Declined` uma vez. Dados de cotação ficam apenas na visão administrativa; o cliente acompanha o estado. Nenhum desses endpoints cria pagamento ou reserva.


