import Link from "next/link";
import { Bike, CarFront, Circle, Cog, Disc3, Filter, Grid2X2, Search, Settings2, Truck, type LucideIcon } from "lucide-react";
import type { ProductFacets } from "@/lib/catalog";

const featuredCategories = [{ slug: "freios", name: "Freios" }, { slug: "filtros", name: "Filtros" }, { slug: "suspensao-e-direcao", name: "Suspensão e Direção" }, { slug: "motor", name: "Motor" }, { slug: "pneus-e-rodas", name: "Pneus e Rodas" }];
const categoryIcons: Record<string, LucideIcon> = { freios: Disc3, filtros: Filter, "suspensao-e-direcao": Settings2, motor: Cog, "pneus-e-rodas": Circle };
const vehicleTabs = [
  { label: "Carro", value: "carro", icon: CarFront },
  { label: "Moto", value: "moto", icon: Bike },
  { label: "Caminhão", value: "caminhao", icon: Truck },
] as const;

type Props = {
  facets: ProductFacets;
  query?: string;
  category?: string;
  brand?: string;
  vehicleType?: string;
  count?: number;
  catalogItems?: boolean;
};

export function CatalogFilters({ facets, query, category, brand, vehicleType, count, catalogItems = false }: Props) {
  const categories = facets.categories;
  const brands = facets.brands.filter(item => item.slug !== "marca-a-confirmar");
  const featured = featuredCategories.map(item => ({ ...item, count: categories.find(category => category.slug === item.slug)?.count }));
  const selected = categories.find(item => item.slug === category);
  if (selected && !featured.some(item => item.slug === selected.slug)) featured.push(selected);
  const categoryOptions = [...featured, ...categories.filter(item => !featured.some(featuredItem => featuredItem.slug === item.slug))];
  function categoryHref(slug?: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (slug) params.set("categoria", slug);
    if (brand) params.set("marca", brand);
    if (vehicleType) params.set("tipo", vehicleType);
    return `/${params.size ? `?${params}` : ""}#catalogo`;
  }
  function vehicleHref(type?: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (category) params.set("categoria", category);
    if (brand) params.set("marca", brand);
    if (type) params.set("tipo", type);
    return `/${params.size ? `?${params}` : ""}#catalogo`;
  }
  return <div className="catalog-filter-area">
    <div className="catalog-search-panel">
      <h3>Personalize sua busca</h3>
      <nav className="catalog-mode-tabs" aria-label="Escolha como buscar">
        <Link href={vehicleHref()} aria-current={!vehicleType ? "page" : undefined}><Grid2X2 size={19} aria-hidden="true" /><span>Peças</span></Link>
        {vehicleTabs.map(({ label, value, icon: Icon }) => <Link href={vehicleHref(value)} aria-current={vehicleType === value ? "page" : undefined} key={value}><Icon size={20} strokeWidth={1.7} aria-hidden="true" /><span>{label}</span></Link>)}
      </nav>
      {catalogItems && <p className="catalog-filter-help">Os filtros por veículo mostram apenas aplicações com fonte registrada.</p>}
      {vehicleType && <p className="catalog-filter-help">Peças relacionadas a esta família de veículo. Para confirmar compatibilidade, <Link href="/veiculo">selecione marca, modelo e ano</Link>.</p>}
      <section className="catalog-filter-section" aria-labelledby="home-categories-title">
        <div className="catalog-filter-section-heading"><h4 id="home-categories-title">Peças em destaque</h4></div>
        <nav className="catalog-group-filters" aria-label="Filtrar produtos por categoria">
          <Link href={categoryHref()} aria-current={!category ? "page" : undefined}><Grid2X2 size={19} aria-hidden="true" /><span>Todas as peças</span></Link>
          {featured.map(item => { const Icon = categoryIcons[item.slug] ?? Cog; return <Link href={categoryHref(item.slug)} aria-current={category === item.slug ? "page" : undefined} key={item.slug}><Icon size={19} aria-hidden="true" /><span>{item.name}</span>{item.count != null && <small>{item.count}</small>}</Link>; })}
        </nav>
      </section>
      <section className="catalog-filter-section" aria-label="Refinar produtos">
        <form className="catalog-filter-form catalog-filter-form-real" action="/#catalogo" method="get">
          {vehicleType && <input type="hidden" name="tipo" value={vehicleType} />}
          <label className="catalog-search-field"><span>Nome ou código</span><span className="catalog-field-control"><Search size={19} aria-hidden="true" /><input name="q" type="search" defaultValue={query ?? ""} placeholder="Ex.: cabo de vela…" autoComplete="off" /></span></label>
          <label className="catalog-price-field"><span>Categoria</span><select name="categoria" defaultValue={category ?? ""}><option value="">Todas as categorias</option>{categoryOptions.map(item => <option key={item.slug} value={item.slug}>{item.name}</option>)}</select></label>
          {!catalogItems && <label className="catalog-price-field"><span>Marca</span><select name="marca" defaultValue={brand ?? ""} disabled={brands.length === 0}><option value="">{brands.length ? "Todas as marcas" : "Marcas não cadastradas"}</option>{brands.map(item => <option key={item.slug} value={item.slug}>{item.name}</option>)}</select></label>}
          <button type="submit">Ver resultados</button>
        </form>
      </section>
    </div>
    <div className="catalog-filter-footer">{count != null && <p className="catalog-filter-count" role="status">{count} {count === 1 ? "produto encontrado" : "produtos encontrados"}</p>}{(query || category || brand || vehicleType) && <Link href="/#catalogo">Limpar filtros</Link>}</div>
  </div>;
}
