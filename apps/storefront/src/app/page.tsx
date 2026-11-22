import Link from "next/link";
import Image from "next/image";
import { ArrowRight, ChevronRight, ShieldCheck } from "lucide-react";
import { PartTypeCard } from "@/components/catalog/PartTypeCard";
import { getPartTypeGroups, getPartTypes } from "@/lib/catalog";

export default async function Home() {
  const [typesResult, groupsResult] = await Promise.allSettled([
    getPartTypes(new URLSearchParams()), getPartTypeGroups()
  ]);
  const types = typesResult.status === "fulfilled" ? typesResult.value : null;
  const groups = groupsResult.status === "fulfilled" ? groupsResult.value : [];
  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/banner-57brasil.png" alt="Peças automotivas e carro em composição nas cores verde e amarelo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content"><div className="store-hero-copy">
        <span className="hero-editorial-label">57 BRASIL <span aria-hidden="true">/</span> AUTOPEÇAS</span>
        <h1 id="hero-title">Encontre sua peça aqui.</h1>
        <p>Explore o catálogo por tipo de peça e confira os detalhes antes de escolher.</p>
        <div className="store-hero-actions"><Link className="btn btn-primary" href="/produtos#tipos">Explorar catálogo <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/veiculo">Selecionar meu veículo <ChevronRight size={17} aria-hidden="true" /></Link></div>
      </div></div>
    </section>
    <section id="catalogo" className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell">
      <div className="store-section-head"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h2 id="showcase-title">Tipos de peças</h2><p className="catalog-intro">{types ? `${types.total} tipos de peças no catálogo` : "Catálogo temporariamente indisponível"}</p></div><Link className="inline-link" href="/produtos#tipos">Ver catálogo completo <ArrowRight size={17} aria-hidden="true" /></Link></div>
      {groups.length > 0 && <nav className="part-type-group-links" aria-label="Explorar grupos de peças">{groups.map(group => <Link key={group.slug} href={`/produtos?grupo=${encodeURIComponent(group.slug)}#tipos`}>{group.name}<span>{group.count}</span></Link>)}</nav>}
      {types?.items.length ? <div className="product-grid">{types.items.slice(0, 12).map(item => <PartTypeCard item={item} key={item.id} />)}</div> : <div className="catalog-empty" role="status"><h3>{types ? "Nenhum tipo de peça cadastrado" : "Não foi possível carregar o catálogo"}</h3><p>{types ? "Explore os grupos ou busque pelo nome da peça." : "Tente novamente em instantes."}</p><Link href="/produtos" className="inline-link">Abrir catálogo</Link></div>}
    </div></section>
    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Escolha com informação</strong><p>A aplicação de uma peça no veículo depende do item específico. Confira os dados técnicos antes da compra.</p></div><Link href="/veiculo">Buscar pelo veículo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
