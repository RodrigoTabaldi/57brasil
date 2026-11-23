
const sources = [
  ["Óleo de motor (imagem ilustrativa, editada com IA)", "Interiot, Where next Columbus? e H Padleckas", "Domínio público", "Motor_oil.JPG"],
  ["Alternador (imagem ilustrativa)", "Angelsharum", "CC BY-SA 3.0", "Alternator.jpg"],
  ["Disco de freio (imagem ilustrativa)", "Envy fstop", "CC BY-SA 4.0", "Disc_brakes.jpg"],
  ["Farol (imagem ilustrativa)", "Prabhupuducherry", "CC BY-SA 3.0", "Car_headlamp.JPG"],
  ["Escape (imagem ilustrativa)", "KoS", "Domínio público", "Pot_d%27échappement.jpeg"],
  ["Cubo de roda (imagem ilustrativa)", "Cschirp", "CC BY 3.0", "Wheel_hub_assembly.jpg"],
  ["Bomba de combustível (imagem ilustrativa do grupo Combustível e escape)", "A7N8X", "CC0 1.0", "Pompa_serbatoio_carburante.jpg"],
  ["Pastilha de freio", "Tiia Monto", "CC BY-SA 4.0", "Automobile_brake_pad.jpg"],
  ["Amortecedor", "TTTNIS", "Domínio público", "NISSAN_FUGA_Y50_front_shock_absorber.jpg"],
  ["Filtro de óleo", "Dvortygirl", "CC BY-SA 3.0", "Engine_oil_filter.JPG"],
  ["Bateria", "Shaddack", "Domínio público", "Photo-CarBattery-top.jpg"],
  ["Vela de ignição", "Alf van Beem", "Domínio público", "AC_spark_plug.JPG"],
  ["Correia dentada", "Cain", "CC BY-SA 1.0", "Timing_belt.jpg"],
  ["Radiador", "Bill Wrigley", "Domínio público", "Automobile_radiator.jpg"],
  ["Lâmpada automotiva", "K.Szonja", "CC BY-SA 4.0", "H4_halogen_car_lamp.jpg"],
  ["Pneu", "Janeset", "CC0 1.0", "Car_Tire.jpg"],
  ["Embreagem", "Piero", "Domínio público", "Koppelingsplaat.JPG"],
  ["Palheta", "Wesha", "Domínio público", "Windshield_Wiper_2.jpg"],
  ["Bomba d'água", "Maly LOLek", "CC BY-SA 3.0", "Pompa_wody2.jpg"],
] as const;

export default function ImageCreditsPage() {
  return <main id="main-content" className="shell image-credits"><h1>Créditos das imagens</h1><p>Fotos de referência dos tipos de peças demonstrativos. As cópias locais foram redimensionadas para a vitrine e são exibidas com recorte no cartão. Nenhuma foto representa um produto à venda.</p><ul>{sources.map(([label,author,license,file])=><li key={file}><strong>{label}</strong><span>{author} · {license}</span><a href={`https://commons.wikimedia.org/wiki/File:${file}`} target="_blank" rel="noopener noreferrer">Ver fonte e licença ↗</a></li>)}</ul></main>;
}
