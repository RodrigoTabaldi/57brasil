import { describe, expect, it } from "vitest";
import { demoPieceMatchesVehicle, demoPieces } from "./demo-catalog";

describe("vehicle tabs for illustrative parts", () => {
  it("shows every illustrative part under Peças and Carro", () => {
    for (const piece of demoPieces) {
      expect(demoPieceMatchesVehicle(piece.slug)).toBe(true);
      expect(demoPieceMatchesVehicle(piece.slug, "carro")).toBe(true);
    }
  });

  it("limits Moto and Caminhão to their related part types", () => {
    expect(demoPieceMatchesVehicle("vela-de-ignicao", "moto")).toBe(true);
    expect(demoPieceMatchesVehicle("filtro-de-cabine", "moto")).toBe(false);
    expect(demoPieceMatchesVehicle("filtro-de-cabine", "caminhao")).toBe(true);
    expect(demoPieceMatchesVehicle("vela-de-ignicao", "caminhao")).toBe(false);
  });
});
