"use client";

import { CatalogRouteError } from "@/components/catalog/CatalogRouteError";

export default function ProductError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <CatalogRouteError itemLabel="este produto" reset={reset} />;
}
