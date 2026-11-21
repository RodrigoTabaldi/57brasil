namespace Brasil57.Domain;

public static class ProductPublicationPolicy
{
    public static bool CanPublish(Product product, string brandSlug) =>
        product.Price > 0 && brandSlug != "marca-a-confirmar" &&
        product.WeightKg is > 0 && product.HeightCm is > 0 &&
        product.WidthCm is > 0 && product.LengthCm is > 0;
}
