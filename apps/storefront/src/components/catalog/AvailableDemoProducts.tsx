"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { AddToCart } from "@/components/ui/AddToCart";
import { formatPrice, type ProductPage, type ProductSummary } from "@/lib/catalog";

export function AvailableDemoProducts({ name }: { name: string }) {
  const [matches, setMatches] = useState<ProductSummary[] | null>(null);
  const [error, setError] = useState(false);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    const params = new URLSearchParams({ q: name });
    fetch(`/api/v1/products?${params}`, { signal: controller.signal, cache: "no-store" })
      .then(response => {
        if (!response.ok) throw new Error("Não foi possível consultar os produtos.");
        return response.json() as Promise<ProductPage>;
      })
      .then(result => {
        setError(false);
        setMatches(result.items.filter(item => item.name.toLocaleLowerCase("pt-BR").includes(name.toLocaleLowerCase("pt-BR"))));
      })
      .catch(() => { if (!controller.signal.aborted) setError(true); });
    return () => controller.abort();
  }, [name, attempt]);

  if (error) return <div role="status"><p>Os produtos cadastrados estão indisponíveis no momento. Esta imagem é demonstrativa e não confirma uma peça à venda.</p><button type="button" onClick={() => { setError(false); setAttempt(value => value + 1); }}>Tentar novamente</button></div>;
  if (matches === null) return <p role="status">Consultando produtos cadastrados…</p>;
  if (matches.length === 0) return <p>Nenhum produto cadastrado e disponível para compra nesta categoria.</p>;

  return matches.map(product => <div className="demo-purchase-item" key={product.id}>
    <Link href={`/produtos/${product.slug}`}>{product.name}</Link>
    <p>{product.brand} · SKU {product.sku}</p>
    <strong>{formatPrice(product.price)}</strong>
    <AddToCart productId={product.id} available={product.available} price={product.price} slug={product.slug} />
  </div>);
}
