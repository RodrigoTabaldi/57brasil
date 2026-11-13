using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Brasil57.Application;

namespace Brasil57.Infrastructure;
public sealed class MercadoPagoPaymentProvider(HttpClient client, string accessToken, string webhookSecret, Uri siteUrl) : IPaymentProvider
{
    private const string ApiBase = "https://api.mercadopago.com";
    public async Task<PaymentPreference> CreatePreferenceAsync(Guid orderId, string buyerEmail, IReadOnlyList<PaymentItem> items, decimal shippingPrice, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || shippingPrice < 0) throw new ArgumentException("Pedido inválido.");
        var paymentItems = items.Select(x => new { id = x.Sku, title = x.Name, quantity = x.Quantity, currency_id = "BRL", unit_price = x.UnitPrice }).ToList();
        if (shippingPrice > 0) paymentItems.Add(new { id = "SHIPPING", title = "Frete", quantity = 1, currency_id = "BRL", unit_price = shippingPrice });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/checkout/preferences");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new {
            items = paymentItems,
            external_reference = orderId.ToString(),
            payer = new { email = buyerEmail },
            back_urls = new { success = new Uri(siteUrl, "/pedido/retorno").ToString(), failure = new Uri(siteUrl, "/pedido/retorno").ToString(), pending = new Uri(siteUrl, "/pedido/retorno").ToString() },
            notification_url = new Uri(siteUrl, "/api/v1/payments/mercado-pago/webhooks").ToString()
        });
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Falha ao criar preferência ({(int)response.StatusCode}).");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var id = json.RootElement.GetProperty("id").GetString();
        var urlText = json.RootElement.GetProperty("init_point").GetString();
        if (string.IsNullOrWhiteSpace(id) || !Uri.TryCreate(urlText, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps || !(url.Host.Equals("mercadopago.com.br", StringComparison.OrdinalIgnoreCase) || url.Host.EndsWith(".mercadopago.com.br", StringComparison.OrdinalIgnoreCase))) throw new JsonException("Resposta de pagamento inválida.");
        return new PaymentPreference(id, url);
    }
    public async Task<PaymentResult> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        if (paymentId.Length is < 1 or > 30 || !paymentId.All(c => c is >= '0' and <= '9')) throw new ArgumentException("Identificador de pagamento inválido.");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/v1/payments/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Falha ao consultar pagamento ({(int)response.StatusCode}).");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = json.RootElement;
        var amount = root.GetProperty("transaction_amount");
        var amountText = amount.ValueKind == JsonValueKind.String ? amount.GetString() : amount.GetRawText();
        if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) throw new JsonException("Valor de pagamento inválido.");
        return new PaymentResult(paymentId, root.GetProperty("external_reference").GetString() ?? "", value, root.GetProperty("status").GetString() ?? "");
    }
    public bool VerifyWebhook(string signature, string requestId, string paymentId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || paymentId.Length is < 1 or > 30 || !paymentId.All(c => c is >= '0' and <= '9')) return false;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in signature.Split(',', StringSplitOptions.TrimEntries)) { var pair = part.Split('=', 2); if (pair.Length != 2 || !fields.TryAdd(pair[0], pair[1])) return false; }
        if (!fields.TryGetValue("ts", out var timestamp) || !fields.TryGetValue("v1", out var received) || received.Length != 64) return false;
        var manifest = $"id:{paymentId};request-id:{requestId};ts:{timestamp};";
        var computed = HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), Encoding.UTF8.GetBytes(manifest));
        try { return CryptographicOperations.FixedTimeEquals(computed, Convert.FromHexString(received)); }
        catch (FormatException) { return false; }
    }
}


