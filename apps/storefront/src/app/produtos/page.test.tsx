// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import ProductsPage from "./page";

const catalogMocks = vi.hoisted(() => ({ getProducts: vi.fn(), getProductFacets: vi.fn() }));

vi.mock("@/lib/catalog", () => ({
  getPartTypeGroups: async () => [{ name: "Freios", slug: "freios", count: 10 }],
  getPartTypes: async () => ({ items: [{ id: "type-1", name: "Pastilha de freio", slug: "pastilha", group: "Freios", groupSlug: "freios", imageUrl: "/piece.jpg" }], total: 10, page: 1, pageSize: 24 }),
  getProductFacets: catalogMocks.getProductFacets,
  getProducts: catalogMocks.getProducts,
}));
vi.mock("@/components/catalog/PartTypeCard", () => ({ PartTypeCard: ({ item }: { item: { name: string } }) => <div>{item.name}</div> }));
vi.mock("@/components/catalog/ProductCard", () => ({ ProductCard: ({ product }: { product: { name: string } }) => <div>{product.name}</div> }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

it("shows group filtering for request types without commercial product filters", async () => {
  render(await ProductsPage({ searchParams: Promise.resolve({ grupo: "freios" }) }));
  expect(screen.getByRole("combobox", { name: "Grupo" })).toBeTruthy();
  expect(screen.getByRole("option", { name: "Freios (10)" }).hasAttribute("selected")).toBe(true);
  expect(screen.getByText("Pastilha de freio")).toBeTruthy();
  expect(screen.queryByRole("combobox", { name: "Marca" })).toBeNull();
  expect(screen.getByRole("link", { name: "Produtos aprovados" }).getAttribute("href")).toContain("modo=aprovados");
});

it("shows category and brand filters only for approved products", async () => {
  catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [{ slug: "freios", name: "Freios", count: 2 }], brands: [{ slug: "bosch", name: "Bosch", count: 2 }] });
  render(await ProductsPage({ searchParams: Promise.resolve({ modo: "aprovados", marca: "bosch" }) }));
  expect(screen.getByRole("combobox", { name: "Categoria" })).toBeTruthy();
  expect(screen.getByRole("combobox", { name: "Marca" })).toBeTruthy();
  expect(screen.queryByRole("combobox", { name: "Grupo" })).toBeNull();
  expect(screen.queryByText("Pastilha de freio")).toBeNull();
  expect(screen.getByRole("link", { name: "Tipos de peças" }).getAttribute("href")).toBe("/produtos#tipos");
});

for (const family of ["carro", "moto", "caminhao"] as const) {
  it(`filters visible catalog records by the existing ${family} family`, async () => {
    catalogMocks.getProducts.mockResolvedValue({
      items: [{ id: family, name: `Cadastro ${family}`, slug: family, sku: `CAT-${family}`, price: null, brand: null, category: "Freios", available: null, imageUrl: `/demo-products/${family}.png` }],
      total: 1, page: 1, pageSize: 24,
    });
    catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

    render(await ProductsPage({ searchParams: Promise.resolve({ vehicleType: family }) }));

    const query = catalogMocks.getProducts.mock.calls[0][0] as URLSearchParams;
    expect(query.get("vehicleType")).toBe(family);
    expect(query.has("approvedOnly")).toBe(false);
    expect(screen.getByText(`Cadastro ${family}`)).toBeTruthy();
    expect(screen.getByText(/não confirma compatibilidade/i)).toBeTruthy();
  });
}

it("shows an explicit empty state when no catalog records match a family", async () => {
  catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

  render(await ProductsPage({ searchParams: Promise.resolve({ vehicleType: "moto" }) }));

  expect(screen.getByRole("status").textContent).toBe("Nenhum cadastro encontrado para esta família.");
});

it("keeps version and year searches restricted to approved, documented products", async () => {
  catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

  render(await ProductsPage({ searchParams: Promise.resolve({ vehicleVersion: "version-id", year: "2024" }) }));

  const query = catalogMocks.getProducts.mock.calls[0][0] as URLSearchParams;
  expect(query.get("vehicleVersion")).toBe("version-id");
  expect(query.get("year")).toBe("2024");
  expect(query.get("approvedOnly")).toBe("true");
  expect(screen.getByText(/aplicação documentada/i)).toBeTruthy();
});
