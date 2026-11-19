import Link from "next/link";
import { demoBrands } from "../../lib/demo-catalog";
import {
  Battery, Bike, CarFront, Circle, Cog, Disc3, Droplets, Fan,
  Filter, Grid2X2, Lightbulb, Search, Settings2, Truck, Wrench, Zap,
  type LucideIcon,
} from "lucide-react";

const groupIcons: Record<string, LucideIcon> = {
  "Freios": Disc3, "Suspensão e direção": Settings2, "Filtros": Filter,
  "Sistema elétrico": Battery, "Ignição": Zap, "Motor": Cog,
  "Arrefecimento": Droplets, "Iluminação": Lightbulb, "Pneus e rodas": Circle,
  "Transmissão e câmbio": Fan, "Acessórios automotivos": Wrench,
};

const vehiclePaths = [
  { label: "Carro", value: "carro", icon: CarFront },
  { label: "Moto", value: "moto", icon: Bike },
  { label: "Caminhão", value: "caminhao", icon: Truck },
] as const;

// Curated shortcuts until verified sales and search analytics are available.
const featuredGroups = ["Freios", "Filtros", "Suspensão e direção", "Motor", "Pneus e rodas"];

type Props = {
  groups: string[];
  selectedGroup?: string;
  query?: string;
  selectedBrand?: string;
  count: number;
  vehicleType?: string;
  showPieceFilters?: boolean;
};

export function DemoCatalogFilters({ groups, selectedGroup, query, selectedBrand, count, vehicleType, showPieceFilters = true }: Props) {
  const visibleGroups = featuredGroups.filter(group => groups.includes(group));
  if (selectedGroup && groups.includes(selectedGroup) && !visibleGroups.includes(selectedGroup)) {
    visibleGroups.push(selectedGroup);
  }

  function groupHref(group?: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (group) params.set("peca", group);
    if (selectedBrand) params.set("marcaDemo", selectedBrand);
    return `/${params.size ? `?${params}` : ""}#catalogo`;
  }

  return <div className="catalog-filter-area">
    <div className="catalog-search-panel">
      <h3>Personalize sua busca</h3>
      <nav className="catalog-mode-tabs" aria-label="Escolha como buscar">
        <Link href="/#catalogo" aria-current={!vehicleType ? "page" : undefined}><Grid2X2 size={19} aria-hidden="true" /><span>Peças</span></Link>
        {vehiclePaths.map(({ label, value, icon: Icon }) => <Link href={`/?tipo=${value}#catalogo`} aria-current={vehicleType === value ? "page" : undefined} key={value}><Icon size={20} strokeWidth={1.7} aria-hidden="true" /><span>{label}</span></Link>)}
      </nav>

    {!vehicleType && showPieceFilters && <>
    <section className="catalog-filter-section" aria-labelledby="piece-filter-title">
      <div className="catalog-filter-section-heading"><h4 id="piece-filter-title">Peças em destaque</h4></div>
      <nav className="catalog-group-filters" aria-label="Filtrar por sistema da peça">
        <Link href={groupHref()} aria-current={!selectedGroup ? "page" : undefined}><Grid2X2 size={22} aria-hidden="true" /><span>Todas as peças</span></Link>
        {visibleGroups.map(group => {
          const Icon = groupIcons[group] ?? Cog;
          return <Link href={groupHref(group)} aria-current={selectedGroup === group ? "page" : undefined} key={group}><Icon size={22} strokeWidth={1.8} aria-hidden="true" /><span>{group}</span></Link>;
        })}
      </nav>
    </section>

    <section className="catalog-filter-section" aria-label="Refinar por nome e marca fictícia">
      <form className="catalog-filter-form" action="/#catalogo" method="get">
        <label className="catalog-search-field"><span>Nome da peça</span><span className="catalog-field-control"><Search size={19} aria-hidden="true" /><input name="q" type="search" defaultValue={query ?? ""} placeholder="Ex.: pastilha de freio…" autoComplete="off" /></span></label>
        <label className="catalog-price-field"><span>Marca fictícia</span><select name="marcaDemo" defaultValue={selectedBrand ?? ""}><option value="">Todas as marcas</option>{demoBrands.map(brand => <option value={brand} key={brand}>{brand}</option>)}</select></label>
        {selectedGroup && <input type="hidden" name="peca" value={selectedGroup} />}
        <button type="submit">Ver resultados</button>
      </form>
    </section>
    </>}
    </div>
    <div className="catalog-filter-footer"><p className="catalog-filter-count" role="status">{count} {count === 1 ? "item encontrado" : "itens encontrados"}</p>{(selectedGroup || query || selectedBrand || vehicleType) && <Link href="/#catalogo">Limpar filtros</Link>}</div>
  </div>;
}
