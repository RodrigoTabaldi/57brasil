// Exemplos de tipos de peças para visualização; não representam produtos à venda.
export const demoPieces = [
  { slug: "pastilha-de-freio", name: "Pastilha de freio", group: "Freios", icon: "◫", description: "Componente do sistema de frenagem." },
  { slug: "amortecedor", name: "Amortecedor", group: "Suspensão e direção", icon: "⌁", description: "Componente de controle da suspensão." },
  { slug: "filtro-de-oleo", name: "Filtro de óleo", group: "Filtros", icon: "◎", description: "Elemento de filtragem do lubrificante." },
  { slug: "bateria", name: "Bateria automotiva", group: "Sistema elétrico", icon: "▣", description: "Fonte de energia para o sistema elétrico." },
  { slug: "vela-de-ignicao", name: "Vela de ignição", group: "Ignição", icon: "✳", description: "Componente do sistema de ignição." },
  { slug: "correia-dentada", name: "Correia dentada", group: "Motor", icon: "◉", description: "Componente de sincronização do motor." },
  { slug: "radiador", name: "Radiador", group: "Arrefecimento", icon: "▦", description: "Componente de troca térmica." },
  { slug: "lampada-automotiva", name: "Lâmpada automotiva", group: "Iluminação", icon: "✦", description: "Componente de iluminação do veículo." },
  { slug: "pneu", name: "Pneu", group: "Pneus e rodas", icon: "◌", description: "Componente de contato com o solo." },
  { slug: "kit-de-embreagem", name: "Kit de embreagem", group: "Transmissão e câmbio", icon: "⊛", description: "Conjunto do sistema de transmissão." },
  { slug: "palheta", name: "Palheta do limpador", group: "Acessórios automotivos", icon: "╱", description: "Componente de limpeza do para-brisa." },
  { slug: "bomba-dagua", name: "Bomba d'água", group: "Arrefecimento", icon: "◇", description: "Componente de circulação do líquido de arrefecimento." },
  { slug: "disco-de-freio", name: "Disco de freio", group: "Freios", icon: "◉", description: "Componente do conjunto de frenagem." },
  { slug: "sapata-de-freio", name: "Sapata de freio", group: "Freios", icon: "◫", description: "Componente de freio a tambor." },
  { slug: "filtro-de-ar", name: "Filtro de ar do motor", group: "Filtros", icon: "▤", description: "Elemento de filtragem do ar de admissão." },
  { slug: "filtro-de-combustivel", name: "Filtro de combustível", group: "Filtros", icon: "▤", description: "Elemento de filtragem do combustível." },
  { slug: "filtro-de-cabine", name: "Filtro de cabine", group: "Filtros", icon: "▤", description: "Elemento de filtragem do ar da cabine." },
  { slug: "bieleta", name: "Bieleta", group: "Suspensão e direção", icon: "⌁", description: "Componente de ligação da barra estabilizadora." },
  { slug: "pivo-de-suspensao", name: "Pivô de suspensão", group: "Suspensão e direção", icon: "⌁", description: "Articulação do sistema de suspensão." },
  { slug: "terminal-de-direcao", name: "Terminal de direção", group: "Suspensão e direção", icon: "⌁", description: "Articulação do sistema de direção." },
  { slug: "bobina-de-ignicao", name: "Bobina de ignição", group: "Ignição", icon: "✳", description: "Componente do sistema de ignição." },
  { slug: "cabo-de-vela", name: "Cabo de vela", group: "Ignição", icon: "✳", description: "Conduz energia no sistema de ignição." },
  { slug: "tensor-da-correia", name: "Tensor da correia", group: "Motor", icon: "◉", description: "Componente de tensionamento da correia." },
  { slug: "alternador", name: "Alternador", group: "Sistema elétrico", icon: "▣", description: "Componente de geração elétrica do veículo." },
  { slug: "motor-de-partida", name: "Motor de partida", group: "Sistema elétrico", icon: "▣", description: "Componente de acionamento da partida." },
  { slug: "fusivel-automotivo", name: "Fusível automotivo", group: "Sistema elétrico", icon: "▣", description: "Proteção de circuitos elétricos." },
  { slug: "rolamento-de-roda", name: "Rolamento de roda", group: "Pneus e rodas", icon: "◌", description: "Componente de rotação da roda." },
  { slug: "kit-do-amortecedor", name: "Kit do amortecedor", group: "Suspensão e direção", icon: "⌁", description: "Conjunto de componentes do amortecedor." },
  { slug: "tambor-de-freio", name: "Tambor de freio", group: "Freios", icon: "◫", description: "Componente de freio a tambor." },
  { slug: "fluido-de-freio", name: "Fluido de freio", group: "Freios", icon: "◫", description: "Fluido do sistema hidráulico de freios." },
] as const;

export const demoBrands = ["Autovia Demo", "Rodax Demo", "Pecar Demo"] as const;
export function demoBrand(index: number) { return demoBrands[index % demoBrands.length]; }

// Broad vehicle families for browsing illustrative part types, not fitment claims.
const motoPieces = new Set([
  "pastilha-de-freio", "amortecedor", "filtro-de-oleo", "bateria", "vela-de-ignicao",
  "radiador", "lampada-automotiva", "pneu", "kit-de-embreagem", "disco-de-freio",
  "filtro-de-ar", "filtro-de-combustivel", "bobina-de-ignicao", "fusivel-automotivo",
  "rolamento-de-roda", "fluido-de-freio",
]);
const caminhaoPieces = new Set([
  "pastilha-de-freio", "amortecedor", "filtro-de-oleo", "bateria", "correia-dentada",
  "radiador", "lampada-automotiva", "pneu", "kit-de-embreagem", "palheta",
  "bomba-dagua", "disco-de-freio", "sapata-de-freio", "filtro-de-ar",
  "filtro-de-combustivel", "filtro-de-cabine", "bieleta", "pivo-de-suspensao",
  "terminal-de-direcao", "alternador", "motor-de-partida", "fusivel-automotivo",
  "rolamento-de-roda", "kit-do-amortecedor", "tambor-de-freio", "fluido-de-freio",
]);

export function demoPieceMatchesVehicle(slug: string, vehicleType?: string): boolean {
  if (!vehicleType || vehicleType === "carro") return true;
  if (vehicleType === "moto") return motoPieces.has(slug);
  if (vehicleType === "caminhao") return caminhaoPieces.has(slug);
  return false;
}
