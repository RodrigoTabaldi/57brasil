// @vitest-environment jsdom
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { DemoCatalogFilters } from "./DemoCatalogFilters";

afterEach(cleanup);

it("keeps vehicle filters on the home catalog and highlights the selected type", () => {
  render(<DemoCatalogFilters groups={["Freios"]} count={0} vehicleType="moto" />);

  expect(screen.getByRole("link", { name: "Carro" }).getAttribute("href")).toBe("/?tipo=carro#catalogo");
  expect(screen.getByRole("link", { name: "Moto" }).getAttribute("href")).toBe("/?tipo=moto#catalogo");
  expect(screen.getByRole("link", { name: "Caminhão" }).getAttribute("href")).toBe("/?tipo=caminhao#catalogo");
  expect(screen.getByRole("link", { name: "Moto" }).getAttribute("aria-current")).toBe("page");
  expect(screen.queryByRole("heading", { name: "Peças em destaque" })).toBeNull();
});

it("filters demo pieces by fictional brand and keeps it when changing groups", () => {
  render(<DemoCatalogFilters groups={["Freios", "Filtros"]} count={10} selectedBrand="Rodax Demo" />);

  expect(screen.getByRole("combobox", { name: "Marca fictícia" }).getAttribute("name")).toBe("marcaDemo");
  expect(screen.getByRole("link", { name: "Freios" }).getAttribute("href")).toContain("marcaDemo=Rodax+Demo");
  expect(screen.queryByText("Preço simulado")).toBeNull();
});
