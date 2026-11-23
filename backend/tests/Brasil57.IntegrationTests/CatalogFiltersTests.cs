using System.Net;
using System.Net.Http.Json;
using Brasil57.Api;
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
    public async Task Seed_classifies_products_and_catalog_filters_parent_and_subcategory()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            await CatalogSeed.EnsureDepartmentsAsync(db);
            await CatalogSeed.EnsureProductsAsync(db);
            await CatalogSeed.EnsureProductsAsync(db);
            var product = await db.Products.Include(x => x.Category).ThenInclude(x => x.Parent).SingleAsync(x => x.Sku == "57BR-0010");
            Assert.Equal("filtros-filtro-de-oleo", product.Category.Slug);
            Assert.Equal("filtros", product.Category.Parent!.Slug);
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-seed-{Guid.NewGuid():N}"));
        });
        using var client = factory.CreateClient();
        var parent = await client.GetFromJsonAsync<ProductPage>("/api/v1/catalog-items?group=filtros");
        var child = await client.GetFromJsonAsync<ProductPage>("/api/v1/catalog-items?group=filtros-filtro-de-oleo");
        Assert.True(parent!.Total >= 33);
        Assert.Equal(9, child!.Total);
    }

    [Fact]
    public async Task Catalog_vehicle_filter_requires_recorded_source_and_combines_category()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category { Name = "Freios teste", Slug = $"freios-{suffix}" };
        var brand = new Brand { Name = "Marca teste", Slug = $"marca-{suffix}" };
        var verified = new Product { Name = "Freio confirmado", Slug = $"confirmado-{suffix}", Sku = $"57BR-{suffix}", Category = category, Brand = brand, CatalogVisible = true, VehicleFamilies = ["moto", "carro"], CompatibilitySourceReference = "Catálogo fabricante", Inventory = new InventoryItem() };
        var unverified = new Product { Name = "Freio pendente", Slug = $"pendente-{suffix}", Sku = $"57BR-P-{suffix}", Category = category, Brand = brand, CatalogVisible = true, VehicleFamilies = ["moto"], Inventory = new InventoryItem() };
        await using (var db = new StoreDbContext(options)) { await db.Database.MigrateAsync(); db.Products.AddRange(verified, unverified); await db.SaveChangesAsync(); }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-catalog-vehicle-{suffix}"));
        });
        using var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<ProductPage>($"/api/v1/catalog-items?group={category.Slug}&vehicleType=moto");
        Assert.Equal(verified.Id, Assert.Single(result!.Items).Id);
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/catalog-items?group={category.Slug}&vehicleType=caminhao"))!.Total);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/catalog-items?vehicleType=onibus")).StatusCode);
    }

    [Fact]
    public async Task Parent_category_description_and_sort_are_applied_in_database()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var suffix = Guid.NewGuid().ToString("N");
        var parent = new Category { Name = "Freios", Slug = $"freios-{suffix}" };
        var child = new Category { Name = "Pastilhas", Slug = $"pastilhas-{suffix}", Parent = parent };
        var brand = new Brand { Name = "Marca teste", Slug = $"marca-{suffix}" };
        var products = new[] {
            new Product { Name = "Primeiro", Slug = $"primeiro-{suffix}", Sku = $"A-{suffix}", Category = child, Brand = brand, Price = 20, Published = true, Description = "termo exclusivo", Inventory = new InventoryItem { OnHand = 1 } },
            new Product { Name = "Segundo", Slug = $"segundo-{suffix}", Sku = $"B-{suffix}", Category = child, Brand = brand, Price = 10, Published = true, Description = "termo exclusivo", Inventory = new InventoryItem { OnHand = 1 } }
        };
        await using (var db = new StoreDbContext(options)) { await db.Database.MigrateAsync(); db.Products.AddRange(products); await db.SaveChangesAsync(); }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-search-{suffix}"));
        });
        using var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={parent.Slug}&q=exclusivo&sort=price_asc");
        Assert.Equal(2, result!.Total);
        Assert.Equal(products[1].Id, result.Items[0].Id);
        Assert.Equal(products[0].Id, result.Items[1].Id);
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={parent.Slug}&q=%25"))!.Total);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/products?sort=unknown")).StatusCode);
    }

    [Fact]
    public async Task Vehicle_family_filter_excludes_unpublished_drafts_and_handles_empty_results()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category { Name = "Categoria de teste", Slug = $"family-test-{suffix}" };
        var brand = new Brand { Name = "Marca de teste", Slug = $"family-brand-{suffix}" };
        var families = new[] { "carro", "moto", "caminhao" };
        var products = families.Select(family => new Product
        {
            Name = $"Cadastro {family}", Slug = $"family-{family}-{suffix}", Sku = $"FAMILY-{family}-{suffix}",
            Category = category, Brand = brand, Published = false, CatalogVisible = true,
            VehicleFamilies = [family], Inventory = new InventoryItem { OnHand = 0 },
            Images = [new ProductImage { Url = $"/demo-products/{family}.png", AltText = "Imagem de referência" }]
        }).ToArray();
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.AddRange(products);
            await setup.SaveChangesAsync();
        }

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-family-{suffix}"));
        });
        using var client = factory.CreateClient();

        foreach (var family in families)
        {
            var result = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleType={family}&category={category.Slug}");
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        var noMatch = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleType=onibus&category={category.Slug}");
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Items);
        Assert.Equal(0, noMatch.Total);
        var draftsOnly = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleType=moto&category={category.Slug}&approvedOnly=true");
        Assert.NotNull(draftsOnly);
        Assert.Empty(draftsOnly.Items);
        Assert.Equal(0, draftsOnly.Total);
    }

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
        Assert.Equal(1, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={category.Slug}"))!.Total);
        Assert.Equal(1, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={category.Slug}&approvedOnly=true"))!.Total);
        Assert.Equal(1, (await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?category={category.Slug}&approvedOnly=false"))!.Total);
        var approvedFacets = await client.GetFromJsonAsync<Facets>($"/api/v1/products/facets?category={category.Slug}&approvedOnly=true");
        Assert.Equal(1, Assert.Single(approvedFacets!.Categories).Count);
        Assert.Equal(1, Assert.Single(approvedFacets.Brands).Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/products?minPrice=20&maxPrice=10")).StatusCode);
    }

    [Fact]
    public async Task Illustrative_vehicle_family_filter_excludes_unpublished_drafts_and_handles_empty_results()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category { Name = "Teste ilustrativo", Slug = $"demo-{suffix}" };
        var brand = new Brand { Name = "Marca pendente", Slug = $"demo-{suffix}" };
        var products = new[]
        {
            new Product { Name = "Peça carro", Slug = $"demo-carro-{suffix}", Sku = $"DEMO-CAR-{suffix}", Category = category, Brand = brand, CatalogVisible = true, VehicleFamilies = ["carro"], Inventory = new InventoryItem { OnHand = 0 } },
            new Product { Name = "Peça moto", Slug = $"demo-moto-{suffix}", Sku = $"DEMO-MOTO-{suffix}", Category = category, Brand = brand, CatalogVisible = true, VehicleFamilies = ["moto"], Inventory = new InventoryItem { OnHand = 0 } },
            new Product { Name = "Peça caminhão", Slug = $"demo-caminhao-{suffix}", Sku = $"DEMO-TRUCK-{suffix}", Category = category, Brand = brand, CatalogVisible = true, VehicleFamilies = ["caminhao"], Inventory = new InventoryItem { OnHand = 0 } },
        };
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.AddRange(products);
            var motorcycleCategory = await setup.Categories.SingleOrDefaultAsync(x => x.Slug == "motocicletas");
            if (motorcycleCategory is null)
            {
                motorcycleCategory = new Category { Name = "Motocicletas", Slug = "motocicletas" };
                setup.Categories.Add(motorcycleCategory);
            }
            setup.Products.Add(new Product
            {
                Name = "Categoria antiga sem família",
                Slug = $"demo-legacy-moto-{suffix}",
                Sku = $"DEMO-LEGACY-{suffix}",
                Category = motorcycleCategory,
                Brand = brand,
                CatalogVisible = true,
                Inventory = new InventoryItem { OnHand = 0 }
            });
            await setup.SaveChangesAsync();
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-demo-family-{suffix}"));
        });
        using var client = factory.CreateClient();

        foreach (var family in new[] { "carro", "moto", "caminhao" })
        {
            var result = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleType={family}&brand={brand.Slug}");
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        var empty = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?vehicleType=moto&q=sem-correspondencia-{suffix}");
        Assert.NotNull(empty);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.Total);
    }

    private sealed record ProductPage(List<ProductItem> Items, int Total);
    private sealed record ProductItem(Guid Id, string? ImageUrl);
    private sealed record Facets(List<Facet> Categories, List<Facet> Brands);
    private sealed record Facet(string Slug, int Count);
}
