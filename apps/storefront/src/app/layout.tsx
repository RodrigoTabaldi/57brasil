import type { Metadata } from "next";
import { SiteHeader } from "@/components/layout/SiteHeader";
import { SiteFooter } from "@/components/layout/SiteFooter";
import "./globals.css";
import "../styles/base.css";
import "../styles/layout.css";
import "../styles/home.css";
import "../styles/catalog.css";
import "../styles/forms.css";

export const metadata: Metadata = {
  title: { default: "57 Brasil | Autopeças", template: "%s | 57 Brasil" },
  description: "Encontre autopeças e acessórios no catálogo da 57 Brasil. Consulte a aplicação antes de comprar.",
  applicationName: "57 Brasil Autopeças",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="pt-BR"><body><a className="skip-link" href="#main-content">Pular para o conteúdo</a><SiteHeader/>{children}<SiteFooter/></body></html>;
}
