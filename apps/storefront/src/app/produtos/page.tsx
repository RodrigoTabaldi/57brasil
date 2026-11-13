import Link from "next/link";
import { ArrowRight, SlidersHorizontal } from "lucide-react";
import { getProducts } from "@/lib/catalog";
import { ProductCard } from "@/components/catalog/ProductCard";

type Filters = { q?: string; categoria?: string; marca?: string; pagina?: string; vehicleVersion?: string; year?: string };
type Option = { id: string; name: string; slug: string };
type Props = { searchParams: Promise<Filters> };
const baseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5000";
async function getOptions(path: string): Promise<Option[]> {
  const response = await fetch(`${baseUrl}/api/v1/${path}`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Não foi possível carregar ${path}.`);
  return response.json() as Promise<Option[]>;
}
function pageHref(filters: Filters, page: number) {
  const params = new URLSearchParams();
  if (filters.q) params.set("q", filters.q);
  if (filters.categoria) params.set("categoria", filters.categoria);
  if (filters.marca) params.set("marca", filters.marca);
  if (filters.vehicleVersion) params.set("vehicleVersion", filters.vehicleVersion);
  if (filters.year) params.set("year", filters.year);
  params.set("pagina", String(page));
  return `/produtos?${params}`;
}
export default async function ProductsPage({ searchParams }: Props) {
  const filters = await searchParams;
  const params = new URLSearchParams();
  if (filters.q) params.set("q", filters.q);
  if (filters.categoria) params.set("category", filters.categoria);
  if (filters.marca) params.set("brand", filters.marca);
  if (filters.pagina) params.set("page", filters.pagina);
  if (filters.vehicleVersion) params.set("vehicleVersion", filters.vehicleVersion);
  if (filters.year) params.set("year", filters.year);
  const [productResult, categoryResult, brandResult] = await Promise.allSettled([
    getProducts(params), getOptions("categories"), getOptions("brands")
  ]);
  const result = productResult.status === "fulfilled" ? productResult.value : null;
  const categories = categoryResult.status === "fulfilled" ? categoryResult.value : [];
  const brands = brandResult.status === "fulfilled" ? brandResult.value : [];
  const totalPages = result ? Math.ceil(result.total / result.pageSize) : 0;
  return <main id="main-content" className="shell catalog-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><span>Produtos</span></nav>
    <div className="catalog-heading"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h1>{filters.q ? `Resultados para “${filters.q}”` : "Encontre sua próxima peça"}</h1><p>{result ? `${result.total} ${result.total === 1 ? "produto encontrado" : "produtos encontrados"}` : "Catálogo indisponível no momento"}</p></div><SlidersHorizontal size={27} aria-hidden="true"/></div>
    <div className="catalog-layout"><aside className="filter-panel"><h2>Filtrar produtos</h2><form action="/produtos" method="get"><label htmlFor="catalog-q">Nome ou código</label><input id="catalog-q" name="q" defaultValue={filters.q ?? ""} placeholder="Peça, SKU ou marca"/><label htmlFor="catalog-category">Departamento</label><select id="catalog-category" name="categoria" defaultValue={filters.categoria ?? ""}><option value="">Todos</option>{categories.map(item=><option key={item.id} value={item.slug}>{item.name}</option>)}</select><label htmlFor="catalog-brand">Marca da peça</label><select id="catalog-brand" name="marca" defaultValue={filters.marca ?? ""}><option value="">Todas</option>{brands.map(item=><option key={item.id} value={item.slug}>{item.name}</option>)}</select>{filters.vehicleVersion&&<input type="hidden" name="vehicleVersion" value={filters.vehicleVersion}/ >}{filters.year&&<input type="hidden" name="year" value={filters.year}/>}<button className="button" type="submit">Aplicar filtros <ArrowRight size={16} aria-hidden="true"/></button><Link className="clear-filters" href="/produtos">Limpar filtros</Link></form>{categoryResult.status === "rejected" || brandResult.status === "rejected" ? <p className="filter-note">Alguns filtros estão temporariamente indisponíveis.</p> : null}</aside><div className="catalog-results">{!result ? <div className="catalog-empty" role="status"><h2>Não foi possível carregar o catálogo</h2><p>Tente novamente em instantes.</p></div> : result.items.length===0 ? <div className="catalog-empty"><h2>Nenhum produto encontrado</h2><p>Altere os filtros ou explore todos os departamentos.</p><Link className="btn btn-primary" href="/produtos">Ver todo o catálogo</Link></div> : <><div className="product-grid">{result.items.map(product=><ProductCard product={product} key={product.id}/>)}</div>{totalPages>1&&<nav className="pagination" aria-label="Paginação">{result.page>1&&<Link href={pageHref(filters,result.page-1)}>Anterior</Link>}<span>Página {result.page} de {totalPages}</span>{result.page<totalPages&&<Link href={pageHref(filters,result.page+1)}>Próxima</Link>}</nav>}</>}</div></div>
  </main>;
}

