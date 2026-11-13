import Link from "next/link";
import { CarFront, ChevronDown, ShoppingBag, Wrench } from "lucide-react";
import { SearchBox } from "@/components/ui/SearchBox";
import { demoPieces } from "@/lib/demo-catalog";

type Department = { id: string; name: string; slug: string; parentId: string | null };
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5000";

export async function SiteHeader() {
  let departments: Department[] = [];
  try {
    const response = await fetch(`${apiUrl}/api/v1/categories`, { cache: "no-store" });
    if (response.ok) departments = ((await response.json()) as Department[]).filter(item => item.parentId === null);
  } catch { /* The catalog remains available when the API is temporarily offline. */ }
  const demoGroups = process.env.NODE_ENV === "development" && departments.length === 0 ? [...new Set(demoPieces.map(piece => piece.group))] : [];
  return <>
    <div className="topbar"><div className="shell topbar-inner"><span>57 Brasil Autopeças</span><span>Explore o catálogo sem cadastro</span></div></div>
    <header className="site-header">
      <div className="shell header-main">
        <Link className="brand" href="/" aria-label="57 Brasil Autopeças, página inicial"><span className="brand-mark" aria-hidden="true">57<span className="brand-slash"/></span><span className="brand-name">BRASIL<small>AUTOPEÇAS</small></span></Link>
        <SearchBox demoMode={demoGroups.length > 0} />
        <div className="header-actions"><Link href="/veiculo" className="header-action"><CarFront size={22} aria-hidden="true"/><span>Meu veículo</span></Link><Link href="/carrinho" className="header-action"><ShoppingBag size={22} aria-hidden="true"/><span>Carrinho</span></Link></div>
      </div>
      <div className="header-nav"><nav className="shell nav-links" aria-label="Departamentos"><details className="department-menu"><summary><Wrench size={17} aria-hidden="true"/> {demoGroups.length ? "Tipos de peças" : "Departamentos"} <ChevronDown size={16} aria-hidden="true"/></summary><div className="department-panel">{departments.map(item=><Link href={`/produtos?categoria=${item.slug}`} key={item.slug}>{item.name}</Link>)}{demoGroups.map(group=><Link href={`/explorar?peca=${encodeURIComponent(group)}`} key={group}>{group}</Link>)}<Link href="/produtos">Ver produtos cadastrados</Link></div></details><Link href="/produtos">Produtos cadastrados</Link>{demoGroups.length ? <><Link href="/explorar?tipo=carro">Carro</Link><Link href="/explorar?tipo=moto">Moto</Link><Link href="/explorar?tipo=caminhao">Caminhão</Link><Link href="/explorar">Tipos de peças</Link></> : departments.slice(0,5).map(item=><Link href={`/produtos?categoria=${item.slug}`} key={item.slug}>{item.name}</Link>)}</nav></div>
    </header>
  </>;
}
