import Link from "next/link";
import Image from "next/image";
import { ArrowRight, ChevronRight, ShieldCheck } from "lucide-react";
import { ProductCard } from "@/components/catalog/ProductCard";
import { CatalogFilters } from "@/components/catalog/CatalogFilters";
import { getProductFacets, getProducts } from "@/lib/catalog";

type Props = { searchParams: Promise<{ q?: string; categoria?: string; marca?: string; tipo?: string }> };
const vehicleTypes = new Set(["carro", "moto", "caminhao"]);

export default async function Home({ searchParams }: Props) {
  const { q, categoria, marca, tipo } = await searchParams;
  const params = new URLSearchParams();
  if (q?.trim()) params.set("q", q.trim());
  const vehicleType = tipo && vehicleTypes.has(tipo) ? tipo : undefined;
  const selectedCategory = categoria;
  if (selectedCategory) params.set("category", selectedCategory);
  if (vehicleType) params.set("vehicleType", vehicleType);
  if (marca) params.set("brand", marca);
  const [firstPage, facets] = await Promise.all([
    getProducts(params).catch(() => null),
    getProductFacets().catch(() => ({ categories: [], brands: [] })),
  ]);
  const secondPage = firstPage && firstPage.total > firstPage.items.length
    ? await getProducts(new URLSearchParams([...params, ["page", "2"]])).catch(() => null)
    : null;
  const products = firstPage && secondPage ? { ...firstPage, items: [...firstPage.items, ...secondPage.items] } : firstPage;
  return <main id="main-content">
    <section className="store-hero" aria-labelledby="hero-title">
      <Image className="store-hero-image" src="/banner-57brasil.png" alt="Peças automotivas e carro em composição nas cores verde e amarelo" fill priority sizes="100vw" />
      <div className="store-hero-shade" aria-hidden="true" />
      <div className="shell store-hero-content"><div className="store-hero-copy">
        <span className="hero-editorial-label">57 BRASIL <span aria-hidden="true">/</span> AUTOPEÇAS</span>
        <h1 id="hero-title">Encontre sua peça aqui.</h1>
        <p>Consulte produtos cadastrados e confira os dados de cada peça.</p>
        <div className="store-hero-actions"><Link className="btn btn-primary" href="/#catalogo">Explorar produtos <ArrowRight size={18} aria-hidden="true" /></Link><Link className="store-hero-secondary" href="/veiculo">Selecionar meu veículo <ChevronRight size={17} aria-hidden="true" /></Link></div>
      </div></div>
    </section>
    <section id="catalogo" className="showcase-section storefront-catalog" aria-labelledby="showcase-title"><div className="shell">
      <div className="store-section-head"><div><span className="section-kicker">CATÁLOGO 57 BRASIL</span><h2 id="showcase-title">Produtos cadastrados</h2><p className="catalog-intro">{products ? `${products.total} produtos encontrados` : "Catálogo temporariamente indisponível"}</p></div><Link className="inline-link" href="/produtos">Ver catálogo completo <ArrowRight size={17} aria-hidden="true" /></Link></div>
      <CatalogFilters facets={facets} query={q} category={selectedCategory} brand={marca} vehicleType={vehicleType} count={products?.total ?? 0} />
      {products?.items.length ? <div className="product-grid">{products.items.map(product => <ProductCard product={product} key={product.id} />)}</div> : <div className="catalog-empty" role="status"><h3>{products ? "Nenhum produto cadastrado para esta busca" : "Não foi possível carregar o catálogo"}</h3><p>{products ? "Altere a busca ou consulte todos os produtos cadastrados." : "Tente novamente em instantes."}</p><Link href="/produtos" className="inline-link">Ver todos os produtos</Link></div>}
    </div></section>
    <section className="shell store-assurance"><ShieldCheck size={27} aria-hidden="true" /><div><strong>Escolha com informação</strong><p>Compatibilidade só é indicada quando há aplicação cadastrada. Confirme os dados técnicos antes da compra.</p></div><Link href="/veiculo">Buscar pelo veículo <ArrowRight size={17} aria-hidden="true" /></Link></section>
  </main>;
}
