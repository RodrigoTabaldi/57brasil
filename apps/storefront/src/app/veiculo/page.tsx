import { VehicleSelector } from "@/components/ui/VehicleSelector";

export default function VehiclePage() {
  return <main id="main-content" className="shell demo-detail"><span className="section-kicker">BUSCA POR VEÍCULO</span><h1>Encontre peças para o seu veículo</h1><p>Explore os cadastros por família ou, quando houver dados verificados, busque por versão e ano.</p><div className="vehicle-card"><VehicleSelector /></div></main>;
}
