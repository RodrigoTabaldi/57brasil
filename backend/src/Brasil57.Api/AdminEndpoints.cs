using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace Brasil57.Api;
public static partial class AdminEndpoints
{
    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin");
        admin.MapPost("/session", async (HttpContext context, AdminLogin input, SignInManager<StoreUser> signIn, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Results.BadRequest();
            var result = await signIn.PasswordSignInAsync(input.Email, input.Password, false, true);
            return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
        });
        var protectedAdmin = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole("Administrator"));
        protectedAdmin.MapDelete("/session", async (HttpContext context, SignInManager<StoreUser> signIn) => { if (!SameOrigin(context)) return Results.StatusCode(403); await signIn.SignOutAsync(); return Results.NoContent(); });
        protectedAdmin.MapGet("/dashboard", async (StoreDbContext db, CancellationToken ct) => Results.Ok(new { products = await db.Products.CountAsync(ct), publishedProducts = await db.Products.CountAsync(x => x.Published, ct), lowStock = await db.Inventory.CountAsync(x => x.OnHand - x.Reserved < 5, ct) }));
        protectedAdmin.MapGet("/products", async (StoreDbContext db, CancellationToken ct) => await db.Products.AsNoTracking().OrderBy(x => x.Name).Take(100).Select(x => new { x.Id, x.Name, x.Sku, x.Slug, x.Price, x.Published }).ToListAsync(ct));
        protectedAdmin.MapPost("/categories", async (HttpContext context, AdminCategory input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 140)) return Results.BadRequest();
            var category = new Category { Name = input.Name.Trim(), Slug = input.Slug.Trim(), ParentId = input.ParentId };
            db.Categories.Add(category);
            Audit(db, context, "create", "category", category.Id);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "Categoria duplicada ou hierarquia inválida." }); }
            return Results.Created($"/api/v1/categories", new { category.Id });
        });
        protectedAdmin.MapPost("/brands", async (HttpContext context, AdminBrand input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 140)) return Results.BadRequest();
            var brand = new Brand { Name = input.Name.Trim(), Slug = input.Slug.Trim() };
            db.Brands.Add(brand);
            Audit(db, context, "create", "brand", brand.Id);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "Marca duplicada." }); }
            return Results.Created("/api/v1/brands", new { brand.Id });
        });
        protectedAdmin.MapPost("/products", async (HttpContext context, AdminProduct input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 180) || string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Trim().Length > 80 || input.Price is < 0 or > 9999999999.99m || input.Stock < 0 || input.WeightKg is < 0 or > 999999 || input.HeightCm is < 0 or > 999999 || input.WidthCm is < 0 or > 999999 || input.LengthCm is < 0 or > 999999 || (input.Published && (input.WeightKg is null or <= 0 || input.HeightCm is null or <= 0 || input.WidthCm is null or <= 0 || input.LengthCm is null or <= 0))) return Results.BadRequest();
            var product = new Product { Name = input.Name.Trim(), Slug = input.Slug.Trim(), Sku = input.Sku.Trim(), CategoryId = input.CategoryId, BrandId = input.BrandId, Price = input.Price, WeightKg = input.WeightKg, HeightCm = input.HeightCm, WidthCm = input.WidthCm, LengthCm = input.LengthCm, Published = input.Published, Inventory = new InventoryItem { OnHand = input.Stock } };
            db.Products.Add(product);
            Audit(db, context, "create", "product", product.Id);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "SKU ou slug duplicado, ou categoria e marca inválidas." }); }
            return Results.Created($"/api/v1/products/{product.Slug}", new { product.Id });
        });
        protectedAdmin.MapPatch("/products/{productId:guid}", async (Guid productId, HttpContext context, UpdateProduct input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (input.Name is not null && !ValidName(input.Name)) return Results.BadRequest();
            if (input.Price is < 0 or > 9999999999.99m) return Results.BadRequest();
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);
            if (product is null) return Results.NotFound();
            if (input.Name is not null) product.Name = input.Name.Trim();
            if (input.Price is not null) product.Price = input.Price.Value;
            if (input.Published is not null) product.Published = input.Published.Value;
            Audit(db, context, "update", "product", product.Id);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        protectedAdmin.MapPost("/inventory/{productId:guid}/adjustments", async (Guid productId, HttpContext context, StockAdjustment input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (input.Delta is 0 or < -100000 or > 100000) return Results.BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var changed = await db.Inventory.Where(x => x.ProductId == productId && x.OnHand + input.Delta >= x.Reserved && x.OnHand + input.Delta >= 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OnHand, x => x.OnHand + input.Delta), ct);
            if (changed == 0) return Results.Conflict(new { message = "Produto inexistente ou ajuste incompatível com o estoque reservado." });
            Audit(db, context, $"stock_adjust_{input.Delta:+#;-#;0}", "product", productId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        });    }
    private static void Audit(StoreDbContext db, HttpContext context, string action, string entity, Guid entityId) { var actorId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Identidade administrativa ausente.")); db.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = action, Entity = entity, EntityId = entityId }); }
    private static bool ValidName(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 120;
    private static bool ValidSlug(string? value, int maxLength) => value is not null && value.Length <= maxLength && SlugPattern().IsMatch(value);
    private static bool SameOrigin(HttpContext context) { var origin = context.Request.Headers.Origin.ToString(); return Uri.TryCreate(origin, UriKind.Absolute, out var uri) && string.Equals(uri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) && string.Equals(uri.Scheme, context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme, StringComparison.OrdinalIgnoreCase) && uri.Port == (context.Request.Host.Port ?? (uri.Scheme == "https" ? 443 : 80)); }
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex SlugPattern();
}
public sealed record AdminLogin(string Email, string Password);
public sealed record AdminCategory(string Name, string Slug, Guid? ParentId);
public sealed record AdminBrand(string Name, string Slug);
public sealed record AdminProduct(string Name, string Slug, string Sku, Guid CategoryId, Guid BrandId, decimal Price, int Stock, bool Published, decimal? WeightKg, decimal? HeightCm, decimal? WidthCm, decimal? LengthCm);





public sealed record UpdateProduct(string? Name, decimal? Price, bool? Published);
public sealed record StockAdjustment(int Delta);


