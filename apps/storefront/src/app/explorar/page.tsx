import Link from "next/link";
import { demoPieces } from "@/lib/demo-catalog";
import { DemoPieceCard } from "@/components/catalog/DemoPieceCard";

const vehicleTypes = [
  { slug: "carro", label: "Carro" },
  { slug: "moto", label: "Moto" },
  { slug: "caminhao", label: "Caminhão" },
] as const;

type Props = { searchParams: Promise<{ tipo?: string; peca?: string; q?: string }> };
export default async function ExplorePage({ searchParams }: Props) {
  const { tipo, peca, q } = await searchParams;
  const selectedType = vehicleTypes.find(item => item.slug === tipo);
  const groups = [...new Set(demoPieces.map(item => item.group))];
  const search = q?.trim().toLocaleLowerCase("pt-BR") ?? "";
  const pieces = demoPieces.filter(item => (!peca || item.group === peca) && (!search || `${item.name} ${item.group}`.toLocaleLowerCase("pt-BR").includes(search)));
  const exploreHref = (nextType?: string, nextGroup?: string) => {
    const query = new URLSearchParams();
    if (nextType) query.set("tipo", nextType);
    if (nextGroup) query.set("peca", nextGroup);
    if (q) query.set("q", q);
    return `/explorar${query.size ? `?${query}` : ""}`;
  };
  return <main id="main-content" className="shell explore-page">
    <Link className="explore-back" href="/">← Voltar à loja</Link>
    <span className="section-kicker">EXPLORAR PEÇAS</span><h1>{selectedType ? `Peças para ${selectedType.label.toLowerCase()}` : "Qual peça você procura?"}</h1>
    <p>Explore exemplos de tipos de peças. A aplicação para {selectedType?.label.toLowerCase() ?? "cada veículo"} precisa ser confirmada com dados técnicos antes da compra.</p>
    <form className="explore-search" action="/explorar"><label htmlFor="explore-q">Buscar tipo de peça</label><div><input id="explore-q" name="q" type="search" defaultValue={q ?? ""} placeholder="Ex.: filtro de óleo…" autoComplete="off" /><button type="submit">Buscar</button></div></form>
    <div className="explore-layout"><aside className="explore-sidebar"><h2>Filtrar por</h2><h3>Veículo</h3><nav className="explore-filters" aria-label="Tipo de veículo"><Link href={exploreHref(undefined,peca)} aria-current={!selectedType ? "page" : undefined}>Todos</Link>{vehicleTypes.map(item=><Link key={item.slug} href={exploreHref(item.slug,peca)} aria-current={selectedType?.slug === item.slug ? "page" : undefined}>{item.label}</Link>)}</nav><h3>Sistema da peça</h3><nav className="explore-filters explore-groups" aria-label="Tipo de peça"><Link href={exploreHref(selectedType?.slug)} aria-current={!peca ? "page" : undefined}>Todas as peças</Link>{groups.map(group=><Link key={group} href={exploreHref(selectedType?.slug,group)} aria-current={peca === group ? "page" : undefined}>{group}</Link>)}</nav></aside><div className="explore-results"><div className="explore-results-head"><strong>{pieces.length} {pieces.length === 1 ? "tipo de peça" : "tipos de peças"}</strong>{(tipo || peca || q) && <Link href="/explorar">Limpar filtros</Link>}</div><p className="demo-notice">Demonstração: estes exemplos não afirmam compatibilidade, preço ou disponibilidade.</p>{process.env.NODE_ENV === "development" ? pieces.length ? <div className="demo-piece-grid">{pieces.map((piece,index)=><DemoPieceCard piece={piece} index={index} key={piece.slug}/>)}</div> : <div className="catalog-empty"><h2>Nenhum tipo encontrado</h2><p>Experimente outro nome ou limpe os filtros.</p><Link href="/explorar" className="btn btn-primary">Limpar filtros</Link></div> : <p>Produtos demonstrativos disponíveis apenas no ambiente de desenvolvimento.</p>}</div></div>
  </main>;
}
