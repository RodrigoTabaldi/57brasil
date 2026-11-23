using System.Net;
using System.Net.Http.Json;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class ProductPurchaseRequestTests
{
    [Fact]
    public async Task Saves_address_and_product_for_authenticated_customer_without_stock()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE") ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-request-{Guid.NewGuid():N}"));
        });
        var suffix = Guid.NewGuid().ToString("N");
        var product = new Product { Name = "Peça", Slug = suffix, Sku = $"57BR-{suffix}", Category = new Category { Name = "Teste", Slug = suffix }, Brand = new Brand { Name = "Teste", Slug = suffix }, CatalogVisible = true, Inventory = new InventoryItem() };
        await using (var scope = factory.Services.CreateAsyncScope()) { var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>(); await db.Database.MigrateAsync(); db.Products.Add(product); await db.SaveChangesAsync(); }
        using var client = factory.CreateClient();
        async Task<HttpResponseMessage> Post(string path, object body) { using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) }; request.Headers.Add("Origin", "http://localhost"); return await client.SendAsync(request); }
        var address = new { quantity = 2, cep = "01001-000", street = "Rua A", number = "10", complement = "", neighborhood = "Centro", city = "São Paulo", state = "sp" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post($"/api/v1/products/{product.Id}/purchase-requests", address)).StatusCode);
        var email = $"{suffix}@example.test";
        Assert.Equal(HttpStatusCode.OK, (await Post("/api/v1/account/register", new { fullName = "Cliente Teste", email, phone = "11999999999", cpf = (string?)null, password = "StrongPassword1!", confirmPassword = "StrongPassword1!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/v1/products/{product.Id}/purchase-requests", new { quantity = 2, cep = "123", street = "Rua A", number = "10", neighborhood = "Centro", city = "São Paulo", state = "SP" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post($"/api/v1/products/{Guid.NewGuid()}/purchase-requests", address)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await Post($"/api/v1/products/{product.Id}/purchase-requests", address)).StatusCode);
        await using var verify = factory.Services.CreateAsyncScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<StoreDbContext>();
        var saved = await dbVerify.ProductPurchaseRequests.SingleAsync(x => x.ProductId == product.Id);
        Assert.Equal("01001000", saved.Cep); Assert.Equal("SP", saved.State); Assert.Equal(2, saved.Quantity);
        Assert.Equal(email, (await dbVerify.Users.SingleAsync(x => x.Id == saved.UserId)).Email);
    }
}
