"use client";

import { useState } from "react";

export type AdminProductRecord = {
  id: string; name: string; slug: string; sku: string; price: number; published: boolean;
  brandId: string; manufacturerCode: string | null; shortDescription: string | null; description: string | null;
  priceSourceReference: string | null; stockSourceReference: string | null; compatibilitySourceReference: string | null;
  supplierConfirmedAt: string | null; applicationNotes: string | null;
  weightKg: number | null; heightCm: number | null; widthCm: number | null; lengthCm: number | null; available: number; imageUrl: string | null;
};
type Brand = { id: string; name: string };

export function ProductEditor({ product, brands, onSaved }: { product: AdminProductRecord; brands: Brand[]; onSaved: () => Promise<void> }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError("");
    const data = new FormData(event.currentTarget);
    let productSaved = false;
    const number = (name: string) => Number(data.get(name));
    const body = {
      name: String(data.get("name") ?? "").trim(), sku: String(data.get("sku") ?? "").trim(),
      brandId: String(data.get("brandId") ?? ""), manufacturerCode: String(data.get("manufacturerCode") ?? "").trim(),
      shortDescription: String(data.get("shortDescription") ?? "").trim(), description: String(data.get("description") ?? "").trim(),
      priceSourceReference: String(data.get("priceSourceReference") ?? "").trim(),
      stockSourceReference: String(data.get("stockSourceReference") ?? "").trim(),
      compatibilitySourceReference: String(data.get("compatibilitySourceReference") ?? "").trim(),
      applicationNotes: String(data.get("applicationNotes") ?? "").trim(),
      supplierConfirmedAt: data.get("supplierConfirmedAt") ? new Date(String(data.get("supplierConfirmedAt"))).toISOString() : null,
      price: number("price"), weightKg: number("weightKg"), heightCm: number("heightCm"), widthCm: number("widthCm"), lengthCm: number("lengthCm"),
    };
    try {
      const response = await fetch(`/api/v1/admin/products/${product.id}`, { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (!response.ok) throw new Error();
      productSaved = true;
      const imageUrl = String(data.get("imageUrl") ?? "").trim();
      if (imageUrl) {
        const imageResponse = await fetch(`/api/v1/admin/products/${product.id}/images`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ url: imageUrl, altText: String(data.get("imageAltText") ?? "").trim() || `Imagem de ${product.name}`, sortOrder: product.imageUrl ? 1 : 0 }) });
        if (!imageResponse.ok) { await onSaved(); setError("Dados salvos, mas não foi possível anexar a imagem. Confira se a URL HTTPS está acessível."); return; }
      }
      await onSaved();
    } catch { setError(productSaved ? "Dados salvos, mas não foi possível anexar a imagem. Confira se a URL HTTPS está acessível." : "Não foi possível salvar. Confira SKU, marca, preço, medidas e referências dos documentos."); }
    finally { setBusy(false); }
  }
  return <details className="admin-product-editor"><summary>Editar dados do produto</summary>
    <form onSubmit={event => void save(event)}><div className="admin-grid">
      <label>Nome<input name="name" defaultValue={product.name} maxLength={120} required /></label>
      <label>SKU real<input name="sku" defaultValue={product.sku} maxLength={80} required /></label>
      <label>Marca<select name="brandId" defaultValue={brands.some(item => item.id === product.brandId) ? product.brandId : ""} required><option value="">Selecione a marca real</option>{brands.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label>Código do fabricante<input name="manufacturerCode" defaultValue={product.manufacturerCode ?? ""} maxLength={100} /></label>
      <label>Preço (R$)<input name="price" type="number" min="0" max="9999999999.99" step="0.01" defaultValue={product.price} required /></label>
      <label>Documento do preço<input name="priceSourceReference" defaultValue={product.priceSourceReference ?? ""} minLength={5} maxLength={200} placeholder="Ex.: tabela 2026, página 4…" /></label>
      <label>Documento do estoque<input name="stockSourceReference" defaultValue={product.stockSourceReference ?? ""} minLength={5} maxLength={200} placeholder="Ex.: nota fiscal 123…" /></label>
      <label>Documento da aplicação veicular<input name="compatibilitySourceReference" defaultValue={product.compatibilitySourceReference ?? ""} minLength={5} maxLength={200} placeholder="Ex.: catálogo fabricante, página 8…" /></label>
      <label>Aplicação informada<textarea name="applicationNotes" defaultValue={product.applicationNotes ?? ""} maxLength={500} /></label>
      <label>Data da confirmação<input name="supplierConfirmedAt" type="date" defaultValue={product.supplierConfirmedAt?.slice(0, 10) ?? ""} /></label>
      <label>Peso (kg)<input name="weightKg" type="number" min="0.001" step="0.001" defaultValue={product.weightKg ?? ""} required /></label>
      <label>Altura (cm)<input name="heightCm" type="number" min="0.01" step="0.01" defaultValue={product.heightCm ?? ""} required /></label>
      <label>Largura (cm)<input name="widthCm" type="number" min="0.01" step="0.01" defaultValue={product.widthCm ?? ""} required /></label>
      <label>Comprimento (cm)<input name="lengthCm" type="number" min="0.01" step="0.01" defaultValue={product.lengthCm ?? ""} required /></label>
      <label>Resumo<input name="shortDescription" defaultValue={product.shortDescription ?? ""} /></label>
      <label>Descrição completa<textarea name="description" defaultValue={product.description ?? ""} /></label>
      <label>Adicionar imagem (URL HTTPS)<input name="imageUrl" type="url" pattern="https://.+" maxLength={500} placeholder="https://exemplo.com/imagem.jpg" /></label>
      <label>Texto alternativo da imagem<input name="imageAltText" maxLength={200} placeholder={`Imagem de ${product.name}`} /></label>
    </div><button className="button" type="submit" disabled={busy}>{busy ? "Salvando…" : "Salvar dados"}</button>{error && <p role="alert">{error}</p>}</form>
  </details>;
}
