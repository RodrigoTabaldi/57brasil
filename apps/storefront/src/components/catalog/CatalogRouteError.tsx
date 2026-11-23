"use client";

import Link from "next/link";
import { useTransition } from "react";

type Props = { itemLabel: string; reset: () => void };

export function CatalogRouteError({ itemLabel, reset }: Props) {
  const [isPending, startTransition] = useTransition();

  return <main id="main-content" className="shell empty-page">
    <div role="alert">
      <h1>Não foi possível carregar {itemLabel}.</h1>
      <p>Tente novamente. Se o problema continuar, volte ao catálogo e escolha outro item.</p>
    </div>
    <div className="route-error-actions">
      <button className="button" type="button" disabled={isPending} onClick={() => startTransition(reset)}>
        {isPending ? "Tentando novamente…" : "Tentar novamente"}
      </button>
      <Link className="inline-link" href="/produtos">Voltar ao catálogo</Link>
      <span className="sr-only" role="status" aria-live="polite" aria-atomic="true">
        {isPending ? `Carregando ${itemLabel}…` : ""}
      </span>
    </div>
  </main>;
}
