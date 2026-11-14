import Link from "next/link";
import Image from "next/image";
import { ArrowRight, ChevronRight, ShieldCheck } from "lucide-react";
import { DemoPieceCard } from "@/components/catalog/DemoPieceCard";
import { DemoCatalogFilters } from "@/components/catalog/DemoCatalogFilters";
import { ProductCard } from "@/components/catalog/ProductCard";
import { demoPieces } from "@/lib/demo-catalog";
import { getProducts } from "@/lib/catalog";

type Props = { searchParams: Promise<{ q?: string; peca?: string; preco?: string }> };

export default async function Home({ searchParams }: Props) {
  const { q, peca, preco } = await searchParams;
  const products = await getProducts(new URLSearchParams()).catch(() => null);
  const allGroups = [...new Set(demoPieces.map(piece => piece.group))];
  const search = q?.trim().toLocaleLowerCase("pt-BR") ?? "";
  const visiblePieces = demoPieces.filter(piece => {
    if (peca && piece.group !== peca) return false;
    if (search && !`${piece.name} ${piece.group}`.toLocaleLowerCase("pt-BR").includes(search)) return false;
    if (preco === "ate-100") return piece.demoPrice <= 100;
    if (preco === "100-300") return piece.demoPrice > 100 && piece.demoPrice <= 300;
    if (preco === "acima-300") return piece.demoPrice > 300;
    return true;
  });
  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/banner-57brasil.png" alt="Peças automotivas e carro em composição nas cores verde e amarelo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content">
        <div className="store-hero-copy">
          <span className="hero-editorial-label">57 BRASIL <span aria-hidden="true">/</span> AUTOPEÇAS</span>
          <h1 id="hero-title">Encontre sua peça aqui.</h1>
          <p>Busque por veículo ou tipo de peça e escolha com mais clareza.</p>
          <div className="store-hero-actions"><Link className="btn btn-primary" href="/#catalogo">Explorar peças <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/veiculo">Selecionar meu veículo <ChevronRight size={17} aria-hidden="true" /></Link></div>
        </div>
      </div>
    </section>

    <section id="catalogo" className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell"><div className="store-section-head"><div><span className="section-kicker">VITRINE 57 BRASIL</span><h2 id="showcase-title">Encontre sua peça</h2><p className="catalog-intro">Veículo, sistema e busca em um só lugar.</p></div><Link className="inline-link" href="/produtos">Produtos cadastrados <ArrowRight size={17} aria-hidden="true" /></Link></div>{products?.items.length ? <div className="product-grid">{products.items.map(product => <ProductCard product={product} key={product.id} />)}</div> : <><DemoCatalogFilters groups={allGroups} selectedGroup={peca} query={q} price={preco} count={visiblePieces.length} /><p className="demo-notice">Valores simulados para demonstração. Sem oferta, estoque ou compatibilidade comercial.</p>{visiblePieces.length ? <div className="demo-piece-grid">{visiblePieces.map((piece,index) => <DemoPieceCard piece={piece} index={index} key={piece.slug} />)}</div> : <div className="catalog-empty"><h3>Nenhuma peça encontrada</h3><p>Altere a busca ou limpe os filtros.</p><Link href="/#catalogo" className="inline-link">Limpar filtros</Link></div>}</>}</div></section>

    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Escolha com informação</strong><p>Compatibilidade só é indicada quando há aplicação cadastrada. Confirme os dados técnicos antes da compra.</p></div><Link href="/veiculo">Buscar pelo veículo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
