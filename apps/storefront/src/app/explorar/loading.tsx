export default function ExploreLoading() {
  return <main id="main-content" className="shell explore-page" aria-busy="true"><div className="loading-line loading-title"/><div className="loading-line loading-copy"/><div className="loading-chips">{Array.from({length:4},(_,index)=><div className="loading-line" key={index}/>)}</div><div className="loading-grid">{Array.from({length:8},(_,index)=><div className="loading-card" key={index}><div className="loading-line"/><div className="loading-line"/></div>)}</div><span className="sr-only" role="status">Carregando peças…</span></main>;
}
