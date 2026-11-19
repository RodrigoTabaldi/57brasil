using Brasil57.Application;

namespace Brasil57.Infrastructure;

// Deliberately unavailable until the selected provider's official API and account are verified.
public sealed class StripePaymentGateway : UnconfiguredPaymentGateway
{
    public override string Provider => "stripe";
}

public sealed class AbacatePayPaymentGateway : UnconfiguredPaymentGateway
{
    public override string Provider => "abacatepay";
}

public abstract class UnconfiguredPaymentGateway : IPaymentGateway
{
    public abstract string Provider { get; }
    public IReadOnlyCollection<PaymentMethod> AvailableMethods => Array.Empty<PaymentMethod>();
    public Task<ChargeResult> CreateChargeAsync(CreateChargeRequest request, CancellationToken cancellationToken) => throw Unavailable();
    public Task<ChargeResult> GetChargeAsync(string providerPaymentId, CancellationToken cancellationToken) => throw Unavailable();
    public Task CancelAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken) => throw Unavailable();
    public Task RefundAsync(string providerPaymentId, decimal amount, string idempotencyKey, CancellationToken cancellationToken) => throw Unavailable();
    public bool VerifyWebhook(ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string> headers) => false;
    public GatewayWebhookEvent ParseWebhook(ReadOnlyMemory<byte> payload) => throw Unavailable();
    private static NotSupportedException Unavailable() => new("Gateway de pagamento ainda não configurado.");
}
