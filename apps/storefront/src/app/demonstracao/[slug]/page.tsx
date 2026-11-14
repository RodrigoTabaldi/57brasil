import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { demoPieces } from "@/lib/demo-catalog";
import { demoImages } from "@/lib/demo-images";
import { formatPrice } from "@/lib/catalog";

export default async function DemoPiecePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const piece = demoPieces.find(item => item.slug === slug);
  if (!piece) notFound();
  return <main id="main-content" className="shell demo-detail"><span className="section-kicker">PEÇA DEMONSTRATIVA · {piece.group}</span><h1>{piece.name}</h1><div className="demo-detail-grid"><span className="demo-detail-art">{demoImages[piece.slug] ? <Image src={demoImages[piece.slug]} alt={`Exemplo visual de ${piece.name.toLowerCase()}`} fill sizes="(max-width: 700px) 100vw, 50vw" /> : <span aria-hidden="true">{piece.icon}</span>}</span><div><h2>Conheça a categoria</h2><p className="demo-detail-price">{formatPrice(piece.demoPrice)} <small>valor simulado</small></p><p>{piece.description}</p><p>Este é um exemplo de navegação. O valor exibido é fictício e não representa oferta. Nenhum produto comercial, estoque ou compatibilidade foi cadastrado para esta categoria.</p><Link className="btn btn-primary" href="/produtos">Consultar produtos cadastrados</Link></div></div></main>;
}
