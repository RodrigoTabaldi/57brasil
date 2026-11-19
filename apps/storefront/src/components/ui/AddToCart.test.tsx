// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { AddToCart } from "./AddToCart";
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn() }) }));

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it("opens authentication for a visitor and adds the pending product once after registration", async () => {
  let authenticated = false;
  const calls: Array<{url:string;body?:{productId:string;quantity:number};key?:string}> = [];
  vi.stubGlobal("fetch", vi.fn(async (input: string, init?: RequestInit) => {
    const url = String(input);
    calls.push({ url, body: init?.body ? JSON.parse(String(init.body)) : undefined, key: new Headers(init?.headers).get("Idempotency-Key") ?? undefined });
    if (url.endsWith("/me")) return { ok: authenticated, status: authenticated ? 200 : 401, json: async () => ({ fullName: "Cliente" }) };
    if (url.endsWith("/register")) { authenticated = true; return { ok: true, status: 200, json: async () => ({ fullName: "Cliente" }) }; }
    if (url.endsWith("/cart/items")) return { ok: true, status: 204 };
    throw new Error(url);
  }));
  render(<AddToCart productId="product-1" available={5} />);
  fireEvent.click(screen.getByRole("button", { name: "Adicionar ao carrinho" }));
  await screen.findByRole("dialog");
  expect(calls.filter(call => call.url.endsWith("/cart/items"))).toHaveLength(0);
  fireEvent.click(screen.getByRole("button", { name: "Criar conta" }));
  fireEvent.change(screen.getByLabelText("Nome completo"), { target: { value: "Cliente Teste" } });
  fireEvent.change(screen.getByLabelText("Celular"), { target: { value: "11999999999" } });
  fireEvent.change(screen.getByLabelText("E-mail"), { target: { value: "cliente@example.test" } });
  fireEvent.change(screen.getByLabelText("Senha", { exact: true }), { target: { value: "StrongPassword1!" } });
  fireEvent.change(screen.getByLabelText("Confirmar senha"), { target: { value: "StrongPassword1!" } });
  fireEvent.click(screen.getByRole("button", { name: "Criar conta" }));
  await waitFor(() => expect(calls.filter(call => call.url.endsWith("/cart/items"))).toHaveLength(1));
  const add = calls.find(call => call.url.endsWith("/cart/items"))!;
  expect(add.body).toEqual({ productId: "product-1", quantity: 1 });
  expect(add.key).toMatch(/^[0-9a-f-]{36}$/);
});

it("keeps the selected quantity through login and submits it once", async () => {
  let authenticated = false;
  const posted: number[] = [];
  vi.stubGlobal("fetch", vi.fn(async (input: string, init?: RequestInit) => {
    const url = String(input);
    if (url.endsWith("/me")) return { ok: authenticated, status: authenticated ? 200 : 401 };
    if (url.endsWith("/login")) { authenticated = true; return { ok: true, status: 200 }; }
    if (url.endsWith("/cart/items")) { posted.push(JSON.parse(String(init?.body)).quantity); return { ok: true, status: 204 }; }
    throw new Error(url);
  }));
  render(<AddToCart productId="product-3" available={4} price={10} />);
  fireEvent.change(screen.getByLabelText("Quantidade"), { target: { value: "3" } });
  expect(screen.getByText(/30,00/)).toBeTruthy();
  fireEvent.click(screen.getByRole("button", { name: "Adicionar ao carrinho" }));
  await screen.findByRole("dialog");
  fireEvent.change(screen.getByLabelText("E-mail"), { target: { value: "cliente@example.test" } });
  fireEvent.change(screen.getByLabelText("Senha", { exact: true }), { target: { value: "password" } });
  fireEvent.click(screen.getByRole("button", { name: /^Entrar$/ }));
  await waitFor(() => expect(posted).toEqual([3]));
});

it("cancels authentication without adding the product", async () => {
  const fetchMock = vi.fn(async () => ({ ok: false, status: 401 }));
  vi.stubGlobal("fetch", fetchMock);
  render(<AddToCart productId="product-2" available={3} />);
  fireEvent.click(screen.getByRole("button", { name: "Adicionar ao carrinho" }));
  await screen.findByRole("dialog");
  fireEvent.click(screen.getByRole("button", { name: "Fechar" }));
  expect(screen.queryByRole("dialog")).toBeNull();
  expect(fetchMock).toHaveBeenCalledTimes(1);
});
