"use client";

import { useEffect, useState } from "react";

export function ProductShare({ name }: { name: string }) {
  const [message, setMessage] = useState("");
  const [pageUrl, setPageUrl] = useState("");
  useEffect(() => setPageUrl(window.location.href), []);
  function url() { return window.location.href; }
  async function copy() {
    try { await navigator.clipboard.writeText(url()); setMessage("Link copiado."); }
    catch { setMessage("Não foi possível copiar o link."); }
  }
  return <details className="product-share"><summary>Enviar para um amigo</summary><div>
    <button type="button" onClick={() => void copy()}>Copiar link</button>
    {pageUrl && <><a href={`https://wa.me/?text=${encodeURIComponent(`${name} ${pageUrl}`)}`} target="_blank" rel="noopener noreferrer">WhatsApp</a>
    <a href={`mailto:?subject=${encodeURIComponent(name)}&body=${encodeURIComponent(pageUrl)}`}>E-mail</a></>}
    <button type="button" onClick={() => { if (navigator.share) void navigator.share({ title: name, url: url() }).catch(() => undefined); else setMessage("Compartilhamento nativo indisponível neste navegador."); }}>Compartilhar no dispositivo</button>
  </div><p role="status">{message}</p></details>;
}
