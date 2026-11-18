"use client";
import { useRef, useState } from "react";
import { AuthModal, currentUser } from "./AuthModal";
export function AddToCart({productId,available}:{productId:string;available:number}) {
  const [status,setStatus]=useState(""); const [pending,setPending]=useState(false); const [auth,setAuth]=useState(false); const intent=useRef(false); const lock=useRef(false); const operationKey=useRef("");
  async function put() { if (lock.current) return; lock.current=true; setPending(true); setStatus(""); try { const response=await fetch("/api/v1/cart/items",{method:"POST",headers:{"Content-Type":"application/json","Idempotency-Key":operationKey.current},body:JSON.stringify({productId,quantity:1})}); if(response.status===401){intent.current=true;setAuth(true);return} if(!response.ok) throw new Error(); intent.current=false;operationKey.current="";setStatus("Produto adicionado ao carrinho."); window.dispatchEvent(new Event("cart-updated")); } catch { setStatus("Não foi possível adicionar ao carrinho."); } finally { lock.current=false;setPending(false); } }
  async function add(){ if(lock.current)return; operationKey.current=crypto.randomUUID();lock.current=true;setPending(true);try{const user=await currentUser();if(!user){intent.current=true;setAuth(true);return}}catch{setStatus("Não foi possível verificar a sessão.");return}finally{lock.current=false;setPending(false)}await put()}
  async function authenticated(){setAuth(false);if(intent.current){intent.current=false;await put()}}
  return <div><button className="button" type="button" onClick={add} disabled={pending||available<1}>{available<1?"Indisponível":pending?"Adicionando...":"Adicionar ao carrinho"}</button><p role="status" aria-live="polite">{status}</p>{auth&&<AuthModal onClose={()=>{intent.current=false;setAuth(false)}} onSuccess={authenticated}/>}</div>;
}
