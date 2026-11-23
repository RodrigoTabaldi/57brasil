using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Brasil57.Domain;
using Brasil57.Application;
using Brasil57.Api;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var keyPath = builder.Configuration["DataProtection:KeyPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".keys");
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection().SetApplicationName("Brasil57").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<StoreDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<CatalogReadRouter>();
var redisHost = builder.Configuration["Redis:Host"];
if (string.IsNullOrWhiteSpace(redisHost)) builder.Services.AddDistributedMemoryCache();
else builder.Services.AddStackExchangeRedisCache(options => {
    var configuration = new ConfigurationOptions { Password = builder.Configuration["Redis:Password"], AbortOnConnectFail = false, ConnectTimeout = 1000, SyncTimeout = 1000 };
    configuration.EndPoints.Add(redisHost, builder.Configuration.GetValue("Redis:Port", 6379));
    options.ConfigurationOptions = configuration;
});
builder.Services.AddScoped<CatalogTaxonomyCache>();
// Future gateway selection is explicit; neither adapter can charge until implemented.
switch (builder.Configuration["Payment:Gateway"]?.ToLowerInvariant())
{
    case "stripe": builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>(); builder.Services.AddScoped<PaymentService>(); break;
    case "abacatepay": builder.Services.AddScoped<IPaymentGateway, AbacatePayPaymentGateway>(); builder.Services.AddScoped<PaymentService>(); break;
    case null or "": break;
    default: throw new InvalidOperationException("Gateway de pagamento desconhecido.");
}
builder.Services.AddSingleton(new PaymentNotificationQueue(builder.Configuration["RabbitMq:Host"], builder.Configuration["RabbitMq:Password"]));
builder.Services.AddIdentityCore<StoreUser>(options => { options.Password.RequiredLength = 12; options.Lockout.MaxFailedAccessAttempts = 5; options.User.RequireUniqueEmail = true; }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<StoreDbContext>().AddSignInManager().AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options => { options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict; options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = true; options.Events.OnValidatePrincipal = async context => {
    var mfa = context.Principal?.HasClaim("admin_mfa", "true") == true;
    var setup = context.Principal?.HasClaim("admin_setup", "true") == true;
    await SecurityStampValidator.ValidatePrincipalAsync(context);
    if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity) {
        if (mfa && !context.Principal.HasClaim("admin_mfa", "true")) { identity.AddClaim(new System.Security.Claims.Claim("admin_mfa", "true")); context.ShouldRenew = true; }
        if (setup && !context.Principal.HasClaim("admin_setup", "true")) { identity.AddClaim(new System.Security.Claims.Claim("admin_setup", "true")); context.ShouldRenew = true; }
    }
}; options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; }; options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }; });
builder.Services.AddAuthorization();
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
var shippingToken = builder.Configuration["Shipping:Token"];
var shippingOriginCep = builder.Configuration["Shipping:OriginCep"];
var shippingUserAgent = builder.Configuration["Shipping:UserAgent"];
var shippingEnvironment = builder.Configuration["Shipping:Environment"];
if (!string.IsNullOrWhiteSpace(shippingToken) && shippingOriginCep is { Length: 8 } && shippingOriginCep.All(char.IsDigit) && !string.IsNullOrWhiteSpace(shippingUserAgent) && shippingEnvironment is "sandbox" or "production")
{
    builder.Services.AddHttpClient("shipping", client => client.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddTransient<IShippingProvider>(sp => new MelhorEnvioShippingProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("shipping"), shippingToken, shippingOriginCep, shippingUserAgent, shippingEnvironment == "sandbox"));
}
var webhookEnabled = builder.Configuration.GetValue<bool>("Payment:EnableWebhook");
if (webhookEnabled)
{
    var accessToken = builder.Configuration["Payment:AccessToken"];
    var webhookSecret = builder.Configuration["Payment:WebhookSecret"];
    if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(webhookSecret) ||
        !Uri.TryCreate(builder.Configuration["Payment:SiteUrl"], UriKind.Absolute, out var siteUrl) || siteUrl.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("Configuração de webhook de pagamento incompleta.");
    builder.Services.AddHttpClient("payment", client => client.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddTransient<IPaymentProvider>(sp => new MercadoPagoPaymentProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("payment"), accessToken, webhookSecret, siteUrl));
}
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost; options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12)); });
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = 429; options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
builder.Services.AddRateLimiter(options => options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
var app = builder.Build();
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    await db.Database.MigrateAsync();
    await CatalogSeed.EnsureDepartmentsAsync(db);
    await CatalogSeed.EnsureProductsAsync(db);
    await PartTypeSeed.EnsureAsync(db);
    await BootstrapAdmin(scope.ServiceProvider, app.Configuration);
    return;
}
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api/v1/account") || context.Request.Path.StartsWithSegments("/api/v1/admin") || context.Request.Path.StartsWithSegments("/api/v1/cart") || context.Request.Path.Value?.Contains("/purchase-requests", StringComparison.Ordinal) == true)
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
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
app.MapCustomers(app.Configuration);
var api = app.MapGroup("/api/v1");
api.MapGet("/catalog-items", async Task<IResult> (StoreDbContext db, string? q, string? group, string? vehicleType, int? page, CancellationToken ct) => {
    if (q?.Length is > 120 || group?.Length is > 120) return Results.BadRequest();
    if (vehicleType is not (null or "carro" or "moto" or "caminhao")) return Results.BadRequest();
    var currentPage = Math.Clamp(page ?? 1, 1, 10000);
    var query = db.Products.AsNoTracking().Where(x => x.CatalogVisible && x.Sku.StartsWith("57BR-"));
    if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Sku, $"%{term}%") || EF.Functions.ILike(x.Brand.Name, $"%{term}%") || (x.ManufacturerCode != null && EF.Functions.ILike(x.ManufacturerCode, $"%{term}%"))); }
    if (group == "suspensao-e-direcao") query = query.Where(x => x.Category.Slug == "suspensao-e-direcao" || x.Category.Slug == "suspensao" || x.Category.Slug == "direcao");
    else if (group == "pneus-e-rodas") query = query.Where(x => x.Category.Slug == "pneus-e-rodas" || x.Category.Slug == "pneus");
    else if (!string.IsNullOrWhiteSpace(group)) query = query.Where(x => x.Category.Slug == group);
    if (vehicleType is not null) query = query.Where(x => x.CompatibilitySourceReference != null && x.CompatibilitySourceReference.Trim().Length >= 5 && x.VehicleFamilies.Contains(vehicleType));
    var total = await query.CountAsync(ct);
    var items = await query.OrderBy(x => x.Sku).Skip((currentPage - 1) * 24).Take(24).Select(x => new { x.Id, x.Name, x.Slug, ReferencePrice = x.Price, Group = x.Category.Name, GroupSlug = x.Category.Slug, ImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() }).ToListAsync(ct);
    return Results.Ok(new { items, total, page = currentPage, pageSize = 24, vehicleTypeApplied = vehicleType });
});
api.MapGet("/catalog-items/groups", async (StoreDbContext db, CancellationToken ct) => await db.Products.AsNoTracking().Where(x => x.CatalogVisible && x.Sku.StartsWith("57BR-")).GroupBy(x => new { x.Category.Name, x.Category.Slug }).Select(x => new { x.Key.Name, x.Key.Slug, Count = x.Count() }).OrderBy(x => x.Name).ToListAsync(ct));
api.MapGet("/catalog-items/{slug}", async (string slug, StoreDbContext db, CancellationToken ct) => await db.Products.AsNoTracking().Where(x => x.CatalogVisible && x.Sku.StartsWith("57BR-") && x.Slug == slug).Select(x => new { x.Id, x.Name, x.Slug, ReferencePrice = x.Price, Group = x.Category.Name, GroupSlug = x.Category.Slug, ImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(), x.Sku, x.ManufacturerCode, Brand = x.Brand.Name, x.ShortDescription, x.Description }).FirstOrDefaultAsync(ct) is { } item ? Results.Ok(item) : Results.NotFound());
api.MapPost("/visits", async (HttpContext context, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    var day = DateOnly.FromDateTime(DateTime.UtcNow);
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"DailyVisits\" (\"Day\", \"Count\") VALUES ({day}, 1) ON CONFLICT (\"Day\") DO UPDATE SET \"Count\" = \"DailyVisits\".\"Count\" + 1", ct);
    return Results.NoContent();
});
api.MapGet("/part-types", async Task<IResult> (StoreDbContext db, string? q, string? group, int? page, CancellationToken ct) => {
    if (q?.Length is > 120 || group?.Length is > 120) return Results.BadRequest();
    var currentPage = Math.Clamp(page ?? 1, 1, 10000);
    var query = db.PartTypes.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = q.Trim();
        var normalized = PartTypeSeed.Slug(term);
        query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Group, $"%{term}%") ||
            (normalized != "" && (x.Slug.Contains(normalized) || x.GroupSlug.Contains(normalized))));
    }
    if (!string.IsNullOrWhiteSpace(group)) query = query.Where(x => x.GroupSlug == group);
    var total = await query.CountAsync(ct);
    var items = await query.OrderBy(x => x.Group).ThenBy(x => x.Name).Skip((currentPage - 1) * 24).Take(24)
        .Select(x => new { x.Id, x.Name, x.Slug, x.Group, x.GroupSlug, x.ImageUrl }).ToListAsync(ct);
    return Results.Ok(new { items, total, page = currentPage, pageSize = 24 });
});
api.MapGet("/part-types/groups", async (StoreDbContext db, CancellationToken ct) =>
    await db.PartTypes.AsNoTracking().GroupBy(x => new { x.Group, x.GroupSlug })
        .Select(x => new { Name = x.Key.Group, Slug = x.Key.GroupSlug, Count = x.Count() }).OrderBy(x => x.Name).ToListAsync(ct));
