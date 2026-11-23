import Link from "next/link";
import Image from "next/image";
import { CarFront, ShoppingBag } from "lucide-react";
import { AccountMenu } from "@/components/layout/AccountMenu";
import { SearchBox } from "@/components/ui/SearchBox";
import { DepartmentMenu } from "@/components/layout/DepartmentMenu";

type Department = { id: string; name: string; slug: string; parentId: string | null };
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5250";

export async function SiteHeader() {
  let departments: Department[] = [];
  try {
    const response = await fetch(`${apiUrl}/api/v1/categories`, { next: { revalidate: 60 } });
    if (response.ok) departments = ((await response.json()) as Department[]).filter(item => item.parentId === null);
  } catch { /* The catalog remains available when the API is temporarily offline. */ }
  return <>
    <div className="topbar"><div className="shell topbar-inner"><span>57 Brasil Autopeças</span><span>Explore o catálogo sem cadastro</span></div></div>
    <header className="site-header">
      <div className="shell header-main">
        <Link className="brand" href="/" aria-label="57 Brasil, página inicial"><Image src="/logo-57brasil-4k.png" alt="57 Brasil" width={3840} height={1312} priority className="brand-logo" /></Link>
        <SearchBox />
        <div className="header-actions"><AccountMenu/><Link href="/veiculo" className="header-action" aria-label="Buscar por veículo"><CarFront size={22} aria-hidden="true"/><span>Meu veículo</span></Link><Link href="/carrinho" className="header-action" aria-label="Abrir carrinho"><ShoppingBag size={22} aria-hidden="true"/><span>Carrinho</span></Link></div>
      </div>
      <div className="header-nav"><nav className="shell nav-links" aria-label="Catálogo"><Link href="/produtos#tipos">Catálogo e filtros</Link><Link href="/veiculo">Buscar por veículo</Link><Link href="/produtos?modo=aprovados#produtos-aprovados">Produtos específicos</Link>{departments.length > 0 && <DepartmentMenu departments={departments} demoGroups={[]} />}</nav></div>
    </header>
  </>;
}
