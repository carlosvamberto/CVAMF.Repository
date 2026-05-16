using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace CVAMF.Repository.Caching;

/// <summary>
/// In-memory cache implementation using IMemoryCache.
/// Suitable for single-server applications or development.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _keys;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _keys = new ConcurrentDictionary<string, byte>();
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = _memoryCache.Get<T>(key);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {
            cacheEntryOptions.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }

        if (slidingExpiration.HasValue)
        {
            cacheEntryOptions.SlidingExpiration = slidingExpiration.Value;
        }

        // Register callback to remove key from tracking when evicted
        cacheEntryOptions.RegisterPostEvictionCallback((k, v, r, s) =>
        {
            _keys.TryRemove(k.ToString()!, out _);
        });

        _memoryCache.Set(key, value, cacheEntryOptions);
        _keys.TryAdd(key, 0);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string? pattern = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            // Remove all keys
            foreach (var key in _keys.Keys.ToList())
            {
                _memoryCache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
        else
        {
            // Remove keys matching pattern (simple wildcard support)
            var patternWithoutWildcard = pattern.Replace("*", "");
            var keysToRemove = _keys.Keys
                .Where(k => k.StartsWith(patternWithoutWildcard, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }
}
