import { notFound, redirect } from "next/navigation";
import { demoPieces } from "@/lib/demo-catalog";
import { getProducts } from "@/lib/catalog";

export default async function LegacyPiecePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const piece = demoPieces.find(item => item.slug === slug);
  if (!piece) notFound();
  const products = await getProducts(new URLSearchParams({ q: piece.name })).catch(() => null);
  const exact = products?.items.find(item => item.name.toLocaleLowerCase("pt-BR") === piece.name.toLocaleLowerCase("pt-BR"));
  if (exact) redirect(`/produtos/${exact.slug}`);
  redirect(`/produtos?q=${encodeURIComponent(piece.name)}`);
}
