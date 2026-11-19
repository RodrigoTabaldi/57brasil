import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = { title: "Privacidade e dados pessoais" };

export default function PrivacyPage() {
  return <main id="main-content" className="container empty-page privacy-page">
    <h1>Privacidade e dados pessoais</h1>
    <p>Este aviso descreve os dados usados nas funções disponíveis da 57 Brasil.</p>
    <section><h2>Dados e finalidades</h2><p>Ao criar uma conta, você informa nome, e-mail, celular e senha. O CPF é opcional. Esses dados são usados para identificar você, permitir o acesso e manter seu cadastro e carrinho. A senha é armazenada pelo sistema de autenticação em formato protegido. Ao solicitar recuperação de senha, usamos seu e-mail para enviar as instruções. Ao calcular o frete, usamos o CEP informado para consultar as opções de entrega.</p></section>
    <section><h2>Cookies e armazenamento</h2><p>O acesso à conta usa um cookie de sessão necessário para a autenticação, com duração configurada de até 8 horas e renovação durante o uso. O site não carrega cookies de publicidade ou análise no frontend. A seleção de veículo fica na URL da busca e pode constar no histórico do navegador.</p></section>
    <section><h2>Seus direitos</h2><p>Você pode consultar e corrigir os dados da conta em <Link href="/conta">Minha conta</Link>. A LGPD também prevê, conforme o caso, confirmação do tratamento, acesso, correção, eliminação, portabilidade e informações sobre compartilhamento. Solicitações que não estejam disponíveis na conta dependem de um canal de atendimento a ser divulgado pela empresa.</p></section>
    <section><h2>Prazo e destinatários</h2><p>O cadastro permanece no sistema enquanto a conta existir ou houver necessidade legal de conservação. A operação técnica do site e o envio de e-mails de recuperação podem envolver prestadores de hospedagem e e-mail. A identidade desses prestadores, os prazos detalhados de retenção e o canal de atendimento serão informados após validação operacional.</p></section>
  </main>;
}
