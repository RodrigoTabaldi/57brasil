"use client";
import { useEffect, useState } from "react";
import Link from "next/link";

type Order = { id: string; publicId: string; status: string; customerName: string; street: string; number: string; complement: string | null; district: string; city: string; state: string; postalCode: string; items: { name: string; quantity: number; listedUnitPrice: number | null }[] };
const methods = ["Pix", "Cartão de crédito", "Boleto bancário"];
const statusLabels: Record<string, string> = {
  AwaitingSupplier: "Aguardando confirmação do fornecedor",
  Quoted: "Resposta do fornecedor registrada",
  Declined: "Indisponível — pedido recusado",
};

export function PaymentStage({ orderId }: { orderId: string }) {
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    void fetch(`/api/v1/supplier-requests/${encodeURIComponent(orderId)}`, { cache: "no-store", signal: controller.signal })
      .then(async response => { if (!response.ok) throw new Error(response.status === 401 ? "Entre na sua conta para consultar este pedido." : "Pedido não encontrado na sua conta."); return response.json() as Promise<Order>; })
      .then(setOrder)
      .catch(cause => { if (!controller.signal.aborted) setError(cause instanceof Error ? cause.message : "Não foi possível carregar o pedido."); });
    return () => controller.abort();
  }, [orderId]);
  return <main id="main-content" className="container empty-page payment-stage"><Link href="/conta">← Meus pedidos</Link><p className="section-kicker">PEDIDO REGISTRADO · PRÓXIMA ETAPA</p><h1>Formas de pagamento</h1>{error ? <p role="alert">{error} <Link href="/conta">Ver minha conta</Link></p> : !order ? <p role="status">Carregando pedido…</p> : <><p role="status"><strong>{order.publicId}</strong> · {statusLabels[order.status] ?? order.status}.</p><section className="payment-summary"><h2>Resumo do pedido</h2><ul>{order.items.map((item, index) => <li key={index}>{item.quantity} × {item.name}</li>)}</ul><p>Entrega para {order.customerName}: {order.street}, {order.number}{order.complement ? `, ${order.complement}` : ""}, {order.district}, {order.city}/{order.state} · CEP {order.postalCode}</p><p>Valor final e frete serão informados após confirmação da peça e da disponibilidade.</p></section>{order.status === "Declined" ? <p>O fornecedor informou indisponibilidade. Consulte os detalhes em sua conta.</p> : <section aria-labelledby="payment-methods-title"><h2 id="payment-methods-title">Meios de pagamento previstos</h2><ul className="payment-methods">{methods.map(method => <li key={method}><strong>{method}</strong><span>Integração pendente</span></li>)}</ul><p>Nenhuma forma de pagamento pode ser selecionada ou cobrada agora. Você receberá uma proposta depois da confirmação do fornecedor.</p></section>}<Link className="button" href="/conta">Acompanhar meu pedido</Link></>}</main>;
}
