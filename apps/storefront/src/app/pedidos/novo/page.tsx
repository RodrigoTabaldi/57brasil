"use client";
import { Suspense, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthModal, currentUser } from "@/components/ui/AuthModal";

type User = { fullName: string; phoneNumber?: string };
export default function NewRequestPage() { return <Suspense fallback={<main className="container empty-page"><p>Carregando...</p></main>}><RequestForm/></Suspense>; }
function RequestForm() {
  const router = useRouter();
  const params = useSearchParams();
  const productId = params.get("produto") ?? "";
  const slug = params.get("slug") ?? "";
  const requestedQuantity = Number(params.get("quantidade"));
  const initialQuantity = Number.isInteger(requestedQuantity) && requestedQuantity >= 1 && requestedQuantity <= 99 ? requestedQuantity : 1;
  const [user, setUser] = useState<User | null>(null);
  const [auth, setAuth] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [orderId, setOrderId] = useState<{ id: string; publicId: string } | null>(null);
  const [productName, setProductName] = useState("");
  const [productError, setProductError] = useState("");
  const key = useRef<string | null>(null);
  useEffect(() => { void currentUser().then(value => { setUser(value as User | null); if (!value) setAuth(true); }); }, []);
  useEffect(() => { if (!slug || !productId) { setProductError("Escolha uma peça no catálogo antes de comprar."); return; } const controller = new AbortController(); void fetch(`/api/v1/products/${encodeURIComponent(slug)}`, { signal: controller.signal }).then(async response => { if (!response.ok) throw new Error(); const product = await response.json() as { id: string; name: string }; if (product.id !== productId) throw new Error(); setProductName(product.name); }).catch(() => { if (!controller.signal.aborted) setProductError("Não foi possível confirmar a peça. Volte ao catálogo e tente novamente."); }); return () => controller.abort(); }, [productId, slug]);
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    if (!productName || productError) { setError("Confirme a peça no catálogo antes de enviar o pedido."); return; }
    const data = Object.fromEntries(new FormData(event.currentTarget).entries());
    const body = { ...data, productId, quantity: Number(data.quantity) };
    if (!/^[0-9]{8}$/.test(String(data.postalCode)) || !/^[A-Za-z]{2}$/.test(String(data.state)) || !Number.isInteger(body.quantity) || body.quantity < 1 || body.quantity > 99) { setError("Confira CEP, UF e quantidade."); return; }
    key.current ??= crypto.randomUUID(); setBusy(true); setError("");
    try {
      const response = await fetch("/api/v1/supplier-requests", { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": key.current }, body: JSON.stringify(body) });
      if (!response.ok) throw new Error(response.status === 401 ? "Entre na sua conta e tente novamente." : "Não foi possível registrar o pedido. Confira os dados e tente novamente.");
      const result = await response.json() as { id: string; publicId: string }; setOrderId(result); router.push(`/pedidos/${result.id}/pagamento`);
    } catch (e) { setError(e instanceof Error ? e.message : "Falha ao registrar o pedido."); } finally { setBusy(false); }
  }
  return <main id="main-content" className="container empty-page request-page"><Link href="/produtos">← Catálogo</Link><h1>Solicitar compra</h1>{orderId ? <section role="status"><h2>Pedido {orderId.publicId} registrado</h2><p>Aguardando confirmação do fornecedor. Você pode acompanhar o pedido na sua conta. Não houve cobrança.</p><Link className="button" href={`/pedidos/${orderId.id}/pagamento`}>Ver meios de pagamento</Link> <Link href="/conta">Acompanhar pedido</Link></section> : <><p>Informe o veículo e o endereço para confirmarmos o pedido. Preço, disponibilidade e prazo dependem da confirmação final. O pagamento ainda não está disponível.</p>{productError ? <p role="alert">{productError}</p> : productName ? <p><strong>Peça:</strong> {productName}</p> : <p role="status">Confirmando a peça…</p>}{!user ? <button type="button" className="button" onClick={() => setAuth(true)}>Entrar ou criar conta</button> : <form onSubmit={submit}><div className="request-grid"><label>Veículo (marca, modelo, ano e versão)<input name="vehicle" required minLength={3} maxLength={240} placeholder="Ex.: Fiat Uno 2015 1.0"/></label><label>Quantidade<input name="quantity" type="number" min={1} max={99} defaultValue={initialQuantity} required/></label><label>Nome completo<input name="customerName" autoComplete="name" defaultValue={user.fullName} minLength={2} maxLength={160} required/></label><label>Contato telefônico<input name="contact" type="tel" autoComplete="tel" defaultValue={user.phoneNumber} minLength={10} maxLength={20} required/></label><label>CEP<input name="postalCode" inputMode="numeric" autoComplete="postal-code" pattern="[0-9]{8}" maxLength={8} required/></label><label>Rua ou avenida<input name="street" autoComplete="address-line1" minLength={2} maxLength={160} required/></label><label>Número<input name="number" maxLength={20} required/></label><label>Complemento (opcional)<input name="complement" autoComplete="address-line2" maxLength={100}/></label><label>Bairro<input name="district" minLength={2} maxLength={100} required/></label><label>Cidade<input name="city" autoComplete="address-level2" minLength={2} maxLength={100} required/></label><label>UF<input name="state" autoComplete="address-level1" pattern="[A-Za-z]{2}" maxLength={2} required/></label><label>Observações (opcional)<textarea name="notes" maxLength={1000} rows={3}/></label></div>{error && <p role="alert">{error}</p>}<button className="button" type="submit" disabled={busy || !productName || !!productError}>{busy ? "Registrando…" : "Confirmar solicitação e gerar pedido"}</button></form>}</>}{auth && <AuthModal onClose={() => setAuth(false)} onSuccess={async () => { setUser(await currentUser() as User); setAuth(false); }}/>}</main>;
}
