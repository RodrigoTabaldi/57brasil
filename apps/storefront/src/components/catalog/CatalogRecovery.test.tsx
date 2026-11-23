// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import type { ComponentProps } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { CatalogImage } from "./CatalogImage";
import { CatalogRetryButton } from "./CatalogRetryButton";
import { DemoImageViewer } from "./DemoImageViewer";

const { refresh } = vi.hoisted(() => ({ refresh: vi.fn() }));

vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh }) }));
vi.mock("next/image", () => ({
  default: (props: ComponentProps<"img"> & { fill?: boolean; priority?: boolean; sizes?: string }) => {
    const { fill: _fill, priority: _priority, sizes: _sizes, ...imageProps } = props;
    return <img {...imageProps} />;
  },
}));

afterEach(() => {
  cleanup();
  refresh.mockReset();
});

it("shows an accessible placeholder when a catalog image fails", () => {
  render(<CatalogImage src="/peca.jpg" alt="Imagem ilustrativa de pastilha" sizes="50vw" />);

  fireEvent.error(screen.getByRole("img", { name: "Imagem ilustrativa de pastilha" }));

  expect(screen.getByRole("img", { name: "Imagem ilustrativa de pastilha. Imagem indisponível" })).toBeTruthy();
  expect(screen.getByText("Imagem indisponível")).toBeTruthy();
});

it("refreshes the current catalog route when retry is selected", () => {
  render(<CatalogRetryButton />);

  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));

  expect(refresh).toHaveBeenCalledOnce();
});

it("returns keyboard focus to the image trigger after closing the viewer", () => {
  render(<DemoImageViewer images={[{ id: "image-1", url: "/peca.jpg", altText: "Pastilha de freio" }]} name="Pastilha de freio" />);

  const trigger = screen.getByRole("button", { name: "Ampliar imagem de Pastilha de freio" });
  fireEvent.click(trigger);

  expect(screen.getByRole("dialog", { name: "Imagens ampliadas de Pastilha de freio" })).toBeTruthy();
  expect(screen.getByRole("button", { name: "Fechar imagem" })).toBe(document.activeElement);
  fireEvent.keyDown(document, { key: "Escape" });

  expect(screen.queryByRole("dialog")).toBeNull();
  expect(trigger).toBe(document.activeElement);
});

it("keeps the unavailable image message reachable by keyboard", () => {
  render(<DemoImageViewer images={[{ id: "image-1", url: "/peca.jpg", altText: "Pastilha de freio" }]} name="Pastilha de freio" />);

  fireEvent.error(screen.getByRole("img", { name: "Pastilha de freio" }));

  const trigger = screen.getByRole("button", { name: "Imagem de Pastilha de freio indisponível" });
  expect(trigger.getAttribute("aria-disabled")).toBe("true");
  expect(trigger.hasAttribute("disabled")).toBe(false);
  expect(screen.getByRole("status").textContent).toBe("Imagem indisponível");
});
