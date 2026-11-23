using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Brasil57.Api;

public static class CatalogSeed
{
    public static async Task EnsureProductsAsync(StoreDbContext db)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "catalog-products.json");
        var products = JsonSerializer.Deserialize<List<InitialProduct>>(await File.ReadAllTextAsync(path), JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Catálogo inicial inválido.");
        var existing = (await db.Products.Select(x => x.Sku).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categories = await db.Categories.ToDictionaryAsync(x => x.Slug);
        var brands = await db.Brands.ToDictionaryAsync(x => x.Slug);
        foreach (var item in products)
        {
            if (existing.Contains(item.Sku)) continue;
            var categorySlug = PartTypeSeed.Slug(item.Category);
            if (!categories.TryGetValue(categorySlug, out var category))
                categories[categorySlug] = category = new Category { Name = item.Category, Slug = categorySlug };
            var brandSlug = PartTypeSeed.Slug(item.Brand);
            if (!brands.TryGetValue(brandSlug, out var brand))
                brands[brandSlug] = brand = new Brand { Name = item.Brand, Slug = brandSlug };
            var image = item.Subcategory switch {
                "Filtro de óleo" => "filtro-de-oleo", "Filtro de ar do motor" => "filtro-de-ar", "Filtro de cabine" => "filtro-de-cabine", "Filtro de combustível" => "filtro-de-combustivel",
                "Pastilha de freio" => "pastilha-de-freio", "Disco de freio" => "disco-de-freio", "Vela de ignição" => "vela-de-ignicao", "Bateria de partida" => "bateria", "Correia ou kit" => "correia-dentada",
                "Amortecedor ou kit" => "amortecedor", "Terminal de direção" => "terminal-de-direcao", "Palheta limpa-vidro" => "palheta", "Pneu passeio" => "pneu",
                _ => null
            };
            db.Products.Add(new Product { Name = item.Name, Slug = item.Sku.ToLowerInvariant(), Sku = item.Sku,
                ManufacturerCode = item.ManufacturerCode, Category = category, Brand = brand, Price = item.ReferencePrice,
                Published = false, CatalogVisible = true, ShortDescription = "Consulte a aplicação no catálogo do fabricante antes da compra.",
                Description = $"{item.Subcategory}. Unidade: {item.Unit}. Consulte o fabricante para confirmar a aplicação.",
                Inventory = new InventoryItem { OnHand = 0 }, Images = [new ProductImage { Url = image is null ? "/catalog-oil.svg" : $"/demo-products/{image}.png", AltText = $"Imagem ilustrativa de {item.Subcategory}" }] });
        }
        await db.SaveChangesAsync();
    }

    private sealed record InitialProduct(string Sku, string Category, string Subcategory, string Brand, string ManufacturerCode, string Name, decimal ReferencePrice, string Unit);
    private static readonly (string Name, string Slug)[] Departments =
    [
        ("Carros", "carros"), ("Caminhonetes e SUVs", "caminhonetes-e-suvs"),
        ("Caminhões e veículos pesados", "caminhoes-e-veiculos-pesados"),
        ("Motocicletas", "motocicletas"), ("Motor", "motor"),
        ("Suspensão e direção", "suspensao-e-direcao"), ("Freios", "freios"),
        ("Pneus e rodas", "pneus-e-rodas"), ("Sistema elétrico", "sistema-eletrico"),
        ("Iluminação", "iluminacao"), ("Transmissão e câmbio", "transmissao-e-cambio"),
        ("Filtros", "filtros"), ("Ignição", "ignicao"),
        ("Arrefecimento", "arrefecimento"), ("Escapamento", "escapamento"),
        ("Carroceria", "carroceria"), ("Acessórios automotivos", "acessorios-automotivos"),
        ("Áudio e vídeo", "audio-e-video"), ("Manutenção e ferramentas", "manutencao-e-ferramentas"),
        ("Lubrificantes e fluidos", "lubrificantes-e-fluidos"), ("Kits automotivos", "kits-automotivos")
    ];

    public static async Task EnsureDepartmentsAsync(StoreDbContext db)
    {
        var existing = await db.Categories.Select(x => x.Slug).ToListAsync();
        var slugs = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, slug) in Departments)
            if (!slugs.Contains(slug)) db.Categories.Add(new Category { Name = name, Slug = slug });
        await db.SaveChangesAsync();
    }

    public static async Task EnsureCatalogDraftsAsync(StoreDbContext db)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "catalog-drafts.json");
        var drafts = JsonSerializer.Deserialize<List<CatalogDraft>>(await File.ReadAllTextAsync(path), JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Catálogo inicial inválido.");
        var existingProducts = await db.Products.Where(x => x.CatalogVisible && !x.Published).ToDictionaryAsync(x => x.Slug);
        var categories = await db.Categories.ToDictionaryAsync(x => x.Name, x => x.Id);
        var brand = await db.Brands.FirstOrDefaultAsync(x => x.Slug == "marca-a-confirmar");
        if (brand is null) { brand = new Brand { Name = "Marca a confirmar", Slug = "marca-a-confirmar" }; db.Brands.Add(brand); }
        foreach (var draft in drafts)
        {
            if (existingProducts.TryGetValue(draft.Slug, out var existing))
            {
                if (existing.Sku == $"CAT-{draft.Slug.ToUpperInvariant()}") existing.VehicleFamilies = draft.VehicleFamilies;
                continue;
            }
            if (await db.Products.AnyAsync(x => x.Slug == draft.Slug)) continue;
            if (!categories.TryGetValue(draft.Group, out var categoryId)) throw new InvalidOperationException($"Categoria ausente: {draft.Group}");
            db.Products.Add(new Product
            {
                Name = draft.Name, Slug = draft.Slug, Sku = $"CAT-{draft.Slug.ToUpperInvariant()}",
                ShortDescription = draft.Description, Description = draft.Description,
                CategoryId = categoryId, Brand = brand, Price = 0, Published = false, CatalogVisible = true, VehicleFamilies = draft.VehicleFamilies,
                Inventory = new InventoryItem { OnHand = 0 },
                Images = [new ProductImage { Url = $"/demo-products/{draft.Slug}.png", AltText = $"Imagem de referência de {draft.Name}", SortOrder = 0 }]
            });
        }
        await db.SaveChangesAsync();
    }

    private sealed record CatalogDraft(string Slug, string Name, string Group, string Description, string[] VehicleFamilies);
}
