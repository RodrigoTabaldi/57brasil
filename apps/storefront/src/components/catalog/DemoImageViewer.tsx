"use client";

import Image from "next/image";
import { useEffect, useRef, useState } from "react";

type ProductImage = { id: string; url: string; altText: string };
export function DemoImageViewer({ images, name }: { images: ProductImage[]; name: string }) {
  const [index, setIndex] = useState(0);
  const [open, setOpen] = useState(false);
  const [failed, setFailed] = useState<string[]>([]);
  const closeButton = useRef<HTMLButtonElement>(null);
  const image = images[index];
  useEffect(() => {
    if (!open) return;
    closeButton.current?.focus();
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
      if (event.key === "ArrowRight") setIndex(current => (current + 1) % images.length);
      if (event.key === "ArrowLeft") setIndex(current => (current - 1 + images.length) % images.length);
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, images.length]);
  if (!image) return null;
  const unavailable = failed.includes(image.id);
  const markFailed = () => setFailed(current => current.includes(image.id) ? current : [...current, image.id]);
  return <div className="product-gallery">
    <button className="demo-image-trigger" type="button" onClick={() => setOpen(true)} aria-label={`Ampliar imagem de ${name}`} disabled={unavailable}>
      {unavailable ? <span>Imagem indisponível</span> : <Image src={image.url} alt={image.altText} fill sizes="(max-width: 700px) 100vw, 50vw" priority onError={markFailed} />}
      {!unavailable && <span>Ampliar imagem</span>}
    </button>
    {images.length > 1 && <div className="product-gallery-thumbnails" aria-label="Imagens do produto">{images.map((item, position) => <button key={item.id} type="button" aria-label={`Ver imagem ${position + 1} de ${name}`} aria-current={position === index ? "true" : undefined} onClick={() => setIndex(position)}><Image src={item.url} alt="" width={72} height={72} onError={() => setFailed(current => current.includes(item.id) ? current : [...current, item.id])} /></button>)}</div>}
    {open && <div className="demo-image-overlay" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setOpen(false); }}><div className="demo-image-dialog" role="dialog" aria-modal="true" aria-label={`Imagens ampliadas de ${name}`} onKeyDown={event => { if (event.key !== "Tab") return; const buttons = [...event.currentTarget.querySelectorAll("button")]; const first = buttons[0]; const last = buttons[buttons.length - 1]; if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); } }}>
      <button ref={closeButton} className="demo-image-close" type="button" onClick={() => setOpen(false)}>Fechar imagem</button>
      <div>{unavailable ? <p>Imagem indisponível.</p> : <Image src={image.url} alt={image.altText} fill sizes="90vw" onError={markFailed} />}</div>
      <p>{index + 1} de {images.length} · Imagem vinculada a este cadastro. Confirme os dados do item antes da compra.</p>
      {images.length > 1 && <div className="product-gallery-controls"><button type="button" onClick={() => setIndex(current => (current - 1 + images.length) % images.length)}>Anterior</button><button type="button" onClick={() => setIndex(current => (current + 1) % images.length)}>Próxima</button></div>}
    </div></div>}
  </div>;
}
