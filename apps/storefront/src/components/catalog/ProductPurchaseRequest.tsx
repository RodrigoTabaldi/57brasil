"use client";

import { useRef, useState } from "react";
import { AuthModal, currentUser } from "@/components/ui/AuthModal";

export function ProductPurchaseRequest({ productId }: { productId: string }) {
  const [open, setOpen] = useState(false);
  const [auth, setAuth] = useState(false);
  const [pending, setPending] = useState(false);
  const [status, setStatus] = useState("");
  const lock = useRef(false);

  async function start() {
    try { if (await currentUser()) setOpen(true); else setAuth(true); }
    catch { setStatus("Não foi possível verificar sua sessão."); }
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (lock.current) return;
    const data = new FormData(event.currentTarget);
    const payload = Object.fromEntries(data.entries());
    lock.current = true; setPending(true); setStatus("");
    try {
      const response = await fetch(`/api/v1/products/${encodeURIComponent(productId)}/purchase-requests`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...payload, quantity: Number(data.get("quantity")) }),
      });
      if (response.status === 401) { setAuth(true); throw new Error("Entre novamente para enviar sua solicitação."); }
      if (!response.ok) throw new Error("Não foi possível salvar. Confira os dados e tente novamente.");
      setOpen(false); setStatus("Solicitação e endereço registrados. Preço e disponibilidade serão confirmados antes da compra.");
    } catch (error) { setStatus(error instanceof Error ? error.message : "Não foi possível registrar a solicitação."); }
    finally { lock.current = false; setPending(false); }
  }

  return <div>
    {!open && <button type="button" className="button" onClick={() => void start()}>Solicitar compra</button>}
    {open && <form onSubmit={event => void submit(event)}>
      <p>O preço e a disponibilidade serão confirmados antes de qualquer compra. Nenhuma cobrança será feita agora.</p>
      <p><label>Quantidade <input name="quantity" type="number" min="1" max="99" defaultValue="1" required /></label></p>
      <p><label>CEP <input name="cep" autoComplete="postal-code" inputMode="numeric" pattern="[0-9]{5}-?[0-9]{3}" maxLength={9} required /></label></p>
      <p><label>Rua ou avenida <input name="street" autoComplete="address-line1" maxLength={200} required /></label></p>
      <p><label>Número <input name="number" maxLength={30} required /></label></p>
      <p><label>Complemento (opcional) <input name="complement" autoComplete="address-line2" maxLength={100} /></label></p>
      <p><label>Bairro <input name="neighborhood" maxLength={100} required /></label></p>
      <p><label>Cidade <input name="city" autoComplete="address-level2" maxLength={100} required /></label></p>
      <p><label>UF <input name="state" autoComplete="address-level1" pattern="[A-Za-z]{2}" maxLength={2} required /></label></p>
      <button type="submit" disabled={pending}>{pending ? "Salvando…" : "Salvar solicitação e endereço"}</button>
    </form>}
    <p role="status">{status}</p>
    {auth && <AuthModal onClose={() => setAuth(false)} onSuccess={() => { setAuth(false); setOpen(true); }} />}
  </div>;
}
