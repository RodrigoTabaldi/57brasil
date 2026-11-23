// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { SupplierRequests } from "./SupplierRequests";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it("shows the customer's order while older API responses have no listed price snapshot", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => ({ ok: true, json: async () => [{
    id: "order-1", publicId: "57-order-1", status: "AwaitingSupplier", createdAt: "2026-11-22T12:00:00Z",
    vehicle: "Fiat Uno 2015", items: [{ id: "item-1", name: "Pastilha de freio", sku: "CAT-PASTILHA", quantity: 1, available: null }]
  }] })));
  render(<SupplierRequests />);
  expect(await screen.findByText(/57-order-1/)).toBeTruthy();
  expect(screen.getByText(/Aguardando confirmação do fornecedor/)).toBeTruthy();
});
