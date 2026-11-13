using Brasil57.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Brasil57.Infrastructure;
public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : IdentityDbContext<StoreUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleVersion> VehicleVersions => Set<VehicleVersion>();
    public DbSet<ProductApplication> ProductApplications => Set<ProductApplication>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<AuditLog>(e => { e.HasKey(x => x.Id); e.Property(x => x.Action).HasMaxLength(80); e.Property(x => x.Entity).HasMaxLength(80); e.HasIndex(x => x.CreatedAt); });
        model.Entity<Category>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Name).HasMaxLength(120); e.Property(x => x.Slug).HasMaxLength(140); e.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<Brand>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Name).HasMaxLength(120); e.Property(x => x.Slug).HasMaxLength(140); });
        model.Entity<Product>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Sku).IsUnique(); e.HasIndex(x => x.Slug).IsUnique(); e.HasIndex(x => x.Name); e.Property(x => x.Price).HasPrecision(12,2); e.Property(x => x.WeightKg).HasPrecision(9,3); e.Property(x => x.HeightCm).HasPrecision(9,2); e.Property(x => x.WidthCm).HasPrecision(9,2); e.Property(x => x.LengthCm).HasPrecision(9,2); e.ToTable(t => t.HasCheckConstraint("ck_product_price", "\"Price\" >= 0")); e.Property(x => x.Sku).HasMaxLength(80); e.Property(x => x.Slug).HasMaxLength(180); e.Property(x => x.Name).HasMaxLength(240); e.Property(x => x.ManufacturerCode).HasMaxLength(100); e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<InventoryItem>(e => { e.HasKey(x => x.ProductId); e.HasOne(x => x.Product).WithOne(x => x.Inventory).HasForeignKey<InventoryItem>(x => x.ProductId); e.ToTable(t => { t.HasCheckConstraint("ck_stock_nonnegative", "\"OnHand\" >= 0 AND \"Reserved\" >= 0 AND \"Reserved\" <= \"OnHand\""); }); });
        model.Entity<VehicleMake>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100); });
        model.Entity<VehicleModel>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(120); e.HasIndex(x => new { x.MakeId, x.Name }).IsUnique(); });
        model.Entity<VehicleVersion>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(160); e.ToTable(t => t.HasCheckConstraint("ck_version_years", "\"StartYear\" <= \"EndYear\"")); });
        model.Entity<ProductApplication>(e => { e.HasKey(x => new { x.ProductId, x.VehicleVersionId, x.StartYear, x.EndYear }); e.Property(x => x.Restriction).HasMaxLength(500); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId); e.HasOne(x => x.VehicleVersion).WithMany().HasForeignKey(x => x.VehicleVersionId); e.ToTable(t => t.HasCheckConstraint("ck_application_years", "\"StartYear\" <= \"EndYear\"")); });
        model.Entity<Cart>(e => e.HasKey(x => x.Id));
        model.Entity<CartItem>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique(); e.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); e.ToTable(t => t.HasCheckConstraint("ck_cart_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 99")); });
    }
}








