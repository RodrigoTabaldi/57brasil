"use client";
import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { ProductEditor, type AdminProductRecord } from "@/components/catalog/ProductEditor";
import { SupplierRequests } from "@/components/ui/SupplierRequests";

type Option = { id: string; name: string; active?: boolean };
type Page<T> = { items: T[]; total: number; page: number; pageSize: number };
type Order = { id: string; status: string; buyerEmail: string; shippingAddress: string | null; itemsTotal: number; shippingTotal: number; createdAt: string; items: { sku: string; name: string; quantity: number; unitPrice: number }[] };
type Account = { id: string; fullName: string; email: string; emailConfirmed: boolean };
type Dashboard = { products: number; publishedProducts: number; lowStock: number };
type Overview = { accounts: number; orders: number; pendingOrders: number; visitsToday: number; visitsSevenDays: number };
const emptyPage = <T,>(): Page<T> => ({ items: [], total: 0, page: 1, pageSize: 50 });
const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export default function AdminPage() {
  const [authorized, setAuthorized] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [codeRequired, setCodeRequired] = useState(false);
  const [setupKey, setSetupKey] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [overview, setOverview] = useState<Overview | null>(null);
  const [products, setProducts] = useState<Page<AdminProductRecord>>(emptyPage());
  const [orders, setOrders] = useState<Page<Order>>(emptyPage());
  const [accounts, setAccounts] = useState<Page<Account>>(emptyPage());
  const [categories, setCategories] = useState<Option[]>([]);
  const [brands, setBrands] = useState<Option[]>([]);
  const [search, setSearch] = useState("");
  const [productPage, setProductPage] = useState(1);
  const [orderPage, setOrderPage] = useState(1);
  const [accountPage, setAccountPage] = useState(1);

  const refresh = useCallback(async () => {
    try {
      const response = await fetch("/api/v1/admin/dashboard", { cache: "no-store" });
      if (!response.ok) { setAuthorized(false); return; }
      const [stats, operational, productResults, orderResults, accountResults, categoryResults, brandResults] = await Promise.all([
        response.json() as Promise<Dashboard>,
        fetch("/api/v1/admin/overview", { cache: "no-store" }).then(r => r.json() as Promise<Overview>),
        fetch(`/api/v1/admin/products?page=${productPage}&q=${encodeURIComponent(search)}`, { cache: "no-store" }).then(r => r.json() as Promise<Page<AdminProductRecord>>),
        fetch(`/api/v1/admin/orders?page=${orderPage}`, { cache: "no-store" }).then(r => r.json() as Promise<Page<Order>>),
        fetch(`/api/v1/admin/accounts?page=${accountPage}`, { cache: "no-store" }).then(r => r.json() as Promise<Page<Account>>),
        fetch("/api/v1/admin/categories", { cache: "no-store" }).then(r => r.json() as Promise<Option[]>),
        fetch("/api/v1/brands", { cache: "no-store" }).then(r => r.json() as Promise<Option[]>),
      ]);
      setDashboard(stats); setOverview(operational); setProducts(productResults); setOrders(orderResults);
      setAccounts(accountResults); setCategories(categoryResults); setBrands(brandResults); setAuthorized(true);
    } catch { setMessage("Não foi possível carregar o painel."); }
    finally { setLoading(false); }
  }, [search, productPage, orderPage, accountPage]);
  useEffect(() => { void refresh(); }, [refresh]);
  useEffect(() => {
    const onAuthUpdated = () => { void refresh(); };
    window.addEventListener("auth-updated", onAuthUpdated);
    return () => window.removeEventListener("auth-updated", onAuthUpdated);
  }, [refresh]);

  async function login(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    const response = await fetch("/api/v1/admin/session", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: data.get("email"), password: data.get("password"), code: data.get("code") }) });
    if (!response.ok) { setMessage("Credenciais ou código inválidos."); return; }
    if (response.status === 200) {
      const result = await response.json() as { setupRequired?: boolean; codeRequired?: boolean };
      if (result.setupRequired) {
        const setup = await fetch("/api/v1/admin/mfa/setup", { cache: "no-store" });
        if (setup.ok) setSetupKey((await setup.json() as { key: string }).key);
      }
      setCodeRequired(Boolean(result.codeRequired)); return;
    }
    setMessage(""); await refresh(); window.dispatchEvent(new Event("auth-updated"));
  }
  async function confirmMfa(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    const response = await fetch("/api/v1/admin/mfa/setup", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ code: data.get("code") }) });
    if (!response.ok) { setMessage("Código inválido. Confira o horário do autenticador."); return; }
    setRecoveryCodes((await response.json() as { recoveryCodes: string[] }).recoveryCodes);
    setSetupKey(""); setMessage(""); await refresh(); window.dispatchEvent(new Event("auth-updated"));
  }
  async function submit(event: React.FormEvent<HTMLFormElement>, kind: "categories" | "brands" | "products") {
    event.preventDefault(); const form = event.currentTarget; const data = Object.fromEntries(new FormData(form));
    const body = kind === "products" ? { ...data, price: Number(data.price), stock: Number(data.stock), published: false,
      weightKg: data.weightKg ? Number(data.weightKg) : null, heightCm: data.heightCm ? Number(data.heightCm) : null,
      widthCm: data.widthCm ? Number(data.widthCm) : null, lengthCm: data.lengthCm ? Number(data.lengthCm) : null,
      supplierConfirmedAt: data.supplierConfirmedAt ? new Date(String(data.supplierConfirmedAt)).toISOString() : null } : data;
    const response = await fetch(`/api/v1/admin/${kind}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    if (!response.ok) { setMessage("Não foi possível salvar. Confira os campos e identificadores únicos."); return; }
    form.reset(); setMessage("Registro salvo."); await refresh();
  }
  async function updateCategory(category: Option) {
    const response = await fetch(`/api/v1/admin/categories/${category.id}`, { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ active: !category.active }) });
    setMessage(response.ok ? "Tipo atualizado." : "Não foi possível atualizar o tipo."); if (response.ok) await refresh();
  }
  async function renameCategory(event: React.FormEvent<HTMLFormElement>, category: Option) {
    event.preventDefault(); const name = String(new FormData(event.currentTarget).get("name") ?? "").trim();
    const response = await fetch(`/api/v1/admin/categories/${category.id}`, { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name }) });
    setMessage(response.ok ? "Tipo renomeado." : "Não foi possível renomear o tipo."); if (response.ok) await refresh();
  }
  async function toggle(product: AdminProductRecord) {
    const response = await fetch(`/api/v1/admin/products/${product.id}`, { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ published: !product.published }) });
    setMessage(response.ok ? "Publicação atualizada." : "Publicação recusada: confira as fontes do fornecedor."); if (response.ok) await refresh();
  }
  async function adjust(event: React.FormEvent<HTMLFormElement>, productId: string) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const delta = Number(data.get("delta"));
    if (!Number.isInteger(delta) || delta === 0) { setMessage("Informe um ajuste inteiro diferente de zero."); return; }
    const response = await fetch(`/api/v1/admin/inventory/${productId}/adjustments`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ delta, sourceReference: String(data.get("sourceReference") ?? "").trim() }) });
    if (!response.ok) { setMessage("Ajuste recusado. Confira o estoque e as reservas."); return; }
    form.reset(); setMessage("Estoque ajustado."); await refresh();
  }
  async function logout() { await fetch("/api/v1/admin/session", { method: "DELETE" }); setAuthorized(false); setRecoveryCodes([]); window.dispatchEvent(new Event("auth-updated")); }
  return <main id="main-content" className="container empty-page admin"><Link href="/">← Loja</Link><h1>Administração</h1>
    {message && <p role="status">{message}</p>}
    {loading ? <p>Carregando…</p> : !authorized ? setupKey ? <form onSubmit={confirmMfa}><h2>Ativar autenticação de dois fatores</h2><p>Cadastre esta chave no aplicativo autenticador e informe o código atual.</p><p><code>{setupKey}</code></p><label>Código<input name="code" inputMode="numeric" autoComplete="one-time-code" required /></label><button className="button">Ativar MFA</button></form> : <form onSubmit={login}><h2>Entrar</h2><label>E-mail<input name="email" type="email" autoComplete="username" required /></label><label>Senha<input name="password" type="password" autoComplete="current-password" required /></label>{codeRequired && <label>Código do autenticador ou recuperação<input name="code" autoComplete="one-time-code" required /></label>}<button className="button">Entrar</button><p><Link href="/conta">Recuperar senha</Link></p></form> : <>
      <button type="button" onClick={() => void logout()}>Sair</button>
      {recoveryCodes.length > 0 && <section role="status"><h2>Guarde os códigos de recuperação</h2><p>Cada código funciona uma vez. Eles não serão exibidos novamente.</p><code>{recoveryCodes.join(" · ")}</code><button type="button" onClick={() => setRecoveryCodes([])}>Já guardei</button></section>}
      <section><h2>Resumo</h2><p>{dashboard?.products} produtos · {dashboard?.publishedProducts} publicados · {dashboard?.lowStock} com estoque baixo</p><p>{overview?.accounts} contas · {overview?.orders} pedidos · {overview?.pendingOrders} aguardando pagamento</p><p>{overview?.visitsToday} acessos hoje · {overview?.visitsSevenDays} nos últimos 7 dias</p></section>
      <div className="admin-grid"><form onSubmit={e => void submit(e, "categories")}><h2>Novo tipo</h2><label>Nome<input name="name" required maxLength={120} /></label><label>Slug<input name="slug" required pattern="[a-z0-9]+(-[a-z0-9]+)*" /></label><button className="button">Salvar tipo</button></form><form onSubmit={e => void submit(e, "brands")}><h2>Nova marca</h2><label>Nome<input name="name" required maxLength={120} /></label><label>Slug<input name="slug" required pattern="[a-z0-9]+(-[a-z0-9]+)*" /></label><button className="button">Salvar marca</button></form></div>
      <section><h2>Tipos cadastrados</h2><ul>{categories.map(x => <li key={x.id}>{x.name} · {x.active ? "Ativo" : "Inativo"} <button type="button" onClick={() => void updateCategory(x)}>{x.active ? "Desativar" : "Ativar"}</button><details><summary>Editar nome</summary><form onSubmit={e => void renameCategory(e, x)}><label>Novo nome<input name="name" defaultValue={x.name} required maxLength={120} /></label><button type="submit">Salvar nome</button></form></details></li>)}</ul></section>
      <form onSubmit={e => void submit(e, "products")}><h2>Novo produto específico</h2><p>O produto será salvo como rascunho. Publicação exige fontes verificadas.</p><div className="admin-grid">
        <label>Nome<input name="name" required maxLength={120} /></label><label>Slug<input name="slug" required pattern="[a-z0-9]+(-[a-z0-9]+)*" /></label><label>SKU<input name="sku" required maxLength={80} /></label><label>Tipo<select name="categoryId" required><option value="">Selecione</option>{categories.filter(x => x.active).map(x => <option value={x.id} key={x.id}>{x.name}</option>)}</select></label><label>Marca<select name="brandId" required><option value="">Selecione</option>{brands.map(x => <option value={x.id} key={x.id}>{x.name}</option>)}</select></label><label>Preço recebido (R$)<input name="price" type="number" min="0" step="0.01" required /></label><label>Fonte do preço<input name="priceSourceReference" minLength={5} maxLength={200} /></label><label>Estoque<input name="stock" type="number" min="0" step="1" required /></label><label>Fonte do estoque<input name="stockSourceReference" minLength={5} maxLength={200} /></label><label>Fonte da aplicação<input name="compatibilitySourceReference" minLength={5} maxLength={200} /></label><label>Aplicação informada<textarea name="applicationNotes" maxLength={500} /></label><label>Data da confirmação<input name="supplierConfirmedAt" type="date" /></label><label>Peso (kg)<input name="weightKg" type="number" min="0.001" step="0.001" /></label><label>Altura (cm)<input name="heightCm" type="number" min="0.01" step="0.01" /></label><label>Largura (cm)<input name="widthCm" type="number" min="0.01" step="0.01" /></label><label>Comprimento (cm)<input name="lengthCm" type="number" min="0.01" step="0.01" /></label>
      </div><button className="button">Salvar rascunho</button></form>
      <SupplierRequests admin />
      <section><h2>Produtos cadastrados</h2><form onSubmit={e => { e.preventDefault(); setProductPage(1); setSearch(String(new FormData(e.currentTarget).get("q") ?? "")); }}><label>Buscar por nome ou SKU<input name="q" defaultValue={search} maxLength={120} /></label><button type="submit">Buscar</button></form><p>{products.total} produtos</p><ul>{products.items.map(x => <li key={x.id}>{x.name} · {x.sku} · {x.published ? "Publicado" : "Rascunho"} <button type="button" onClick={() => void toggle(x)}>{x.published ? "Desativar" : "Publicar"}</button><ProductEditor product={x} brands={brands} onSaved={refresh} /><form onSubmit={event => void adjust(event, x.id)}><label>Ajuste de estoque (+/-)<input type="number" name="delta" min="-100000" max="100000" required /></label><label>Documento do ajuste<input name="sourceReference" minLength={5} maxLength={200} placeholder="Ex.: nota fiscal 123…" required /></label><button type="submit">Aplicar ajuste</button></form></li>)}</ul><Pager page={products.page} total={products.total} size={products.pageSize} setPage={setProductPage} /></section>
      <section><h2>Pedidos</h2>{orders.items.length === 0 ? <p>Nenhum pedido registrado.</p> : <ul>{orders.items.map(x => <li key={x.id}><strong>{x.id}</strong> · {x.status} · {x.buyerEmail} · {new Date(x.createdAt).toLocaleDateString("pt-BR")} · {money.format(x.itemsTotal + x.shippingTotal)}<p>Endereço: {x.shippingAddress || "Não informado"}</p><ul>{x.items.map(i => <li key={i.sku}>{i.quantity} × {i.name} ({i.sku}) · {money.format(i.unitPrice)}</li>)}</ul></li>)}</ul>}<Pager page={orders.page} total={orders.total} size={orders.pageSize} setPage={setOrderPage} /></section>
      <section><h2>Contas de usuários</h2>{accounts.items.length === 0 ? <p>Nenhuma conta nesta página.</p> : <ul>{accounts.items.map(x => <li key={x.id}>{x.fullName || "Sem nome"} · {x.email} · {x.emailConfirmed ? "E-mail confirmado" : "E-mail não confirmado"}</li>)}</ul>}<Pager page={accounts.page} total={accounts.total} size={accounts.pageSize} setPage={setAccountPage} /></section>
    </>}
  </main>;
}
function Pager({ page, total, size, setPage }: { page: number; total: number; size: number; setPage: (page: number) => void }) {
  return <nav aria-label="Paginação"><button type="button" disabled={page <= 1} onClick={() => setPage(page - 1)}>Anterior</button> Página {page} de {Math.max(1, Math.ceil(total / size))} <button type="button" disabled={page * size >= total} onClick={() => setPage(page + 1)}>Próxima</button></nav>;
}
