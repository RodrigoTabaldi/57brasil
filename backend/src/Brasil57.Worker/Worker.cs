using Brasil57.Application;
using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Brasil57.Worker;

public sealed class Worker(IServiceScopeFactory scopes, PaymentNotificationQueue queue, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (queue.Enabled) await queue.ProcessNextAsync((id, ct) => ProcessBatchAsync(ct, id), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogWarning(exception, "Fila indisponível; processando inbox pelo PostgreSQL."); }
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Falha ao consultar notificações de pagamento."); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct, Guid? notificationId = null)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<IPaymentProvider>();
        var now = DateTimeOffset.UtcNow;
        var due = await db.PaymentNotifications.AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.NextAttemptAt <= now && (x.LeaseUntil == null || x.LeaseUntil < now) && (notificationId == null || x.Id == notificationId))
            .OrderBy(x => x.ReceivedAt).Take(10).Select(x => new { x.Id, x.PaymentId, x.Attempts }).ToListAsync(ct);
        foreach (var notification in due)
        {
            now = DateTimeOffset.UtcNow;
            var claimed = await db.PaymentNotifications.Where(x => x.Id == notification.Id && x.ProcessedAt == null && (x.LeaseUntil == null || x.LeaseUntil < now))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntil, now.AddMinutes(1)), ct);
            if (claimed != 1) continue;
            try
            {
                var payment = await provider.GetPaymentAsync(notification.PaymentId, ct);
                var outcome = await new OrderReservationService(db).ConfirmPaymentAsync(payment, ct);
                if (outcome == ConfirmPaymentOutcome.NotApproved)
                {
                    await ScheduleRetryAsync(db, notification.Id, notification.Attempts, outcome.ToString(), ct);
                    continue;
                }
                await db.PaymentNotifications.Where(x => x.Id == notification.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAt, DateTimeOffset.UtcNow)
                        .SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null)
                        .SetProperty(x => x.Outcome, outcome.ToString())
                        .SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
                if (outcome is ConfirmPaymentOutcome.Mismatch or ConfirmPaymentOutcome.ConflictingPayment)
                    logger.LogError("Pagamento {PaymentId} exige revisão: {Outcome}", notification.PaymentId, outcome);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                await ScheduleRetryAsync(db, notification.Id, notification.Attempts, "ProviderError", ct);
                logger.LogWarning(exception, "Falha ao processar pagamento {PaymentId}; nova tentativa agendada.", notification.PaymentId);
            }
        }
    }

    private static Task ScheduleRetryAsync(StoreDbContext db, Guid id, int attempts, string outcome, CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(Math.Min(300, 1 << Math.Min(attempts + 1, 8)));
        return db.PaymentNotifications.Where(x => x.Id == id && x.ProcessedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.Add(delay))
                .SetProperty(x => x.Outcome, outcome)
                .SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
    }
}
