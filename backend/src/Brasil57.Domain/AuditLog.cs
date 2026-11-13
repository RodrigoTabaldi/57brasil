namespace Brasil57.Domain;
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    public required string Action { get; set; }
    public required string Entity { get; set; }
    public Guid EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
