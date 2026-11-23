"use client";

import { CatalogRouteError } from "@/components/catalog/CatalogRouteError";

export default function PartTypeError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <CatalogRouteError itemLabel="este tipo de peça" reset={reset} />;
}
