using Brasil57.Application;
using Brasil57.Domain;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Infrastructure;

public sealed class PaymentService(StoreDbContext db, IPaymentGateway gateway)
{
    public async Task<Payment> CreatePaymentAsync(Guid orderId, PaymentMethod method, string idempotencyKey, CancellationToken ct)
    {
        if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Dados de pagamento inválidos.");
        if (!gateway.AvailableMethods.Contains(method)) throw new NotSupportedException("Forma de pagamento indisponível.");

        var existing = await db.Payments.SingleOrDefaultAsync(x => x.Provider == gateway.Provider && x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OrderId != orderId || existing.PaymentMethod != method.ToString())
                throw new InvalidOperationException("Chave de idempotência usada para outro pagamento.");
            return existing;
        }

        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new InvalidOperationException("Pedido inexistente.");
        if (order.Status != OrderStatus.AwaitingPayment) throw new InvalidOperationException("Pedido não aguarda pagamento.");
        var amount = order.ItemsTotal + order.ShippingTotal;
        if (amount <= 0) throw new InvalidOperationException("Total do pedido inválido.");
        if (await db.Payments.AnyAsync(x => x.OrderId == orderId && (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid), ct))
            throw new InvalidOperationException("Já existe pagamento ativo para este pedido.");

        // Persist the key before an external call. A future live adapter must reconcile uncertain outcomes.
        var payment = new Payment { OrderId = orderId, Provider = gateway.Provider, PaymentMethod = method.ToString(),
            Amount = amount, IdempotencyKey = idempotencyKey };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        var charge = await gateway.CreateChargeAsync(new(orderId, amount, payment.Currency, method, idempotencyKey), ct);
        payment.ProviderPaymentId = charge.ProviderPaymentId;
        payment.ExpiresAt = charge.ExpiresAt;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return payment;
    }

    public Task<Payment?> GetPaymentStatusAsync(Guid paymentId, CancellationToken ct) =>
        db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId, ct);

    public async Task CancelPaymentAsync(Guid paymentId, string idempotencyKey, CancellationToken ct)
    {
        var payment = await LoadChargeAsync(paymentId, ct);
        if (payment.Status != PaymentStatus.Pending) throw new InvalidOperationException("Pagamento não pode ser cancelado.");
        await gateway.CancelAsync(payment.ProviderPaymentId!, idempotencyKey, ct);
        await RefreshStatusAsync(payment, ct);
    }

    public async Task RefundPaymentAsync(Guid paymentId, string idempotencyKey, CancellationToken ct)
    {
        var payment = await LoadChargeAsync(paymentId, ct);
        if (payment.Status != PaymentStatus.Paid) throw new InvalidOperationException("Pagamento não pode ser reembolsado.");
        await gateway.RefundAsync(payment.ProviderPaymentId!, payment.Amount, idempotencyKey, ct);
        await RefreshStatusAsync(payment, ct);
    }

    public async Task<bool> ProcessWebhookAsync(ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        if (!gateway.VerifyWebhook(payload, headers)) return false;
        var notification = gateway.ParseWebhook(payload);
        if (string.IsNullOrWhiteSpace(notification.EventId) || string.IsNullOrWhiteSpace(notification.ProviderPaymentId)) return false;
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Provider == gateway.Provider && x.ProviderPaymentId == notification.ProviderPaymentId, ct);
        if (payment is null) return false;
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PaymentEvents" ("Id", "Provider", "ProviderEventId", "ProviderPaymentId", "ReceivedAt")
            VALUES ({eventId}, {gateway.Provider}, {notification.EventId}, {notification.ProviderPaymentId}, {now})
            ON CONFLICT ("Provider", "ProviderEventId") DO NOTHING
            """, ct);
        if (inserted == 0 && await db.PaymentEvents.AnyAsync(x => x.Provider == gateway.Provider && x.ProviderEventId == notification.EventId && x.ProcessedAt != null, ct)) return true;
        await RefreshStatusAsync(payment, ct);
        await db.PaymentEvents.Where(x => x.Id == eventId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAt, DateTimeOffset.UtcNow), ct);
        return true;
    }

    private async Task<Payment> LoadChargeAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == paymentId && x.Provider == gateway.Provider, ct)
            ?? throw new InvalidOperationException("Pagamento inexistente.");
        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId)) throw new InvalidOperationException("Cobrança ainda não vinculada.");
        return payment;
    }

    private async Task RefreshStatusAsync(Payment payment, CancellationToken ct)
    {
        var charge = await gateway.GetChargeAsync(payment.ProviderPaymentId!, ct);
        if (charge.ProviderPaymentId != payment.ProviderPaymentId) throw new InvalidOperationException("Identificador de cobrança divergente.");
        if (charge.Status == GatewayPaymentStatus.Paid)
        {
            var result = await new OrderReservationService(db).ConfirmPaymentAsync(
                new PaymentResult(charge.ProviderPaymentId, payment.OrderId.ToString(), payment.Amount, payment.Currency, "approved"), ct);
            if (result is not (ConfirmPaymentOutcome.Confirmed or ConfirmPaymentOutcome.Replayed))
                throw new InvalidOperationException($"Confirmação do pedido recusada: {result}.");
        }
        payment.Status = charge.Status switch {
            GatewayPaymentStatus.Paid => PaymentStatus.Paid,
            GatewayPaymentStatus.Failed => PaymentStatus.Failed,
            GatewayPaymentStatus.Cancelled => PaymentStatus.Cancelled,
            GatewayPaymentStatus.Refunded => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };
        if (payment.Status == PaymentStatus.Paid) payment.PaidAt ??= DateTimeOffset.UtcNow;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
