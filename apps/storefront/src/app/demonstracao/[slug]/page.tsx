import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { ArrowLeft, ArrowUpRight, ChevronDown } from "lucide-react";
import { demoPieces } from "@/lib/demo-catalog";
import { demoImages } from "@/lib/demo-images";
import { formatPrice } from "@/lib/catalog";

export default async function DemoPiecePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const piece = demoPieces.find(item => item.slug === slug);
  if (!piece) notFound();

  return <main id="main-content" className="shell demo-detail demo-product-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><Link href="/#catalogo">Peças</Link><span>/</span><span>{piece.group}</span></nav>
    <div className="demo-detail-grid">
      <div className="demo-detail-art" role="img" aria-label={`Exemplo visual de ${piece.name.toLowerCase()}`}>
        <span className="showcase-art-mark">57<span>BRASIL</span></span>
        {demoImages[piece.slug] ? <Image src={demoImages[piece.slug]} alt="" fill sizes="(max-width: 700px) 100vw, 50vw" priority /> : <span aria-hidden="true">{piece.icon}</span>}
      </div>
      <div className="demo-product-info">
        <span className="section-kicker">PEÇA DEMONSTRATIVA · {piece.group}</span>
        <h1>{piece.name}</h1>
        <p className="demo-product-description">{piece.description}</p>
        <p className="demo-product-availability"><strong>Disponibilidade:</strong> exemplo de navegação</p>
        <div className="demo-product-price"><strong>{formatPrice(piece.demoPrice)}</strong><span>valor simulado · não é uma oferta</span></div>
        <Link className="btn btn-primary" href="/produtos">Consultar produtos cadastrados <ArrowUpRight size={17} aria-hidden="true" /></Link>
        <p className="demo-product-caution">Nenhum estoque ou compatibilidade comercial foi cadastrado para esta peça demonstrativa.</p>
        <Link className="back-link" href="/#catalogo"><ArrowLeft size={17} aria-hidden="true" /> Voltar à vitrine</Link>
      </div>
    </div>
    <section className="demo-product-more" aria-label="Informações da peça">
      <div className="demo-product-tabs"><span>MAIS INFORMAÇÕES</span><Link href="/produtos">PRODUTOS CADASTRADOS <ArrowUpRight size={14} aria-hidden="true" /></Link></div>
      <div className="demo-product-overview"><h2>Descrição geral da peça</h2><p>{piece.description}</p><p>Esta página apresenta um exemplo visual da categoria {piece.group.toLowerCase()}. O valor mostrado é fictício e não representa uma oferta.</p></div>
      <details className="demo-product-disclosure"><summary>Sobre este exemplo <ChevronDown size={18} aria-hidden="true" /></summary><p>As imagens e os valores desta vitrine são demonstrativos. Consulte o catálogo de produtos cadastrados para ver as peças disponíveis.</p></details>
    </section>
  </main>;
}
