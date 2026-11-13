import Link from "next/link";
import { CarFront, ChevronDown, ShoppingBag, Wrench } from "lucide-react";
import { SearchBox } from "@/components/ui/SearchBox";

export const departments = [
  { name: "Motor", slug: "motor" },
  { name: "Freios", slug: "freios" },
  { name: "Suspensão e direção", slug: "suspensao-e-direcao" },
  { name: "Elétrica", slug: "eletrica" },
  { name: "Iluminação", slug: "iluminacao" },
  { name: "Acessórios", slug: "acessorios" },
];

export function SiteHeader() {
  return <>
    <div className="topbar"><div className="shell topbar-inner"><span>57 Brasil Autopeças</span><span>Explore o catálogo sem cadastro</span></div></div>
    <header className="site-header">
      <div className="shell header-main">
        <Link className="brand" href="/" aria-label="57 Brasil Autopeças, página inicial"><span className="brand-mark" aria-hidden="true">57<span className="brand-slash"/></span><span className="brand-name">BRASIL<small>AUTOPEÇAS</small></span></Link>
        <SearchBox />
        <div className="header-actions"><Link href="/#encontre-veiculo" className="header-action"><CarFront size={22} aria-hidden="true"/><span>Meu veículo</span></Link><Link href="/carrinho" className="header-action"><ShoppingBag size={22} aria-hidden="true"/><span>Carrinho</span></Link></div>
      </div>
      <div className="header-nav"><nav className="shell nav-links" aria-label="Departamentos"><details className="department-menu"><summary><Wrench size={17} aria-hidden="true"/> Departamentos <ChevronDown size={16} aria-hidden="true"/></summary><div className="department-panel">{departments.map(item=><Link href={`/produtos?categoria=${item.slug}`} key={item.slug}>{item.name}</Link>)}<Link href="/produtos">Ver todo o catálogo</Link></div></details><Link href="/produtos">Todos os produtos</Link>{departments.slice(0,5).map(item=><Link href={`/produtos?categoria=${item.slug}`} key={item.slug}>{item.name}</Link>)}</nav></div>
    </header>
  </>;
}
