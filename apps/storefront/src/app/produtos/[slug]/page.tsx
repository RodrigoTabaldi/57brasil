import Link from "next/link";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArrowLeft, ChevronDown, Package } from "lucide-react";
import { getProduct, getApplications, formatPrice } from "@/lib/catalog";
import { AddToCart } from "@/components/ui/AddToCart";
import { ProductShare } from "@/components/ui/ProductShare";
import { DemoImageViewer } from "@/components/catalog/DemoImageViewer";

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
  const image = product.images[0];
  const buyable = product.price !== null && product.available !== null;
  return <main id="main-content" className="shell product-page product-showcase-page">
    <nav className="breadcrumb" aria-label="Navegação estrutural"><Link href="/">Início</Link><span>/</span><Link href="/produtos">Produtos</Link><span>/</span><span>{product.category}</span></nav>
    <div className="product-detail">
      <div className="product-image product-showcase-image">{image ? <DemoImageViewer images={product.images} name={product.name} /> : <div role="img" aria-label="Imagem do produto ainda não cadastrada"><Package size={92} strokeWidth={1} aria-hidden="true" /><p>Imagem ainda não cadastrada</p></div>}</div>
      <div className="product-info">
        {product.brand && product.brandSlug && <Link className="section-kicker" href={`/produtos?marca=${encodeURIComponent(product.brandSlug)}`}>{product.brand}</Link>}
        <h1>{product.name}</h1>
        <Link className="product-showcase-category" href={`/produtos?categoria=${encodeURIComponent(product.categorySlug)}`}>{product.category}</Link>
        {product.shortDescription && <p>{product.shortDescription}</p>}
        <div className="product-codes"><span>SKU {product.sku}</span>{product.manufacturerCode && <span>Cód. fabricante {product.manufacturerCode}</span>}</div>
        <div className="purchase-box">{buyable ? <><strong className="detail-price">{formatPrice(product.price!)}</strong><p className={product.available! > 0 ? "stock-ok" : "stock-out"}>{product.available! > 0 ? `Em estoque · ${product.available} disponíveis` : "Produto indisponível"}</p><AddToCart productId={product.id} available={product.available!} price={product.price!} slug={product.slug} /></> : <><strong>Preço sob consulta</strong><p id="purchase-unavailable">Preço, estoque e dados logísticos ainda não validados para venda.</p><button className="button" type="button" disabled aria-describedby="purchase-unavailable">COMPRAR</button></>}<ProductShare name={product.name} /></div>
        <Link className="back-link" href="/produtos"><ArrowLeft size={17} aria-hidden="true" /> Voltar ao catálogo</Link>
      </div>
    </div>
    <div className="product-showcase-sections">
      <details open><summary>Descrição do produto <ChevronDown size={19} aria-hidden="true" /></summary><div className="product-showcase-panel"><p>{product.description || product.shortDescription || "Descrição ainda não cadastrada."}</p></div></details>
      <details><summary>Especificações técnicas <ChevronDown size={19} aria-hidden="true" /></summary><div className="product-showcase-panel"><dl><dt>SKU interno</dt><dd>{product.sku}</dd>{product.brand && <><dt>Marca</dt><dd>{product.brand}</dd></>}{product.manufacturerCode && <><dt>Código do fabricante</dt><dd>{product.manufacturerCode}</dd></>}{product.weightKg != null && <><dt>Peso</dt><dd>{product.weightKg} kg</dd></>}{product.heightCm != null && product.widthCm != null && product.lengthCm != null && <><dt>Dimensões</dt><dd>{product.heightCm} × {product.widthCm} × {product.lengthCm} cm</dd></>}</dl></div></details>
      <details><summary>Compatibilidade com veículos <ChevronDown size={19} aria-hidden="true" /></summary><div className="product-showcase-panel">{applications === null ? <p>Não foi possível consultar a compatibilidade.</p> : applications.length === 0 ? <p>Compatibilidade ainda não cadastrada.</p> : <ul>{applications.map((item, index) => <li key={`${item.make}-${item.model}-${item.version}-${index}`}>{item.make} {item.model} {item.version}, {item.startYear}–{item.endYear}{item.restriction ? ` — ${item.restriction}` : ""}</li>)}</ul>}</div></details>
    </div>
  </main>;
}
