using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Brasil57.Application;

namespace Brasil57.Infrastructure;

public sealed class AbacatePayPaymentGateway(HttpClient client, string apiKey, bool sandbox) : IPaymentGateway
{
    private const string BaseUrl = "https://api.abacatepay.com/v2";
    public string Provider => "abacatepay";
    public IReadOnlyCollection<PaymentMethod> AvailableMethods => [PaymentMethod.Pix];

    public async Task<ChargeResult> CreateChargeAsync(CreateChargeRequest request, CancellationToken cancellationToken)
    {
        if (request.Method != PaymentMethod.Pix || request.Currency != "BRL" || request.Amount < 1 ||
            request.Amount * 100 != decimal.Truncate(request.Amount * 100))
            throw new ArgumentException("Cobrança Pix inválida.");
        var cents = checked((long)(request.Amount * 100));
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/transparents/create");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Content = JsonContent.Create(new {
            method = "PIX",
            data = new { amount = cents, expiresIn = 3600, externalId = request.OrderId.ToString("N"), description = $"Pedido {request.OrderId:N}" }
        });
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Falha ao criar cobrança Pix ({(int)response.StatusCode}).");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var charge = ParseCharge(json.RootElement.GetProperty("data"), requireCode: true);
        if (charge.Amount != request.Amount) throw new JsonException("Valor da cobrança Pix divergente.");
        return charge;
    }

    public async Task<ChargeResult> GetChargeAsync(string providerPaymentId, CancellationToken cancellationToken)
    {
        if (!ValidId(providerPaymentId)) throw new ArgumentException("Identificador de cobrança Pix inválido.");
        using var message = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/transparents/list?id={Uri.EscapeDataString(providerPaymentId)}&limit=2");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Falha ao consultar cobrança Pix ({(int)response.StatusCode}).");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var data = json.RootElement.GetProperty("data");
        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() != 1) throw new JsonException("Cobrança Pix não encontrada ou ambígua.");
        var charge = ParseCharge(data[0], requireCode: false);
        if (charge.ProviderPaymentId != providerPaymentId) throw new JsonException("Identificador de cobrança Pix divergente.");
        return charge;
    }

    public Task CancelAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Cancelamento de cobrança Pix não configurado.");
    public Task RefundAsync(string providerPaymentId, decimal amount, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Reembolso Pix não configurado.");
    public bool VerifyWebhook(ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string> headers) => false;
    public GatewayWebhookEvent ParseWebhook(ReadOnlyMemory<byte> payload) => throw new NotSupportedException("Webhook AbacatePay não configurado.");

    private ChargeResult ParseCharge(JsonElement data, bool requireCode)
    {
        var id = data.GetProperty("id").GetString();
        if (!ValidId(id)) throw new JsonException("Identificador de cobrança Pix inválido.");
        if (!data.TryGetProperty("devMode", out var devMode) ||
            devMode.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
            (devMode.ValueKind == JsonValueKind.True) != sandbox)
            throw new JsonException("Ambiente da cobrança Pix divergente da configuração.");
        var cents = data.GetProperty("amount").GetInt64();
        if (cents < 100) throw new JsonException("Valor de cobrança Pix inválido.");
        var status = data.GetProperty("status").GetString() switch {
            "PENDING" => GatewayPaymentStatus.Pending,
            "PAID" => GatewayPaymentStatus.Paid,
            "EXPIRED" => GatewayPaymentStatus.Failed,
            "CANCELLED" => GatewayPaymentStatus.Cancelled,
            "REFUNDED" => GatewayPaymentStatus.Refunded,
            _ => throw new JsonException("Estado de cobrança Pix desconhecido.")
        };
        var code = data.TryGetProperty("brCode", out var brCode) ? brCode.GetString() : null;
        var qr = data.TryGetProperty("brCodeBase64", out var brCodeBase64) ? brCodeBase64.GetString() : null;
        if (requireCode && (string.IsNullOrWhiteSpace(code) || code.Length > 1000 || qr is null || !qr.StartsWith("data:image/png;base64,", StringComparison.Ordinal) || qr.Length > 100000))
            throw new JsonException("Código Pix ausente ou inválido.");
        var expiresAt = data.TryGetProperty("expiresAt", out var expires) && expires.ValueKind == JsonValueKind.String
            ? expires.GetDateTimeOffset() : (DateTimeOffset?)null;
        return new ChargeResult(id!, status, null, expiresAt, cents / 100m, code, qr);
    }

    private static bool ValidId(string? value) => value is { Length: >= 10 and <= 100 } &&
        value.StartsWith("pix_char_", StringComparison.Ordinal) && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
}
