import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { CatalogImage } from "@/components/catalog/CatalogImage";
import { formatPrice, getPartType } from "@/lib/catalog";

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const item = await getPartType((await params).slug);
  return { title: item ? `${item.name} | Catálogo 57 Brasil` : "Peça não encontrada" };
}

export default async function PartTypeDetail({ params }: Props) {
  const item = await getPartType((await params).slug);
  if (!item) notFound();

  return <main id="main-content" className="shell catalog-page part-type-detail-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><Link href="/produtos">Catálogo</Link><span>/</span><span>{item.name}</span></nav>
    <Link className="back-link" href={`/produtos?grupo=${encodeURIComponent(item.groupSlug)}`}>Voltar ao grupo {item.group}</Link>
    <article className="part-type-detail">
      <div className="part-type-detail-image"><CatalogImage src={item.imageUrl} alt={`Imagem ilustrativa de ${item.name}`} sizes="(max-width: 800px) 100vw, 50vw" priority /></div>
      <div className="part-type-detail-info"><span className="section-kicker">{item.group}</span><h1>{item.name}</h1><p className="part-type-status">Preço de referência: <strong>{formatPrice(item.referencePrice)}</strong>. Disponibilidade a confirmar.</p>
        <section className="part-type-purchase-box"><h2>Informações do produto</h2><p>SKU: {item.sku} · Marca: {item.brand} · Código do fabricante: {item.manufacturerCode}</p><p>{item.description}</p><p>Confirme a aplicação com o fabricante antes da compra.</p></section>
        <p className="part-type-image-note">A imagem é ilustrativa e segue o padrão visual do catálogo; ela não confirma fabricante, aplicação ou compatibilidade.</p>
        <Link className="inline-link" href="/creditos-imagens">Ver créditos das imagens</Link>
      </div>
    </article>
  </main>;
}
