// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { CatalogFilters } from "./CatalogFilters";

afterEach(cleanup);

const facets = {
  categories: [
    { slug: "freios", name: "Freios", count: 5 },
    { slug: "ignicao", name: "Ignição", count: 3 },
  ],
  brands: [{ slug: "bosch", name: "Bosch", count: 2 }],
};

it("restores real catalog filters and preserves the search when changing category", () => {
  render(<CatalogFilters facets={facets} query="cabo" brand="bosch" category="ignicao" count={1} />);

  expect(screen.getByRole("searchbox", { name: "Nome ou código" }).getAttribute("value")).toBe("cabo");
  expect(screen.getByRole("combobox", { name: "Categoria" }).getAttribute("name")).toBe("categoria");
  expect(screen.getByRole("combobox", { name: "Marca" }).getAttribute("name")).toBe("marca");
  expect(screen.getByRole("link", { name: /Freios/ }).getAttribute("href")).toBe("/?q=cabo&categoria=freios&marca=bosch#catalogo");
  expect(screen.getByRole("link", { name: /Ignição/ }).getAttribute("aria-current")).toBe("page");
  expect(screen.getByRole("link", { name: "Carro" }).getAttribute("href")).toBe("/?q=cabo&categoria=ignicao&marca=bosch&tipo=carro#catalogo");
});

it("disables the brand filter when no real brands are registered", () => {
  render(<CatalogFilters facets={{ categories: facets.categories, brands: [] }} count={30} />);
  expect(screen.getByRole("combobox", { name: "Marca" }).hasAttribute("disabled")).toBe(true);
});

it("restores the four original search tabs without claiming unverified vehicle fitment", () => {
  render(<CatalogFilters facets={facets} vehicleType="moto" count={0} />);
  expect(screen.getByRole("link", { name: "Carro" }).getAttribute("href")).toBe("/?tipo=carro#catalogo");
  expect(screen.getByRole("link", { name: "Moto" }).getAttribute("aria-current")).toBe("page");
  expect(screen.getByRole("link", { name: "Caminhão" }).getAttribute("href")).toBe("/?tipo=caminhao#catalogo");
  expect(screen.getByRole("link", { name: /selecione marca, modelo e ano/i }).getAttribute("href")).toBe("/veiculo");
  expect(screen.getByRole("heading", { name: "Peças em destaque" })).toBeTruthy();
  expect(screen.getByRole("link", { name: /Freios/ }).getAttribute("href")).toBe("/?categoria=freios&tipo=moto#catalogo");
  expect(screen.getByRole("link", { name: "Todas as peças" }).getAttribute("href")).toBe("/?tipo=moto#catalogo");
});

it("only offers supported filters for the illustrative catalog", () => {
  render(<CatalogFilters facets={facets} catalogItems count={5} />);
  expect(screen.getByRole("link", { name: /Freios/ }).getAttribute("href")).toBe("/?categoria=freios#catalogo");
  expect(screen.getByRole("searchbox", { name: "Nome ou código" })).toBeTruthy();
  expect(screen.getByRole("link", { name: "Moto" }).getAttribute("href")).toBe("/?tipo=moto#catalogo");
  expect(screen.queryByRole("combobox", { name: "Marca" })).toBeNull();
});

it("keeps the selected category and search field synchronized after navigation", () => {
  const { rerender } = render(<CatalogFilters facets={facets} category="suspensao-e-direcao" query="filtro" vehicleType="moto" catalogItems />);
  expect(screen.getByRole("combobox", { name: "Categoria" })).toHaveProperty("value", "suspensao-e-direcao");
  rerender(<CatalogFilters facets={facets} category="freios" vehicleType="moto" catalogItems />);
  expect(screen.getByRole("combobox", { name: "Categoria" })).toHaveProperty("value", "freios");
  expect(screen.getByRole("link", { name: /Freios/ }).getAttribute("aria-current")).toBe("page");
  expect(screen.getByRole("searchbox", { name: "Nome ou código" })).toHaveProperty("value", "");
});

it("does not show unfiltered category totals when the vehicle or search filter is active", () => {
  const { rerender } = render(<CatalogFilters facets={facets} catalogItems />);
  expect(screen.getByRole("link", { name: /Freios/ }).textContent).toContain("5");
  rerender(<CatalogFilters facets={facets} catalogItems vehicleType="moto" />);
  expect(screen.getByRole("link", { name: /Freios/ }).textContent).not.toContain("5");
  rerender(<CatalogFilters facets={facets} catalogItems query="vela" />);
  expect(screen.getByRole("link", { name: /Freios/ }).textContent).not.toContain("5");
});
