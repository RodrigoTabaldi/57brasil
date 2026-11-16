using System.Net;
using Brasil57.Application;
using Brasil57.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class PaymentWebhookEndpointTests
{
    [Fact]
    public async Task Valid_signature_is_persisted_once_and_invalid_signature_is_rejected()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        await using (var setup = new StoreDbContext(options)) await setup.Database.MigrateAsync();

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Default", connection);
            builder.UseSetting("Payment:EnableWebhook", "true");
            builder.UseSetting("Payment:AccessToken", "test-only");
            builder.UseSetting("Payment:WebhookSecret", "test-only");
            builder.UseSetting("Payment:SiteUrl", "https://shop.example");
            builder.UseSetting("DataProtection:KeyPath", Path.Combine(Path.GetTempPath(), "brasil57-webhook-test-keys"));
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IPaymentProvider>();
                services.AddSingleton<IPaymentProvider, FakeProvider>();
            });
        });
        using var client = factory.CreateClient();
        var paymentId = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000).ToString();
        var requestId = Guid.NewGuid().ToString("N");
        using var invalid = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/mercado-pago/webhooks?type=payment&data.id={paymentId}");
        invalid.Headers.Add("x-request-id", requestId);
        invalid.Headers.Add("x-signature", "bad");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(invalid)).StatusCode);

        for (var i = 0; i < 2; i++)
        {
            using var valid = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/mercado-pago/webhooks?type=payment&data.id={paymentId}");
            valid.Headers.Add("x-request-id", requestId);
            valid.Headers.Add("x-signature", "valid");
            Assert.Equal(HttpStatusCode.Accepted, (await client.SendAsync(valid)).StatusCode);
        }
        await using var verify = new StoreDbContext(options);
        Assert.Equal(1, await verify.PaymentNotifications.CountAsync(x => x.RequestId == requestId && x.PaymentId == paymentId));
    }

    private sealed class FakeProvider : IPaymentProvider
    {
        public bool VerifyWebhook(string signature, string requestId, string paymentId) => signature == "valid";
        public Task<PaymentPreference> CreatePreferenceAsync(Guid orderId, string buyerEmail, IReadOnlyList<PaymentItem> items, decimal shippingPrice, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentResult> GetPaymentAsync(string paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
