// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { PaymentStage } from "./PaymentStage";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it("shows the registered order and planned payment methods without starting payment", async () => {
  const fetchMock = vi.fn(async () => ({ ok: true, json: async () => ({
    id: "order-1", publicId: "57-order-1", status: "AwaitingSupplier", customerName: "Cliente Teste",
    street: "Rua Um", number: "12", complement: null, district: "Centro", city: "São Paulo", state: "SP", postalCode: "01001000",
    items: [{ name: "Pastilha de freio", quantity: 2, listedUnitPrice: null }]
  }) }));
  vi.stubGlobal("fetch", fetchMock);
  render(<PaymentStage orderId="order-1" />);
  expect(await screen.findByText(/57-order-1/)).toBeTruthy();
  for (const method of ["Pix", "Cartão de crédito", "Boleto bancário"]) expect(screen.getByText(method)).toBeTruthy();
  expect(screen.queryByRole("button", { name: /pagar/i })).toBeNull();
  expect(fetchMock).toHaveBeenCalledTimes(1);
  expect(fetchMock).toHaveBeenCalledWith("/api/v1/supplier-requests/order-1", expect.objectContaining({ cache: "no-store" }));
});

it("does not present payment methods for a declined order", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => ({ ok: true, json: async () => ({
    id: "order-2", publicId: "57-order-2", status: "Declined", customerName: "Cliente Teste",
    street: "Rua Um", number: "12", complement: null, district: "Centro", city: "São Paulo", state: "SP", postalCode: "01001000",
    items: [{ name: "Pastilha de freio", quantity: 1, listedUnitPrice: null }]
  }) })));
  render(<PaymentStage orderId="order-2" />);
  expect(await screen.findByText(/Indisponível — pedido recusado/)).toBeTruthy();
  expect(screen.queryByText("Pix")).toBeNull();
});
