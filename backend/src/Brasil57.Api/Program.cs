using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Brasil57.Domain;
using Brasil57.Application;
using Brasil57.Api;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var keyPath = builder.Configuration["DataProtection:KeyPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".keys");
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<StoreDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentityCore<StoreUser>(options => { options.Password.RequiredLength = 12; options.Lockout.MaxFailedAccessAttempts = 5; options.User.RequireUniqueEmail = true; }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<StoreDbContext>().AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options => { options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict; options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = true; options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; }; options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }; });
builder.Services.AddAuthorization();
var shippingToken = builder.Configuration["Shipping:Token"];
var shippingOriginCep = builder.Configuration["Shipping:OriginCep"];
var shippingUserAgent = builder.Configuration["Shipping:UserAgent"];
var shippingEnvironment = builder.Configuration["Shipping:Environment"];
if (!string.IsNullOrWhiteSpace(shippingToken) && shippingOriginCep is { Length: 8 } && shippingOriginCep.All(char.IsDigit) && !string.IsNullOrWhiteSpace(shippingUserAgent) && shippingEnvironment is "sandbox" or "production")
{
    builder.Services.AddHttpClient("shipping", client => client.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddTransient<IShippingProvider>(sp => new MelhorEnvioShippingProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("shipping"), shippingToken, shippingOriginCep, shippingUserAgent, shippingEnvironment == "sandbox"));
}
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12)); });
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = 429; options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
var app = builder.Build();
if (app.Configuration.GetValue<bool>("RunMigrations")) { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<StoreDbContext>().Database.MigrateAsync(); await BootstrapAdmin(scope.ServiceProvider, app.Configuration); }
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/live");
app.MapGet("/health/ready", async (StoreDbContext db, CancellationToken ct) => {
    try { return await db.Database.CanConnectAsync(ct) && !((await db.Database.GetPendingMigrationsAsync(ct)).Any()) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch (Exception) when (!ct.IsCancellationRequested) { return Results.StatusCode(503); }
});
app.MapAdmin();
var api = app.MapGroup("/api/v1");
api.MapGet("/vehicles/makes", async (StoreDbContext db, CancellationToken ct) => await db.VehicleMakes.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));
api.MapGet("/vehicles/models", async (Guid makeId, StoreDbContext db, CancellationToken ct) => await db.VehicleModels.AsNoTracking().Where(x => x.MakeId == makeId).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));
api.MapGet("/vehicles/versions", async (Guid modelId, StoreDbContext db, CancellationToken ct) => await db.VehicleVersions.AsNoTracking().Where(x => x.ModelId == modelId).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.StartYear, x.EndYear }).ToListAsync(ct));
api.MapGet("/categories", async (StoreDbContext db, CancellationToken ct) => await db.Categories.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug, x.ParentId }).ToListAsync(ct));
api.MapGet("/brands", async (StoreDbContext db, CancellationToken ct) => await db.Brands.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug }).ToListAsync(ct));
api.MapGet("/products", async (StoreDbContext db, string? q, string? category, string? brand, Guid? vehicleVersion, int? year, int? page, CancellationToken ct) => {
    var currentPage = Math.Clamp(page ?? 1, 1, 10000);
    var query = db.Products.AsNoTracking().Where(x => x.Published);
    if (!string.IsNullOrWhiteSpace(q)) { var search = q.Trim(); query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%") || EF.Functions.ILike(x.Sku, $"%{search}%") || (x.ManufacturerCode != null && EF.Functions.ILike(x.ManufacturerCode, $"%{search}%")) || EF.Functions.ILike(x.Brand.Name, $"%{search}%") || EF.Functions.ILike(x.Category.Name, $"%{search}%")); }
    if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category.Slug == category);
    if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(x => x.Brand.Slug == brand);
    if (vehicleVersion is not null) query = query.Where(x => db.ProductApplications.Any(a => a.ProductId == x.Id && a.VehicleVersionId == vehicleVersion && (year == null || (a.StartYear <= year && a.EndYear >= year))));
    var total = await query.CountAsync(ct);
    var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((currentPage - 1) * 24).Take(24).Select(x => new { x.Id, x.Name, x.Slug, x.Sku, x.Price, Brand = x.Brand.Name, Category = x.Category.Name, Available = x.Inventory.OnHand - x.Inventory.Reserved }).ToListAsync(ct);
    return Results.Ok(new { items, total, page = currentPage, pageSize = 24 });
});
api.MapGet("/products/{slug}", async (string slug, StoreDbContext db, CancellationToken ct) => {
    var product = await db.Products.AsNoTracking().Where(x => x.Published && x.Slug == slug).Select(x => new { x.Id, x.Name, x.Slug, x.Sku, x.ManufacturerCode, x.ShortDescription, x.Description, x.Price, Brand = x.Brand.Name, Category = x.Category.Name, Available = x.Inventory.OnHand - x.Inventory.Reserved }).FirstOrDefaultAsync(ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});
api.MapGet("/products/{slug}/applications", async (string slug, StoreDbContext db, CancellationToken ct) => await db.ProductApplications.AsNoTracking().Where(x => x.Product.Slug == slug && x.Product.Published).OrderBy(x => x.VehicleVersion.Model.Make.Name).ThenBy(x => x.VehicleVersion.Model.Name).Select(x => new { Make = x.VehicleVersion.Model.Make.Name, Model = x.VehicleVersion.Model.Name, Version = x.VehicleVersion.Name, x.StartYear, x.EndYear, x.Restriction }).ToListAsync(ct));static bool SameOrigin(HttpContext context)
{
    var origin = context.Request.Headers.Origin.ToString();
    return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(uri.Scheme, context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme, StringComparison.OrdinalIgnoreCase) && uri.Port == (context.Request.Host.Port ?? (uri.Scheme == "https" ? 443 : 80));
}
static Guid? CartId(HttpContext context) => Guid.TryParse(context.Request.Cookies["cart"], out var id) ? id : null;
api.MapPost("/checkout/quote", async (HttpContext context, ShippingQuoteRequest input, StoreDbContext db, IServiceProvider services, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Cep is not { Length: 8 } || !input.Cep.All(char.IsDigit)) return Results.ValidationProblem(new Dictionary<string,string[]> { ["cep"] = ["Informe um CEP com oito dígitos."] });
    var provider = services.GetService<IShippingProvider>();
    if (provider is null) return Results.Problem("Frete não configurado.", statusCode: 503);
    var cartId = CartId(context);
    if (cartId is null) return Results.BadRequest(new { message = "Carrinho vazio." });
    var cart = await db.Carts.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == cartId && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    if (cart is null || cart.Items.Count == 0) return Results.BadRequest(new { message = "Carrinho vazio." });
    if (cart.Items.Any(x => !x.Product.Published || x.Quantity > x.Product.Inventory.Available || x.Product.WeightKg is null or <= 0 || x.Product.HeightCm is null or <= 0 || x.Product.WidthCm is null or <= 0 || x.Product.LengthCm is null or <= 0)) return Results.Conflict(new { message = "Revise disponibilidade, peso e dimensões dos produtos." });
    var products = cart.Items.Select(x => new ShippingProduct(x.Product.Sku, x.Product.WidthCm!.Value, x.Product.HeightCm!.Value, x.Product.LengthCm!.Value, x.Product.WeightKg!.Value, x.Product.Price, x.Quantity)).ToArray();
    try { return Results.Ok(await provider.QuoteAsync(input.Cep, products, ct)); }
    catch (HttpRequestException) { return Results.Problem("Serviço de frete indisponível. Tente novamente.", statusCode: 503); }
    catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return Results.Problem("Tempo de cotação excedido. Tente novamente.", statusCode: 503); }
    catch (System.Text.Json.JsonException) { return Results.Problem("Resposta de frete inválida.", statusCode: 502); }
});api.MapGet("/cart", async (HttpContext context, StoreDbContext db, CancellationToken ct) => {
    var id = CartId(context);
    if (id is null) return Results.Ok(new { items = Array.Empty<object>(), subtotal = 0m });
    var cart = await db.Carts.AsNoTracking().Where(x => x.Id == id && x.ExpiresAt > DateTimeOffset.UtcNow).Select(x => new { Items = x.Items.Select(i => new { i.Id, i.ProductId, i.Product.Name, i.Product.Slug, i.Quantity, i.Product.Price, Available = i.Product.Inventory.OnHand - i.Product.Inventory.Reserved }).ToList() }).FirstOrDefaultAsync(ct);
    if (cart is null) return Results.Ok(new { items = Array.Empty<object>(), subtotal = 0m });
    return Results.Ok(new { cart.Items, subtotal = cart.Items.Sum(x => x.Price * x.Quantity) });
});
api.MapPost("/cart/items", async (HttpContext context, AddCartItem input, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Quantity is < 1 or > 99) return Results.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Informe uma quantidade entre 1 e 99."] });
    var product = await db.Products.Include(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == input.ProductId && x.Published, ct);
    if (product is null) return Results.NotFound();
    var id = CartId(context);
    var cart = id is null ? null : await db.Carts.FirstOrDefaultAsync(x => x.Id == id && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    if (cart is null) { cart = new Cart(); db.Carts.Add(cart); }
    var item = await db.CartItems.FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == input.ProductId, ct);
    var quantity = (item?.Quantity ?? 0) + input.Quantity;
    if (!CartQuantityPolicy.CanAdd(item?.Quantity ?? 0, input.Quantity, product.Inventory.Available)) return Results.Conflict(new { message = "Quantidade indisponível." });
    if (item is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = input.ProductId, Quantity = quantity }); else item.Quantity = quantity;
    await db.SaveChangesAsync(ct);
    context.Response.Cookies.Append("cart", cart.Id.ToString(), new CookieOptions { HttpOnly = true, Secure = !app.Environment.IsDevelopment(), SameSite = SameSiteMode.Strict, Expires = cart.ExpiresAt, Path = "/" });
    return Results.NoContent();
});
api.MapDelete("/cart/items/{itemId:guid}", async (Guid itemId, HttpContext context, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    var id = CartId(context);
    if (id is null) return Results.NotFound();
    var item = await db.CartItems.FirstOrDefaultAsync(x => x.Id == itemId && x.CartId == id, ct);
    if (item is null) return Results.NotFound();
    db.CartItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent();
});
api.MapPatch("/cart/items/{itemId:guid}", async (Guid itemId, HttpContext context, UpdateCartItem input, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Quantity is < 1 or > 99) return Results.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Informe uma quantidade entre 1 e 99."] });
    var id = CartId(context);
    if (id is null) return Results.NotFound();
    var item = await db.CartItems.Include(x => x.Product).ThenInclude(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == itemId && x.CartId == id, ct);
    if (item is null) return Results.NotFound();
    if (input.Quantity > item.Product.Inventory.Available) return Results.Conflict(new { message = "Quantidade indisponível." });
    item.Quantity = input.Quantity;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});static async Task BootstrapAdmin(IServiceProvider services, IConfiguration configuration)
{
    var email = configuration["BootstrapAdmin:Email"]; var password = configuration["BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
    var users = services.GetRequiredService<UserManager<StoreUser>>(); var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    if (!await roles.RoleExistsAsync("Administrator")) { var roleResult = await roles.CreateAsync(new IdentityRole<Guid>("Administrator")); if (!roleResult.Succeeded) throw new InvalidOperationException("Falha ao criar papel administrativo."); }
    var user = await users.FindByEmailAsync(email);
    if (user is null) { user = new StoreUser { UserName = email, Email = email, EmailConfirmed = true }; var result = await users.CreateAsync(user, password); if (!result.Succeeded) throw new InvalidOperationException("Credenciais administrativas iniciais inválidas."); }
    if (!await users.IsInRoleAsync(user, "Administrator")) await users.AddToRoleAsync(user, "Administrator");
}
app.Run();
public sealed record AddCartItem(Guid ProductId, int Quantity);
public sealed record UpdateCartItem(int Quantity);
public sealed record ShippingQuoteRequest(string Cep);
public partial class Program;


















