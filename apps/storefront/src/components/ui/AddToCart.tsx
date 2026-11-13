"use client";
import { useState } from "react";
export function AddToCart({productId,available}:{productId:string;available:number}) {
  const [status,setStatus]=useState(""); const [pending,setPending]=useState(false);
  async function add() { setPending(true); setStatus(""); try { const response=await fetch("/api/v1/cart/items",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({productId,quantity:1})}); if(!response.ok) throw new Error(); setStatus("Produto adicionado ao carrinho."); } catch { setStatus("Não foi possível adicionar ao carrinho."); } finally { setPending(false); } }
  return <div><button className="button" type="button" onClick={add} disabled={pending||available<1}>{available<1?"Indisponível":pending?"Adicionando...":"Adicionar ao carrinho"}</button><p role="status" aria-live="polite">{status}</p></div>;
}
