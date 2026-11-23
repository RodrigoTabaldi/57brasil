// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { VehicleSelector } from "./VehicleSelector";

const { push } = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }) }));

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  push.mockReset();
});

describe("illustrative vehicle search", () => {
  it.each([
    ["Carro", "carro"],
    ["Moto", "moto"],
    ["Caminhão", "caminhao"],
  ])("searches the existing %s family", async (label, family) => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => [] }));
    const user = userEvent.setup();
    render(<VehicleSelector />);

    await waitFor(() => expect(fetch).toHaveBeenCalledWith("/api/v1/vehicles/makes"));
    await user.selectOptions(screen.getByRole("combobox", { name: "Família do veículo" }), family);
    await user.click(screen.getByRole("button", { name: "Ver peças ilustrativas" }));

    expect(push).toHaveBeenCalledWith(`/produtos?modo=demonstrativo&vehicleType=${family}`);
  });
});
