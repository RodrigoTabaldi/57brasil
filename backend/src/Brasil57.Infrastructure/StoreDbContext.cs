using Brasil57.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Brasil57.Infrastructure;
public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : IdentityDbContext<StoreUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<PartType> PartTypes => Set<PartType>();
    public DbSet<PartTypePurchaseRequest> PartTypePurchaseRequests => Set<PartTypePurchaseRequest>();
    public DbSet<ProductPurchaseRequest> ProductPurchaseRequests => Set<ProductPurchaseRequest>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartOperation> CartOperations => Set<CartOperation>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleVersion> VehicleVersions => Set<VehicleVersion>();
    public DbSet<ProductApplication> ProductApplications => Set<ProductApplication>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DailyVisit> DailyVisits => Set<DailyVisit>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentNotification> PaymentNotifications => Set<PaymentNotification>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<ProductPurchaseRequest>(e => {
            e.HasKey(x => x.Id); e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne<StoreUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Cep).HasMaxLength(8); e.Property(x => x.Street).HasMaxLength(200);
            e.Property(x => x.Number).HasMaxLength(30); e.Property(x => x.Complement).HasMaxLength(100);
            e.Property(x => x.Neighborhood).HasMaxLength(100); e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(2); e.Property(x => x.Status).HasMaxLength(40);
            e.ToTable(t => t.HasCheckConstraint("ck_product_purchase_request_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 99"));
        });
        model.Entity<PartType>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); e.HasIndex(x => x.GroupSlug); e.Property(x => x.Name).HasMaxLength(160); e.Property(x => x.Slug).HasMaxLength(180); e.Property(x => x.Group).HasMaxLength(100); e.Property(x => x.GroupSlug).HasMaxLength(120); e.Property(x => x.Status).HasMaxLength(80); e.Property(x => x.ImageUrl).HasMaxLength(300); });
        model.Entity<PartTypePurchaseRequest>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.CreatedAt); e.HasIndex(x => new { x.UserId, x.PartTypeId, x.CreatedAt }); e.Property(x => x.Status).HasMaxLength(40); e.HasOne<StoreUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.PartType).WithMany().HasForeignKey(x => x.PartTypeId).OnDelete(DeleteBehavior.Restrict); e.ToTable(t => t.HasCheckConstraint("ck_part_type_purchase_request_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 99")); });
        model.Entity<AuditLog>(e => { e.HasKey(x => x.Id); e.Property(x => x.Action).HasMaxLength(80); e.Property(x => x.Entity).HasMaxLength(80); e.Property(x => x.EvidenceSnapshot).HasColumnType("jsonb"); e.HasIndex(x => x.CreatedAt); });
        model.Entity<DailyVisit>(e => { e.HasKey(x => x.Day); e.ToTable(t => t.HasCheckConstraint("ck_daily_visit_count", "\"Count\" >= 0")); });
        model.Entity<Category>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Name).HasMaxLength(120); e.Property(x => x.Slug).HasMaxLength(140); e.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<Brand>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Name).HasMaxLength(120); e.Property(x => x.Slug).HasMaxLength(140); });
        model.Entity<Product>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Sku).IsUnique(); e.HasIndex(x => x.Slug).IsUnique(); e.HasIndex(x => x.Name); e.Property(x => x.Price).HasPrecision(12,2); e.Property(x => x.PriceSourceReference).HasMaxLength(200); e.Property(x => x.StockSourceReference).HasMaxLength(200); e.Property(x => x.CompatibilitySourceReference).HasMaxLength(200); e.Property(x => x.WeightKg).HasPrecision(9,3); e.Property(x => x.HeightCm).HasPrecision(9,2); e.Property(x => x.WidthCm).HasPrecision(9,2); e.Property(x => x.LengthCm).HasPrecision(9,2); e.ToTable(t => t.HasCheckConstraint("ck_product_price", "\"Price\" >= 0")); e.Property(x => x.Sku).HasMaxLength(80); e.Property(x => x.Slug).HasMaxLength(180); e.Property(x => x.Name).HasMaxLength(240); e.Property(x => x.ManufacturerCode).HasMaxLength(100); e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<ProductImage>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProductId, x.SortOrder }); e.Property(x => x.Url).HasMaxLength(500); e.Property(x => x.AltText).HasMaxLength(240); e.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade); });
        model.Entity<InventoryItem>(e => { e.HasKey(x => x.ProductId); e.HasOne(x => x.Product).WithOne(x => x.Inventory).HasForeignKey<InventoryItem>(x => x.ProductId); e.ToTable(t => { t.HasCheckConstraint("ck_stock_nonnegative", "\"OnHand\" >= 0 AND \"Reserved\" >= 0 AND \"Reserved\" <= \"OnHand\""); }); });
        model.Entity<VehicleMake>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100); });
        model.Entity<VehicleModel>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(120); e.HasIndex(x => new { x.MakeId, x.Name }).IsUnique(); });
        model.Entity<VehicleVersion>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(160); e.ToTable(t => t.HasCheckConstraint("ck_version_years", "\"StartYear\" <= \"EndYear\"")); });
        model.Entity<ProductApplication>(e => { e.HasKey(x => new { x.ProductId, x.VehicleVersionId, x.StartYear, x.EndYear }); e.Property(x => x.Restriction).HasMaxLength(500); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId); e.HasOne(x => x.VehicleVersion).WithMany().HasForeignKey(x => x.VehicleVersionId); e.ToTable(t => t.HasCheckConstraint("ck_application_years", "\"StartYear\" <= \"EndYear\"")); });
        model.Entity<StoreUser>(e => { e.Property(x => x.FullName).HasMaxLength(160); e.Property(x => x.Cpf).HasMaxLength(11); e.HasIndex(x => x.Cpf).IsUnique(); e.HasIndex(x => x.NormalizedEmail).IsUnique(); });
        model.Entity<Cart>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); e.HasOne<StoreUser>().WithOne().HasForeignKey<Cart>(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        model.Entity<CartItem>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique(); e.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); e.ToTable(t => t.HasCheckConstraint("ck_cart_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 99")); });
        model.Entity<CartOperation>(e => { e.HasKey(x => x.Id); e.Property(x => x.Key).HasMaxLength(100); e.HasIndex(x => new { x.UserId, x.Key }).IsUnique(); e.HasOne<StoreUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => x.CreatedAt); });
        model.Entity<Order>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.CartId).IsUnique(); e.HasIndex(x => x.IdempotencyKey).IsUnique(); e.HasIndex(x => x.ProviderPaymentId).IsUnique(); e.Property(x => x.IdempotencyKey).HasMaxLength(100); e.Property(x => x.RequestHash).HasMaxLength(64); e.Property(x => x.BuyerEmail).HasMaxLength(254); e.Property(x => x.ProviderPaymentId).HasMaxLength(80); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); e.Property(x => x.ItemsTotal).HasPrecision(12, 2); e.Property(x => x.ShippingTotal).HasPrecision(12, 2); e.ToTable(t => { t.HasCheckConstraint("ck_order_totals", "\"ItemsTotal\" >= 0 AND \"ShippingTotal\" >= 0"); }); });
        model.Entity<OrderItem>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique(); e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); e.Property(x => x.Sku).HasMaxLength(80); e.Property(x => x.Name).HasMaxLength(240); e.Property(x => x.UnitPrice).HasPrecision(12, 2); e.ToTable(t => t.HasCheckConstraint("ck_order_item", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0")); });
        model.Entity<PaymentNotification>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.RequestId).IsUnique(); e.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt }); e.Property(x => x.RequestId).HasMaxLength(100); e.Property(x => x.PaymentId).HasMaxLength(80); e.Property(x => x.Outcome).HasMaxLength(40); });
        model.Entity<Payment>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => new { x.Provider, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.Provider, x.ProviderPaymentId }).IsUnique();
            e.Property(x => x.Provider).HasMaxLength(40);
            e.Property(x => x.ProviderPaymentId).HasMaxLength(160);
            e.Property(x => x.PaymentMethod).HasMaxLength(32);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.ToTable(t => t.HasCheckConstraint("ck_payment_amount", "\"Amount\" > 0"));
        });
        model.Entity<PaymentEvent>(e => {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
            e.Property(x => x.Provider).HasMaxLength(40);
            e.Property(x => x.ProviderEventId).HasMaxLength(160);
            e.Property(x => x.ProviderPaymentId).HasMaxLength(160);
        });
    }
}








