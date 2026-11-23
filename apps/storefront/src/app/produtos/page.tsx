import Link from "next/link";
import { ArrowRight, SlidersHorizontal } from "lucide-react";
import { getPartTypeGroups, getPartTypes, getProductFacets, getProducts } from "@/lib/catalog";
import { PartTypeCard } from "@/components/catalog/PartTypeCard";
import { ProductCard } from "@/components/catalog/ProductCard";
import { CatalogRetryButton } from "@/components/catalog/CatalogRetryButton";

type Filters = { q?: string; grupo?: string; categoria?: string; marca?: string; pagina?: string; produtosPagina?: string; vehicleVersion?: string; vehicleType?: string; year?: string; modo?: string };
type Props = { searchParams: Promise<Filters> };
function pageHref(filters: Filters, key: "pagina" | "produtosPagina", page: number) {
  const params = new URLSearchParams();
  for (const [name, value] of Object.entries(filters)) if (value && name !== key) params.set(name, value);
  params.set(key, String(page));
  return `/produtos?${params}#${key === "pagina" ? "tipos" : "produtos-aprovados"}`;
}
export default async function ProductsPage({ searchParams }: Props) {
  const filters = await searchParams;
  const vehicleSearch = Boolean(filters.vehicleVersion);
  const familySearch = Boolean(filters.vehicleType);
  const productMode = vehicleSearch || familySearch || filters.modo === "aprovados" || Boolean(filters.categoria || filters.marca);
  const typeParams = new URLSearchParams();
  if (filters.q) typeParams.set("q", filters.q);
  if (filters.grupo) typeParams.set("group", filters.grupo);
  if (filters.pagina) typeParams.set("page", filters.pagina);
  const productParams = new URLSearchParams();
  if (!familySearch || filters.modo === "aprovados") productParams.set("approvedOnly", "true");
  if (filters.q) productParams.set("q", filters.q);
  if (filters.categoria) productParams.set("category", filters.categoria);
  if (filters.marca) productParams.set("brand", filters.marca);
  if (filters.produtosPagina) productParams.set("page", filters.produtosPagina);
  if (filters.vehicleVersion) productParams.set("vehicleVersion", filters.vehicleVersion);
  if (filters.vehicleType) productParams.set("vehicleType", filters.vehicleType);
  if (filters.year) productParams.set("year", filters.year);
  const facetParams = new URLSearchParams();
  if (!familySearch || filters.modo === "aprovados") facetParams.set("approvedOnly", "true");
  if (filters.vehicleType) facetParams.set("vehicleType", filters.vehicleType);
  const [typeResult, groupResult, productResult, facetResult] = await Promise.allSettled([
    productMode ? Promise.resolve(null) : getPartTypes(typeParams),
    productMode ? Promise.resolve([]) : getPartTypeGroups(),
    productMode ? getProducts(productParams) : Promise.resolve(null),
    productMode ? getProductFacets(facetParams) : Promise.resolve(null)
  ]);
  const types = typeResult.status === "fulfilled" ? typeResult.value : null;
  const groups = groupResult.status === "fulfilled" ? groupResult.value : [];
  const products = productResult.status === "fulfilled" ? productResult.value : null;
  const facets = facetResult.status === "fulfilled" ? facetResult.value : null;
  const typeUrl = filters.q ? `/produtos?q=${encodeURIComponent(filters.q)}#tipos` : "/produtos#tipos";
  const productUrl = filters.q ? `/produtos?modo=aprovados&q=${encodeURIComponent(filters.q)}#produtos-aprovados` : "/produtos?modo=aprovados#produtos-aprovados";
  return <main id="main-content" className="shell catalog-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><span>Catálogo</span></nav>
    <div className="catalog-heading"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h1>{filters.q ? `Resultados para “${filters.q}”` : familySearch ? "Peças por família de veículo" : "Encontre sua próxima peça"}</h1><p>{familySearch ? "Busca ilustrativa por família com base nos cadastros existentes; não confirma compatibilidade com modelo ou ano." : vehicleSearch ? "Esta busca por veículo mostra apenas produtos com aplicação documentada." : "Explore tipos de peças e produtos específicos cadastrados."}</p></div><SlidersHorizontal size={27} aria-hidden="true" /></div>
    {!vehicleSearch && !familySearch && <nav className="catalog-mode-nav" aria-label="Tipo de catálogo"><Link href={typeUrl} aria-current={!productMode ? "page" : undefined}>Tipos de peças</Link><Link href={productUrl} aria-current={productMode ? "page" : undefined}>Produtos aprovados</Link></nav>}
    <div className="catalog-layout"><aside className="filter-panel"><h2>{familySearch ? "Filtrar cadastros da família" : productMode ? "Filtrar produtos aprovados" : "Filtrar tipos de peças"}</h2><form action="/produtos" method="get">
      <label htmlFor="catalog-q">{productMode ? "Nome do produto" : "Nome ou grupo"}</label><input id="catalog-q" name="q" type="search" defaultValue={filters.q ?? ""} placeholder="Ex.: filtro de óleo…" autoComplete="off" />
      {productMode ? <><input type="hidden" name="modo" value={familySearch ? "ilustrativos" : "aprovados"} />
        <label htmlFor="catalog-category">Categoria</label><select id="catalog-category" name="categoria" defaultValue={filters.categoria ?? ""}><option value="">Todas as categorias</option>{facets?.categories.map(item => <option key={item.slug} value={item.slug}>{item.name} ({item.count})</option>)}</select>
        <label htmlFor="catalog-brand">Marca</label><select id="catalog-brand" name="marca" defaultValue={filters.marca ?? ""}><option value="">Todas as marcas</option>{facets?.brands.map(item => <option key={item.slug} value={item.slug}>{item.name} ({item.count})</option>)}</select>
        {filters.vehicleVersion && <input type="hidden" name="vehicleVersion" value={filters.vehicleVersion} />}{filters.vehicleType && <input type="hidden" name="vehicleType" value={filters.vehicleType} />}{filters.year && <input type="hidden" name="year" value={filters.year} />}
      </> : <><label htmlFor="catalog-group">Grupo</label><select id="catalog-group" name="grupo" defaultValue={filters.grupo ?? ""}><option value="">Todos os grupos</option>{groups.map(item => <option key={item.slug} value={item.slug}>{item.name} ({item.count})</option>)}</select></>}
      <button className="button" type="submit">Aplicar filtros <ArrowRight size={16} aria-hidden="true" /></button><Link className="clear-filters" href={familySearch ? `/produtos?vehicleType=${encodeURIComponent(filters.vehicleType!)}` : productMode && !vehicleSearch ? "/produtos?modo=aprovados" : "/produtos"}>Limpar filtros</Link>
    </form>{(productMode && facets === null && products !== null || !productMode && groupResult.status === "rejected") && <div className="filter-note"><p role="status">{productMode ? "Não foi possível carregar os filtros de marca e categoria." : "Não foi possível carregar os grupos de peças."}</p><CatalogRetryButton /></div>}</aside><div className="catalog-results">
      {!productMode && <section id="tipos" className="catalog-section" aria-labelledby="types-title"><h2 id="types-title">Tipos de peças</h2><p>Encontre a peça que procura por nome ou grupo. Fotos ilustrativas.</p>
        {types === null ? <div className="catalog-empty"><p role="status">Não foi possível carregar os tipos de peças. Tente novamente.</p><CatalogRetryButton /></div> : types.items.length === 0 ? <div className="catalog-empty"><p>Nenhum tipo de peça encontrado.</p><Link className="inline-link" href="/produtos">Remover filtros</Link></div> : <><p className="catalog-count">{types.total} tipos encontrados</p><div className="product-grid">{types.items.map(item => <PartTypeCard key={item.id} item={item} />)}</div>{types.total > types.pageSize && <nav className="pagination" aria-label="Páginas de tipos de peças">{types.page > 1 && <Link href={pageHref(filters, "pagina", types.page - 1)}>Anterior</Link>}<span>Página {types.page} de {Math.ceil(types.total / types.pageSize)}</span>{types.page * types.pageSize < types.total && <Link href={pageHref(filters, "pagina", types.page + 1)}>Próxima</Link>}</nav>}</>}
      </section>}
      {productMode && <section id="produtos-aprovados" className="catalog-section" aria-labelledby="products-title"><h2 id="products-title">{familySearch ? "Peças cadastradas para esta família" : "Produtos específicos aprovados"}</h2><p>{familySearch ? "Imagens de referência dos cadastros. A família não confirma aplicação em um veículo específico." : "Cadastros do fornecedor. Confira os dados de cada item antes da compra."}</p>
        {products === null ? <div className="catalog-empty"><p role="status">Não foi possível carregar os produtos. Tente novamente.</p><CatalogRetryButton /></div> : products.items.length === 0 ? <div className="catalog-empty"><p role="status">{familySearch ? "Nenhum cadastro encontrado para esta família." : "Nenhum produto específico encontrado."}</p><Link className="inline-link" href={familySearch ? `/produtos?vehicleType=${encodeURIComponent(filters.vehicleType!)}` : productMode && !vehicleSearch ? "/produtos?modo=aprovados" : "/produtos"}>Remover filtros</Link></div> : <><p className="catalog-count" role="status">{products.total} {familySearch ? "cadastros encontrados" : "produtos encontrados"}</p><div className="product-grid">{products.items.map(item => <ProductCard key={item.id} product={item} />)}</div>{products.total > products.pageSize && <nav className="pagination" aria-label="Páginas de produtos">{products.page > 1 && <Link href={pageHref(filters, "produtosPagina", products.page - 1)}>Anterior</Link>}<span>Página {products.page} de {Math.ceil(products.total / products.pageSize)}</span>{products.page * products.pageSize < products.total && <Link href={pageHref(filters, "produtosPagina", products.page + 1)}>Próxima</Link>}</nav>}</>}
      </section>}
    </div></div>
  </main>;
}
