import Link from "next/link";
import Image from "next/image";
import { ArrowRight, Bike, CarFront, ChevronRight, PackageSearch, ShieldCheck, Truck, Wrench } from "lucide-react";
import { DemoPieceCard } from "@/components/catalog/DemoPieceCard";
import { ProductCard } from "@/components/catalog/ProductCard";
import { demoPieces } from "@/lib/demo-catalog";
import { getProducts } from "@/lib/catalog";

const journeys = [
  { label: "Carro e SUV", href: "/explorar?tipo=carro", icon: CarFront, description: "Explore peças por tipo de veículo" },
  { label: "Moto", href: "/explorar?tipo=moto", icon: Bike, description: "Conheça linhas de peças" },
  { label: "Caminhão", href: "/explorar?tipo=caminhao", icon: Truck, description: "Explore a linha pesada" },
  { label: "Tipo de peça", href: "/explorar", icon: Wrench, description: "Navegue por sistemas" },
] as const;

export default async function Home() {
  const products = await getProducts(new URLSearchParams()).catch(() => null);
  const groups = [...new Set(demoPieces.map(piece => piece.group))].slice(0, 8);
  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/hero-fleet.png" alt="Carro, motocicleta e caminhão em estúdio automotivo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content">
        <div className="store-hero-copy">
          <h1 id="hero-title">Sua peça começa por aqui.</h1>
          <p>Explore por veículo ou por tipo de peça. Consulte os detalhes antes de escolher.</p>
          <div className="store-hero-actions"><Link className="btn btn-primary" href="/explorar">Explorar peças <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/veiculo">Selecionar meu veículo <ChevronRight size={17} aria-hidden="true" /></Link></div>
        </div>
      </div>
    </section>

    <section className="shell journey-section" aria-labelledby="journey-title">
      <div className="journey-heading"><div><span className="section-kicker">COMECE POR AQUI</span><h2 id="journey-title">Como você quer encontrar sua peça?</h2></div><p>Escolha um caminho para abrir o catálogo.</p></div>
      <div className="journey-grid">{journeys.map(({label,href,icon:Icon,description}) => <Link className="journey-card" href={href} key={href}><span className="journey-icon"><Icon size={27} strokeWidth={1.7} aria-hidden="true" /></span><strong>{label}</strong><small>{description}</small><ArrowRight className="journey-arrow" size={19} aria-hidden="true" /></Link>)}</div>
    </section>

    <section className="store-departments" aria-labelledby="departments-title"><div className="shell"><div className="store-section-head"><div><span className="section-kicker">ENCONTRE MAIS RÁPIDO</span><h2 id="departments-title">Peças por sistema</h2></div><Link href="/explorar" className="inline-link">Ver todos os tipos <ArrowRight size={17} aria-hidden="true" /></Link></div><div className="store-group-grid">{groups.map((group,index) => <Link href={`/explorar?peca=${encodeURIComponent(group)}`} className="store-group" key={group}><span>{String(index+1).padStart(2,"0")}</span><strong>{group}</strong><ArrowRight size={18} aria-hidden="true" /></Link>)}</div></div></section>

    <section className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell"><div className="store-section-head"><div><span className="section-kicker">VITRINE 57 BRASIL</span><h2 id="showcase-title">Explore o catálogo</h2></div><Link className="inline-link" href="/produtos">Produtos cadastrados <ArrowRight size={17} aria-hidden="true" /></Link></div>{products?.items.length ? <div className="product-grid">{products.items.slice(0,8).map(product => <ProductCard product={product} key={product.id} />)}</div> : process.env.NODE_ENV === "development" ? <><p className="demo-notice">Demonstração de tipos de peças. Sem preço, estoque ou compatibilidade comercial.</p><div className="demo-piece-grid">{demoPieces.slice(0,8).map((piece,index) => <DemoPieceCard piece={piece} index={index} key={piece.slug} />)}</div></> : <div className="home-empty"><PackageSearch size={30} aria-hidden="true" /><div><h3>Catálogo em preparação</h3><p>Os produtos aparecerão aqui após o cadastro pela loja.</p></div></div>}</div></section>

    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Escolha com informação</strong><p>Compatibilidade só é indicada quando há aplicação cadastrada. Confirme os dados técnicos antes da compra.</p></div><Link href="/veiculo">Buscar pelo veículo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
