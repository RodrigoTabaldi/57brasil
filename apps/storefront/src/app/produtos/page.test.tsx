// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import ProductsPage from "./page";

const catalogMocks = vi.hoisted(() => ({ getPartTypes: vi.fn(), getPartTypeGroups: vi.fn(), getCatalogCategories: vi.fn() }));
vi.mock("@/lib/catalog", () => catalogMocks);
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));
vi.mock("@/components/catalog/PartTypeCard", () => ({ PartTypeCard: ({ item }: { item: { name: string } }) => <div>{item.name}</div> }));

beforeEach(() => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getPartTypeGroups.mockResolvedValue([]);
  catalogMocks.getCatalogCategories.mockResolvedValue([]);
});
afterEach(() => { cleanup(); vi.resetAllMocks(); });

it("shows cataloged parts and forwards the search and group filters", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Filtro de óleo" }], total: 1, page: 1, pageSize: 24 });
  catalogMocks.getPartTypeGroups.mockResolvedValue([{ slug: "motor", name: "Motor", count: 1 }]);
  render(await ProductsPage({ searchParams: Promise.resolve({ q: "filtro", grupo: "motor" }) }));
  const query = catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams;
  expect(query.get("q")).toBe("filtro");
  expect(query.get("group")).toBe("motor");
  expect(screen.getByRole("combobox", { name: "Grupo ou tipo de peça" })).toHaveProperty("value", "motor");
  expect(screen.getByText("Filtro de óleo")).toBeTruthy();
  expect(screen.getByRole("status").textContent).toBe("1 item cadastrado");
});

it("exposes categories under their group and forwards the selected category", async () => {
  catalogMocks.getPartTypeGroups.mockResolvedValue([{ slug: "filtros", name: "Filtros", count: 33 }]);
  catalogMocks.getCatalogCategories.mockResolvedValue([{ slug: "filtros-filtro-de-oleo", name: "Filtro de óleo", groupSlug: "filtros", count: 9 }]);
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Tecfil PSL55" }], total: 30, page: 1, pageSize: 24 });
  render(await ProductsPage({ searchParams: Promise.resolve({ grupo: "filtros-filtro-de-oleo" }) }));
  expect((catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams).get("group")).toBe("filtros-filtro-de-oleo");
  expect(screen.getByRole("combobox", { name: "Grupo ou tipo de peça" })).toHaveProperty("value", "filtros-filtro-de-oleo");
  expect(screen.getByRole("option", { name: "Filtro de óleo (9)" }).parentElement?.getAttribute("label")).toBe("Filtros");
  expect(screen.getByRole("link", { name: "Próxima" }).getAttribute("href")).toContain("grupo=filtros-filtro-de-oleo");
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

it("applies the selected vehicle and preserves it in the form and pagination", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "1", name: "Filtro" }], total: 30, page: 1, pageSize: 24 });
  const { container } = render(await ProductsPage({ searchParams: Promise.resolve({ vehicleType: "moto" }) }));
  expect((catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams).get("vehicleType")).toBe("moto");
  expect(container.querySelector<HTMLInputElement>('input[name="vehicleType"]')?.value).toBe("moto");
  expect(screen.getByRole("link", { name: "Próxima" }).getAttribute("href")).toContain("vehicleType=moto");
});

it("makes all 100 catalog items reachable through pagination", async () => {
  catalogMocks.getPartTypes.mockResolvedValue({ items: [{ id: "100", name: "Última peça" }], total: 100, page: 5, pageSize: 24 });
  render(await ProductsPage({ searchParams: Promise.resolve({ page: "5" }) }));
  expect((catalogMocks.getPartTypes.mock.calls[0][0] as URLSearchParams).get("page")).toBe("5");
  expect(screen.getByText("Última peça")).toBeTruthy();
  expect(screen.getByRole("status").textContent).toBe("100 itens cadastrados");
  expect(screen.getByRole("link", { name: "Anterior" }).getAttribute("href")).toContain("page=4");
  expect(screen.queryByRole("link", { name: "Próxima" })).toBeNull();
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
