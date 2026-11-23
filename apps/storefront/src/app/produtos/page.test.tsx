// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import ProductsPage from "./page";

const catalogMocks = vi.hoisted(() => ({ getProducts: vi.fn(), getProductFacets: vi.fn() }));

vi.mock("@/lib/catalog", () => ({
  getProductFacets: catalogMocks.getProductFacets,
  getProducts: catalogMocks.getProducts,
}));
vi.mock("@/components/catalog/ProductCard", () => ({ ProductCard: ({ product }: { product: { name: string } }) => <div>{product.name}</div> }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

it("shows products with category and brand filters", async () => {
  catalogMocks.getProducts.mockResolvedValue({
    items: [{ id: "published-1", name: "Filtro publicado", slug: "filtro", sku: "SKU-1", price: 25, brand: "Bosch", category: "Filtros", available: 4, imageUrl: "/filtro.jpg" }],
    total: 1, page: 1, pageSize: 24,
  });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [{ slug: "filtros", name: "Filtros", count: 1 }], brands: [{ slug: "bosch", name: "Bosch", count: 1 }] });

  render(await ProductsPage({ searchParams: Promise.resolve({ marca: "bosch" }) }));

  const productQuery = catalogMocks.getProducts.mock.calls[0][0] as URLSearchParams;
  const facetsQuery = catalogMocks.getProductFacets.mock.calls[0][0] as URLSearchParams;
  expect(productQuery.get("brand")).toBe("bosch");
  expect(productQuery.has("approvedOnly")).toBe(false);
  expect(facetsQuery.has("approvedOnly")).toBe(false);
  expect(screen.getByRole("heading", { level: 1, name: "Produtos disponíveis para compra" })).toBeTruthy();
  expect(screen.getByRole("combobox", { name: "Categoria" })).toBeTruthy();
  expect(screen.getByRole("combobox", { name: "Marca" })).toBeTruthy();
  expect(screen.getByText("Filtro publicado")).toBeTruthy();
  expect(screen.queryByText("Pastilha de freio")).toBeNull();
});

for (const family of ["carro", "moto", "caminhao"] as const) {
  it(`filters published products by the ${family} vehicle family`, async () => {
    catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
    catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

    render(await ProductsPage({ searchParams: Promise.resolve({ vehicleType: family }) }));

    const query = catalogMocks.getProducts.mock.calls[0][0] as URLSearchParams;
    expect(query.get("vehicleType")).toBe(family);
    expect(screen.getByRole("heading", { name: "Produtos disponíveis para compra nesta família" })).toBeTruthy();
    expect(screen.getByRole("status").textContent).toBe("Nenhum produto disponível para compra encontrado com estes filtros.");
  });
}

it("shows an explicit empty state when no published products match", async () => {
  catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

  render(await ProductsPage({ searchParams: Promise.resolve({}) }));

  expect(screen.getByRole("status").textContent).toBe("Nenhum produto disponível para compra encontrado com estes filtros.");
});

it("keeps version and year filters in the published-product search", async () => {
  catalogMocks.getProducts.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 24 });
  catalogMocks.getProductFacets.mockResolvedValue({ categories: [], brands: [] });

  render(await ProductsPage({ searchParams: Promise.resolve({ vehicleVersion: "version-id", year: "2024" }) }));

  const query = catalogMocks.getProducts.mock.calls[0][0] as URLSearchParams;
  expect(query.get("vehicleVersion")).toBe("version-id");
  expect(query.get("year")).toBe("2024");
  expect(screen.getByText(/somente produtos publicados/i)).toBeTruthy();
});
