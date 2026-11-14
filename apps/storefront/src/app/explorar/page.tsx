import { redirect } from "next/navigation";

type Props = { searchParams: Promise<{ tipo?: string; peca?: string; q?: string; preco?: string }> };

export default async function ExplorePage({ searchParams }: Props) {
  const { tipo, peca, q, preco } = await searchParams;
  if (tipo) redirect("/veiculo");

  const params = new URLSearchParams();
  if (peca) params.set("peca", peca);
  if (q) params.set("q", q);
  if (preco) params.set("preco", preco);
  redirect(`/${params.size ? `?${params}` : ""}#catalogo`);
}
