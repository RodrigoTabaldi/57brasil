"use client";

import { useEffect, useState } from "react";

type Address = {
  id: string;
  recipient: string;
  phone: string;
  cep: string;
  street: string;
  number: string;
  complement: string | null;
  neighborhood: string;
  city: string;
  state: string;
};

export function AddressBook() {
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [editing, setEditing] = useState<Address | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");

  async function load() {
    try {
      const response = await fetch("/api/v1/account/addresses", { cache: "no-store" });
      if (!response.ok) throw new Error();
      setAddresses(await response.json() as Address[]);
    } catch { setMessage("Não foi possível carregar os endereços. Tente novamente."); }
  }

  useEffect(() => { void load(); }, []);

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const form = event.currentTarget;
    const data = Object.fromEntries(new FormData(form).entries());
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(editing ? `/api/v1/account/addresses/${editing.id}` : "/api/v1/account/addresses", {
        method: editing ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!response.ok) throw new Error();
      setEditing(null);
      form.reset();
      await load();
      setMessage("Endereço salvo.");
    } catch { setMessage("Não foi possível salvar. Confira os campos e tente novamente."); }
    finally { setBusy(false); }
  }

  async function remove(address: Address) {
    if (!window.confirm(`Excluir o endereço em ${address.street}, ${address.number}?`)) return;
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/account/addresses/${address.id}`, { method: "DELETE" });
      if (!response.ok) throw new Error();
      if (editing?.id === address.id) setEditing(null);
      await load();
      setMessage("Endereço excluído.");
    } catch { setMessage("Não foi possível excluir o endereço. Tente novamente."); }
    finally { setBusy(false); }
  }

  return <section className="address-book" aria-labelledby="address-book-title">
    <h2 id="address-book-title">Meus endereços</h2>
    <p>Cadastre um endereço para futuras entregas. O CEP usado para cotação não salva o endereço automaticamente.</p>
    {message && <p role="status" aria-live="polite">{message}</p>}
    {addresses.length === 0 ? <p>Nenhum endereço cadastrado.</p> : <ul>
      {addresses.map(address => <li key={address.id}>
        <strong>{address.recipient}</strong>
        <p>{address.street}, {address.number}{address.complement ? `, ${address.complement}` : ""}<br />
          {address.neighborhood} · {address.city}/{address.state} · CEP {address.cep}</p>
        <button type="button" disabled={busy} onClick={() => setEditing(address)}>Editar</button>{" "}
        <button type="button" disabled={busy} onClick={() => void remove(address)}>Excluir</button>
      </li>)}
    </ul>}
    <h3>{editing ? "Editar endereço" : "Adicionar endereço"}</h3>
    <form key={editing?.id ?? "new"} onSubmit={event => void save(event)}>
      <label>Destinatário<input name="recipient" autoComplete="shipping name" defaultValue={editing?.recipient} minLength={2} maxLength={160} required /></label>
      <label>Celular<input name="phone" type="tel" autoComplete="shipping tel" defaultValue={editing?.phone} required /></label>
      <label>CEP<input name="cep" inputMode="numeric" autoComplete="shipping postal-code" defaultValue={editing?.cep} pattern="[0-9]{5}-?[0-9]{3}" required /></label>
      <label>Rua ou avenida<input name="street" autoComplete="shipping address-line1" defaultValue={editing?.street} minLength={2} maxLength={160} required /></label>
      <label>Número<input name="number" autoComplete="off" defaultValue={editing?.number} maxLength={20} required /></label>
      <label>Complemento (opcional)<input name="complement" autoComplete="shipping address-line2" defaultValue={editing?.complement ?? ""} maxLength={120} /></label>
      <label>Bairro<input name="neighborhood" autoComplete="shipping address-level3" defaultValue={editing?.neighborhood} minLength={2} maxLength={120} required /></label>
      <label>Cidade<input name="city" autoComplete="shipping address-level2" defaultValue={editing?.city} minLength={2} maxLength={120} required /></label>
      <label>UF<input name="state" autoComplete="shipping address-level1" defaultValue={editing?.state} minLength={2} maxLength={2} required /></label>
      <button type="submit" disabled={busy}>{busy ? "Salvando…" : "Salvar endereço"}</button>{" "}
      {editing && <button type="button" disabled={busy} onClick={() => setEditing(null)}>Cancelar edição</button>}
    </form>
  </section>;
}
