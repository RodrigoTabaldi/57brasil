using System.Net;
using System.Security.Cryptography;
using System.Text;
using Brasil57.Application;
using Brasil57.Infrastructure;
namespace Brasil57.UnitTests;
public class MercadoPagoPaymentProviderTests
{
    [Fact]
    public async Task CreatesHostedPreferenceWithHistoricalServerAmounts()
    {
        string? payload = null;
        var client = new HttpClient(new Handler(async request => {
            Assert.Equal("api.mercadopago.com", request.RequestUri!.Host);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            payload = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{\"id\":\"pref-123\",\"init_point\":\"https://www.mercadopago.com.br/checkout/v1/redirect\"}") };
        }));
        var provider = new MercadoPagoPaymentProvider(client, "test-token", "test-secret", new Uri("https://loja.example"));
        var orderId = Guid.NewGuid();
        var preference = await provider.CreatePreferenceAsync(orderId, "cliente@example.com", [new PaymentItem("SKU", "Peça", 2, 50m)], 10m, CancellationToken.None);
        Assert.Equal("pref-123", preference.ProviderId);
        Assert.Contains(orderId.ToString(), payload);
        Assert.Contains("\"unit_price\":50", payload);
        Assert.Contains("\"unit_price\":10", payload);
        Assert.Contains("https://loja.example/api/v1/payments/mercado-pago/webhooks", payload);
    }
    [Fact]
    public async Task RejectsCheckoutUrlOutsideMercadoPagoDomain()
    {
        var client = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{\"id\":\"pref-123\",\"init_point\":\"https://evilmercadopago.com.br/checkout\"}") })));
        var provider = new MercadoPagoPaymentProvider(client, "test-token", "test-secret", new Uri("https://loja.example"));
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => provider.CreatePreferenceAsync(Guid.NewGuid(), "cliente@example.com", [new PaymentItem("SKU", "Peça", 1, 10m)], 0m, CancellationToken.None));
    }
    [Fact]
    public void ValidatesWebhookSignatureAndRejectsTampering()
    {
        const string manifest = "id:123;request-id:req-1;ts:123456;";
        var digest = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        var provider = new MercadoPagoPaymentProvider(new HttpClient(new Handler(_ => throw new Exception())), "token", "secret", new Uri("https://loja.example"));
        Assert.True(provider.VerifyWebhook($"ts=123456,v1={digest}", "req-1", "123"));
        Assert.False(provider.VerifyWebhook($"ts=123456,v1={digest}", "req-2", "123"));
        Assert.False(provider.VerifyWebhook($"ts=123456,v1={digest},v1={digest}", "req-1", "123"));
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}

