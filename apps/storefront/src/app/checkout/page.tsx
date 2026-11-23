"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

type Cart = { items: { productId: string; name: string; quantity: number; available: number }[] };

export default function CheckoutPage() {
  const [cart, setCart] = useState<Cart | null>(null);
  const [error, setError] = useState("");
  const [addressReady, setAddressReady] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    fetch("/api/v1/cart", { cache: "no-store", signal: controller.signal })
      .then(async response => {
        if (!response.ok) throw new Error();
        setCart(await response.json() as Cart);
      })
      .catch(() => { if (!controller.signal.aborted) setError("Não foi possível carregar o carrinho. Entre na sua conta e tente novamente."); });
    return () => controller.abort();
  }, []);

  function continueToPayment(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const cep = String(data.get("cep") ?? "").replace(/\D/g, "");
    if (!/^\d{8}$/.test(cep)) { setError("Informe um CEP com oito dígitos."); return; }
    if (["street", "number", "neighborhood", "city", "state"].some(key => !String(data.get(key) ?? "").trim())) {
      setError("Preencha todos os campos obrigatórios do endereço."); return;
    }
    setError("");
    setAddressReady(true);
  }

  const unavailable = cart?.items.some(item => item.quantity > item.available);
  return <main id="main-content" className="container empty-page">
    <h1>{addressReady ? "Pagamento" : "Endereço de entrega"}</h1>
    {error && <p role="alert">{error}</p>}
    {!cart && !error && <p>Carregando carrinho…</p>}
    {cart && cart.items.length === 0 && <p>Seu carrinho está vazio. <Link href="/produtos">Ver produtos</Link></p>}
    {cart && cart.items.length > 0 && <>
      <ul>{cart.items.map(item => <li key={item.productId}>{item.quantity} × {item.name}</li>)}</ul>
      {unavailable ? <p>Revise a disponibilidade dos produtos no <Link href="/carrinho">carrinho</Link>.</p> : addressReady ? <section>
        <p>O pagamento ainda não está configurado. Nenhum pedido foi criado e nenhuma cobrança será realizada.</p>
        <p>O endereço informado não foi salvo. A finalização será liberada após a integração e os testes de pedido, reserva, frete e pagamento.</p>
        <button type="button" onClick={() => setAddressReady(false)}>Voltar ao endereço</button>
      </section> : <form onSubmit={continueToPayment}>
        <p>Informe o endereço para continuar. Os dados ainda não serão salvos.</p>
        <p><label htmlFor="cep">CEP</label> <input id="cep" name="cep" autoComplete="postal-code" inputMode="numeric" maxLength={9} required /></p>
        <p><label htmlFor="street">Rua ou avenida</label> <input id="street" name="street" autoComplete="address-line1" maxLength={200} required /></p>
        <p><label htmlFor="number">Número</label> <input id="number" name="number" maxLength={30} required /></p>
        <p><label htmlFor="complement">Complemento (opcional)</label> <input id="complement" name="complement" autoComplete="address-line2" maxLength={100} /></p>
        <p><label htmlFor="neighborhood">Bairro</label> <input id="neighborhood" name="neighborhood" maxLength={100} required /></p>
        <p><label htmlFor="city">Cidade</label> <input id="city" name="city" autoComplete="address-level2" maxLength={100} required /></p>
        <p><label htmlFor="state">UF</label> <input id="state" name="state" autoComplete="address-level1" maxLength={2} pattern="[A-Za-z]{2}" required /></p>
        <button className="button" type="submit">Continuar para pagamento</button>
      </form>}
    </>}
    <p><Link href="/carrinho">Voltar ao carrinho</Link></p>
  </main>;
}
