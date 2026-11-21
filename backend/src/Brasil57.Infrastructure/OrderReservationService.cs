using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Brasil57.Domain;
using Brasil57.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Brasil57.Infrastructure;

public enum ReserveOrderOutcome { Created, Replayed, Conflict, OutOfStock, EmptyCart }
public sealed record ReserveOrderResult(ReserveOrderOutcome Outcome, Guid? OrderId);
public enum ConfirmPaymentOutcome { Confirmed, Replayed, NotApproved, Mismatch, ConflictingPayment }

// The shipping amount must come from a server-verified quote, never from an HTTP request body.
public sealed class OrderReservationService(StoreDbContext db)
{
    // The caller must obtain this result from the authenticated payment provider, not from webhook JSON.
    public async Task<ConfirmPaymentOutcome> ConfirmPaymentAsync(PaymentResult payment, CancellationToken ct)
    {
        if (payment.Status != "approved") return ConfirmPaymentOutcome.NotApproved;
        if (payment.Currency != "BRL" || !Guid.TryParse(payment.ExternalReference, out var orderId) || string.IsNullOrWhiteSpace(payment.ProviderPaymentId) || payment.ProviderPaymentId.Length > 80)
            return ConfirmPaymentOutcome.Mismatch;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.AsNoTracking().Where(x => x.Id == orderId)
            .Select(x => new { x.Id, x.CartId, x.Status, x.ProviderPaymentId, x.ItemsTotal, x.ShippingTotal }).FirstOrDefaultAsync(ct);
        if (order is null || payment.Amount != order.ItemsTotal + order.ShippingTotal) return ConfirmPaymentOutcome.Mismatch;
        var ownerId = await db.Carts.Where(x => x.Id == order.CartId).Select(x => x.UserId).FirstOrDefaultAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({(ownerId ?? order.CartId).ToString()}))", ct);
        if (order.Status == OrderStatus.Paid) return order.ProviderPaymentId == payment.ProviderPaymentId
            ? ConfirmPaymentOutcome.Replayed : ConfirmPaymentOutcome.ConflictingPayment;
        if (order.Status != OrderStatus.AwaitingPayment) return ConfirmPaymentOutcome.ConflictingPayment;

