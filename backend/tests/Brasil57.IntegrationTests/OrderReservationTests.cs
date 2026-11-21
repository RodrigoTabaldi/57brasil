using Brasil57.Domain;
using Brasil57.Infrastructure;
using Brasil57.Application;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class OrderReservationTests
{
    [Fact]
    public async Task Concurrent_reservations_never_exceed_stock_and_replay_is_idempotent()
    {
        var connection = Environment.GetEnvironmentVariable("BRASIL57_TEST_DATABASE")
            ?? throw new InvalidOperationException("Defina BRASIL57_TEST_DATABASE para um PostgreSQL descartável.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(connection).Options;
        await using (var setup = new StoreDbContext(options))
        {
            await setup.Database.MigrateAsync();
        }

        var category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var brand = new Brand { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var product = new Product { Name = "Peça de teste", Slug = $"teste-{Guid.NewGuid():N}", Sku = $"TEST-{Guid.NewGuid():N}", Category = category, Brand = brand, Price = 10m, Published = true, CommercialSource = "Integration test fixture", CommercialVerifiedOn = new DateOnly(2026, 11, 18), Inventory = new InventoryItem { OnHand = 1 } };
        var firstCart = new Cart { Items = [new CartItem { Product = product, Quantity = 1 }] };
        var secondCart = new Cart { Items = [new CartItem { Product = product, Quantity = 1 }] };
        await using (var setup = new StoreDbContext(options))
        {
            setup.Carts.AddRange(firstCart, secondCart);
            await setup.SaveChangesAsync();
        }

        var firstKey = Guid.NewGuid().ToString("N");
        var secondKey = Guid.NewGuid().ToString("N");
        async Task<ReserveOrderResult> Reserve(Guid cartId, string key)
        {
            await using var db = new StoreDbContext(options);
            return await new OrderReservationService(db).ReserveAsync(cartId, "buyer@example.com", 5m, key, CancellationToken.None);
        }
        var results = await Task.WhenAll(Reserve(firstCart.Id, firstKey), Reserve(secondCart.Id, secondKey));
        Assert.Single(results, x => x.Outcome == ReserveOrderOutcome.Created);
        Assert.Single(results, x => x.Outcome == ReserveOrderOutcome.OutOfStock);

        var winner = results[0].Outcome == ReserveOrderOutcome.Created ? (firstCart.Id, firstKey, results[0].OrderId) : (secondCart.Id, secondKey, results[1].OrderId);
        var replay = await Reserve(winner.Item1, winner.Item2);
        Assert.Equal(ReserveOrderOutcome.Replayed, replay.Outcome);
        Assert.Equal(winner.OrderId, replay.OrderId);
        await using (var conflictDb = new StoreDbContext(options))
        {
            var conflict = await new OrderReservationService(conflictDb).ReserveAsync(winner.Item1, "other@example.com", 5m, winner.Item2, CancellationToken.None);
            Assert.Equal(ReserveOrderOutcome.Conflict, conflict.Outcome);
        }
        await using var verify = new StoreDbContext(options);
        Assert.Equal(1, await verify.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.Reserved).SingleAsync());
        Assert.Equal(1, await verify.Orders.CountAsync(x => x.CartId == firstCart.Id || x.CartId == secondCart.Id));
        await using (var cancelDb = new StoreDbContext(options))
        {
            var service = new OrderReservationService(cancelDb);
            Assert.True(await service.CancelUnpaidAsync(winner.OrderId!.Value, CancellationToken.None));
            Assert.False(await service.CancelUnpaidAsync(winner.OrderId.Value, CancellationToken.None));
        }
        Assert.Equal(0, await verify.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.Reserved).SingleAsync());

        var nextCart = new Cart { Items = [new CartItem { ProductId = product.Id, Quantity = 1 }] };
        await using (var setup = new StoreDbContext(options))
        {
            setup.Carts.Add(nextCart);
            await setup.SaveChangesAsync();
        }
        var sharedKey = Guid.NewGuid().ToString("N");
        var sameRequest = await Task.WhenAll(Reserve(nextCart.Id, sharedKey), Reserve(nextCart.Id, sharedKey));
        Assert.Contains(sameRequest, x => x.Outcome == ReserveOrderOutcome.Created);
        Assert.All(sameRequest, x => Assert.Contains(x.Outcome, new[] { ReserveOrderOutcome.Created, ReserveOrderOutcome.Replayed }));
        Assert.Equal(sameRequest[0].OrderId, sameRequest[1].OrderId);
        Assert.Equal(1, await verify.Orders.CountAsync(x => x.CartId == nextCart.Id));
        var orderId = sameRequest[0].OrderId!.Value;
        var paymentId = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000).ToString();
        await using (var paymentDb = new StoreDbContext(options))
        {
            var service = new OrderReservationService(paymentDb);
            Assert.Equal(ConfirmPaymentOutcome.Mismatch, await service.ConfirmPaymentAsync(new PaymentResult(paymentId, orderId.ToString(), 14m, "BRL", "approved"), CancellationToken.None));
            Assert.Equal(ConfirmPaymentOutcome.Mismatch, await service.ConfirmPaymentAsync(new PaymentResult(paymentId, orderId.ToString(), 15m, "USD", "approved"), CancellationToken.None));
            Assert.Equal(ConfirmPaymentOutcome.Confirmed, await service.ConfirmPaymentAsync(new PaymentResult(paymentId, orderId.ToString(), 15m, "BRL", "approved"), CancellationToken.None));
            Assert.Equal(ConfirmPaymentOutcome.Replayed, await service.ConfirmPaymentAsync(new PaymentResult(paymentId, orderId.ToString(), 15m, "BRL", "approved"), CancellationToken.None));
            Assert.Equal(ConfirmPaymentOutcome.ConflictingPayment, await service.ConfirmPaymentAsync(new PaymentResult(paymentId + "9", orderId.ToString(), 15m, "BRL", "approved"), CancellationToken.None));
        }
        Assert.Equal(0, await verify.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.Reserved).SingleAsync());
        Assert.Equal(0, await verify.Inventory.Where(x => x.ProductId == product.Id).Select(x => x.OnHand).SingleAsync());
        Assert.Empty(await verify.CartItems.Where(x => x.CartId == nextCart.Id).ToListAsync());
        await using (var refill = new StoreDbContext(options))
        {
            await refill.Inventory.Where(x => x.ProductId == product.Id).ExecuteUpdateAsync(x => x.SetProperty(i => i.OnHand, 1));
            refill.CartItems.Add(new CartItem { CartId = nextCart.Id, ProductId = product.Id, Quantity = 1 });
            await refill.SaveChangesAsync();
        }
        var repeat = await Reserve(nextCart.Id, Guid.NewGuid().ToString("N"));
        Assert.Equal(ReserveOrderOutcome.Created, repeat.Outcome);
        Assert.Equal(2, await verify.Orders.CountAsync(x => x.CartId == nextCart.Id));
    }
}
