import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { ArrowLeft, ArrowUpRight, ChevronDown } from "lucide-react";
import { demoPieces } from "@/lib/demo-catalog";
import { demoImages } from "@/lib/demo-images";
import { AvailableDemoProducts } from "@/components/catalog/AvailableDemoProducts";

export function generateStaticParams() {
  return demoPieces.map(({ slug }) => ({ slug }));
}

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
        <section className="demo-purchase" aria-label="Produtos cadastrados">
          <h2>Produtos disponíveis</h2>
          <AvailableDemoProducts name={piece.name} />
        </section>
        <Link className="btn btn-primary" href="/produtos">Consultar todos os produtos cadastrados <ArrowUpRight size={17} aria-hidden="true" /></Link>
        <Link className="back-link" href="/#catalogo"><ArrowLeft size={17} aria-hidden="true" /> Voltar à vitrine</Link>
      </div>
    </div>
    <section className="demo-product-more" aria-label="Informações da peça">
      <div className="demo-product-tabs"><span>MAIS INFORMAÇÕES</span><Link href="/produtos">PRODUTOS CADASTRADOS <ArrowUpRight size={14} aria-hidden="true" /></Link></div>
      <div className="demo-product-overview"><h2>Descrição geral da peça</h2><p>{piece.description}</p><p>Esta página apresenta um exemplo visual da categoria {piece.group.toLowerCase()}. As ofertas acima, quando disponíveis, vêm do catálogo cadastrado.</p></div>
      <details className="demo-product-disclosure"><summary>Sobre este exemplo <ChevronDown size={18} aria-hidden="true" /></summary><p>A imagem desta peça é demonstrativa. Consulte os produtos cadastrados para verificar preço e disponibilidade reais.</p></details>
    </section>
  </main>;
}