        var changed = await db.Orders.Where(x => x.Id == orderId && x.Status == OrderStatus.AwaitingPayment)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, OrderStatus.Paid)
                .SetProperty(x => x.ProviderPaymentId, payment.ProviderPaymentId)
                .SetProperty(x => x.PaidAt, DateTimeOffset.UtcNow), ct);
        if (changed != 1)
        {
            var current = await db.Orders.AsNoTracking().Where(x => x.Id == orderId)
                .Select(x => new { x.Status, x.ProviderPaymentId }).SingleAsync(ct);
            return current.Status == OrderStatus.Paid && current.ProviderPaymentId == payment.ProviderPaymentId
                ? ConfirmPaymentOutcome.Replayed : ConfirmPaymentOutcome.ConflictingPayment;
        }
        var items = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == orderId).ToListAsync(ct);
        foreach (var item in items.OrderBy(x => x.ProductId))
        {
            var committed = await db.Inventory.Where(x => x.ProductId == item.ProductId && x.Reserved >= item.Quantity && x.OnHand >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved - item.Quantity)
                    .SetProperty(x => x.OnHand, x => x.OnHand - item.Quantity), ct);
            if (committed != 1) throw new InvalidOperationException("Reserva inconsistente para pagamento aprovado.");
        }
        await db.CartItems.Where(x => x.CartId == order.CartId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return ConfirmPaymentOutcome.Confirmed;
    }

    // Call only after payment reconciliation has established that no charge was approved.
    public async Task<bool> CancelUnpaidAsync(Guid orderId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var changed = await db.Orders.Where(x => x.Id == orderId && x.Status == OrderStatus.AwaitingPayment)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, OrderStatus.Cancelled), ct);
        if (changed == 0) return false;
        var items = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == orderId).ToListAsync(ct);
        foreach (var item in items.OrderBy(x => x.ProductId))
        {
            var released = await db.Inventory.Where(x => x.ProductId == item.ProductId && x.Reserved >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved - item.Quantity), ct);
            if (released != 1) throw new InvalidOperationException("Reserva inconsistente.");
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<ReserveOrderResult> ReserveAsync(Guid cartId, string buyerEmail, decimal shippingTotal, string idempotencyKey, CancellationToken ct)
    {
        if (cartId == Guid.Empty || !MailAddress.TryCreate(buyerEmail, out var address) || address.Address != buyerEmail || buyerEmail.Length > 254 ||
            shippingTotal < 0 || shippingTotal > 9999999999.99m || string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Dados de reserva inválidos.");

        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{cartId:N}|{buyerEmail.ToLowerInvariant()}|{shippingTotal.ToString("0.00", CultureInfo.InvariantCulture)}")));
        var prior = await db.Orders.AsNoTracking().Where(x => x.IdempotencyKey == idempotencyKey).Select(x => new { x.Id, x.RequestHash }).FirstOrDefaultAsync(ct);
        if (prior is not null) return prior.RequestHash == requestHash
            ? new(ReserveOrderOutcome.Replayed, prior.Id) : new(ReserveOrderOutcome.Conflict, null);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var ownerId = await db.Carts.Where(x => x.Id == cartId).Select(x => x.UserId).FirstOrDefaultAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({(ownerId ?? cartId).ToString()}))", ct);
            var concurrent = await db.Orders.AsNoTracking().Where(x => x.IdempotencyKey == idempotencyKey)
                .Select(x => new { x.Id, x.RequestHash }).FirstOrDefaultAsync(ct);
            if (concurrent is not null) return concurrent.RequestHash == requestHash
                ? new(ReserveOrderOutcome.Replayed, concurrent.Id) : new(ReserveOrderOutcome.Conflict, null);
            if (await db.Orders.AnyAsync(x => x.CartId == cartId && x.Status == OrderStatus.AwaitingPayment, ct))
                return new(ReserveOrderOutcome.Conflict, null);
            var cart = await db.Carts.AsNoTracking().Where(x => x.Id == cartId && x.ExpiresAt > DateTimeOffset.UtcNow)
                .Select(x => new { Items = x.Items.Select(i => new { i.ProductId, i.Quantity, i.Product.Sku, i.Product.Name, i.Product.Price, i.Product.Published }).ToList() })
                .FirstOrDefaultAsync(ct);
            if (cart is null || cart.Items.Count == 0) return new(ReserveOrderOutcome.EmptyCart, null);
            if (cart.Items.Any(x => !x.Published || x.Quantity is < 1 or > 99)) return new(ReserveOrderOutcome.Conflict, null);

            var order = new Order {
                CartId = cartId, BuyerEmail = buyerEmail, ShippingTotal = shippingTotal,
                IdempotencyKey = idempotencyKey, RequestHash = requestHash,
                ItemsTotal = cart.Items.Sum(x => x.Price * x.Quantity),
                Items = cart.Items.Select(x => new OrderItem { ProductId = x.ProductId, Sku = x.Sku, Name = x.Name, Quantity = x.Quantity, UnitPrice = x.Price }).ToList()
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            foreach (var item in cart.Items.OrderBy(x => x.ProductId))
            {
                var changed = await db.Inventory.Where(x => x.ProductId == item.ProductId && x.OnHand - x.Reserved >= item.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved + item.Quantity), ct);
                if (changed != 1) return new(ReserveOrderOutcome.OutOfStock, null);
            }
            await transaction.CommitAsync(ct);
            return new(ReserveOrderOutcome.Created, order.Id);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(ct);
            var existing = await db.Orders.AsNoTracking().Where(x => x.IdempotencyKey == idempotencyKey)
                .Select(x => new { x.Id, x.RequestHash }).FirstOrDefaultAsync(ct);
            return existing is not null && existing.RequestHash == requestHash
                ? new(ReserveOrderOutcome.Replayed, existing.Id) : new(ReserveOrderOutcome.Conflict, null);
        }
    }
}
