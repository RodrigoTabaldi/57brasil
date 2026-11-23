type Props = { label: string; variant?: "catalog" | "detail" };

export function CatalogLoading({ label, variant = "catalog" }: Props) {
  if (variant === "detail") {
    return <main id="main-content" className="shell product-page catalog-loading-detail" aria-busy="true">
      <p className="sr-only" role="status">Carregando {label}…</p>
      <div className="breadcrumb" aria-hidden="true"><div className="loading-line" /></div>
      <div className="product-detail" aria-hidden="true">
        <div className="product-image"><div className="loading-line" /></div>
        <div className="product-info">
          <div className="loading-line" />
          <div className="loading-line" />
          <div className="loading-line" />
          <div className="loading-line" />
        </div>
      </div>
    </main>;
  }

  return <main id="main-content" className="shell catalog-page" aria-busy="true">
    <p className="sr-only" role="status">Carregando {label}…</p>
    <div className="loading-line loading-title" aria-hidden="true" />
    <div className="loading-line loading-copy" aria-hidden="true" />
    <div className="catalog-loading-layout" aria-hidden="true">
      <aside className="filter-panel"><div className="loading-line" /><div className="loading-line" /><div className="loading-line" /></aside>
      <div className="catalog-loading-grid">
        {Array.from({ length: 6 }, (_, index) => <div className="loading-card" key={index}><div className="loading-line" /><div className="loading-line" /></div>)}
      </div>
    </div>
  </main>;
}
