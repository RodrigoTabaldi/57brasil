using System.Net.Http.Json;
using System.Text.Json;
using Brasil57.Api;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class PartTypeCatalogTests
{
    [Fact]
    public async Task Seed_is_unique_searchable_and_preserves_admin_edits()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            await PartTypeSeed.EnsureAsync(db);
            Assert.Equal(100, PartTypeSeed.InitialTypes.Count);
            Assert.Equal(100, PartTypeSeed.InitialTypes.Select(x => x.Id).Distinct().Count());
            Assert.Equal(100, PartTypeSeed.InitialTypes.Select(x => x.Slug).Distinct().Count());
            Assert.Equal(10, PartTypeSeed.InitialTypes.Select(x => x.GroupSlug).Distinct().Count());
            Assert.All(PartTypeSeed.InitialTypes, x => Assert.Equal(PartTypeSeed.AwaitingSupplier, x.Status));
            var id = PartTypeSeed.InitialTypes[0].Id;
            await db.PartTypes.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(p => p.Name, "Nome editado pelo administrador"));
            await PartTypeSeed.EnsureAsync(db);
            Assert.Equal("Nome editado pelo administrador", await db.PartTypes.Where(x => x.Id == id).Select(x => x.Name).SingleAsync());
            var ids = PartTypeSeed.InitialTypes.Select(x => x.Id).ToArray();
            Assert.Equal(100, await db.PartTypes.CountAsync(x => ids.Contains(x.Id)));
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), $"brasil57-part-types-{Guid.NewGuid():N}"));
        });
        using var client = factory.CreateClient();
        var first = await client.GetFromJsonAsync<Page>("/api/v1/part-types");
        Assert.NotNull(first);
        Assert.True(first.Total >= 100);
        Assert.Equal(24, first.Items.Count);
        var pages = new List<Item>();
        for (var page = 1; page <= 5; page++)
            pages.AddRange((await client.GetFromJsonAsync<Page>($"/api/v1/part-types?page={page}"))!.Items);
        Assert.Equal(100, pages.Count);
        Assert.Equal(100, pages.Select(x => x.Id).Distinct().Count());
        foreach (var item in pages)
            Assert.Equal(item.Id, (await client.GetFromJsonAsync<Item>($"/api/v1/part-types/{item.Slug}"))!.Id);
        var search = await client.GetFromJsonAsync<Page>("/api/v1/part-types?q=homocin%C3%A9tica");
        Assert.Equal(2, search!.Total);
        Assert.Equal(2, (await client.GetFromJsonAsync<Page>("/api/v1/part-types?q=homocinetica"))!.Total);
        var group = await client.GetFromJsonAsync<Page>("/api/v1/part-types?group=filtros");
        Assert.Equal(10, group!.Total);
        Assert.All(group.Items, x => Assert.Equal("filtros", x.GroupSlug));
        var fourth = await client.GetFromJsonAsync<Page>("/api/v1/part-types?page=4");
        Assert.True(fourth!.Items.Count >= 4);
        var detail = await client.GetFromJsonAsync<Item>("/api/v1/part-types/filtro-de-oleo");
        Assert.Equal("Nome editado pelo administrador", detail!.Name);
        Assert.NotNull(detail.ImageUrl);
        using var response = JsonDocument.Parse(await client.GetStringAsync("/api/v1/part-types/filtro-de-oleo"));
        var fields = response.RootElement.EnumerateObject().Select(x => x.Name).ToHashSet();
        Assert.DoesNotContain("sku", fields);
        Assert.DoesNotContain("price", fields);
        Assert.DoesNotContain("available", fields);
        Assert.DoesNotContain("vehicleFamilies", fields);
        Assert.DoesNotContain("status", fields);
    }

    private sealed record Page(List<Item> Items, int Total);
    private sealed record Item(Guid Id, string Name, string Slug, string Group, string GroupSlug, string ImageUrl);
}
