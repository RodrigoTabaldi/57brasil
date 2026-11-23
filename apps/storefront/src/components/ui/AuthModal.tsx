"use client";
import Link from "next/link";
import { useEffect, useRef, useState } from "react";

type User = { fullName: string; email: string };
type Mode = "login" | "register" | "forgot";
export async function currentUser(): Promise<User | null> { const r = await fetch("/api/v1/account/me", { cache: "no-store" }); return r.ok ? r.json() : null; }
export function AuthModal({ onClose, onSuccess }: { onClose: () => void; onSuccess: () => Promise<void> | void }) {
  const [mode, setMode] = useState<Mode>("login"); const [busy, setBusy] = useState(false); const [error, setError] = useState(""); const [show, setShow] = useState(false); const [done, setDone] = useState(false);
  const ref = useRef<HTMLDivElement>(null); const first = useRef<HTMLInputElement>(null);
  useEffect(() => { const previous = document.activeElement as HTMLElement | null; first.current?.focus(); return () => previous?.focus(); }, [mode]);
  async function submit(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); if (busy) return; const data = new FormData(event.currentTarget); const body = Object.fromEntries(data.entries()); if (mode === "register" && body.password !== body.confirmPassword) { setError("As senhas não coincidem."); return; } setBusy(true); setError("");
    try { const r = await fetch(`/api/v1/account/${mode === "register" ? "register" : mode === "forgot" ? "forgot-password" : "login"}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (!r.ok) { const result = await r.json().catch(() => ({})); const fallback = r.status === 401 && mode === "login" ? "E-mail ou senha inválidos." : r.status === 403 ? "Não foi possível validar a origem da solicitação. Acesse o site pelo endereço oficial." : r.status === 429 ? "Muitas tentativas. Aguarde um minuto e tente novamente." : r.status >= 500 ? "Serviço de contas indisponível. Tente novamente mais tarde." : "Não foi possível concluir. Confira os dados."; throw new Error(result.errors?.form?.[0] ?? result.message ?? fallback); }
      if (mode === "forgot") setDone(true); else await onSuccess();
    } catch (e) { setError(e instanceof TypeError ? "Não foi possível conectar ao serviço de contas. Verifique sua conexão e tente novamente." : e instanceof Error ? e.message : "Falha inesperada."); } finally { setBusy(false); }
  }
  return <div className="auth-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}><div className="auth-dialog" ref={ref} role="dialog" aria-modal="true" aria-labelledby="auth-title" onKeyDown={e => { if (e.key === "Escape") onClose(); if (e.key === "Tab") { const nodes = ref.current?.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled)'); if (!nodes?.length) return; const first = nodes[0], last = nodes[nodes.length - 1]; if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); } else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); } } }}>
    <button type="button" className="auth-close" onClick={onClose} aria-label="Fechar">×</button><h2 id="auth-title">{mode === "register" ? "Criar conta" : mode === "forgot" ? "Recuperar senha" : "Entrar"}</h2>
    {done ? <p>Se a conta existir, você receberá um e-mail com as instruções.</p> : <form onSubmit={submit}>
      {mode === "register" && <><label>Nome completo<input ref={first} name="fullName" autoComplete="name" required minLength={2} maxLength={160}/></label><label>Celular<input name="phone" inputMode="tel" autoComplete="tel" required pattern="[0-9() +.-]{10,20}"/></label><label>CPF (opcional)<input name="cpf" inputMode="numeric" pattern="[0-9.\-]{11,14}"/></label></>}
      <label>E-mail<input ref={mode === "register" ? undefined : first} name="email" type="email" autoComplete="email" required/></label>
      {mode !== "forgot" && <><label>Senha<input name="password" type={show ? "text" : "password"} autoComplete={mode === "register" ? "new-password" : "current-password"} required minLength={mode === "register" ? 12 : undefined}/></label>{mode === "register" && <><small>Mínimo de 12 caracteres com maiúscula, minúscula, número e símbolo.</small><label>Confirmar senha<input name="confirmPassword" type={show ? "text" : "password"} autoComplete="new-password" required/></label></>}<button type="button" onClick={() => setShow(!show)}>{show ? "Ocultar" : "Mostrar"} senha</button></>}
      {mode === "register" && <p className="auth-privacy">Usamos seus dados para criar e administrar sua conta. Saiba mais em <Link href="/privacidade" target="_blank" rel="noopener noreferrer">Privacidade e dados pessoais (abre em nova aba)</Link>.</p>}
      {error && <p role="alert">{error}</p>}<button className="button" disabled={busy} type="submit">{busy ? "Aguarde..." : mode === "register" ? "Criar conta" : mode === "forgot" ? "Enviar instruções" : "Entrar"}</button>
    </form>}
    <div className="auth-switch"><button type="button" onClick={() => { setMode(mode === "register" ? "login" : "register"); setError(""); setDone(false); }}>{mode === "register" ? "Já tenho conta" : "Criar conta"}</button><button type="button" onClick={() => { setMode(mode === "forgot" ? "login" : "forgot"); setError(""); setDone(false); }}>{mode === "forgot" ? "Voltar ao login" : "Esqueci minha senha"}</button></div>
  </div></div>;
}
