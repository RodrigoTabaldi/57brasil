export type ProductSummary = { id: string; name: string; slug: string; sku: string; price: number; brand: string; category: string; available: number };
export type ProductDetail = ProductSummary & { manufacturerCode: string | null; shortDescription: string | null; description: string | null };
export type ProductPage = { items: ProductSummary[]; total: number; page: number; pageSize: number };
const baseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5000";
export async function getProducts(params: URLSearchParams): Promise<ProductPage> {
  const response = await fetch(`${baseUrl}/api/v1/products?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Não foi possível carregar o catálogo.");
  return response.json() as Promise<ProductPage>;
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
