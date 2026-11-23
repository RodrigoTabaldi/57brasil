"use client";

import Image from "next/image";
import { useEffect, useRef, useState } from "react";

type ProductImage = { id: string; url: string; altText: string };
export function DemoImageViewer({ images, name }: { images: ProductImage[]; name: string }) {
  const [index, setIndex] = useState(0);
  const [open, setOpen] = useState(false);
  const [failed, setFailed] = useState<string[]>([]);
  const triggerButton = useRef<HTMLButtonElement>(null);
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
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      triggerButton.current?.focus();
    };
  }, [open, images.length]);
  if (!image) return null;
  const unavailable = failed.includes(image.id);
  const markFailed = () => setFailed(current => current.includes(image.id) ? current : [...current, image.id]);
  return <div className="product-gallery">
    <button ref={triggerButton} className="demo-image-trigger" type="button" onClick={() => { if (!unavailable) setOpen(true); }} aria-label={unavailable ? `Imagem de ${name} indisponível` : `Ampliar imagem de ${name}`} aria-disabled={unavailable}>
      {unavailable ? <span role="status">Imagem indisponível</span> : <Image src={image.url} alt={image.altText} fill sizes="(max-width: 700px) 100vw, 50vw" priority unoptimized={image.url.startsWith("https://")} onError={markFailed} />}
      {!unavailable && <span>Ampliar imagem</span>}
    </button>
    {images.length > 1 && <div className="product-gallery-thumbnails" aria-label="Imagens do produto">{images.map((item, position) => <button key={item.id} type="button" aria-label={`Ver imagem ${position + 1} de ${name}`} aria-current={position === index ? "true" : undefined} onClick={() => setIndex(position)}><Image src={item.url} alt="" width={72} height={72} unoptimized={item.url.startsWith("https://")} onError={() => setFailed(current => current.includes(item.id) ? current : [...current, item.id])} /></button>)}</div>}
    {open && <div className="demo-image-overlay" role="presentation" onClick={event => { if (event.target === event.currentTarget) setOpen(false); }}><div className="demo-image-dialog" role="dialog" aria-modal="true" aria-label={`Imagens ampliadas de ${name}`} onKeyDown={event => { if (event.key !== "Tab") return; const buttons = [...event.currentTarget.querySelectorAll("button")]; const first = buttons[0]; const last = buttons[buttons.length - 1]; if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); } }}>
      <button ref={closeButton} className="demo-image-close" type="button" onClick={() => setOpen(false)}>Fechar imagem</button>
      <div>{unavailable ? <p role="status">Imagem indisponível.</p> : <Image src={image.url} alt={image.altText} fill sizes="90vw" unoptimized={image.url.startsWith("https://")} onError={markFailed} />}</div>
      <p aria-live="polite" aria-atomic="true">{index + 1} de {images.length} · Imagem vinculada a este cadastro. Confirme os dados do item antes da compra.</p>
      {images.length > 1 && <div className="product-gallery-controls"><button type="button" onClick={() => setIndex(current => (current - 1 + images.length) % images.length)}>Anterior</button><button type="button" onClick={() => setIndex(current => (current + 1) % images.length)}>Próxima</button></div>}
    </div></div>}
  </div>;
}
