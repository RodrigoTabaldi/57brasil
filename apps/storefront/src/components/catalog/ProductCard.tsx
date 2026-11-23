import Link from "next/link";
import { ArrowUpRight, Package } from "lucide-react";
import { formatPrice, type ProductSummary } from "@/lib/catalog";
import { CatalogImage } from "@/components/catalog/CatalogImage";

export function ProductCard({ product }: { product: ProductSummary }) {
  return <Link className="product-card showcase-card" href={`/produtos/${product.slug}`}>
    <div className="product-art showcase-art">
      <span className="showcase-art-mark">57<span>BRASIL</span></span>
      {product.imageUrl ? <CatalogImage src={product.imageUrl} alt={`Imagem de referência de ${product.name}`} sizes="(max-width: 650px) 50vw, (max-width: 900px) 33vw, 25vw" /> : <><Package size={64} strokeWidth={1} aria-hidden="true" /><span className="showcase-image-note">Imagem não cadastrada</span></>}
      {product.price == null && product.imageUrl && <span className="product-image-label">Imagem ilustrativa</span>}
    </div>
    <div className="product-card-body">
      {product.brand && <span className="product-brand">{product.brand}</span>}
      <h3>{product.name}</h3>
      <p className="product-sku">Cód. {product.sku}</p>
      <div className="product-card-bottom"><strong>{product.price == null ? "Preço sob consulta" : formatPrice(product.price)}</strong><span className={product.available != null && product.available > 0 ? "stock-ok" : "stock-out"}>{product.available == null ? "Venda indisponível" : product.available > 0 ? "Disponível" : "Indisponível"}</span></div>
      <span className="showcase-card-link">Ver detalhes <ArrowUpRight size={16} aria-hidden="true" /></span>
    </div>
  </Link>;
}
