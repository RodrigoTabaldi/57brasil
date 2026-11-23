// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import CheckoutPage from "./page";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it("accepts an address for the selected cart product and stops at unavailable payment", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => ({ ok: true, json: async () => ({ items: [{ productId: "product-1", name: "Peça", quantity: 1, available: 2 }] }) })));
  render(<CheckoutPage />);
  expect(await screen.findByText("1 × Peça")).toBeTruthy();
  for (const [label, value] of [["CEP", "01001-000"], ["Rua ou avenida", "Rua A"], ["Número", "12"], ["Bairro", "Centro"], ["Cidade", "São Paulo"], ["UF", "SP"]]) {
    fireEvent.change(screen.getByLabelText(label), { target: { value } });
  }
  fireEvent.click(screen.getByRole("button", { name: "Continuar para pagamento" }));
  expect(screen.getByRole("heading", { name: "Pagamento" })).toBeTruthy();
  expect(screen.getByText(/Nenhum pedido foi criado/)).toBeTruthy();
  expect(screen.getByText(/endereço informado não foi salvo/)).toBeTruthy();
});

it("does not allow an empty cart to proceed", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => ({ ok: true, json: async () => ({ items: [] }) })));
  render(<CheckoutPage />);
  expect(await screen.findByText(/Seu carrinho está vazio/)).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Continuar para pagamento" })).toBeNull();
});
