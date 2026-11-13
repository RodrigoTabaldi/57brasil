# Decisões

- Next.js App Router e ASP.NET Core .NET 10 em containers independentes para deploy sem Vercel.
- PostgreSQL como fonte de verdade; busca textual inicial via índices e trigramas, sem serviço de busca prematuro.
- Nenhum dado comercial demonstrativo é exibido como real.
- PitStop mostrou navegação por departamentos e seletor progressivo tipo/marca/modelo/ano/versão. MercadoCar e Jocar retornaram HTTP 403 na consulta automatizada em 2026-11-12; suas interfaces não foram inferidas.
- A skill `frontend-design` solicitada não foi encontrada no ambiente em 2026-11-12; as outras duas foram lidas. A auditoria web completa depende da interface implementada e das regras atualizadas.

## Frontend da loja — 2026-11-13

- Referências: PitStop acessível, com busca, departamentos e seletor progressivo de veículo; MercadoCar e Jocar bloquearam a leitura automatizada das páginas (HTTP 403). Usei apenas padrões observáveis e uma identidade visual própria para a 57 Brasil.
- `web-design-guidelines`: aplicado ao layout semântico, link de pular conteúdo, foco visível, rótulos, navegação por teclado no autocomplete, estados vazios e de erro, e respeito a `prefers-reduced-motion`.
- `vercel-react-best-practices`: homepage, catálogo e produto continuam como Server Components. Apenas busca, veículo, carrinho e administração carregam JavaScript de interação. As consultas independentes de produtos, categorias e marcas no catálogo são paralelas.
- Auditoria: o build de produção e o TypeScript estrito passaram. Não foi realizada inspeção visual em navegador neste ambiente; conferir desktop e mobile com API e dados reais antes de publicar. A skill `frontend-design` permanece indisponível neste ambiente.
- As categorias na homepage representam a taxonomia prevista. Preços, estoque e aplicações só aparecem quando retornados pela API. O checkout permanece indisponível até a integração transacional de frete e pagamento.
