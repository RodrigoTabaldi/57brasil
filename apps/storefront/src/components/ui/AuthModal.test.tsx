// @vitest-environment jsdom
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { AuthModal } from "./AuthModal";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it.each([
  [401, "E-mail ou senha inválidos."],
  [403, "Não foi possível validar a origem da solicitação. Acesse o site pelo endereço oficial."],
  [429, "Muitas tentativas. Aguarde um minuto e tente novamente."],
  [502, "Serviço de contas indisponível. Tente novamente mais tarde."],
])("shows the appropriate login error for HTTP %i", async (status, message) => {
  vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status, json: async () => ({}) })));
  render(<AuthModal onClose={() => {}} onSuccess={() => {}} />);
  fireEvent.change(screen.getByLabelText("E-mail"), { target: { value: "cliente@example.test" } });
  fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "password" } });
  fireEvent.click(screen.getByRole("button", { name: "Entrar" }));
  expect((await screen.findByRole("alert")).textContent).toBe(message);
});
