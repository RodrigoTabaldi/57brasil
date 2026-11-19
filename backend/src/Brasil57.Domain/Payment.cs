namespace Brasil57.Domain;

public enum PaymentStatus { Pending, Paid, Failed, Cancelled, Refunded }

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public required string Provider { get; set; }
    public string? ProviderPaymentId { get; set; }
    public required string PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public required string IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class PaymentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Provider { get; set; }
    public required string ProviderEventId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}
