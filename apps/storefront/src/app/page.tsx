import Link from "next/link";
import Image from "next/image";
import { ArrowRight, ChevronRight, ShieldCheck } from "lucide-react";
import { PartTypeCard } from "@/components/catalog/PartTypeCard";
import { CatalogFilters } from "@/components/catalog/CatalogFilters";
import { CatalogRetryButton } from "@/components/catalog/CatalogRetryButton";
import { getPartTypeGroups, getPartTypes } from "@/lib/catalog";

type Filters = { q?: string; categoria?: string; marca?: string; tipo?: string; page?: string };

export default async function Home({ searchParams }: { searchParams: Promise<Filters> }) {
  const filters = await searchParams;
  const params = new URLSearchParams();
  if (filters.q) params.set("q", filters.q);
  if (filters.categoria) params.set("group", filters.categoria);
  if (filters.tipo) params.set("vehicleType", filters.tipo);
  if (filters.page) params.set("page", filters.page);
  const [parts, facets] = await Promise.all([
    getPartTypes(params).catch(() => null),
    getPartTypeGroups().catch(() => null),
  ]);
  function pageHref(page: number) {
    const next = new URLSearchParams();
    for (const key of ["q", "categoria", "marca", "tipo"] as const) if (filters[key]) next.set(key, filters[key]!);
    next.set("page", String(page));
    return `/?${next}#catalogo`;
  }

  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/banner-57brasil.png" alt="Peças automotivas e carro em composição nas cores verde e amarelo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content"><div className="store-hero-copy">
        <span className="hero-editorial-label">57 BRASIL <span aria-hidden="true">/</span> AUTOPEÇAS</span>
        <h1 id="hero-title">Encontre sua peça aqui.</h1>
        <p>Encontre peças no catálogo da 57 Brasil.</p>
        <div className="store-hero-actions"><Link className="btn btn-primary" href="/produtos">Ver catálogo <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/produtos">Filtrar por grupo <ChevronRight size={17} aria-hidden="true" /></Link></div>
      </div></div>
    </section>
    <section id="catalogo" className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell">
      <div className="store-section-head"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h2 id="showcase-title">Peças cadastradas</h2><p className="catalog-intro">Itens ilustrativos; aplicação e disponibilidade devem ser confirmadas.</p></div><Link className="inline-link" href="/produtos">Ver catálogo completo <ArrowRight size={17} aria-hidden="true" /></Link></div>
      <CatalogFilters facets={{ categories: facets ?? [], brands: [] }} query={filters.q} category={filters.categoria} vehicleType={filters.tipo} count={parts?.total} catalogItems />
      {facets === null && <p role="status">Não foi possível carregar as categorias.</p>}
      {parts === null ? <div className="catalog-empty"><p role="status">Não foi possível carregar as peças. Verifique se a API está atualizada e tente novamente.</p><CatalogRetryButton /></div> : parts.items.length > 0 ? <><div className="product-grid">{parts.items.map(item => <PartTypeCard item={item} key={item.id} />)}</div>{parts.total > parts.pageSize && <nav className="pagination" aria-label="Páginas do catálogo">{parts.page > 1 && <Link href={pageHref(parts.page - 1)}>Anterior</Link>}<span>Página {parts.page} de {Math.ceil(parts.total / parts.pageSize)}</span>{parts.page * parts.pageSize < parts.total && <Link href={pageHref(parts.page + 1)}>Próxima</Link>}</nav>}</> : <div className="catalog-empty"><p role="status">Nenhuma peça encontrada com estes filtros. Aplicações não verificadas não aparecem no filtro por veículo.</p><Link className="inline-link" href="/#catalogo">Limpar filtros</Link></div>}
    </div></section>
    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Solicite a compra</strong><p>O botão COMPRAR registra uma solicitação; preço e disponibilidade são confirmados pela equipe.</p></div><Link href="/produtos">Explorar catálogo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
