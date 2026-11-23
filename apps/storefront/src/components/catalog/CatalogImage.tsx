"use client";

import Image from "next/image";
import { ImageOff } from "lucide-react";
import { useState } from "react";

type Props = {
  src: string;
  alt: string;
  sizes: string;
  priority?: boolean;
};

export function CatalogImage({ src, alt, sizes, priority = false }: Props) {
  const [failed, setFailed] = useState(false);

  if (failed) {
    return <div className="catalog-image-fallback" role="img" aria-label={`${alt}. Imagem indisponível`}>
      <ImageOff size={30} strokeWidth={1.5} aria-hidden="true" />
      <span>Imagem indisponível</span>
    </div>;
  }

  return <Image src={src} alt={alt} fill sizes={sizes} priority={priority} onError={() => setFailed(true)} />;
}
