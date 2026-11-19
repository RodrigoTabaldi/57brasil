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

public sealed class CustomerFlowTests
{
    [Fact]
    public async Task Visitor_registers_replays_cart_action_and_recovers_account_without_cross_user_access()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-customer-{Guid.NewGuid():N}"));
        });
        Guid productId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            await db.Database.MigrateAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var product = new Product { Name = "Peça de teste", Slug = $"customer-{suffix}", Sku = $"CUSTOMER-{suffix}", Category = new Category { Name = "Teste", Slug = $"category-{suffix}" }, Brand = new Brand { Name = "Teste", Slug = $"brand-{suffix}" }, Price = 25m, Published = true, Inventory = new InventoryItem { OnHand = 10 } };
            db.Products.Add(product); await db.SaveChangesAsync(); productId = product.Id;
        }
        using var customer = factory.CreateClient();
        using var other = factory.CreateClient();
        var email = $"customer-{Guid.NewGuid():N}@example.test";
        var otherEmail = $"other-{Guid.NewGuid():N}@example.test";
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/cart")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Add(customer, productId, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(customer, "/api/v1/account/register", new { fullName = "Cliente Teste", email, phone = "+5566996308666", cpf = "52998224725", password = "StrongPassword1!", confirmPassword = "StrongPassword1!" })).StatusCode);
        Assert.Equal("66996308666", (await customer.GetFromJsonAsync<ProfileResponse>("/api/v1/account/me"))!.PhoneNumber);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(other, "/api/v1/account/register", new { fullName = "Outro Cliente", email = otherEmail, phone = "11999999998", cpf = "52998224725", password = "StrongPassword1!", confirmPassword = "StrongPassword1!" })).StatusCode);
        var key = Guid.NewGuid().ToString();
        var requests = await Task.WhenAll(Add(customer, productId, key), Add(customer, productId, key));
        Assert.All(requests, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        var cart = await customer.GetFromJsonAsync<CartResponse>("/api/v1/cart");
        var item = Assert.Single(cart!.Items); Assert.Equal(1, item.Quantity); Assert.Equal(25m, cart.Subtotal);
        Assert.Equal(HttpStatusCode.Conflict, (await Add(customer, productId, key, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(customer, "/api/v1/account/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/cart")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(customer, "/api/v1/account/login", new { email, password = "WrongPassword1!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(customer, "/api/v1/account/login", new { email, password = "StrongPassword1!" })).StatusCode);
        Assert.Equal(email, (await customer.GetFromJsonAsync<ProfileResponse>("/api/v1/account/me"))!.Email);
        Assert.Single((await customer.GetFromJsonAsync<CartResponse>("/api/v1/cart"))!.Items);
        Assert.Equal(HttpStatusCode.OK, (await Post(other, "/api/v1/account/register", new { fullName = "Outro Cliente", email = otherEmail, phone = "11999999998", cpf = (string?)null, password = "StrongPassword1!", confirmPassword = "StrongPassword1!" })).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<CartResponse>("/api/v1/cart"))!.Items);
        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/cart/items/{item.Id}"); delete.Headers.Add("Origin", "http://localhost");
        Assert.Equal(HttpStatusCode.NotFound, (await other.SendAsync(delete)).StatusCode);
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<StoreUser>>();
            token = await users.GeneratePasswordResetTokenAsync((await users.FindByEmailAsync(email))!);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await Post(customer, "/api/v1/account/reset-password", new { email, token, password = "NewStrongPassword1!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(customer, "/api/v1/account/reset-password", new { email, token, password = "AnotherPassword1!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/account/me")).StatusCode);
    }
    static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Origin", "http://localhost"); return await client.SendAsync(request);
    }
    static async Task<HttpResponseMessage> Add(HttpClient client, Guid productId, string key, int quantity = 1)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items") { Content = JsonContent.Create(new { productId, quantity }) };
        request.Headers.Add("Origin", "http://localhost"); request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
    }
    sealed record CartResponse(List<CartItemResponse> Items, decimal Subtotal);
    sealed record CartItemResponse(Guid Id, int Quantity);
    sealed record ProfileResponse(string Email, string PhoneNumber);
}
