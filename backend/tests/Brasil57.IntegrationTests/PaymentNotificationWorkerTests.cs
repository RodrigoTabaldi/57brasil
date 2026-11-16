using Brasil57.Application;
using Brasil57.Domain;
using Brasil57.Infrastructure;
using PaymentWorker = Brasil57.Worker.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class PaymentNotificationWorkerTests
{
    [Fact]
    public async Task Worker_confirms_inbox_payment_once()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        var product = new Product { Name = "Peça de teste", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" }, Brand = new Brand { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" }, Price = 10m, Published = true, Inventory = new InventoryItem { OnHand = 1, Reserved = 1 } };
        var order = new Order { CartId = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid().ToString("N"), RequestHash = new string('A', 64), BuyerEmail = "buyer@example.com", ItemsTotal = 10m, ShippingTotal = 5m,
            Items = [new OrderItem { ProductId = product.Id, Sku = product.Sku, Name = product.Name, Quantity = 1, UnitPrice = 10m }] };
        var paymentId = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000).ToString();
        var notification = new PaymentNotification { RequestId = Guid.NewGuid().ToString("N"), PaymentId = paymentId };
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
            await setup.PaymentNotifications.ExecuteDeleteAsync();
            setup.Products.Add(product);
            await setup.SaveChangesAsync();
            setup.Orders.Add(order);
            setup.PaymentNotifications.Add(notification);
            await setup.SaveChangesAsync();
        }

        using var services = new ServiceCollection()
            .AddDbContext<StoreDbContext>(o => o.UseNpgsql(connection))
            .AddSingleton<IPaymentProvider>(new FakePaymentProvider(new PaymentResult(paymentId, order.Id.ToString(), 15m, "BRL", "approved")))
            .BuildServiceProvider();
        using var worker = new PaymentWorker(services.GetRequiredService<IServiceScopeFactory>(), new PaymentNotificationQueue(null, null), NullLogger<PaymentWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (!timeout.IsCancellationRequested)
            {
                await using var db = new StoreDbContext(options);
                if (await db.PaymentNotifications.AnyAsync(x => x.Id == notification.Id && x.ProcessedAt != null, timeout.Token)) break;
                await Task.Delay(200, timeout.Token);
            }
        }
        finally { await worker.StopAsync(CancellationToken.None); }

        await using var verify = new StoreDbContext(options);
        Assert.Equal("Confirmed", await verify.PaymentNotifications.Where(x => x.Id == notification.Id).Select(x => x.Outcome).SingleAsync());
        Assert.Equal(OrderStatus.Paid, await verify.Orders.Where(x => x.Id == order.Id).Select(x => x.Status).SingleAsync());
        var stock = await verify.Inventory.Where(x => x.ProductId == product.Id).Select(x => new { x.OnHand, x.Reserved }).SingleAsync();
        Assert.Equal(0, stock.OnHand);
        Assert.Equal(0, stock.Reserved);
    }

    private sealed class FakePaymentProvider(PaymentResult payment) : IPaymentProvider
    {
        public Task<PaymentPreference> CreatePreferenceAsync(Guid orderId, string buyerEmail, IReadOnlyList<PaymentItem> items, decimal shippingPrice, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentResult> GetPaymentAsync(string paymentId, CancellationToken cancellationToken) => Task.FromResult(payment);
        public bool VerifyWebhook(string signature, string requestId, string paymentId) => false;
    }
}
