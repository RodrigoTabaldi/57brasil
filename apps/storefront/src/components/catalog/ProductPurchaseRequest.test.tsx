// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ProductPurchaseRequest } from "./ProductPurchaseRequest";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it("sends the address and quantity for the chosen product", async () => {
  const fetchMock = vi.fn(async (input: string) => String(input).endsWith("/me") ? { ok: true, status: 200, json: async () => ({ fullName: "Cliente", email: "cliente@example.test" }) } : { ok: true, status: 202 });
  vi.stubGlobal("fetch", fetchMock);
  render(<ProductPurchaseRequest productId="product-123" />);
  fireEvent.click(screen.getByRole("button", { name: "Solicitar compra" }));
  await screen.findByLabelText("CEP");
  for (const [label, value] of [["CEP", "01001-000"], ["Rua ou avenida", "Rua A"], ["Número", "12"], ["Bairro", "Centro"], ["Cidade", "São Paulo"], ["UF", "SP"]])
    fireEvent.change(screen.getByLabelText(label), { target: { value } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar solicitação e endereço" }));
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  const [url, options] = fetchMock.mock.calls[1] as unknown as [string, RequestInit];
  expect(url).toBe("/api/v1/products/product-123/purchase-requests");
  expect(JSON.parse(String(options.body))).toMatchObject({ quantity: 1, cep: "01001-000", street: "Rua A" });
  expect(await screen.findByText(/Solicitação e endereço registrados/)).toBeTruthy();
});
