"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

type Option = { id: string; name: string; startYear?: number; endYear?: number };

const vehicleFamilies = [
  { value: "carro", label: "Carro" },
  { value: "moto", label: "Moto" },
  { value: "caminhao", label: "Caminhão" },
] as const;

export function VehicleSelector() {
  const router = useRouter();
  const [family, setFamily] = useState("");
  const [makes, setMakes] = useState<Option[]>([]);
  const [models, setModels] = useState<Option[]>([]);
  const [versions, setVersions] = useState<Option[]>([]);
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [version, setVersion] = useState("");
  const [year, setYear] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    fetch("/api/v1/vehicles/makes")
      .then(response => {
        if (!response.ok) throw new Error();
        return response.json() as Promise<Option[]>;
      })
      .then(data => { if (active) setMakes(data); })
      .catch(() => { if (active) setError("Não foi possível carregar os veículos."); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!make) return;
    let active = true;
    fetch(`/api/v1/vehicles/models?makeId=${encodeURIComponent(make)}`)
      .then(response => {
        if (!response.ok) throw new Error();
        return response.json() as Promise<Option[]>;
      })
      .then(data => { if (active) setModels(data); })
      .catch(() => { if (active) setError("Não foi possível carregar os modelos."); });
    return () => { active = false; };
  }, [make]);

  useEffect(() => {
    if (!model) return;
    let active = true;
    fetch(`/api/v1/vehicles/versions?modelId=${encodeURIComponent(model)}`)
      .then(response => {
        if (!response.ok) throw new Error();
        return response.json() as Promise<Option[]>;
      })
      .then(data => { if (active) setVersions(data); })
      .catch(() => { if (active) setError("Não foi possível carregar as versões."); });
    return () => { active = false; };
  }, [model]);

  const selected = versions.find(item => item.id === version);
  const years = selected?.startYear != null && selected.endYear != null
    ? Array.from({ length: selected.endYear - selected.startYear + 1 }, (_, index) => selected.endYear! - index)
    : [];

  function submitFamily(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (family) router.push(`/produtos?vehicleType=${encodeURIComponent(family)}`);
  }

  function submitVersion(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (version && year) router.push(`/produtos?vehicleVersion=${encodeURIComponent(version)}&year=${encodeURIComponent(year)}`);
  }

  return <div className="vehicle-selector">
    <form onSubmit={submitFamily}>
      <label>Família do veículo
        <select name="vehicleType" value={family} onChange={event => setFamily(event.target.value)} required>
          <option value="">Selecione</option>
          {vehicleFamilies.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}
        </select>
      </label>
      <button className="button" type="submit" disabled={!family}>Ver peças relacionadas</button>
    </form>
    <p role="note">Busca ilustrativa por família, com base nos cadastros existentes. Não confirma compatibilidade com um veículo específico.</p>

    <details className="vehicle-version-search">
      <summary>Buscar por versão e ano com aplicações verificadas</summary>
      <p>Esta busca depende dos dados de veículos e aplicações fornecidos e confirmados pelo fornecedor.</p>
      <form onSubmit={submitVersion}>
        <label>Marca<select name="make" value={make} onChange={event => { setMake(event.target.value); setModel(""); setVersion(""); setYear(""); setModels([]); setVersions([]); }}><option value="">Selecione</option>{makes.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Modelo<select name="model" value={model} disabled={!make} onChange={event => { setModel(event.target.value); setVersion(""); setYear(""); setVersions([]); }}><option value="">Selecione</option>{models.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Versão<select name="version" value={version} disabled={!model} onChange={event => { setVersion(event.target.value); setYear(""); }}><option value="">Selecione</option>{versions.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Ano<select name="year" value={year} disabled={!version} onChange={event => setYear(event.target.value)}><option value="">Selecione</option>{years.map(value => <option key={value} value={value}>{value}</option>)}</select></label>
        <button className="button" type="submit" disabled={!year}>Buscar por versão</button>
      </form>
      {makes.length === 0 && !error && <p>Aplicações verificadas estarão disponíveis após o cadastro dos dados do fornecedor.</p>}
      {error && <p role="alert">{error}</p>}
    </details>
  </div>;
}
