namespace Brasil57.Domain;

public enum SupplierRequestStatus { AwaitingSupplier, Quoted, Declined }

public sealed class SupplierRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestHash { get; set; }
    public SupplierRequestStatus Status { get; set; } = SupplierRequestStatus.AwaitingSupplier;
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string Contact { get; set; }
    public required string PostalCode { get; set; }
    public required string Street { get; set; }
    public required string Number { get; set; }
    public string? Complement { get; set; }
    public required string District { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string Vehicle { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<SupplierRequestItem> Items { get; set; } = [];
}

public sealed class SupplierRequestItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierRequestId { get; set; }
    public SupplierRequest SupplierRequest { get; set; } = null!;
    public Guid ProductId { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public decimal? ListedUnitPrice { get; set; }
    public string? ListedPriceSource { get; set; }
    public string? ExactPart { get; set; }
    public bool? Available { get; set; }
    public decimal? UnitPrice { get; set; }
    public int? LeadDays { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? LengthCm { get; set; }
    public string? Source { get; set; }
}
