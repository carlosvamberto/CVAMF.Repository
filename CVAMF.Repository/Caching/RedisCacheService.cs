using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CVAMF.Repository.Caching;

/// <summary>
/// Redis distributed cache implementation using IDistributedCache.
/// Suitable for multi-server applications and production scenarios.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _distributedCache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(cachedData))
            return null;

        return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var serializedData = JsonSerializer.Serialize(value, _jsonOptions);

        var options = new DistributedCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }

        if (slidingExpiration.HasValue)
        {
            options.SlidingExpiration = slidingExpiration.Value;
        }

        await _distributedCache.SetStringAsync(key, serializedData, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string? pattern = null, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based deletion requires direct access to Redis via IConnectionMultiplexer
        // This is a simplified implementation that removes a single key
        // For true pattern-based deletion, consider using IConnectionMultiplexer directly
        // See documentation for advanced Redis pattern deletion

        if (!string.IsNullOrEmpty(pattern))
        {
            await _distributedCache.RemoveAsync(pattern, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _distributedCache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }
}
