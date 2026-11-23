import Link from "next/link";
import { ArrowUpRight, Package } from "lucide-react";
import { formatPrice, type ProductSummary } from "@/lib/catalog";
import { CatalogImage } from "@/components/catalog/CatalogImage";
import { AddToCart } from "@/components/ui/AddToCart";

export function ProductCard({ product }: { product: ProductSummary }) {
  const detailUrl = `/produtos/${product.slug}`;
  return <article className="product-card showcase-card catalog-product-card">
    <Link className="catalog-product-image-link" href={detailUrl} aria-label={`Ver detalhes de ${product.name}`}>
      <div className="product-art showcase-art">
        <span className="showcase-art-mark">57<span>BRASIL</span></span>
        {product.imageUrl ? <CatalogImage src={product.imageUrl} alt={`Imagem de referência de ${product.name}`} sizes="(max-width: 650px) 50vw, (max-width: 900px) 33vw, 25vw" /> : <><Package size={64} strokeWidth={1} aria-hidden="true" /><span className="showcase-image-note">Imagem não cadastrada</span></>}
      </div>
    </Link>
    <div className="product-card-body">
      <span className="product-brand">{product.brand}</span>
      <Link className="catalog-product-name" href={detailUrl}><h3>{product.name}</h3></Link>
      <p className="product-sku">Cód. {product.sku}</p>
      <div className="product-card-bottom"><strong>{formatPrice(product.price)}</strong><span className="stock-ok">Disponível</span></div>
      <div className="product-card-actions">
        <Link className="showcase-card-link" href={detailUrl}>Ver detalhes <ArrowUpRight size={16} aria-hidden="true" /></Link>
        <AddToCart compact productId={product.id} available={product.available} price={product.price} slug={product.slug} />
      </div>
    </div>
  </article>;
}
