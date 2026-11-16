using Brasil57.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Brasil57.IntegrationTests;

public sealed class InfrastructureServicesTests
{
    [Fact]
    public async Task RabbitMqDeliversPersistedNotificationId()
    {
        var password = Environment.GetEnvironmentVariable("BRASIL57_TEST_RABBIT_PASSWORD");
        if (string.IsNullOrEmpty(password)) return;
        var queue = new PaymentNotificationQueue("127.0.0.1", password, 15673);
        var id = Guid.NewGuid();
        await queue.PublishAsync(id, CancellationToken.None);
        Guid? received = null;
        for (var attempt = 0; attempt < 5 && received is null; attempt++)
            await queue.ProcessNextAsync((value, _) => { received = value; return Task.CompletedTask; }, CancellationToken.None);
        Assert.Equal(id, received);
    }

    [Fact]
    public async Task RedisStoresAndRetrievesSharedValue()
    {
        var password = Environment.GetEnvironmentVariable("BRASIL57_TEST_REDIS_PASSWORD");
        if (string.IsNullOrEmpty(password)) return;
        var options = new ConfigurationOptions { Password = password, AbortOnConnectFail = false };
        options.EndPoints.Add("127.0.0.1", 16379);
        await using var connection = await ConnectionMultiplexer.ConnectAsync(options);
        var key = $"integration:{Guid.NewGuid():N}";
        var db = connection.GetDatabase();
        await db.StringSetAsync(key, "ok", TimeSpan.FromSeconds(10));
        Assert.Equal("ok", (string?)await db.StringGetAsync(key));
        await db.KeyDeleteAsync(key);
    }
}
