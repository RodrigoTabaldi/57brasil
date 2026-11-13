using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Brasil57.Application;

namespace Brasil57.Infrastructure;
public sealed class MelhorEnvioShippingProvider(HttpClient client, string token, string originCep, string userAgent, bool sandbox) : IShippingProvider
{
    public async Task<IReadOnlyList<ShippingOption>> QuoteAsync(string destinationCep, IReadOnlyList<ShippingProduct> products, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, sandbox ? "https://sandbox.melhorenvio.com.br/api/v2/me/shipment/calculate" : "https://www.melhorenvio.com.br/api/v2/me/shipment/calculate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd(userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(new {
            from = new { postal_code = originCep }, to = new { postal_code = destinationCep },
            products = products.Select(p => new { id = p.Sku, width = p.WidthCm, height = p.HeightCm, length = p.LengthCm, weight = p.WeightKg, insurance_value = p.UnitPrice, quantity = p.Quantity }).ToArray()
        });
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Falha na cotação do frete ({(int)response.StatusCode}).");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (json.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Resposta de frete inválida.");
        var options = new List<ShippingOption>();
        foreach (var item in json.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var id) || !item.TryGetProperty("name", out var name) || !item.TryGetProperty("custom_price", out var price) || !item.TryGetProperty("custom_delivery_time", out var days)) continue;
            var priceText = price.ValueKind == JsonValueKind.String ? price.GetString() : price.GetRawText();
            if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0 || !days.TryGetInt32(out var deliveryDays) || deliveryDays < 0) continue;
            var serviceId = id.ValueKind == JsonValueKind.String ? id.GetString() : id.GetRawText();
            if (!string.IsNullOrWhiteSpace(serviceId) && !string.IsNullOrWhiteSpace(name.GetString())) options.Add(new ShippingOption(serviceId, name.GetString()!, value, deliveryDays));
        }
        return options;
    }
}
