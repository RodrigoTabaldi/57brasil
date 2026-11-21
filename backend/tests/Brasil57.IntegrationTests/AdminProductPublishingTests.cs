using System.Net;
using System.Net.Http.Json;
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
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, "/api/v1/admin/session", new { email = adminEmail, password = "StrongPassword1!" })).StatusCode);
        var sku = $"PUBLISH-{Guid.NewGuid():N}";
        object Input(Guid brandId) => new { name = "Peça verificada", slug = sku.ToLowerInvariant(), sku, categoryId, brandId,
            price = 10m, stock = 1, published = true, weightKg = 1m, heightCm = 1m, widthCm = 1m, lengthCm = 1m };
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Post, "/api/v1/admin/products", Input(placeholderBrandId))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/v1/admin/products", Input(realBrandId))).StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var product = await verifyDb.Products.AsNoTracking().SingleAsync(x => x.Sku == sku);
        var path = $"/api/v1/admin/products/{product.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Patch, path, new { price = 0m })).StatusCode);
        Assert.Equal(10m, await verifyDb.Products.Where(x => x.Id == product.Id).Select(x => x.Price).SingleAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Patch, path, new { price = 0m, published = false })).StatusCode);
        Assert.False(await verifyDb.Products.Where(x => x.Id == product.Id).Select(x => x.Published).SingleAsync());
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object body)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Origin", "http://localhost");
        return await client.SendAsync(request);
    }
}
