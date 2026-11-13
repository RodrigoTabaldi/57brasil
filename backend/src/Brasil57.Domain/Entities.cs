namespace Brasil57.Domain;
public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public bool Active { get; set; } = true;
}
public sealed class Brand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
}
public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Sku { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? LengthCm { get; set; }
    public bool Published { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InventoryItem Inventory { get; set; } = null!;
}
public sealed class InventoryItem
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int OnHand { get; set; }
    public int Reserved { get; set; }

    public int Available => OnHand - Reserved;
}
public sealed class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
    public List<CartItem> Items { get; set; } = [];
}
public sealed class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}


