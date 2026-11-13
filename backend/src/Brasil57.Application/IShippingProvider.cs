namespace Brasil57.Application;
public sealed record ShippingProduct(string Sku, decimal WidthCm, decimal HeightCm, decimal LengthCm, decimal WeightKg, decimal UnitPrice, int Quantity);
public sealed record ShippingOption(string ServiceId, string Name, decimal Price, int DeliveryDays);
public interface IShippingProvider
{
    Task<IReadOnlyList<ShippingOption>> QuoteAsync(string destinationCep, IReadOnlyList<ShippingProduct> products, CancellationToken cancellationToken);
}
