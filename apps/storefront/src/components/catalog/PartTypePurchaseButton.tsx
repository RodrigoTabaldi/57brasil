"use client";

import { useRef, useState } from "react";
import { AuthModal, currentUser } from "@/components/ui/AuthModal";

type Props = { slug: string };

export function PartTypePurchaseButton({ slug }: Props) {
  const [auth, setAuth] = useState(false);
  const [pending, setPending] = useState(false);
  const [status, setStatus] = useState("");
  const lock = useRef(false);

  async function submitRequest() {
    if (lock.current) return;
    lock.current = true;
    setPending(true);
    setStatus("");
    try {
      const response = await fetch(`/api/v1/part-types/${encodeURIComponent(slug)}/purchase-requests`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ quantity: 1 }),
      });
      if (response.status === 401) { setAuth(true); return; }
      const result = await response.json().catch(() => ({})) as { message?: string };
      if (!response.ok) throw new Error(result.message);
      setStatus(result.message ?? "Solicitação de compra registrada.");
    } catch (error) {
      setStatus(error instanceof Error && error.message ? error.message : "Não foi possível registrar. Tente novamente.");
    } finally {
      lock.current = false;
      setPending(false);
    }
  }

  async function buy() {
    if (lock.current) return;
    try {
      if (!await currentUser()) { setAuth(true); return; }
      await submitRequest();
    } catch {
      setStatus("Não foi possível verificar sua sessão. Tente novamente.");
    }
  }

  return <div className="part-type-purchase">
    <button className="button" type="button" onClick={() => void buy()} disabled={pending}>
      {pending ? "Enviando…" : "COMPRAR"}
    </button>
    <p role="status" aria-live="polite">{status}</p>
    {auth && <AuthModal onClose={() => setAuth(false)} onSuccess={async () => { setAuth(false); await submitRequest(); }} />}
  </div>;
}