api.MapGet("/part-types/{slug}", async (string slug, StoreDbContext db, CancellationToken ct) =>
    await db.PartTypes.AsNoTracking().Where(x => x.Slug == slug)
        .Select(x => new { x.Id, x.Name, x.Slug, x.Group, x.GroupSlug, x.ImageUrl }).FirstOrDefaultAsync(ct) is { } type
            ? Results.Ok(type) : Results.NotFound());
api.MapPost("/part-types/{slug}/purchase-requests", async (string slug, HttpContext context, PartTypePurchaseRequestInput input, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Quantity is < 1 or > 99) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Informe uma quantidade entre 1 e 99."] });
    var userId = CustomerId(context);
    if (userId is null) return Results.Unauthorized();
    var partTypeId = await db.PartTypes.Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    if (partTypeId is null) return Results.NotFound();
    var request = new PartTypePurchaseRequest { UserId = userId.Value, PartTypeId = partTypeId.Value, Quantity = input.Quantity };
    db.PartTypePurchaseRequests.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Accepted(value: new { request.Id, message = "Solicitação registrada. A equipe confirmará preço e disponibilidade antes de concluir a compra." });
}).RequireAuthorization().RequireRateLimiting("account");
if (webhookEnabled)
{
    api.MapPost("/payments/mercado-pago/webhooks", async (HttpContext context, StoreDbContext db, IPaymentProvider provider, PaymentNotificationQueue queue, ILogger<Program> logger, CancellationToken ct) => {
        if (context.Request.Query["type"] != "payment") return Results.Ok();
        var paymentId = context.Request.Query["data.id"].ToString();
        var requestId = context.Request.Headers["x-request-id"].ToString();
        var signature = context.Request.Headers["x-signature"].ToString();
        if (requestId.Length is < 1 or > 100 || paymentId.Length is < 1 or > 30 || !paymentId.All(char.IsDigit) ||
            !provider.VerifyWebhook(signature, requestId, paymentId)) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        var notificationId = Guid.NewGuid();
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PaymentNotifications" ("Id", "RequestId", "PaymentId", "ReceivedAt", "NextAttemptAt", "Attempts")
            VALUES ({notificationId}, {requestId}, {paymentId}, {now}, {now}, 0)
            ON CONFLICT ("RequestId") DO NOTHING
            """, ct);
        if (inserted == 1)
        {
            try { await queue.PublishAsync(notificationId, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception exception) { logger.LogWarning(exception, "Fila de pagamentos indisponível; inbox será processada pelo polling."); }
        }
        return Results.Accepted();
    });
}
api.MapGet("/vehicles/makes", async (StoreDbContext db, CancellationToken ct) => await db.VehicleMakes.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));
api.MapGet("/vehicles/models", async (Guid makeId, StoreDbContext db, CancellationToken ct) => await db.VehicleModels.AsNoTracking().Where(x => x.MakeId == makeId).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));
api.MapGet("/vehicles/versions", async (Guid modelId, StoreDbContext db, CancellationToken ct) => await db.VehicleVersions.AsNoTracking().Where(x => x.ModelId == modelId).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.StartYear, x.EndYear }).ToListAsync(ct));
api.MapGet("/categories", async (StoreDbContext db, CatalogTaxonomyCache cache, CancellationToken ct) =>
    await cache.GetOrLoadAsync(CatalogTaxonomyCache.CategoriesKey,
        () => db.Categories.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).Select(x => new CategoryOption(x.Id, x.Name, x.Slug, x.ParentId)).ToListAsync(ct), ct));
api.MapGet("/brands", async (StoreDbContext db, CatalogTaxonomyCache cache, CancellationToken ct) =>
    await cache.GetOrLoadAsync(CatalogTaxonomyCache.BrandsKey,
        () => db.Brands.AsNoTracking().Where(x => x.Slug != "marca-a-confirmar").OrderBy(x => x.Name).Select(x => new BrandOption(x.Id, x.Name, x.Slug)).ToListAsync(ct), ct));
api.MapGet("/products", async Task<IResult> (CatalogReadRouter reader, string? q, string? category, string? brand, string? vehicleType, Guid? vehicleVersion, int? year, decimal? minPrice, decimal? maxPrice, string? sort, int? page, CancellationToken ct) => {
    if (!CatalogQueries.Valid(q, minPrice, maxPrice))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["filters"] = ["Busca ou faixa de preço inválida."] });
    if (sort is not (null or "name" or "price_asc" or "price_desc" or "newest"))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sort"] = ["Ordenação inválida."] });
    var currentPage = Math.Clamp(page ?? 1, 1, 10000);
    return await reader.ReadAsync<IResult>(async db => {
        var query = CatalogQueries.Filter(db, q, category, brand, vehicleType, vehicleVersion, year, minPrice, maxPrice);
        var total = await query.CountAsync(ct);
        var ordered = sort switch {
            "price_asc" => query.OrderBy(x => x.Price),
            "price_desc" => query.OrderByDescending(x => x.Price),
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Name)
        };
        var items = await ordered.ThenBy(x => x.Id).Skip((currentPage - 1) * 24).Take(24).Select(x => new { x.Id, x.Name, x.Slug, x.Sku, Price = x.Price, Brand = x.Brand.Name, Category = x.Category.Name, Available = x.Inventory.OnHand - x.Inventory.Reserved, ImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() }).ToListAsync(ct);
        return Results.Ok(new { items, total, page = currentPage, pageSize = 24 });
    }, ct);
});
api.MapGet("/products/facets", async Task<IResult> (CatalogReadRouter reader, string? q, string? category, string? brand, string? vehicleType, Guid? vehicleVersion, int? year, decimal? minPrice, decimal? maxPrice, CancellationToken ct) => {
    if (!CatalogQueries.Valid(q, minPrice, maxPrice))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["filters"] = ["Busca ou faixa de preço inválida."] });
    return await reader.ReadAsync<IResult>(async db => {
        var query = CatalogQueries.Filter(db, q, category, brand, vehicleType, vehicleVersion, year, minPrice, maxPrice);
        var categories = await query.GroupBy(x => new { x.Category.Slug, x.Category.Name })
            .Select(x => new { x.Key.Slug, x.Key.Name, Count = x.Count() }).OrderByDescending(x => x.Count).Take(50).ToListAsync(ct);
        var brands = await query.GroupBy(x => new { x.Brand.Slug, x.Brand.Name })
            .Select(x => new { x.Key.Slug, x.Key.Name, Count = x.Count() }).OrderByDescending(x => x.Count).Take(50).ToListAsync(ct);
        return Results.Ok(new { categories, brands });
    }, ct);
});
api.MapGet("/products/{slug}", async (string slug, StoreDbContext db, CancellationToken ct) => {
    var product = await CatalogQueries.Purchasable(db).Where(x => x.Slug == slug).Select(x => new { x.Id, x.Name, x.Slug, x.Sku, x.ManufacturerCode, x.ShortDescription, x.Description, Price = x.Price, Brand = x.Brand.Name, BrandSlug = x.Brand.Slug, Category = x.Category.Name, CategorySlug = x.Category.Slug, x.WeightKg, x.HeightCm, x.WidthCm, x.LengthCm, Available = x.Inventory.OnHand - x.Inventory.Reserved, Images = x.Images.OrderBy(i => i.SortOrder).Select(i => new { i.Id, i.Url, i.AltText }).ToList() }).FirstOrDefaultAsync(ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});
api.MapGet("/products/{slug}/applications", async (string slug, StoreDbContext db, CancellationToken ct) => await db.ProductApplications.AsNoTracking().Where(x => x.Product.Slug == slug && x.Product.Published && x.Product.CompatibilitySourceReference != null).OrderBy(x => x.VehicleVersion.Model.Make.Name).ThenBy(x => x.VehicleVersion.Model.Name).Select(x => new { Make = x.VehicleVersion.Model.Make.Name, Model = x.VehicleVersion.Model.Name, Version = x.VehicleVersion.Name, x.StartYear, x.EndYear, x.Restriction }).ToListAsync(ct));
api.MapPost("/products/{slug}/shipping-quote", async (string slug, HttpContext context, ProductShippingQuoteRequest input, StoreDbContext db, IServiceProvider services, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Cep is not { Length: 8 } || !input.Cep.All(char.IsDigit) || input.Quantity is < 1 or > 99)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cep"] = ["Informe um CEP com oito dígitos e quantidade entre 1 e 99."] });
    var provider = services.GetService<IShippingProvider>();
    if (provider is null) return Results.Problem("Frete não configurado.", statusCode: 503);
    var product = await CatalogQueries.Purchasable(db).Where(x => x.Slug == slug)
        .Select(x => new { x.Sku, x.Price, x.WeightKg, x.HeightCm, x.WidthCm, x.LengthCm, Available = x.Inventory.OnHand - x.Inventory.Reserved }).FirstOrDefaultAsync(ct);
    if (product is null) return Results.NotFound();
    if (input.Quantity > product.Available || product.WeightKg is null or <= 0 || product.HeightCm is null or <= 0 || product.WidthCm is null or <= 0 || product.LengthCm is null or <= 0)
        return Results.Conflict(new { message = "Revise disponibilidade, peso e dimensões do produto." });
    var items = new[] { new ShippingProduct(product.Sku, product.WidthCm.Value, product.HeightCm.Value, product.LengthCm.Value, product.WeightKg.Value, product.Price, input.Quantity) };
    try { return Results.Ok(await provider.QuoteAsync(input.Cep, items, ct)); }
    catch (HttpRequestException) { return Results.Problem("Serviço de frete indisponível.", statusCode: 503); }
    catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return Results.Problem("Tempo de cotação excedido.", statusCode: 503); }
    catch (System.Text.Json.JsonException) { return Results.Problem("Resposta de frete inválida.", statusCode: 502); }
}).RequireRateLimiting("account");
static bool SameOrigin(HttpContext context)
{
    var origin = context.Request.Headers.Origin.ToString();
    return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(uri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase) && uri.Port == (context.Request.Host.Port ?? (uri.Scheme == "https" ? 443 : 80));
}
static Guid? CustomerId(HttpContext context) => Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
api.MapPost("/checkout/quote", async (HttpContext context, ShippingQuoteRequest input, StoreDbContext db, IServiceProvider services, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Cep is not { Length: 8 } || !input.Cep.All(char.IsDigit)) return Results.ValidationProblem(new Dictionary<string,string[]> { ["cep"] = ["Informe um CEP com oito dígitos."] });
    var provider = services.GetService<IShippingProvider>();
    if (provider is null) return Results.Problem("Frete não configurado.", statusCode: 503);
    var userId = CustomerId(context);
    var cartId = await db.Carts.Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    if (cartId is null) return Results.BadRequest(new { message = "Carrinho vazio." });
    var cart = await db.Carts.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == cartId && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    if (cart is null || cart.Items.Count == 0) return Results.BadRequest(new { message = "Carrinho vazio." });
    if (cart.Items.Any(x => !x.Product.Published || x.Quantity > x.Product.Inventory.Available || x.Product.WeightKg is null or <= 0 || x.Product.HeightCm is null or <= 0 || x.Product.WidthCm is null or <= 0 || x.Product.LengthCm is null or <= 0)) return Results.Conflict(new { message = "Revise disponibilidade, peso e dimensões dos produtos." });
    var products = cart.Items.Select(x => new ShippingProduct(x.Product.Sku, x.Product.WidthCm!.Value, x.Product.HeightCm!.Value, x.Product.LengthCm!.Value, x.Product.WeightKg!.Value, x.Product.Price, x.Quantity)).ToArray();
    try { return Results.Ok(await provider.QuoteAsync(input.Cep, products, ct)); }
    catch (HttpRequestException) { return Results.Problem("Serviço de frete indisponível. Tente novamente.", statusCode: 503); }
    catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return Results.Problem("Tempo de cotação excedido. Tente novamente.", statusCode: 503); }
    catch (System.Text.Json.JsonException) { return Results.Problem("Resposta de frete inválida.", statusCode: 502); }
}).RequireAuthorization().RequireRateLimiting("account");
api.MapGet("/cart", async (HttpContext context, StoreDbContext db, CancellationToken ct) => {
    var userId = CustomerId(context);
    var id = await db.Carts.Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    if (id is null) return Results.Ok(new { items = Array.Empty<object>(), subtotal = 0m });
    var cart = await db.Carts.AsNoTracking().Where(x => x.Id == id && x.ExpiresAt > DateTimeOffset.UtcNow).Select(x => new { Items = x.Items.Select(i => new { i.Id, i.ProductId, i.Product.Name, i.Product.Slug, i.Quantity, i.Product.Price, Available = i.Product.Inventory.OnHand - i.Product.Inventory.Reserved }).ToList() }).FirstOrDefaultAsync(ct);
    if (cart is null) return Results.Ok(new { items = Array.Empty<object>(), subtotal = 0m });
    return Results.Ok(new { cart.Items, subtotal = cart.Items.Sum(x => x.Price * x.Quantity) });
}).RequireAuthorization();
api.MapPost("/cart/items", async (HttpContext context, AddCartItem input, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Quantity is < 1 or > 99) return Results.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Informe uma quantidade entre 1 e 99."] });
    var userId = CustomerId(context);
    if (userId is null) return Results.Unauthorized();
    var key = context.Request.Headers["Idempotency-Key"].ToString();
    if (key.Length is < 16 or > 100 || !key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        return Results.ValidationProblem(new Dictionary<string,string[]> { ["Idempotency-Key"] = ["Informe uma chave de operação válida."] });
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({userId.Value.ToString()}))", ct);
    var prior = await db.CartOperations.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Key == key, ct);
    if (prior is not null)
        return prior.ProductId == input.ProductId && prior.Quantity == input.Quantity ? Results.NoContent() : Results.Conflict(new { message = "Chave já usada para outra operação." });
    var product = await db.Products.Include(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == input.ProductId && x.Published, ct);
    if (product is null) return Results.NotFound();
    var id = await db.Carts.Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    var cart = id is null ? null : await db.Carts.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (cart is null) { cart = new Cart { UserId = userId, ExpiresAt = DateTimeOffset.UtcNow.AddYears(10) }; db.Carts.Add(cart); }
    var item = await db.CartItems.FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == input.ProductId, ct);
    var quantity = (item?.Quantity ?? 0) + input.Quantity;
    if (!CartQuantityPolicy.CanAdd(item?.Quantity ?? 0, input.Quantity, product.Inventory.Available)) return Results.Conflict(new { message = "Quantidade indisponível." });
    if (item is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = input.ProductId, Quantity = quantity }); else item.Quantity = quantity;
    db.CartOperations.Add(new CartOperation { UserId = userId.Value, Key = key, ProductId = input.ProductId, Quantity = input.Quantity });
    await db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
api.MapDelete("/cart/items/{itemId:guid}", async (Guid itemId, HttpContext context, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    var userId = CustomerId(context);
    var id = await db.Carts.Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    if (id is null) return Results.NotFound();
    var item = await db.CartItems.FirstOrDefaultAsync(x => x.Id == itemId && x.CartId == id, ct);
    if (item is null) return Results.NotFound();
    db.CartItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent();
}).RequireAuthorization();
api.MapPatch("/cart/items/{itemId:guid}", async (Guid itemId, HttpContext context, UpdateCartItem input, StoreDbContext db, CancellationToken ct) => {
    if (!SameOrigin(context)) return Results.StatusCode(403);
    if (input.Quantity is < 1 or > 99) return Results.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Informe uma quantidade entre 1 e 99."] });
    var userId = CustomerId(context);
    var id = await db.Carts.Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
    if (id is null) return Results.NotFound();
    var item = await db.CartItems.Include(x => x.Product).ThenInclude(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == itemId && x.CartId == id, ct);
    if (item is null) return Results.NotFound();
    if (input.Quantity > item.Product.Inventory.Available) return Results.Conflict(new { message = "Quantidade indisponível." });
    item.Quantity = input.Quantity;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();static async Task BootstrapAdmin(IServiceProvider services, IConfiguration configuration)
{
    var email = configuration["BootstrapAdmin:Email"]; var password = configuration["BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Informe e-mail e senha apenas no bootstrap inicial; remova ambas as variÃ¡veis depois.");
    var users = services.GetRequiredService<UserManager<StoreUser>>(); var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    if (!await roles.RoleExistsAsync("Administrator")) { var roleResult = await roles.CreateAsync(new IdentityRole<Guid>("Administrator")); if (!roleResult.Succeeded) throw new InvalidOperationException("Falha ao criar papel administrativo."); }
    var existingAdmins = await users.GetUsersInRoleAsync("Administrator");
    if (existingAdmins.Count > 1 || (existingAdmins.Count == 1 && !string.Equals(existingAdmins[0].Email, email, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException("JÃ¡ existe outra conta administrativa. Remova as credenciais de bootstrap e investigue a divergÃªncia.");
    // Never elevate an account that could have been registered through the public endpoint.
    var existing = await users.FindByEmailAsync(email);
    if (existing is not null)
    {
        if (await users.IsInRoleAsync(existing, "Administrator")) throw new InvalidOperationException("Administrador jÃ¡ criado. Remova BOOTSTRAP_ADMIN_PASSWORD do ambiente antes de executar nova migraÃ§Ã£o.");
        throw new InvalidOperationException("O e-mail de bootstrap já pertence a uma conta existente. Escolha outro e-mail e investigue a conta antes de prosseguir.");
    }
    var user = new StoreUser { UserName = email, Email = email, EmailConfirmed = true };
    var result = await users.CreateAsync(user, password);
    if (!result.Succeeded) throw new InvalidOperationException("Credenciais administrativas iniciais inválidas.");
    var assignment = await users.AddToRoleAsync(user, "Administrator");
    if (!assignment.Succeeded) throw new InvalidOperationException("Falha ao atribuir papel administrativo.");
}
app.Run();
public sealed record AddCartItem(Guid ProductId, int Quantity);
public sealed record UpdateCartItem(int Quantity);
public sealed record ShippingQuoteRequest(string Cep);
public sealed record ProductShippingQuoteRequest(string Cep, int Quantity);
public partial class Program;
