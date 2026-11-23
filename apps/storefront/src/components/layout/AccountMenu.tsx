"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { AuthModal, currentUser } from "@/components/ui/AuthModal";

type Session = { fullName: string } | null;

export function AccountMenu() {
  const [user, setUser] = useState<Session>(null);
  const [admin, setAdmin] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    let active = true;
    async function refresh() {
      const [account, adminSession] = await Promise.all([
        currentUser().catch(() => null),
        fetch("/api/v1/admin/session", { cache: "no-store" }).then(r => r.ok).catch(() => false),
      ]);
      if (active) { setUser(account); setAdmin(adminSession); }
    }
    void refresh();
    window.addEventListener("auth-updated", refresh);
    return () => { active = false; window.removeEventListener("auth-updated", refresh); };
  }, []);

  async function logout() {
    const response = await fetch(admin ? "/api/v1/admin/session" : "/api/v1/account/logout", { method: admin ? "DELETE" : "POST" });
    if (response.ok) {
      setUser(null); setAdmin(false);
      window.dispatchEvent(new Event("auth-updated"));
      window.dispatchEvent(new Event("cart-updated"));
    }
  }

  return <>{admin ? <Link href="/admin" className="header-action">Administração</Link> : user ? <Link href="/conta" className="header-action">{user.fullName || "Minha conta"}</Link> : <button type="button" className="header-action" onClick={() => setOpen(true)}>Entrar</button>}
    {(admin || user) && <button type="button" onClick={() => void logout()}>Sair</button>}
    {open && <AuthModal onClose={() => setOpen(false)} onSuccess={() => { setOpen(false); window.dispatchEvent(new Event("auth-updated")); }} />}
  </>;
}
