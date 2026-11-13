import Link from "next/link";
import { notFound } from "next/navigation";
import type { Metadata } from "next";
import { ArrowLeft, CircleHelp, Package } from "lucide-react";
import { getProduct, getApplications, formatPrice } from "@/lib/catalog";
import { AddToCart } from "@/components/ui/AddToCart";

type Props = { params: Promise<{ slug: string }> };
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProduct(slug);
  return { title: product?.name ?? "Produto não encontrado", description: product?.shortDescription ?? undefined };
}
export default async function ProductPage({ params }: Props) {
  const { slug } = await params;
  const [product, applications] = await Promise.all([getProduct(slug), getApplications(slug).catch(() => null)]);
  if (!product) notFound();
  return <main id="main-content" className="shell product-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><Link href="/produtos">Produtos</Link><span>/</span><span>{product.category}</span></nav>
    <div className="product-detail"><div className="product-image" role="img" aria-label="Imagem não cadastrada"><div><Package size={66} strokeWidth={1.1} aria-hidden="true"/><p>Imagem ainda não cadastrada</p></div></div><div className="product-info"><span className="section-kicker">{product.brand}</span><h1>{product.name}</h1><div className="product-codes"><span>SKU {product.sku}</span>{product.manufacturerCode&&<span>Cód. fabricante {product.manufacturerCode}</span>}</div><div className="purchase-box"><strong className="detail-price">{formatPrice(product.price)}</strong><p className={product.available>0?"stock-ok":"stock-out"}>{product.available>0?"Disponível para compra":"Indisponível no momento"}</p><AddToCart productId={product.id} available={product.available}/></div><Link className="back-link" href="/produtos"><ArrowLeft size={17} aria-hidden="true"/> Continuar explorando peças</Link></div></div>
    <div className="product-lower"><section><h2>Descrição da peça</h2><p>{product.description || product.shortDescription || "Descrição técnica ainda não cadastrada."}</p></section><section><h2>Aplicação em veículos</h2><p>Confira se esta peça é compatível com seu veículo.</p>{applications===null?<p role="status">Não foi possível consultar as aplicações agora. Confirme a aplicação antes da compra.</p>:applications.length===0?<p>Compatibilidade não confirmada. Consulte a aplicação da peça.</p>:<ul>{applications.map((item,index)=><li key={`${item.make}-${item.model}-${item.version}-${index}`}>{item.make} {item.model} {item.version}, {item.startYear}–{item.endYear}{item.restriction?` — ${item.restriction}`:""}</li>)}</ul>}<div className="application-note"><CircleHelp size={18} aria-hidden="true"/><span>As aplicações exibidas dependem dos dados técnicos cadastrados para este produto.</span></div></section></div>
  </main>;
}
