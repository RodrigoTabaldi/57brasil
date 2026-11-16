import Link from "next/link";
import { ArrowUpRight, Package } from "lucide-react";
import { formatPrice, type ProductSummary } from "@/lib/catalog";

export function ProductCard({ product }: { product: ProductSummary }) {
  return <Link className="product-card showcase-card" href={`/produtos/${product.slug}`}>
    <div className="product-art showcase-art" role="img" aria-label="Imagem do produto ainda não cadastrada">
      <span className="showcase-art-mark">57<span>BRASIL</span></span>
      <Package size={64} strokeWidth={1} aria-hidden="true" />
      <span className="showcase-image-note">Imagem em preparação</span>
    </div>
    <div className="product-card-body">
      <span className="product-brand">{product.brand}</span>
      <h3>{product.name}</h3>
      <p className="product-sku">Cód. {product.sku}</p>
      <div className="product-card-bottom"><strong>{formatPrice(product.price)}</strong><span className={product.available > 0 ? "stock-ok" : "stock-out"}>{product.available > 0 ? "Disponível" : "Indisponível"}</span></div>
      <span className="showcase-card-link">Ver detalhes <ArrowUpRight size={16} aria-hidden="true" /></span>
    </div>
  </Link>;
}
