import Link from "next/link";
import Image from "next/image";
import { ArrowRight, ChevronRight, ShieldCheck } from "lucide-react";
import { PartTypeCard } from "@/components/catalog/PartTypeCard";
import { CatalogRetryButton } from "@/components/catalog/CatalogRetryButton";
import { getPartTypes } from "@/lib/catalog";

export default async function Home() {
  const parts = await getPartTypes(new URLSearchParams()).catch(() => null);

  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/banner-57brasil.png" alt="Peças automotivas e carro em composição nas cores verde e amarelo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content"><div className="store-hero-copy">
        <span className="hero-editorial-label">57 BRASIL <span aria-hidden="true">/</span> AUTOPEÇAS</span>
        <h1 id="hero-title">Encontre sua peça aqui.</h1>
        <p>Explore os 100 itens cadastrados, organizados por grupo e com imagens padronizadas.</p>
        <div className="store-hero-actions"><Link className="btn btn-primary" href="/produtos">Ver catálogo <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/produtos">Filtrar por grupo <ChevronRight size={17} aria-hidden="true" /></Link></div>
      </div></div>
    </section>
    <section id="catalogo" className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell">
      <div className="store-section-head"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h2 id="showcase-title">Peças cadastradas</h2><p className="catalog-intro">{parts ? `${parts.total} itens fictícios no catálogo` : "Catálogo temporariamente indisponível"}</p></div><Link className="inline-link" href="/produtos">Ver catálogo e filtros <ArrowRight size={17} aria-hidden="true" /></Link></div>
      {parts === null ? <div className="catalog-empty"><p role="status">Não foi possível carregar as peças.</p><CatalogRetryButton /></div> : parts.items.length > 0 ? <><div className="product-grid">{parts.items.slice(0, 8).map(item => <PartTypeCard item={item} key={item.id} />)}</div><div className="store-section-head"><span>{parts.total} itens para explorar</span><Link className="inline-link" href="/produtos">Abrir catálogo completo <ArrowRight size={17} aria-hidden="true" /></Link></div></> : <div className="catalog-empty"><p>Nenhuma peça cadastrada.</p><Link className="inline-link" href="/produtos">Abrir catálogo e filtros</Link></div>}
    </div></section>
    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Solicite a compra</strong><p>O botão COMPRAR registra uma solicitação; preço e disponibilidade são confirmados pela equipe.</p></div><Link href="/produtos">Explorar catálogo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
