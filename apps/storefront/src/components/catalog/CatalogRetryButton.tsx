"use client";

import { useTransition } from "react";
import { useRouter } from "next/navigation";

export function CatalogRetryButton() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  return <>
    <button className="button" type="button" disabled={isPending} onClick={() => startTransition(() => router.refresh())}>
      {isPending ? "Atualizando catálogo…" : "Tentar novamente"}
    </button>
    <span className="sr-only" role="status" aria-live="polite" aria-atomic="true">
      {isPending ? "Atualizando o catálogo…" : ""}
    </span>
  </>;
}
