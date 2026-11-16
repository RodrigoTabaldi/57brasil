# API v1

- `GET /health/live`, `GET /health/ready`
- `GET /api/v1/categories`, `/brands`, `/products`, `/products/{slug}`
- `GET /api/v1/products` aceita `q`, `category`, `brand`, `vehicleVersion`, `year`, `minPrice`, `maxPrice`, `availableOnly` e `page`; os filtros e a paginação são executados no PostgreSQL.
- `GET /api/v1/products/facets` aceita os mesmos filtros (sem `page`) e retorna contagens limitadas a 50 categorias e 50 marcas do conjunto filtrado.
- `GET /api/v1/products/{slug}/applications`
- `GET /api/v1/vehicles/makes`, `/models?makeId=`, `/versions?modelId=`
- `POST /api/v1/checkout/quote` com `{ "cep": "01001000" }` (requer Melhor Envio configurado)
- `GET /api/v1/cart`
- `POST /api/v1/cart/items` com `{ "productId": "uuid", "quantity": 1 }`
- `PATCH /api/v1/cart/items/{itemId}` com `{ "quantity": 1 }`
- `DELETE /api/v1/cart/items/{itemId}`
- `POST /api/v1/payments/mercado-pago/webhooks` somente com `Payment__EnableWebhook=true`; valida assinatura e grava na inbox para o worker. Não aceita valores de pagamento enviados no corpo como confirmação.

O carrinho usa cookie HttpOnly de 30 dias. Escritas exigem `Origin` igual ao host da requisição. Preços e disponibilidade são sempre obtidos do banco. A área administrativa usa ASP.NET Identity e exige papel `Administrator` no servidor: `POST/DELETE /api/v1/admin/session`, `GET /api/v1/admin/dashboard`, `GET/POST /api/v1/admin/products`, `POST /api/v1/admin/categories`, `POST /api/v1/admin/brands`. Escritas exigem Origin da mesma origem. Ainda não há checkout.


