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
    public async Task Vehicle_application_is_hidden_until_its_document_is_recorded()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var suffix = Guid.NewGuid().ToString("N");
        var vehicle = new VehicleVersion { Name = "Versão teste", StartYear = 2020, EndYear = 2022,
            Model = new VehicleModel { Name = "Modelo teste", Make = new VehicleMake { Name = $"Marca teste {suffix}" } } };
        var product = new Product { Name = "Peça teste", Slug = $"application-{suffix}", Sku = $"APP-{suffix}",
            Category = new Category { Name = "Teste", Slug = $"category-{suffix}" },
            Brand = new Brand { Name = "Teste", Slug = $"brand-{suffix}" }, Price = 10m, Published = true,
            PriceSourceReference = "Tabela preço 4", StockSourceReference = "Nota fiscal 123",
            Inventory = new InventoryItem { OnHand = 1 } };
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.Add(product);
            setup.ProductApplications.Add(new ProductApplication { Product = product, VehicleVersion = vehicle, StartYear = 2020, EndYear = 2022 });
            await setup.SaveChangesAsync();
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-application-{suffix}"));
        });
        using var client = factory.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<List<object>>($"/api/v1/products/{product.Slug}/applications"))!);
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleVersion={vehicle.Id}&category={product.Category.Slug}"))!.Total);

        await using (var db = new StoreDbContext(options))
            await db.Products.Where(x => x.Id == product.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.CompatibilitySourceReference, "Catálogo fabricante 5"));
        Assert.Single((await client.GetFromJsonAsync<List<object>>($"/api/v1/products/{product.Slug}/applications"))!);
        Assert.Equal(1, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleVersion={vehicle.Id}&category={product.Category.Slug}"))!.Total);
    }

    [Fact]
    public async Task Price_stock_and_facets_use_the_same_database_filters()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var brand = new Brand { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var available = new Product { Name = "Peça disponível", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 10m, Published = true, Inventory = new InventoryItem { OnHand = 1 } };
        var unavailable = new Product { Name = "Peça indisponível", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 20m, Published = true, Inventory = new InventoryItem { OnHand = 0 } };
        var draft = new Product { Name = "Peça em cadastro", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 25m, CatalogVisible = true, Inventory = new InventoryItem { OnHand = 0 } };
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.AddRange(available, unavailable, draft);
            await setup.SaveChangesAsync();
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
        Assert.Equal(3, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={category.Slug}"))!.Total);
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={category.Slug}&approvedOnly=true"))!.Total);
        var approvedFacets = await client.GetFromJsonAsync<Facets>($"/api/v1/products/facets?category={category.Slug}&approvedOnly=true");
        Assert.Equal(2, Assert.Single(approvedFacets!.Categories).Count);
        Assert.Equal(2, Assert.Single(approvedFacets.Brands).Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/products?minPrice=20&maxPrice=10")).StatusCode);
    }

    private sealed record ProductPage(List<ProductItem> Items, int Total);
    private sealed record ProductItem(Guid Id);
    private sealed record Facets(List<Facet> Categories, List<Facet> Brands);
    private sealed record Facet(string Slug, int Count);
}
