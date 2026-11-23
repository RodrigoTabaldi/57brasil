import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { demoPieces } from "./demo-catalog";

describe("catalog draft images", () => {
  it("lists every seeded catalog draft with an image in the same public format", () => {
    const draftsPath = fileURLToPath(new URL("../../../../backend/src/Brasil57.Api/catalog-drafts.json", import.meta.url));
    const drafts = JSON.parse(readFileSync(draftsPath, "utf8")) as { slug: string; vehicleFamilies: string[] }[];
    const demoSlugs = demoPieces.map(piece => piece.slug).sort();
    const imageDimensions = new Set<string>();

    expect(drafts.length).toBeGreaterThan(0);
    expect(drafts.map(draft => draft.slug).sort()).toEqual(demoSlugs);
    for (const draft of drafts) {
      expect(draft.vehicleFamilies.length).toBeGreaterThan(0);
      expect(draft.vehicleFamilies.every(family => ["carro", "moto", "caminhao"].includes(family))).toBe(true);

      const imagePath = fileURLToPath(new URL(`../../public/demo-products/${draft.slug}.png`, import.meta.url));
      expect(existsSync(imagePath)).toBe(true);
      const image = readFileSync(imagePath);
      expect(image.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]))).toBe(true);
      imageDimensions.add(`${image.readUInt32BE(16)}x${image.readUInt32BE(20)}`);
    }

    expect(imageDimensions.size).toBe(1);
  });
});
