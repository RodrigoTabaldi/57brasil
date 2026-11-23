using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class AdminProductPublishingTests
{
    [Fact]
    public async Task Published_product_cannot_be_created_with_placeholder_brand_or_made_incomplete()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-admin-{Guid.NewGuid():N}"));
        });
        Guid categoryId, placeholderBrandId, realBrandId;
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.test";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            await db.Database.MigrateAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var category = new Category { Name = "Teste", Slug = $"category-{suffix}" };
            var placeholder = new Brand { Name = "A confirmar", Slug = "marca-a-confirmar" };
            var real = new Brand { Name = "Marca real", Slug = $"brand-{suffix}" };
            // The shared disposable database may already contain the catalog placeholder.
            placeholderBrandId = await db.Brands.Where(x => x.Slug == placeholder.Slug).Select(x => x.Id).FirstOrDefaultAsync();
            if (placeholderBrandId == Guid.Empty) { db.Brands.Add(placeholder); placeholderBrandId = placeholder.Id; }
            db.Categories.Add(category); db.Brands.Add(real);
            await db.SaveChangesAsync();
            categoryId = category.Id; realBrandId = real.Id;

            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roles.RoleExistsAsync("Administrator")) Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("Administrator"))).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<StoreUser>>();
            var admin = new StoreUser { UserName = adminEmail, Email = adminEmail, FullName = "Admin Teste" };
            Assert.True((await users.CreateAsync(admin, "StrongPassword1!")).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, "Administrator")).Succeeded);
        }

        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/v1/admin/session", new { email = adminEmail, password = "StrongPassword1!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var users = setupScope.ServiceProvider.GetRequiredService<UserManager<StoreUser>>();
            var user = (await users.FindByEmailAsync(adminEmail))!;
            var code = AuthenticatorCode((await users.GetAuthenticatorKeyAsync(user))!);
            Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/v1/admin/mfa/setup", new { code })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, "/api/v1/admin/session", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        await using (var loginScope = factory.Services.CreateAsyncScope())
        {
            var users = loginScope.ServiceProvider.GetRequiredService<UserManager<StoreUser>>();
            var user = (await users.FindByEmailAsync(adminEmail))!;
            var code = AuthenticatorCode((await users.GetAuthenticatorKeyAsync(user))!);
            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, "/api/v1/admin/session", new { email = adminEmail, password = "StrongPassword1!", code })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, "/api/v1/visits", new { })).StatusCode);
        using (var overview = await client.GetAsync("/api/v1/admin/overview"))
        {
            Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
            using var json = System.Text.Json.JsonDocument.Parse(await overview.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("visitsToday").GetInt64() >= 1);
        }
        var pageSuffix = Guid.NewGuid().ToString("N");
        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<StoreDbContext>();
            for (var i = 0; i < 101; i++) db.Products.Add(new Product { Name = $"Pagination {pageSuffix} {i:000}", Slug = $"pagination-{pageSuffix}-{i}", Sku = $"PAGE-{pageSuffix}-{i}", CategoryId = categoryId, BrandId = realBrandId, Inventory = new InventoryItem { OnHand = 0 } });
            await db.SaveChangesAsync();
        }
        using (var page = await client.GetAsync($"/api/v1/admin/products?q=Pagination%20{pageSuffix}&page=2"))
        {
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            using var json = System.Text.Json.JsonDocument.Parse(await page.Content.ReadAsStringAsync());
            Assert.Equal(101, json.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(50, json.RootElement.GetProperty("items").GetArrayLength());
        }
        using (var customer = factory.CreateClient())
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/admin/accounts")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Send(customer, HttpMethod.Post, "/api/v1/account/login", new { email = adminEmail, password = "StrongPassword1!" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/admin/accounts")).StatusCode);
        }
        var sku = $"PUBLISH-{Guid.NewGuid():N}";
        object Input(Guid brandId) => new { name = "Peça verificada", slug = sku.ToLowerInvariant(), sku, categoryId, brandId,
            price = 10m, stock = 1, published = true, weightKg = 1m, heightCm = 1m, widthCm = 1m, lengthCm = 1m,
            priceSourceReference = "Tabela de preços 4", stockSourceReference = "Nota fiscal 123" };
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Post, "/api/v1/admin/products", Input(placeholderBrandId))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/v1/admin/products", Input(realBrandId))).StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var product = await verifyDb.Products.AsNoTracking().SingleAsync(x => x.Sku == sku);
        var path = $"/api/v1/admin/products/{product.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Patch, path, new { price = 0m })).StatusCode);
        Assert.Equal(10m, await verifyDb.Products.Where(x => x.Id == product.Id).Select(x => x.Price).SingleAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Patch, path, new { price = 12m })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Patch, path, new { price = 12m, priceSourceReference = "Tabela de preços 5" })).StatusCode);
        Assert.Equal(12m, await verifyDb.Products.Where(x => x.Id == product.Id).Select(x => x.Price).SingleAsync());
        Assert.True(await verifyDb.AuditLogs.AsNoTracking().AnyAsync(x => x.EntityId == product.Id && x.Action == "update_price"));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Post, $"/api/v1/admin/inventory/{product.Id}/adjustments", new { delta = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, $"/api/v1/admin/inventory/{product.Id}/adjustments", new { delta = 1, sourceReference = "Nota fiscal 124" })).StatusCode);
        Assert.Contains("Nota fiscal 124", (await verifyDb.AuditLogs.AsNoTracking().Where(x => x.EntityId == product.Id && x.Action.StartsWith("stock_adjust_")).OrderByDescending(x => x.CreatedAt).Select(x => x.EvidenceSnapshot).FirstAsync())!);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Patch, path, new { price = 0m, published = false })).StatusCode);
        Assert.False(await verifyDb.Products.Where(x => x.Id == product.Id).Select(x => x.Published).SingleAsync());
        Assert.True(await verifyDb.AuditLogs.AsNoTracking().AnyAsync(x => x.EntityId == product.Id && x.Action == "update_status"));
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object body)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Origin", "http://localhost");
        return await client.SendAsync(request);
    }
    private static string AuthenticatorCode(string key)
    {
        var bytes = new List<byte>(); var buffer = 0; var bits = 0;
        foreach (var character in key.TrimEnd('=')) {
            var value = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(char.ToUpperInvariant(character));
            if (value < 0) continue;
            buffer = (buffer << 5) | value; bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(buffer >> bits)); }
        }
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var offset = hash[^1] & 15;
        var number = ((hash[offset] & 127) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (number % 1000000).ToString("D6");
    }
}
