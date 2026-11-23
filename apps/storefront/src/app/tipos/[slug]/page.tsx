import Link from "next/link";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getPartType } from "@/lib/catalog";
import { CatalogImage } from "@/components/catalog/CatalogImage";

type Props = { params: Promise<{ slug: string }> };
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const item = await getPartType((await params).slug);
  return { title: item ? `${item.name} | Tipo de peça` : "Tipo de peça não encontrado" };
}
export default async function PartTypeDetail({ params }: Props) {
  const item = await getPartType((await params).slug);
  if (!item) notFound();
  return <main id="main-content" className="shell product-page part-type-detail">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><Link href="/produtos">Catálogo</Link><span>/</span><span>{item.name}</span></nav>
    <div className="product-detail">
      <div className="product-image part-type-detail-image"><CatalogImage src={item.imageUrl} alt={`Imagem ilustrativa de ${item.name}, do grupo ${item.group}`} sizes="(max-width: 700px) 100vw, 50vw" /></div>
      <div className="product-info"><span className="section-kicker">PEÇA PARA CONSULTA</span><h1>{item.name}</h1>
        <p>{item.group}</p>
        <div className="purchase-box"><strong>Sobre este tipo de peça</strong><p>Este cadastro identifica uma categoria de peça para ajudar na sua busca. A foto é ilustrativa; a escolha de um item específico e sua aplicação no veículo precisam ser confirmadas antes da compra.</p><Link href="/creditos-imagens">Ver créditos das imagens</Link></div>
        <Link className="back-link" href="/produtos">Voltar ao catálogo</Link>
      </div>
    </div>
  </main>;
}
