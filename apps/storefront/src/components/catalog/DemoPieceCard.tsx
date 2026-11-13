import Link from "next/link";
import Image from "next/image";
import { ArrowUpRight } from "lucide-react";
import type { demoPieces } from "@/lib/demo-catalog";
import { demoImages } from "@/lib/demo-images";

type DemoPiece = (typeof demoPieces)[number];

export function DemoPieceCard({ piece, index = 0 }: { piece: DemoPiece; index?: number }) {
  return <Link className="demo-piece-card" href={`/demonstracao/${piece.slug}`} style={{"--card-index":index} as React.CSSProperties}>
    <span className="demo-piece-art">{demoImages[piece.slug] ? <Image src={demoImages[piece.slug]} alt={`Exemplo visual de ${piece.name.toLowerCase()}`} fill sizes="(max-width: 650px) 50vw, (max-width: 900px) 33vw, 25vw" /> : <span aria-hidden="true">{piece.icon}</span>}</span>
    <span className="demo-piece-content"><small>EXEMPLO · {piece.group}</small><strong>{piece.name}</strong><span>Conhecer esta peça <ArrowUpRight size={17} aria-hidden="true" /></span></span>
  </Link>;
}
