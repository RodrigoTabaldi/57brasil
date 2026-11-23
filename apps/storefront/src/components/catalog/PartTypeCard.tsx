import Link from "next/link";
import type { PartType } from "@/lib/catalog";
import { CatalogImage } from "@/components/catalog/CatalogImage";

export function PartTypeCard({ item }: { item: PartType }) {
  return <article className="product-card showcase-card part-type-card">
    <Link className="part-type-card-link" href={`/tipos/${item.slug}`}>
      <div className="product-art showcase-art">
        <CatalogImage src={item.imageUrl} alt={`Imagem ilustrativa de ${item.name}, do grupo ${item.group}`} sizes="(max-width: 650px) 50vw, (max-width: 900px) 33vw, 25vw" />
        <span className="part-type-image-label">Imagem ilustrativa</span>
      </div>
      <div className="product-card-body">
        <span className="product-brand">{item.group}</span>
        <h3>{item.name}</h3>
        <p className="part-type-status">Preço e disponibilidade a confirmar</p>
      </div>
    </Link>

  </article>;
}
