using Brasil57.Domain;

namespace Brasil57.UnitTests;

public sealed class ProductPublicationPolicyTests
{
    [Theory]
    [InlineData(0, "marca-real", 1, 1, 1, 1)]
    [InlineData(10, "marca-a-confirmar", 1, 1, 1, 1)]
    [InlineData(10, "marca-real", 0, 1, 1, 1)]
    [InlineData(10, "marca-real", 1, 0, 1, 1)]
    [InlineData(10, "marca-real", 1, 1, 0, 1)]
    [InlineData(10, "marca-real", 1, 1, 1, 0)]
    public void Sale_publication_requires_complete_verified_fields(decimal price, string brandSlug,
        decimal weight, decimal height, decimal width, decimal length)
    {
        var product = Product(price, weight, height, width, length);
        Assert.False(ProductPublicationPolicy.CanPublish(product, brandSlug));
    }

    [Fact]
    public void Complete_product_can_be_published()
    {
        Assert.True(ProductPublicationPolicy.CanPublish(Product(10, 1, 1, 1, 1), "marca-real"));
    }

    private static Product Product(decimal price, decimal weight, decimal height, decimal width, decimal length) =>
        new() { Name = "Produto", Slug = "produto", Sku = "SKU", Price = price,
            WeightKg = weight, HeightCm = height, WidthCm = width, LengthCm = length };
}
