using System.Net;
using System.Text;
using Brasil57.Application;
using Brasil57.Infrastructure;
namespace Brasil57.UnitTests;
public class MelhorEnvioShippingProviderTests
{
    [Fact]
    public async Task UsesRealCustomQuoteAndSendsCatalogMeasurements()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var client = new HttpClient(new Handler(async request => {
            captured = request;
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"id\":1,\"name\":\"Expresso\",\"price\":\"25.00\",\"custom_price\":\"29.90\",\"delivery_time\":3,\"custom_delivery_time\":4}]", Encoding.UTF8, "application/json") };
        }));
        var provider = new MelhorEnvioShippingProvider(client, "test-token", "01001000", "57Brasil (teste@example.com)", true);
        var options = await provider.QuoteAsync("20040002", [new ShippingProduct("SKU-1", 10, 20, 30, 1.5m, 100m, 2)], CancellationToken.None);
        Assert.Single(options);
        Assert.Equal(29.90m, options[0].Price);
        Assert.Equal(4, options[0].DeliveryDays);
        Assert.Equal("1", options[0].ServiceId);
        Assert.Equal("sandbox.melhorenvio.com.br", captured!.RequestUri!.Host);
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Contains("\"postal_code\":\"20040002\"", body);
        Assert.Contains("\"weight\":1.5", body);
        Assert.Contains("\"quantity\":2", body);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
