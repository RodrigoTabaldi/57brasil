namespace Brasil57.Application;

public enum PaymentMethod { Pix, CreditCard, Boleto }
public enum GatewayPaymentStatus { Pending, Paid, Failed, Cancelled, Refunded }
public sealed record CreateChargeRequest(Guid OrderId, decimal Amount, string Currency, PaymentMethod Method, string IdempotencyKey);
public sealed record ChargeResult(string ProviderPaymentId, GatewayPaymentStatus Status, Uri? PaymentUrl, DateTimeOffset? ExpiresAt,
    decimal? Amount = null, string? PixCode = null, string? PixQrCode = null);
public sealed record GatewayWebhookEvent(string EventId, string ProviderPaymentId, GatewayPaymentStatus Status);

// Each adapter must verify its provider's current API and account capabilities before activation.
public interface IPaymentGateway
{
    string Provider { get; }
    IReadOnlyCollection<PaymentMethod> AvailableMethods { get; }
    Task<ChargeResult> CreateChargeAsync(CreateChargeRequest request, CancellationToken cancellationToken);
    Task<ChargeResult> GetChargeAsync(string providerPaymentId, CancellationToken cancellationToken);
    Task CancelAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken);
    Task RefundAsync(string providerPaymentId, decimal amount, string idempotencyKey, CancellationToken cancellationToken);
    bool VerifyWebhook(ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string> headers);
    GatewayWebhookEvent ParseWebhook(ReadOnlyMemory<byte> payload);
}
