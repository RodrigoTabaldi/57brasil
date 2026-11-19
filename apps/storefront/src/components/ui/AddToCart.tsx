"use client";
import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { AuthModal, currentUser } from "./AuthModal";
import { formatPrice } from "../../lib/catalog";

export function AddToCart({ productId, available, price }: { productId: string; available: number; price?: number }) {
  const router = useRouter();
  const maximum = Math.min(99, Math.max(0, available));
  const [quantity, setQuantity] = useState("1");
  const [status, setStatus] = useState("");
  const [pending, setPending] = useState(false);
  const [auth, setAuth] = useState(false);
  const intent = useRef<"cart" | "buy" | null>(null);
  const lock = useRef(false);
  const operationKey = useRef("");
  const selected = Number(quantity);
  const valid = quantity !== "" && Number.isInteger(selected) && selected >= 1 && selected <= maximum;

  async function put() {
    if (lock.current || !valid) return;
    lock.current = true;
    setPending(true);
    setStatus("");
    try {
      const response = await fetch("/api/v1/cart/items", {
        method: "POST",
        headers: { "Content-Type": "application/json", "Idempotency-Key": operationKey.current },
        body: JSON.stringify({ productId, quantity: selected }),
      });
      if (response.status === 401) { setAuth(true); return; }
      if (!response.ok) throw new Error();
      const action = intent.current;
      intent.current = null;
      operationKey.current = "";
      window.dispatchEvent(new Event("cart-updated"));
      if (action === "buy") router.push("/carrinho");
      else setStatus("Produto adicionado ao carrinho.");
    } catch {
      setStatus("Não foi possível adicionar esta quantidade. Confira o estoque e tente novamente.");
    } finally { lock.current = false; setPending(false); }
  }

  async function add(action: "cart" | "buy") {
    if (lock.current || !valid) return;
    intent.current = action;
    operationKey.current = crypto.randomUUID();
    lock.current = true;
    setPending(true);
    try {
      if (!await currentUser()) { setAuth(true); return; }
    } catch { setStatus("Não foi possível verificar a sessão."); return; }
    finally { lock.current = false; setPending(false); }
    await put();
  }

  return <div className="purchase-actions">
    <><label htmlFor={`product-quantity-${productId || "unavailable"}`}>Quantidade</label><div className="quantity-control">
      <button type="button" aria-label="Diminuir quantidade" disabled={pending || !valid || selected <= 1} onClick={() => setQuantity(String(selected - 1))}>−</button>
      <input id={`product-quantity-${productId || "unavailable"}`} type="number" inputMode="numeric" min={1} max={maximum || undefined} step={1} value={quantity} disabled={maximum === 0} onChange={event => setQuantity(event.target.value)} onBlur={() => { if (!valid) setQuantity("1"); }} />
      <button type="button" aria-label="Aumentar quantidade" disabled={pending || !valid || selected >= maximum} onClick={() => setQuantity(String(selected + 1))}>+</button>
    </div><p>{maximum === 0 ? "Indisponível para compra" : `Máximo disponível: ${maximum}`}</p>{price !== undefined && valid && <p>Subtotal: <strong>{formatPrice(price * selected)}</strong></p>}</>
    <button className="button" type="button" onClick={() => void add("buy")} disabled={pending || !valid}>{pending ? "Aguarde..." : "Comprar agora"}</button>
    <button type="button" onClick={() => void add("cart")} disabled={pending || !valid}>Adicionar ao carrinho</button>
    <p role="status" aria-live="polite">{status || (!valid && maximum > 0 ? "Informe uma quantidade válida." : "")}</p>
    {auth && <AuthModal onClose={() => { intent.current = null; setAuth(false); }} onSuccess={async () => { setAuth(false); await put(); }} />}
  </div>;
}
