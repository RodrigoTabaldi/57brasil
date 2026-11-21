using System.Net;
using System.Net.Http.Json;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class CatalogFiltersTests
{
    [Fact]
    public async Task Price_stock_and_facets_use_the_same_database_filters()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var brand = new Brand { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var available = new Product { Name = "Peça disponível", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 10m, Published = true, CommercialSource = "Integration test fixture", CommercialVerifiedOn = new DateOnly(2026, 11, 18), Inventory = new InventoryItem { OnHand = 1 } };
        var unavailable = new Product { Name = "Peça indisponível", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 20m, Published = true, CommercialSource = "Integration test fixture", CommercialVerifiedOn = new DateOnly(2026, 11, 18), Inventory = new InventoryItem { OnHand = 0 } };
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.AddRange(available, unavailable);
            await setup.SaveChangesAsync();
        }
        await using (var invalid = new StoreDbContext(options))
        {
            invalid.Products.Add(new Product { Name = "Sem fonte", Slug = $"sem-fonte-{Guid.NewGuid():N}", Sku = $"SEM-{Guid.NewGuid():N}", CategoryId = category.Id, BrandId = brand.Id, Price = 10m, Published = true, Inventory = new InventoryItem { OnHand = 1 } });
            await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync());
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), "brasil57-catalog-test-keys"));
        });
        using var client = factory.CreateClient();
        var query = $"category={category.Slug}&minPrice=0&maxPrice=15&availableOnly=true";
        var products = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?{query}");
        Assert.NotNull(products);
        Assert.Equal(1, products.Total);
        Assert.Equal(available.Id, Assert.Single(products.Items).Id);
        var facets = await client.GetFromJsonAsync<Facets>($"/api/v1/products/facets?{query}");
        Assert.NotNull(facets);
        Assert.Equal(category.Slug, Assert.Single(facets.Categories).Slug);
        Assert.Equal(1, Assert.Single(facets.Brands).Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/products?minPrice=20&maxPrice=10")).StatusCode);
    }

    private sealed record ProductPage(List<ProductItem> Items, int Total);
    private sealed record ProductItem(Guid Id);
    private sealed record Facets(List<Facet> Categories, List<Facet> Brands);
    private sealed record Facet(string Slug, int Count);
}
