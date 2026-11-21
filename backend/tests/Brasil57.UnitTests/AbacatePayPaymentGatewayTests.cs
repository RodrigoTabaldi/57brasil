using System.Net;
using System.Text.Json;
using Brasil57.Application;
using Brasil57.Infrastructure;
using Xunit;

namespace Brasil57.UnitTests;

public sealed class AbacatePayPaymentGatewayTests
{
    [Fact]
    public async Task Creates_pix_with_server_amount_in_cents_and_order_reference()
    {
        var orderId = Guid.NewGuid();
        var client = new HttpClient(new Handler(async request => {
            Assert.Equal("api.abacatepay.com", request.RequestUri!.Host);
            Assert.Equal("/v2/transparents/create", request.RequestUri.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("PIX", body.RootElement.GetProperty("method").GetString());
            var data = body.RootElement.GetProperty("data");
            Assert.Equal(12345, data.GetProperty("amount").GetInt64());
            Assert.Equal(orderId.ToString("N"), data.GetProperty("externalId").GetString());
            return Json("{\"data\":{\"id\":\"pix_char_123\",\"amount\":12345,\"status\":\"PENDING\",\"devMode\":true,\"brCode\":\"000201123\",\"brCodeBase64\":\"data:image/png;base64,AAAA\"}}");
        }));
        var gateway = new AbacatePayPaymentGateway(client, "sandbox-key", true);
        var result = await gateway.CreateChargeAsync(new(orderId, 123.45m, "BRL", PaymentMethod.Pix, "request-1"), CancellationToken.None);
        Assert.Equal("pix_char_123", result.ProviderPaymentId);
        Assert.Equal(123.45m, result.Amount);
        Assert.Equal("000201123", result.PixCode);
    }

    [Fact]
    public async Task Reconciles_only_exact_provider_id_and_amount()
    {
        var client = new HttpClient(new Handler(request => {
            Assert.Equal("/v2/transparents/list", request.RequestUri!.AbsolutePath);
            Assert.Contains("id=pix_char_123", request.RequestUri.Query);
            return Task.FromResult(Json("{\"data\":[{\"id\":\"pix_char_123\",\"amount\":12345,\"status\":\"PAID\",\"devMode\":true}]}"));
        }));
        var result = await new AbacatePayPaymentGateway(client, "sandbox-key", true).GetChargeAsync("pix_char_123", CancellationToken.None);
        Assert.Equal(GatewayPaymentStatus.Paid, result.Status);
        Assert.Equal(123.45m, result.Amount);
    }

    [Fact]
    public async Task Rejects_a_live_charge_in_sandbox()
    {
        var client = new HttpClient(new Handler(_ => Task.FromResult(Json("{\"data\":{\"id\":\"pix_char_123\",\"amount\":1000,\"status\":\"PENDING\",\"devMode\":false,\"brCode\":\"000201123\",\"brCodeBase64\":\"data:image/png;base64,AAAA\"}}"))));
        var gateway = new AbacatePayPaymentGateway(client, "sandbox-key", true);
        await Assert.ThrowsAsync<JsonException>(() => gateway.CreateChargeAsync(new(Guid.NewGuid(), 10m, "BRL", PaymentMethod.Pix, "request-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_a_sandbox_charge_in_production()
    {
        var client = new HttpClient(new Handler(_ => Task.FromResult(Json("{\"data\":{\"id\":\"pix_char_123\",\"amount\":1000,\"status\":\"PENDING\",\"devMode\":true,\"brCode\":\"000201123\",\"brCodeBase64\":\"data:image/png;base64,AAAA\"}}"))));
        var gateway = new AbacatePayPaymentGateway(client, "production-key", false);
        await Assert.ThrowsAsync<JsonException>(() => gateway.CreateChargeAsync(new(Guid.NewGuid(), 10m, "BRL", PaymentMethod.Pix, "request-1"), CancellationToken.None));
    }

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value) };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
