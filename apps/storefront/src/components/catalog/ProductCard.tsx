import Link from "next/link";
import { ArrowUpRight, Package } from "lucide-react";
import { formatPrice, type ProductSummary } from "@/lib/catalog";
export function ProductCard({product}:{product:ProductSummary}) { return <Link className="product-card" href={`/produtos/${product.slug}`}><div className="product-art" role="img" aria-label="Imagem não cadastrada"><Package size={42} strokeWidth={1.2} aria-hidden="true"/><span>IMAGEM EM PREPARAÇÃO</span></div><div className="product-card-body"><span className="product-brand">{product.brand}</span><h3>{product.name}</h3><p className="product-sku">Cód. {product.sku}</p><div className="product-card-bottom"><strong>{formatPrice(product.price)}</strong><span className={product.available>0?"stock-ok":"stock-out"}>{product.available>0?"Disponível":"Indisponível"}</span></div></div><ArrowUpRight className="product-corner" size={18} aria-hidden="true"/></Link> }

