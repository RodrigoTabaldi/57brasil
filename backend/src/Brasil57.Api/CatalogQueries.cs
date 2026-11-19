using Brasil57.Domain;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Api;

internal static class CatalogQueries
{
    internal static bool Valid(string? q, decimal? minPrice, decimal? maxPrice) =>
        q?.Length is not > 120 && minPrice is not < 0 && maxPrice is not < 0 &&
        (minPrice is null || maxPrice is null || maxPrice >= minPrice);

    internal static IQueryable<Product> Filter(StoreDbContext db, string? q, string? category, string? brand, string? vehicleType,
        Guid? vehicleVersion, int? year, decimal? minPrice, decimal? maxPrice, bool? availableOnly)
    {
        var query = db.Products.AsNoTracking().Where(x => x.Published || x.CatalogVisible);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%") || EF.Functions.ILike(x.Sku, $"%{search}%") ||
                (x.ManufacturerCode != null && EF.Functions.ILike(x.ManufacturerCode, $"%{search}%")) ||
                EF.Functions.ILike(x.Brand.Name, $"%{search}%") || EF.Functions.ILike(x.Category.Name, $"%{search}%"));
        }
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            var legacyCategories = vehicleType switch
            {
                "carro" => new[] { "carros", "caminhonetes-e-suvs" },
                "moto" => new[] { "motocicletas" },
                "caminhao" => new[] { "caminhoes-e-veiculos-pesados" },
                _ => []
            };
            query = query.Where(x => x.VehicleFamilies.Contains(vehicleType) || legacyCategories.Contains(x.Category.Slug));
        }
        if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(x => x.Brand.Slug == brand);
        if (vehicleVersion is not null) query = query.Where(x => db.ProductApplications.Any(a => a.ProductId == x.Id && a.VehicleVersionId == vehicleVersion && (year == null || (a.StartYear <= year && a.EndYear >= year))));
        if (minPrice is not null) query = query.Where(x => x.Published && x.Price >= minPrice);
        if (maxPrice is not null) query = query.Where(x => x.Published && x.Price <= maxPrice);
        if (availableOnly == true) query = query.Where(x => x.Published && x.Inventory.OnHand > x.Inventory.Reserved);
        return query;
    }
}
