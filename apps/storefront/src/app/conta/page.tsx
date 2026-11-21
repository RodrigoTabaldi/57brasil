"use client";

import { useEffect, useState } from "react";
import { AddressBook } from "@/components/ui/AddressBook";
import { AuthModal, currentUser } from "@/components/ui/AuthModal";

type User = { fullName: string; email: string; phoneNumber: string; cpf: string | null };

export default function AccountPage() {
  const [user, setUser] = useState<User | null>(null);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [reset, setReset] = useState<{ email: string; token: string } | null>(null);

  useEffect(() => {
    void currentUser().then(value => setUser(value as User | null))
      .catch(() => setMessage("Não foi possível carregar sua conta. Atualize a página e tente novamente."))
      .finally(() => setLoading(false));
    const params = new URLSearchParams(location.hash.slice(1) || location.search.slice(1));
    if (params.has("token") && params.has("email")) {
      setReset({ email: params.get("email")!, token: params.get("token")! });
      history.replaceState(null, "", "/conta");
    }
  }, []);

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.currentTarget).entries());
    try {
      const response = await fetch("/api/v1/account/me", { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data) });
      if (!response.ok) throw new Error();
      setUser(await response.json() as User);
      setMessage("Dados atualizados.");
    } catch { setMessage("Verifique os dados. O CPF pode já estar cadastrado."); }
  }

  async function changePassword(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!reset) return;
    const password = String(new FormData(event.currentTarget).get("password"));
    try {
      const response = await fetch("/api/v1/account/reset-password", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...reset, password }) });
      if (!response.ok) throw new Error();
      setMessage("Senha redefinida. Faça login.");
      setReset(null);
      history.replaceState(null, "", "/conta");
    } catch { setMessage("Link inválido ou senha fora dos requisitos."); }
  }

  return <main id="main-content" className="container empty-page">
    <h1>Minha conta</h1>
    {message && <p role="status" aria-live="polite">{message}</p>}
    {reset ? <form onSubmit={event => void changePassword(event)}>
      <label>Nova senha<input name="password" type="password" autoComplete="new-password" minLength={12} required /></label>
      <button type="submit">Redefinir senha</button>
    </form> : loading ? <p>Carregando…</p> : user ? <>
      <form onSubmit={event => void save(event)}>
        <p>{user.email}</p>
        <label>Nome completo<input name="fullName" autoComplete="name" defaultValue={user.fullName} required /></label>
        <label>Celular<input name="phone" type="tel" autoComplete="tel" defaultValue={user.phoneNumber} required /></label>
        <label>CPF (opcional)<input name="cpf" inputMode="numeric" defaultValue={user.cpf ?? ""} /></label>
        <button type="submit">Salvar dados</button>
      </form>
      <AddressBook />
    </> : <>
      <p>Entre para ver seus dados e seu carrinho.</p>
      <button type="button" onClick={() => setOpen(true)}>Entrar ou criar conta</button>
    </>}
    {open && <AuthModal onClose={() => setOpen(false)} onSuccess={async () => { setUser(await currentUser() as User); setOpen(false); }} />}
  </main>;
}
