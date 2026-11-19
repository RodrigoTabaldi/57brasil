import { demoPieces } from "./demo-catalog";

export const demoImages: Record<string, string> = Object.fromEntries(
  demoPieces.map(({ slug }) => [slug, `/demo-products/${slug}.png`]),
);
