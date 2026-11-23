// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import ProductsPage from "./page";

const catalogMocks = vi.hoisted(() => ({ getPartTypes: vi.fn(), getPartTypeGroups: vi.fn() }));
vi.mock("@/lib/catalog", () => catalogMocks);
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));
vi.mock("@/components/catalog/PartTypeCard", () => ({ PartTypeCard: ({ item }: { item: { name: string } }) => <div>{item.name}</div> }));

beforeEach(() => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getPartTypeGroups.mockResolvedValue([]);
});
afterEach(() => { cleanup(); vi.resetAllMocks(); });

it("shows cataloged parts and forwards the search and group filters", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Filtro de óleo" }], total: 1, page: 1, pageSize: 24 });
  catalogMocks.getPartTypeGroups.mockResolvedValue([{ slug: "motor", name: "Motor", count: 1 }]);
  render(await ProductsPage({ searchParams: Promise.resolve({ q: "filtro", grupo: "motor" }) }));
  const query = catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams;
  expect(query.get("q")).toBe("filtro");
  expect(query.get("group")).toBe("motor");
  expect(screen.getByRole("combobox", { name: "Grupo" })).toHaveProperty("value", "motor");
  expect(screen.getByText("Filtro de óleo")).toBeTruthy();
  expect(screen.getByRole("status").textContent).toBe("1 item cadastrado");
});

it("shows an empty state when no parts match", async () => {
  render(await ProductsPage({ searchParams: Promise.resolve({}) }));
  expect(screen.getByRole("status").textContent).toBe("Nenhuma peça encontrada com estes filtros.");
});

it("keeps filters in pagination links", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Filtro" }], total: 60, page: 2, pageSize: 24 });
  render(await ProductsPage({ searchParams: Promise.resolve({ q: "filtro", categoria: "motor", page: "2" }) }));
  const query = catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams;
  expect(query.get("page")).toBe("2");
  expect(query.get("group")).toBe("motor");
  expect(screen.getByRole("link", { name: "Próxima" }).getAttribute("href")).toBe("/produtos?q=filtro&grupo=motor&page=3#pecas-catalogadas");
});

it("shows a retry message when loading parts fails", async () => {
  catalogMocks.getPartTypes.mockRejectedValue(new Error("API unavailable"));
  render(await ProductsPage({ searchParams: Promise.resolve({}) }));
  expect(screen.getByText("Não foi possível carregar as peças. Tente novamente.").getAttribute("role")).toBe("status");
});

it("keeps the parts visible when loading groups fails", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Filtro" }], total: 1, page: 1, pageSize: 24 });
  catalogMocks.getPartTypeGroups.mockRejectedValue(new Error("API unavailable"));
  render(await ProductsPage({ searchParams: Promise.resolve({}) }));
  expect(screen.getByText("Filtro")).toBeTruthy();
  expect(screen.getByText("Não foi possível carregar os grupos.")).toBeTruthy();
});
