import Link from "next/link";
import { ArrowRight, SlidersHorizontal } from "lucide-react";
import { getPartTypeGroups, getPartTypes } from "@/lib/catalog";
import { PartTypeCard } from "@/components/catalog/PartTypeCard";
import { CatalogRetryButton } from "@/components/catalog/CatalogRetryButton";

type Filters = { q?: string; grupo?: string; categoria?: string; page?: string; produtosPagina?: string; marca?: string; vehicleType?: string; vehicleVersion?: string; year?: string };
type Props = { searchParams: Promise<Filters> };

function pageHref(filters: Filters, page: number) {
  const params = new URLSearchParams();
  if (filters.q) params.set("q", filters.q);
  if (filters.grupo ?? filters.categoria) params.set("grupo", filters.grupo ?? filters.categoria!);
  params.set("page", String(page));
  return `/produtos?${params}#pecas-catalogadas`;
}

export default async function ProductsPage({ searchParams }: Props) {
  const filters = await searchParams;
  const selectedGroup = filters.grupo ?? filters.categoria;
  const currentPage = filters.page ?? filters.produtosPagina ?? "1";
  const params = new URLSearchParams({ page: currentPage });
  if (filters.q) params.set("q", filters.q);
  if (selectedGroup) params.set("group", selectedGroup);
  const [typesResult, groupsResult] = await Promise.allSettled([
    getPartTypes(params),
    getPartTypeGroups(),
  ]);
  const types = typesResult.status === "fulfilled" ? typesResult.value : null;
  const groups = groupsResult.status === "fulfilled" ? groupsResult.value : null;

  return <main id="main-content" className="shell catalog-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><span>Catálogo de peças</span></nav>
    <div className="catalog-heading"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h1>{filters.q ? `Resultados para “${filters.q}”` : "Peças catalogadas"}</h1><p>Produtos cadastrados com imagens ilustrativas. Confirme aplicação, preço e disponibilidade antes da compra.</p></div><SlidersHorizontal size={27} aria-hidden="true" /></div>
    <div className="catalog-layout"><aside className="filter-panel"><h2>Filtrar peças</h2><form action="/produtos" method="get">
      <label htmlFor="catalog-q">Nome da peça</label><input id="catalog-q" name="q" type="search" defaultValue={filters.q ?? ""} placeholder="Ex.: filtro de óleo…" autoComplete="off" />
      <label htmlFor="catalog-group">Grupo</label><select id="catalog-group" name="grupo" defaultValue={selectedGroup ?? ""}><option value="">Todos os grupos</option>{groups?.map(item => <option key={item.slug} value={item.slug}>{item.name} ({item.count})</option>)}</select>
      <button className="button" type="submit">Aplicar filtros <ArrowRight size={16} aria-hidden="true" /></button><Link className="clear-filters" href="/produtos">Limpar filtros</Link>
    </form>{types !== null && groups === null && <div className="filter-note"><p role="status">Não foi possível carregar os grupos.</p><CatalogRetryButton /></div>}</aside>
      <div className="catalog-results"><section id="pecas-catalogadas" className="catalog-section" aria-labelledby="parts-title"><h2 id="parts-title">Peças no catálogo</h2>
        {types === null ? <div className="catalog-empty"><p role="status">Não foi possível carregar as peças. Tente novamente.</p><CatalogRetryButton /></div> : types.items.length === 0 ? <div className="catalog-empty"><p role="status">Nenhuma peça encontrada com estes filtros.</p><Link className="inline-link" href="/produtos">Limpar filtros</Link></div> : <><p className="catalog-count" role="status">{types.total} {types.total === 1 ? "item cadastrado" : "itens cadastrados"}</p><div className="product-grid">{types.items.map(item => <PartTypeCard key={item.id} item={item} />)}</div>{types.total > types.pageSize && <nav className="pagination" aria-label="Páginas do catálogo">{types.page > 1 && <Link href={pageHref(filters, types.page - 1)}>Anterior</Link>}<span>Página {types.page} de {Math.ceil(types.total / types.pageSize)}</span>{types.page * types.pageSize < types.total && <Link href={pageHref(filters, types.page + 1)}>Próxima</Link>}</nav>}</>}
      </section></div>
    </div>
  </main>;
}
