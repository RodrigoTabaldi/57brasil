using System.Text;
using RabbitMQ.Client;

namespace Brasil57.Infrastructure;

// The PostgreSQL inbox is authoritative. This queue only reduces processing latency.
public sealed class PaymentNotificationQueue(string? host, string? password, int port = 5672)
{
    private const string QueueName = "payment-notifications";
    public bool Enabled => !string.IsNullOrWhiteSpace(host);

    private async Task<IConnection> ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { HostName = host!, Port = port, UserName = "brasil57", Password = password ?? string.Empty, RequestedConnectionTimeout = TimeSpan.FromSeconds(2) };
        return await factory.CreateConnectionAsync(ct);
    }

    public async Task PublishAsync(Guid notificationId, CancellationToken ct)
    {
        if (!Enabled) return;
        await using var connection = await ConnectAsync(ct);
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        var properties = new BasicProperties { Persistent = true };
        await channel.BasicPublishAsync(string.Empty, QueueName, mandatory: true, properties, Encoding.ASCII.GetBytes(notificationId.ToString("D")), ct);
    }

    public async Task<bool> ProcessNextAsync(Func<Guid, CancellationToken, Task> process, CancellationToken ct)
    {
        if (!Enabled) return false;
        await using var connection = await ConnectAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, ct);
        if (delivery is null) return false;
        if (Guid.TryParse(Encoding.ASCII.GetString(delivery.Body.Span), out var id))
            await process(id, ct);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        return true;
    }
}
