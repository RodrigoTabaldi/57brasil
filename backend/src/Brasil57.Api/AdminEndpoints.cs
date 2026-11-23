using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Text.Json;

namespace Brasil57.Api;
public static partial class AdminEndpoints
{
    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin");
        admin.MapPost("/session", async (HttpContext context, AdminLogin input, UserManager<StoreUser> users, SignInManager<StoreUser> signIn) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Results.BadRequest();
            var user = await users.FindByEmailAsync(input.Email.Trim());
            if (user is null || !await users.IsInRoleAsync(user, "Administrator")) return Results.Unauthorized();
            var result = await signIn.CheckPasswordSignInAsync(user, input.Password, true);
            if (!result.Succeeded) return Results.Unauthorized();
            if (!await users.GetTwoFactorEnabledAsync(user)) {
                if (await users.GetAuthenticatorKeyAsync(user) is null) await users.ResetAuthenticatorKeyAsync(user);
                await signIn.SignInWithClaimsAsync(user, false, [new Claim("admin_setup", "true")]); return Results.Ok(new { setupRequired = true });
            }
            if (string.IsNullOrWhiteSpace(input.Code)) return Results.Ok(new { codeRequired = true });
            var valid = await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, input.Code.Replace(" ", "").Replace("-", ""));
            if (!valid) valid = (await users.RedeemTwoFactorRecoveryCodeAsync(user, input.Code.Trim())).Succeeded;
            if (!valid) { await users.AccessFailedAsync(user); return Results.Unauthorized(); }
            await users.ResetAccessFailedCountAsync(user);
            await signIn.SignInWithClaimsAsync(user, false, [new Claim("admin_mfa", "true")]);
            return Results.NoContent();
        }).RequireRateLimiting("account");
        var setup = admin.MapGroup("/mfa").RequireAuthorization(policy => policy.RequireRole("Administrator").RequireClaim("admin_setup", "true"));
        setup.MapGet("/setup", async (HttpContext context, UserManager<StoreUser> users) => {
            var user = await users.GetUserAsync(context.User);
            if (user is null || await users.GetTwoFactorEnabledAsync(user)) return Results.Forbid();
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (key is null) return Results.Problem("Chave MFA indisponível.", statusCode: 503);
            return Results.Ok(new { key });
        });
        setup.MapPost("/setup", async (HttpContext context, AdminMfaCode input, UserManager<StoreUser> users, SignInManager<StoreUser> signIn) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            var user = await users.GetUserAsync(context.User);
            if (user is null || await users.GetTwoFactorEnabledAsync(user)) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(input.Code) || !await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, input.Code.Replace(" ", "").Replace("-", ""))) return Results.BadRequest();
            await users.SetTwoFactorEnabledAsync(user, true);
            var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
            await signIn.SignInWithClaimsAsync(user, false, [new Claim("admin_mfa", "true")]);
            return Results.Ok(new { recoveryCodes = codes });
        }).RequireRateLimiting("account");
        var protectedAdmin = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole("Administrator").RequireClaim("admin_mfa", "true"));
        protectedAdmin.MapGet("/session", () => Results.NoContent());
        protectedAdmin.MapDelete("/session", async (HttpContext context, SignInManager<StoreUser> signIn) => { if (!SameOrigin(context)) return Results.StatusCode(403); await signIn.SignOutAsync(); return Results.NoContent(); });
        protectedAdmin.MapGet("/dashboard", async (StoreDbContext db, CancellationToken ct) => Results.Ok(new { products = await db.Products.CountAsync(ct), publishedProducts = await db.Products.CountAsync(x => x.Published, ct), lowStock = await db.Inventory.CountAsync(x => x.OnHand - x.Reserved < 5, ct) }));
        protectedAdmin.MapGet("/overview", async (StoreDbContext db, CancellationToken ct) => Results.Ok(new {
            accounts = await db.Users.CountAsync(ct), orders = await db.Orders.CountAsync(ct),
            pendingOrders = await db.Orders.CountAsync(x => x.Status == OrderStatus.AwaitingPayment, ct),
            visitsToday = await db.DailyVisits.Where(x => x.Day == DateOnly.FromDateTime(DateTime.UtcNow)).SumAsync(x => x.Count, ct),
            visitsSevenDays = await db.DailyVisits.Where(x => x.Day >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6))).SumAsync(x => x.Count, ct)
        }));
        protectedAdmin.MapGet("/accounts", async (StoreDbContext db, string? q, int? page, CancellationToken ct) => {
            var currentPage = Math.Clamp(page ?? 1, 1, 100000);
            if (q?.Length > 120) return Results.BadRequest();
            var query = db.Users.AsNoTracking().Where(x => q == null || EF.Functions.ILike(x.Email!, $"%{q}%") || EF.Functions.ILike(x.FullName, $"%{q}%"));
            return Results.Ok(new { total = await query.CountAsync(ct), page = currentPage, pageSize = 50,
                items = await query.OrderBy(x => x.Email).ThenBy(x => x.Id).Skip((currentPage - 1) * 50).Take(50).Select(x => new { x.Id, x.FullName, x.Email, x.EmailConfirmed, x.LockoutEnd }).ToListAsync(ct) });
        });
        protectedAdmin.MapGet("/orders", async (StoreDbContext db, int? page, CancellationToken ct) => {
            var currentPage = Math.Clamp(page ?? 1, 1, 100000);
            return Results.Ok(new { total = await db.Orders.CountAsync(ct), page = currentPage, pageSize = 25,
                items = await db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((currentPage - 1) * 25).Take(25)
                    .Select(x => new { x.Id, x.Status, x.BuyerEmail, x.ShippingAddress, x.ItemsTotal, x.ShippingTotal, x.CreatedAt, x.PaidAt,
                        Items = x.Items.Select(i => new { i.Sku, i.Name, i.Quantity, i.UnitPrice }).ToList() }).ToListAsync(ct) });
        });
        protectedAdmin.MapGet("/part-type-purchase-requests", async (StoreDbContext db, int? page, CancellationToken ct) => {
            var currentPage = Math.Clamp(page ?? 1, 1, 100000);
            var query = from request in db.PartTypePurchaseRequests.AsNoTracking()
                        join user in db.Users.AsNoTracking() on request.UserId equals user.Id
                        select new { request.Id, request.PartType.Name, request.PartType.Group, request.Quantity, request.Status, request.CreatedAt, user.FullName, user.Email };
            return Results.Ok(new { total = await query.CountAsync(ct), page = currentPage, pageSize = 25,
                items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((currentPage - 1) * 25).Take(25).ToListAsync(ct) });
        });
        protectedAdmin.MapGet("/products", async (StoreDbContext db, string? q, int? page, CancellationToken ct) => {
            var currentPage = Math.Clamp(page ?? 1, 1, 100000);
            var query = db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); if (term.Length > 120) return Results.BadRequest(); query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Sku, $"%{term}%")); }
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((currentPage - 1) * 50).Take(50).Select(x => new { x.Id, x.Name, x.Sku, x.Slug, x.Price, x.Published, x.BrandId, x.ManufacturerCode, x.ShortDescription, x.Description, x.PriceSourceReference, x.StockSourceReference, x.CompatibilitySourceReference, x.VehicleFamilies, x.SupplierConfirmedAt, x.ApplicationNotes, x.WeightKg, x.HeightCm, x.WidthCm, x.LengthCm, ImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(), Available = x.Inventory.OnHand - x.Inventory.Reserved }).ToListAsync(ct);
            return Results.Ok(new { items, total, page = currentPage, pageSize = 50 });
        });
        protectedAdmin.MapGet("/categories", async (StoreDbContext db, string? q, CancellationToken ct) => await db.Categories.AsNoTracking().Where(x => q == null || EF.Functions.ILike(x.Name, $"%{q}%")).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug, x.Active }).ToListAsync(ct));
        protectedAdmin.MapPatch("/categories/{id:guid}", async (Guid id, HttpContext context, AdminCategoryUpdate input, StoreDbContext db, CatalogTaxonomyCache cache, CancellationToken ct) => {
            if (!SameOrigin(context) || (input.Name is not null && !ValidName(input.Name))) return Results.BadRequest();
            var category = await db.Categories.FindAsync([id], ct); if (category is null) return Results.NotFound();
            if (input.Name is not null) category.Name = input.Name.Trim();
            if (input.Active is not null) category.Active = input.Active.Value;
            Audit(db, context, "update", "category", id);
            await db.SaveChangesAsync(ct); await cache.InvalidateAsync(CatalogTaxonomyCache.CategoriesKey, ct); return Results.NoContent();
        });
        protectedAdmin.MapPost("/categories", async (HttpContext context, AdminCategory input, StoreDbContext db, CatalogTaxonomyCache cache, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 140)) return Results.BadRequest();
            var category = new Category { Name = input.Name.Trim(), Slug = input.Slug.Trim(), ParentId = input.ParentId };
            db.Categories.Add(category);
            Audit(db, context, "create", "category", category.Id);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "Categoria duplicada ou hierarquia inválida." }); }
            await cache.InvalidateAsync(CatalogTaxonomyCache.CategoriesKey, ct);
            return Results.Created($"/api/v1/categories", new { category.Id });
        });
        protectedAdmin.MapPost("/brands", async (HttpContext context, AdminBrand input, StoreDbContext db, CatalogTaxonomyCache cache, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 140)) return Results.BadRequest();
            var brand = new Brand { Name = input.Name.Trim(), Slug = input.Slug.Trim() };
            db.Brands.Add(brand);
            Audit(db, context, "create", "brand", brand.Id);
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "Marca duplicada." }); }
            await cache.InvalidateAsync(CatalogTaxonomyCache.BrandsKey, ct);
            return Results.Created("/api/v1/brands", new { brand.Id });
        });
        protectedAdmin.MapPost("/products", async (HttpContext context, AdminProduct input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!ValidName(input.Name) || !ValidSlug(input.Slug, 180) || string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Trim().Length > 80 || input.Price is < 0 or > 9999999999.99m || input.Stock < 0 || input.WeightKg is < 0 or > 999999 || input.HeightCm is < 0 or > 999999 || input.WidthCm is < 0 or > 999999 || input.LengthCm is < 0 or > 999999) return Results.BadRequest();
            if (input.VehicleFamilies?.Any(x => x is not ("carro" or "moto" or "caminhao")) == true) return Results.BadRequest();
            var product = new Product { Name = input.Name.Trim(), Slug = input.Slug.Trim(), Sku = input.Sku.Trim(), CategoryId = input.CategoryId, BrandId = input.BrandId, Price = input.Price, PriceSourceReference = ReferenceOrNull(input.PriceSourceReference), StockSourceReference = ReferenceOrNull(input.StockSourceReference), CompatibilitySourceReference = ReferenceOrNull(input.CompatibilitySourceReference), WeightKg = input.WeightKg, HeightCm = input.HeightCm, WidthCm = input.WidthCm, LengthCm = input.LengthCm, Published = input.Published, CatalogVisible = true, VehicleFamilies = input.VehicleFamilies?.Distinct().ToArray() ?? [], Inventory = new InventoryItem { OnHand = input.Stock } };
            product.SupplierConfirmedAt = input.SupplierConfirmedAt;
            product.ApplicationNotes = input.ApplicationNotes?.Trim();
            if (product.ApplicationNotes?.Length > 500 || product.SupplierConfirmedAt > DateTimeOffset.UtcNow) return Results.BadRequest();
            if (!ValidOptionalReference(product.PriceSourceReference) || !ValidOptionalReference(product.StockSourceReference) || !ValidOptionalReference(product.CompatibilitySourceReference)) return Results.BadRequest();
            if (product.Published)
            {
                var brandSlug = await db.Brands.Where(x => x.Id == product.BrandId).Select(x => x.Slug).FirstOrDefaultAsync(ct);
                if (brandSlug is null || !ProductPublicationPolicy.CanPublish(product, brandSlug))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = ["Cadastre preço, marca real, peso, dimensões e referências dos documentos antes de publicar a venda."] });
            }
            db.Products.Add(product);
            Audit(db, context, "create", "product", product.Id, ProductEvidenceSnapshot(product));
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "SKU ou slug duplicado, ou categoria e marca inválidas." }); }
            return Results.Created($"/api/v1/products/{product.Slug}", new { product.Id });
        });
        protectedAdmin.MapPost("/products/{productId:guid}/images", async (Guid productId, HttpContext context, AdminProductImage input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || input.Url.Length > 500 || !ValidName(input.AltText) || input.SortOrder is < 0 or > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = ["Informe URL HTTPS, texto alternativo e ordem válidos."] });
            if (!await db.Products.AnyAsync(x => x.Id == productId, ct)) return Results.NotFound();
            var image = new ProductImage { ProductId = productId, Url = input.Url, AltText = input.AltText.Trim(), SortOrder = input.SortOrder };
            db.ProductImages.Add(image);
            Audit(db, context, "add_image", "product", productId);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/products/{productId}/images/{image.Id}", new { image.Id });
        });
        protectedAdmin.MapPatch("/products/{productId:guid}", async (Guid productId, HttpContext context, UpdateProduct input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (input.Name is not null && !ValidName(input.Name)) return Results.BadRequest();
            if (input.Price is < 0 or > 9999999999.99m) return Results.BadRequest();
            if (input.Sku is not null && (string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Length > 80)) return Results.BadRequest();
            if (input.WeightKg is < 0 or > 999999 || input.HeightCm is < 0 or > 999999 || input.WidthCm is < 0 or > 999999 || input.LengthCm is < 0 or > 999999) return Results.BadRequest();
            var product = await db.Products.Include(x => x.Brand).FirstOrDefaultAsync(x => x.Id == productId, ct);
            if (product is null) return Results.NotFound();
            var previousPrice = product.Price;
            var previouslyPublished = product.Published;
            if (input.Name is not null) product.Name = input.Name.Trim();
            if (input.Sku is not null) product.Sku = input.Sku.Trim();
            if (input.ManufacturerCode is not null) product.ManufacturerCode = input.ManufacturerCode.Trim();
            if (input.ShortDescription is not null) product.ShortDescription = input.ShortDescription.Trim();
            if (input.Description is not null) product.Description = input.Description.Trim();
            if (input.BrandId is not null) {
                var brand = await db.Brands.FirstOrDefaultAsync(x => x.Id == input.BrandId, ct);
                if (brand is null) return Results.BadRequest(new { message = "Marca inválida." });
                product.Brand = brand; product.BrandId = brand.Id;
            }
            if (input.Price is not null && input.Price != product.Price && input.PriceSourceReference is null) product.PriceSourceReference = null;
            if (input.Price is not null) product.Price = input.Price.Value;
            if (input.PriceSourceReference is not null) product.PriceSourceReference = ReferenceOrNull(input.PriceSourceReference);
            if (input.StockSourceReference is not null) product.StockSourceReference = ReferenceOrNull(input.StockSourceReference);
            if (input.CompatibilitySourceReference is not null) product.CompatibilitySourceReference = ReferenceOrNull(input.CompatibilitySourceReference);
            if (input.VehicleFamilies is not null) {
                if (input.VehicleFamilies.Any(x => x is not ("carro" or "moto" or "caminhao"))) return Results.BadRequest();
                if (input.VehicleFamilies.Length > 0 && !ProductPublicationPolicy.ValidReference(product.CompatibilitySourceReference)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["vehicleFamilies"] = ["Informe o documento que confirma as aplicações veiculares."] });
                product.VehicleFamilies = input.VehicleFamilies.Distinct().ToArray();
            }
            if (!ValidOptionalReference(product.PriceSourceReference) || !ValidOptionalReference(product.StockSourceReference) || !ValidOptionalReference(product.CompatibilitySourceReference)) return Results.BadRequest();
            if (input.ApplicationNotes is not null) product.ApplicationNotes = input.ApplicationNotes.Trim();
            if (input.SupplierConfirmedAt is not null) product.SupplierConfirmedAt = input.SupplierConfirmedAt;
            if (product.ApplicationNotes?.Length > 500 || product.SupplierConfirmedAt > DateTimeOffset.UtcNow) return Results.BadRequest();
            if (input.WeightKg is not null) product.WeightKg = input.WeightKg;
            if (input.HeightCm is not null) product.HeightCm = input.HeightCm;
            if (input.WidthCm is not null) product.WidthCm = input.WidthCm;
            if (input.LengthCm is not null) product.LengthCm = input.LengthCm;
            var hasApplications = (input.Published ?? product.Published) && await db.ProductApplications.AnyAsync(x => x.ProductId == product.Id, ct);
            if ((input.Published ?? product.Published) && !ProductPublicationPolicy.CanPublish(product, product.Brand.Slug, hasApplications))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = ["Cadastre preço, marca real, peso, dimensões e referências dos documentos antes de publicar a venda."] });
            if (input.Published is not null) product.Published = input.Published.Value;
            Audit(db, context, "update", "product", product.Id, ProductEvidenceSnapshot(product));
            if (previousPrice != product.Price) Audit(db, context, "update_price", "product", product.Id, JsonSerializer.Serialize(new { Before = previousPrice, After = product.Price, product.PriceSourceReference }));
            if (previouslyPublished != product.Published) Audit(db, context, "update_status", "product", product.Id, JsonSerializer.Serialize(new { Before = previouslyPublished, After = product.Published }));
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Results.Conflict(new { message = "SKU duplicado ou dados inválidos." }); }
            return Results.NoContent();
        });
        protectedAdmin.MapPost("/inventory/{productId:guid}/adjustments", async (Guid productId, HttpContext context, StockAdjustment input, StoreDbContext db, CancellationToken ct) => {
            if (!SameOrigin(context)) return Results.StatusCode(403);
            if (input.Delta is 0 or < -100000 or > 100000 || !ProductPublicationPolicy.ValidReference(input.SourceReference)) return Results.BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var changed = await db.Inventory.Where(x => x.ProductId == productId && x.OnHand + input.Delta >= x.Reserved && x.OnHand + input.Delta >= 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OnHand, x => x.OnHand + input.Delta), ct);
            if (changed == 0) return Results.Conflict(new { message = "Produto inexistente ou ajuste incompatível com o estoque reservado." });
            await db.Products.Where(x => x.Id == productId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StockSourceReference, input.SourceReference.Trim()), ct);
            Audit(db, context, $"stock_adjust_{input.Delta:+#;-#;0}", "product", productId,
                JsonSerializer.Serialize(new { input.Delta, SourceReference = input.SourceReference.Trim() }));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        });    }
    private static void Audit(StoreDbContext db, HttpContext context, string action, string entity, Guid entityId, string? evidenceSnapshot = null) { var actorId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Identidade administrativa ausente.")); db.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = action, Entity = entity, EntityId = entityId, EvidenceSnapshot = evidenceSnapshot }); }
    private static string ProductEvidenceSnapshot(Product product) => JsonSerializer.Serialize(new { product.Price, product.PriceSourceReference, product.StockSourceReference, product.CompatibilitySourceReference, product.SupplierConfirmedAt, product.ApplicationNotes, product.VehicleFamilies, product.Published });
    private static string? ReferenceOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidName(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 120;
    private static bool ValidOptionalReference(string? value) => value is null || ProductPublicationPolicy.ValidReference(value);
    private static bool ValidSlug(string? value, int maxLength) => value is not null && value.Length <= maxLength && SlugPattern().IsMatch(value);
    private static bool SameOrigin(HttpContext context) { var origin = context.Request.Headers.Origin.ToString(); return Uri.TryCreate(origin, UriKind.Absolute, out var uri) && string.Equals(uri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) && string.Equals(uri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase) && uri.Port == (context.Request.Host.Port ?? (uri.Scheme == "https" ? 443 : 80)); }
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex SlugPattern();
}
public sealed record AdminLogin(string Email, string Password, string? Code = null);
public sealed record AdminMfaCode(string Code);
public sealed record AdminCategoryUpdate(string? Name, bool? Active);
public sealed record AdminCategory(string Name, string Slug, Guid? ParentId);
public sealed record AdminBrand(string Name, string Slug);
public sealed record AdminProduct(string Name, string Slug, string Sku, Guid CategoryId, Guid BrandId, decimal Price, int Stock, bool Published, decimal? WeightKg, decimal? HeightCm, decimal? WidthCm, decimal? LengthCm, string[]? VehicleFamilies = null, string? PriceSourceReference = null, string? StockSourceReference = null, string? CompatibilitySourceReference = null, DateTimeOffset? SupplierConfirmedAt = null, string? ApplicationNotes = null);
public sealed record AdminProductImage(string Url, string AltText, int SortOrder);
public sealed record PartTypePurchaseRequestInput(int Quantity = 1);





public sealed record UpdateProduct(string? Name, decimal? Price, bool? Published, string? Sku = null, Guid? BrandId = null, string? ManufacturerCode = null, string? ShortDescription = null, string? Description = null, decimal? WeightKg = null, decimal? HeightCm = null, decimal? WidthCm = null, decimal? LengthCm = null, string? PriceSourceReference = null, string? StockSourceReference = null, string? CompatibilitySourceReference = null, DateTimeOffset? SupplierConfirmedAt = null, string? ApplicationNotes = null, string[]? VehicleFamilies = null);
public sealed record StockAdjustment(int Delta, string SourceReference);


