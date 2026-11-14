import Link from "next/link";
import Image from "next/image";
import { CarFront, ShoppingBag } from "lucide-react";
import { SearchBox } from "@/components/ui/SearchBox";
import { DepartmentMenu } from "@/components/layout/DepartmentMenu";
import { demoPieces } from "@/lib/demo-catalog";

type Department = { id: string; name: string; slug: string; parentId: string | null };
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5000";

export async function SiteHeader() {
  let departments: Department[] = [];
  try {
    const response = await fetch(`${apiUrl}/api/v1/categories`, { cache: "no-store" });
    if (response.ok) departments = ((await response.json()) as Department[]).filter(item => item.parentId === null);
  } catch { /* The catalog remains available when the API is temporarily offline. */ }
  const demoGroups = departments.length === 0 ? [...new Set(demoPieces.map(piece => piece.group))] : [];
  return <>
    <div className="topbar"><div className="shell topbar-inner"><span>57 Brasil Autopeças</span><span>Explore o catálogo sem cadastro</span></div></div>
    <header className="site-header">
      <div className="shell header-main">
        <Link className="brand" href="/" aria-label="57 Brasil, página inicial"><Image src="/logo-57brasil-4k.png" alt="57 Brasil" width={3840} height={1312} priority className="brand-logo" /></Link>
        <SearchBox demoMode={demoGroups.length > 0} />
        <div className="header-actions"><Link href="/veiculo" className="header-action"><CarFront size={22} aria-hidden="true"/><span>Meu veículo</span></Link><Link href="/carrinho" className="header-action"><ShoppingBag size={22} aria-hidden="true"/><span>Carrinho</span></Link></div>
      </div>
      <div className="header-nav"><nav className="shell nav-links" aria-label="Catálogo"><Link href="/#catalogo">Catálogo e filtros</Link><Link href="/veiculo">Buscar por veículo</Link><Link href="/produtos">Produtos cadastrados</Link>{departments.length > 0 && <DepartmentMenu departments={departments} demoGroups={[]} />}</nav></div>
    </header>
  </>;
}
