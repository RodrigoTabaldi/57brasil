using Brasil57.Worker;
using Brasil57.Application;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
if (builder.Configuration.GetValue<bool>("Payment:EnableWebhook"))
{
    var accessToken = builder.Configuration["Payment:AccessToken"];
    var webhookSecret = builder.Configuration["Payment:WebhookSecret"];
    if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(webhookSecret) ||
        !Uri.TryCreate(builder.Configuration["Payment:SiteUrl"], UriKind.Absolute, out var siteUrl) || siteUrl.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("Configuração de pagamento incompleta.");
    builder.Services.AddDbContext<StoreDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
    builder.Services.AddSingleton(new PaymentNotificationQueue(builder.Configuration["RabbitMq:Host"], builder.Configuration["RabbitMq:Password"]));
    builder.Services.AddHttpClient("payment", client => client.Timeout = TimeSpan.FromSeconds(10));
    builder.Services.AddTransient<IPaymentProvider>(sp => new MercadoPagoPaymentProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("payment"), accessToken, webhookSecret, siteUrl));
    builder.Services.AddHostedService<Worker>();
}

var host = builder.Build();
host.Run();
