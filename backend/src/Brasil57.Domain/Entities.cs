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
    public string? PriceSourceReference { get; set; }
    public string? StockSourceReference { get; set; }
    public string? CompatibilitySourceReference { get; set; }
    public DateTimeOffset? SupplierConfirmedAt { get; set; }
    public string? ApplicationNotes { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? LengthCm { get; set; }
    public bool Published { get; set; }
    public bool CatalogVisible { get; set; }
    public string[] VehicleFamilies { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InventoryItem Inventory { get; set; } = null!;
    public List<ProductImage> Images { get; set; } = [];
}
public sealed class ProductImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Url { get; set; }
    public required string AltText { get; set; }
    public int SortOrder { get; set; }
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
    public Guid? UserId { get; set; }
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
public sealed class CartOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Key { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class DailyVisit
{
    public DateOnly Day { get; set; }
    public long Count { get; set; }
}


