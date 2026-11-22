// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import ProductsPage from "./page";

vi.mock("@/lib/catalog", () => ({
  getPartTypeGroups: async () => [{ name: "Freios", slug: "freios", count: 10 }],
  getPartTypes: async () => ({ items: [{ id: "type-1", name: "Pastilha de freio", slug: "pastilha", group: "Freios", groupSlug: "freios", imageUrl: "/piece.jpg" }], total: 10, page: 1, pageSize: 24 }),
  getProductFacets: async () => ({ categories: [{ slug: "freios", name: "Freios", count: 2 }], brands: [{ slug: "bosch", name: "Bosch", count: 2 }] }),
  getProducts: async () => ({ items: [], total: 0, page: 1, pageSize: 24 }),
}));
vi.mock("@/components/catalog/PartTypeCard", () => ({ PartTypeCard: ({ item }: { item: { name: string } }) => <div>{item.name}</div> }));

afterEach(cleanup);

it("shows group filtering for the 100 request types without irrelevant commercial filters", async () => {
  render(await ProductsPage({ searchParams: Promise.resolve({ grupo: "freios" }) }));
  expect(screen.getByRole("combobox", { name: "Grupo" })).toBeTruthy();
  expect(screen.getByRole("option", { name: "Freios (10)" }).hasAttribute("selected")).toBe(true);
  expect(screen.getByText("Pastilha de freio")).toBeTruthy();
  expect(screen.queryByRole("combobox", { name: "Marca" })).toBeNull();
  expect(screen.getByRole("link", { name: "Produtos aprovados" }).getAttribute("href")).toContain("modo=aprovados");
});

it("shows category and brand filters only for approved products", async () => {
  render(await ProductsPage({ searchParams: Promise.resolve({ modo: "aprovados", marca: "bosch" }) }));
  expect(screen.getByRole("combobox", { name: "Categoria" })).toBeTruthy();
  expect(screen.getByRole("combobox", { name: "Marca" })).toBeTruthy();
  expect(screen.queryByRole("combobox", { name: "Grupo" })).toBeNull();
  expect(screen.queryByText("Pastilha de freio")).toBeNull();
  expect(screen.getByRole("link", { name: "Tipos de peças" }).getAttribute("href")).toBe("/produtos#tipos");
});
