namespace Brasil57.Domain;

public sealed class PaymentNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string RequestId { get; set; }
    public required string PaymentId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeaseUntil { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Outcome { get; set; }
}
