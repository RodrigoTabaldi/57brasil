namespace Brasil57.Application;
public sealed record PaymentItem(string Sku, string Name, int Quantity, decimal UnitPrice);
public sealed record PaymentPreference(string ProviderId, Uri CheckoutUrl);
public sealed record PaymentResult(string ProviderPaymentId, string ExternalReference, decimal Amount, string Status);
public interface IPaymentProvider
{
    Task<PaymentPreference> CreatePreferenceAsync(Guid orderId, string buyerEmail, IReadOnlyList<PaymentItem> items, decimal shippingPrice, CancellationToken cancellationToken);
    Task<PaymentResult> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);
    bool VerifyWebhook(string signature, string requestId, string paymentId);
}
