// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { VehicleSelector } from "./VehicleSelector";

const routerMocks = vi.hoisted(() => ({ push: vi.fn() }));

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: routerMocks.push }) }));

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

for (const [label, family] of [["Carro", "carro"], ["Moto", "moto"], ["Caminhão", "caminhao"]] as const) {
  it(`starts an illustrative search for ${label}`, () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<VehicleSelector />);

    const select = screen.getByRole("combobox", { name: "Família do veículo" });
    expect(screen.getByRole("option", { name: label }).getAttribute("value")).toBe(family);
    fireEvent.change(select, { target: { value: family } });
    fireEvent.submit(select.closest("form")!);

    expect(routerMocks.push).toHaveBeenCalledWith(`/produtos?vehicleType=${family}`);
    expect(screen.getByRole("note").textContent).toMatch(/não confirma compatibilidade/i);
  });
}
