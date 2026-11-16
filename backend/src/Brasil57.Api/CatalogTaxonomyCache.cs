using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Brasil57.Api;

internal sealed record CategoryOption(Guid Id, string Name, string Slug, Guid? ParentId);
internal sealed record BrandOption(Guid Id, string Name, string Slug);

internal sealed class CatalogTaxonomyCache(IDistributedCache cache, ILogger<CatalogTaxonomyCache> logger)
{
    internal const string CategoriesKey = "catalog:categories:v1";
    internal const string BrandsKey = "catalog:brands:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    internal async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> load, CancellationToken ct)
    {
        var cacheAvailable = true;
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is not null)
            {
                var value = JsonSerializer.Deserialize<T>(bytes);
                if (value is not null) return value;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cacheAvailable = false;
            logger.LogWarning(exception, "Cache de catálogo indisponível; consultando PostgreSQL.");
        }
        var loaded = await load();
        if (!cacheAvailable) return loaded;
        try
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(loaded), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Não foi possível preencher cache de catálogo.");
        }
        return loaded;
    }

    internal async Task InvalidateAsync(string key, CancellationToken ct)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Não foi possível invalidar cache de catálogo; TTL máximo de 60 segundos.");
        }
    }
}
