namespace Brasil57.Domain;

public static class ProductPublicationPolicy
{
    public static bool CanPublish(Product product, string brandSlug, bool hasApplications = false) =>
        product.Price > 0 && brandSlug != "marca-a-confirmar" &&
        ValidReference(product.PriceSourceReference) && ValidReference(product.StockSourceReference) &&
        ((product.VehicleFamilies.Length == 0 && !hasApplications) || ValidReference(product.CompatibilitySourceReference)) &&
        product.WeightKg is > 0 && product.HeightCm is > 0 &&
        product.WidthCm is > 0 && product.LengthCm is > 0;

    public static bool ValidReference(string? reference) =>
        !string.IsNullOrWhiteSpace(reference) && reference.Trim().Length is >= 5 and <= 200;
}
