namespace Brasil57.Domain;

public enum OrderStatus { AwaitingPayment, Paid, Cancelled }

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestHash { get; set; }
    public required string BuyerEmail { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.AwaitingPayment;
    public decimal ItemsTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public string? ProviderPaymentId { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
