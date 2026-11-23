export type ProductSummary = { id: string; name: string; slug: string; sku: string; price: number; brand: string; category: string; available: number; imageUrl: string | null };
export type ProductDetail = ProductSummary & { manufacturerCode: string | null; shortDescription: string | null; description: string | null; brandSlug: string; categorySlug: string; weightKg: number | null; heightCm: number | null; widthCm: number | null; lengthCm: number | null; images: { id: string; url: string; altText: string }[] };
export type ProductPage = { items: ProductSummary[]; total: number; page: number; pageSize: number };
export type ProductFacets = { categories: { slug: string; name: string; count: number }[]; brands: { slug: string; name: string; count: number }[] };
export type PartType = { id: string; name: string; slug: string; group: string; groupSlug: string; imageUrl: string; referencePrice: number; sku?: string; manufacturerCode?: string; brand?: string; description?: string; shortDescription?: string };
export type PartTypePage = { items: PartType[]; total: number; page: number; pageSize: number; vehicleTypeApplied?: string | null };
export type PartTypeGroup = { name: string; slug: string; count: number };
const baseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5250";
export async function getPartTypes(params: URLSearchParams): Promise<PartTypePage> {
  const response = await fetch(`${baseUrl}/api/v1/catalog-items?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Não foi possível carregar os tipos de peças.");
  const result = await response.json() as PartTypePage;
  const vehicleType = params.get("vehicleType");
  if (vehicleType && result.vehicleTypeApplied !== vehicleType) throw new Error("O filtro de veículo não está disponível nesta versão da API.");
  return result;
}
export async function getPartTypeGroups(): Promise<PartTypeGroup[]> {
  const response = await fetch(`${baseUrl}/api/v1/catalog-items/groups`, { cache: "no-store" });
  if (!response.ok) throw new Error("Não foi possível carregar os grupos de peças.");
  return response.json() as Promise<PartTypeGroup[]>;
}
export async function getPartType(slug: string): Promise<PartType | null> {
  const response = await fetch(`${baseUrl}/api/v1/catalog-items/${encodeURIComponent(slug)}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Não foi possível carregar o tipo de peça.");
  return response.json() as Promise<PartType>;
}
export async function getProducts(params: URLSearchParams): Promise<ProductPage> {
  const response = await fetch(`${baseUrl}/api/v1/products?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Não foi possível carregar o catálogo.");
  return response.json() as Promise<ProductPage>;
}
export async function getProductFacets(params = new URLSearchParams()): Promise<ProductFacets> {
  const response = await fetch(`${baseUrl}/api/v1/products/facets?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Não foi possível carregar os filtros.");
  return response.json() as Promise<ProductFacets>;
}
export async function getProduct(slug: string): Promise<ProductDetail | null> {
  const response = await fetch(`${baseUrl}/api/v1/products/${encodeURIComponent(slug)}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Não foi possível carregar o produto.");
  return response.json() as Promise<ProductDetail>;
}
export function formatPrice(value: number): string { return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value); }
export type Application = {make:string;model:string;version:string;startYear:number;endYear:number;restriction:string|null};
export async function getApplications(slug:string):Promise<Application[]>{const response=await fetch(`${baseUrl}/api/v1/products/${encodeURIComponent(slug)}/applications`,{cache:"no-store"});if(!response.ok)throw new Error("Não foi possível carregar as aplicações.");return response.json() as Promise<Application[]>}
