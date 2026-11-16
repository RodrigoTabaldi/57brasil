using Brasil57.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Brasil57.Api;

// Only catalog queries explicitly routed here may use an asynchronously replicated standby.
internal sealed class CatalogReadRouter(StoreDbContext primary, IConfiguration configuration, ILogger<CatalogReadRouter> logger)
{
    private readonly string? _replicaConnection = configuration.GetConnectionString("CatalogReplica");
    private readonly int _maxLagSeconds = Math.Clamp(configuration.GetValue("Catalog:ReplicaMaxLagSeconds", 30), 1, 300);

    internal async Task<T> ReadAsync<T>(Func<StoreDbContext, Task<T>> read, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_replicaConnection)) return await read(primary);
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseNpgsql(_replicaConnection).Options;
        await using var replica = new StoreDbContext(options);
        try
        {
            await replica.Database.OpenConnectionAsync(ct);
            await using var command = replica.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT pg_is_in_recovery() AND COALESCE(EXTRACT(EPOCH FROM now() - pg_last_xact_replay_timestamp()) <= @max_lag, false)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "max_lag";
            parameter.Value = _maxLagSeconds;
            command.Parameters.Add(parameter);
            if (await command.ExecuteScalarAsync(ct) is true) return await read(replica);
            logger.LogWarning("Réplica do catálogo fora do limite de lag; leitura no primary.");
        }
        catch (NpgsqlException exception) when (exception is not PostgresException)
        {
            logger.LogWarning(exception, "Réplica do catálogo indisponível; leitura no primary.");
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Timeout da réplica do catálogo; leitura no primary.");
        }
        catch (TaskCanceledException exception) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Timeout da réplica do catálogo; leitura no primary.");
        }
        return await read(primary);
    }
}
