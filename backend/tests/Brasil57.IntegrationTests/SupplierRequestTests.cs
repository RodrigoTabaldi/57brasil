using System.Net;
using System.Net.Http.Json;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class SupplierRequestTests
{
    [Fact]
    public async Task Request_is_persisted_once_private_exported_safely_and_answered_once()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE") ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseSetting("ConnectionStrings:Default", connection); b.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-supplier-{Guid.NewGuid():N}")); });
        Guid productId; Guid publishedId; string adminEmail;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>(); await db.Database.MigrateAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var product = new Product { Name = "Peça de teste", Slug = $"supplier-{suffix}", Sku = $"SUP-{suffix}", Category = new Category { Name = "Teste", Slug = $"supplier-cat-{suffix}" }, Brand = new Brand { Name = "Teste", Slug = $"supplier-brand-{suffix}" }, Price = 0, Published = false, CatalogVisible = true, Inventory = new InventoryItem { OnHand = 0 } };
            var published = new Product { Name = "Produto publicado", Slug = $"published-{suffix}", Sku = $"PUB-{suffix}", Category = product.Category, Brand = product.Brand, Price = 100, PriceSourceReference = "Tabela fabricante 2026", Published = true, CatalogVisible = false, Inventory = new InventoryItem { OnHand = 2 } };
            db.Products.AddRange(product, published); await db.SaveChangesAsync(); productId = product.Id; publishedId = published.Id;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<StoreUser>>(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roles.RoleExistsAsync("Administrator")) await roles.CreateAsync(new IdentityRole<Guid>("Administrator"));
            adminEmail = $"admin-{suffix}@example.test";
            var admin = new StoreUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(admin, "StrongPassword1!")).Succeeded); Assert.True((await users.AddToRoleAsync(admin, "Administrator")).Succeeded);
        }
        using var buyer = factory.CreateClient(); using var other = factory.CreateClient(); using var adminClient = factory.CreateClient();
        var body = new { productId, quantity = 2, vehicle = "Fiat Uno 2015 1.0", customerName = "Cliente Teste", contact = "11999999999", postalCode = "01001000", street = "Rua Um", number = "12", complement = "", district = "Centro", city = "São Paulo", state = "SP", notes = "=HYPERLINK(\"https://bad.test\")" };
        var key = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(buyer, "/api/v1/supplier-requests", body, key)).StatusCode);
        await Register(buyer); await Register(other);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(buyer, "/api/v1/supplier-requests", body, "bad")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(buyer, "/api/v1/supplier-requests", new { productId, quantity = 0, body.vehicle, body.customerName, body.contact, body.postalCode, body.street, body.number, body.complement, body.district, body.city, body.state, body.notes }, Guid.NewGuid().ToString())).StatusCode);
        var responses = await Task.WhenAll(Post(buyer, "/api/v1/supplier-requests", body, key), Post(buyer, "/api/v1/supplier-requests", body, key));
        Assert.Contains(responses, x => x.StatusCode == HttpStatusCode.Created); Assert.All(responses, x => Assert.True(x.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));
        var created = await responses[0].Content.ReadFromJsonAsync<RequestResponse>(); Assert.NotNull(created);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(buyer, "/api/v1/supplier-requests", new { productId, quantity = 3, body.vehicle, body.customerName, body.contact, body.postalCode, body.street, body.number, body.complement, body.district, body.city, body.state, body.notes }, key)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/supplier-requests/{created.Id}")).StatusCode);
        Assert.Equal("[]", await (await other.GetAsync("/api/v1/supplier-requests")).Content.ReadAsStringAsync());
        Assert.Single((await buyer.GetFromJsonAsync<List<RequestResponse>>("/api/v1/supplier-requests"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync("/api/v1/admin/supplier-requests")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(adminClient, "/api/v1/admin/session", new { email = adminEmail, password = "StrongPassword1!" })).StatusCode);
        var all = await adminClient.GetFromJsonAsync<List<RequestResponse>>("/api/v1/admin/supplier-requests"); Assert.Contains(all!, x => x.Id == created.Id);
        var csv = await (await adminClient.GetAsync("/api/v1/admin/supplier-requests/export.csv")).Content.ReadAsStringAsync(); Assert.Contains("'=HYPERLINK", csv);
        var answer = new { exactPart = "Peça exata ABC", available = true, unitPrice = 120m, leadDays = 5, weightKg = 1m, heightCm = 10m, widthCm = 12m, lengthCm = 14m, source = "Fornecedor, cotação 123" };
        Assert.Equal(HttpStatusCode.BadRequest, (await Patch(adminClient, created.Id, new { answer.exactPart, answer.available, unitPrice = -1m, answer.leadDays, answer.weightKg, answer.heightCm, answer.widthCm, answer.lengthCm, answer.source })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Patch(adminClient, created.Id, answer)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Patch(adminClient, created.Id, answer)).StatusCode);
        Assert.Equal("Quoted", (await buyer.GetFromJsonAsync<RequestResponse>($"/api/v1/supplier-requests/{created.Id}"))!.Status);
        var customerJson = await (await buyer.GetAsync($"/api/v1/supplier-requests/{created.Id}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Peça exata ABC", customerJson); Assert.DoesNotContain("Fornecedor, cotação 123", customerJson);
        var declined = await Post(buyer, "/api/v1/supplier-requests", body, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, declined.StatusCode);
        var declinedId = (await declined.Content.ReadFromJsonAsync<RequestResponse>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await Patch(adminClient, declinedId, new { available = false, source = "Fornecedor, cotação 124" })).StatusCode);
        Assert.Equal("Declined", (await buyer.GetFromJsonAsync<RequestResponse>($"/api/v1/supplier-requests/{declinedId}"))!.Status);
        var publishedBody = new { productId = publishedId, body.quantity, body.vehicle, body.customerName, body.contact, body.postalCode, body.street, body.number, body.complement, body.district, body.city, body.state, body.notes };
        var publishedRequest = await Post(buyer, "/api/v1/supplier-requests", publishedBody, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, publishedRequest.StatusCode);
        var publishedOrderId = (await publishedRequest.Content.ReadFromJsonAsync<RequestResponse>())!.Id;
        Assert.Equal("AwaitingSupplier", (await buyer.GetFromJsonAsync<RequestResponse>($"/api/v1/supplier-requests/{publishedOrderId}"))!.Status);
        await using var verify = factory.Services.CreateAsyncScope();
        var database = verify.ServiceProvider.GetRequiredService<StoreDbContext>();
        Assert.Single(await database.SupplierRequests.Where(x => x.Id == created.Id).ToListAsync());
        Assert.Single(await database.SupplierRequestItems.Where(x => x.SupplierRequestId == created.Id).ToListAsync());
        Assert.Null((await database.SupplierRequestItems.SingleAsync(x => x.SupplierRequestId == created.Id)).ListedUnitPrice);
        var listed = await database.SupplierRequestItems.SingleAsync(x => x.SupplierRequestId == publishedOrderId);
        Assert.Equal(100m, listed.ListedUnitPrice); Assert.Equal("Tabela fabricante 2026", listed.ListedPriceSource);
        Assert.False(await database.Payments.AnyAsync(x => x.OrderId == created.Id));
    }
    private static async Task Register(HttpClient client) => Assert.Equal(HttpStatusCode.OK, (await Post(client, "/api/v1/account/register", new { fullName = "Cliente Teste", email = $"buyer-{Guid.NewGuid():N}@example.test", phone = "11999999999", password = "StrongPassword1!", confirmPassword = "StrongPassword1!" })).StatusCode);
    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? key = null) { using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) }; request.Headers.Add("Origin", "http://localhost"); if (key is not null) request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request); }
    private static async Task<HttpResponseMessage> Patch(HttpClient client, Guid id, object body) { using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/supplier-requests/{id}/answer") { Content = JsonContent.Create(body) }; request.Headers.Add("Origin", "http://localhost"); return await client.SendAsync(request); }
    private sealed record RequestResponse(Guid Id, string PublicId, string Status);
}
