import { PaymentStage } from "@/components/ui/PaymentStage";

export default async function PaymentPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PaymentStage orderId={id} />;
}
