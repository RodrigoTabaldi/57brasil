import { redirect } from "next/navigation";

type Props = { searchParams: Promise<{ tipo?: string; peca?: string; q?: string; marcaDemo?: string }> };

export default async function ExplorePage({ searchParams }: Props) {
  const { tipo, peca, q, marcaDemo } = await searchParams;
  if (tipo) redirect("/veiculo");

  const params = new URLSearchParams();
  if (q) params.set("q", q);
  else if (peca) params.set("q", peca);
  void marcaDemo;
  redirect(`/produtos${params.size ? `?${params}` : ""}`);
}
