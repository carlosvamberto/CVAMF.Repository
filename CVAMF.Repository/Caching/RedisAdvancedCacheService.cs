using StackExchange.Redis;
using System.Text.Json;

namespace CVAMF.Repository.Caching;

/// <summary>
/// Advanced Redis cache implementation with pattern-based operations.
/// Uses IConnectionMultiplexer for direct Redis access and advanced features.
/// </summary>
public class RedisAdvancedCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisAdvancedCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _database.StringGetAsync(key);

        if (cachedData.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<T>(cachedData.ToString(), _jsonOptions);
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

        // Redis doesn't support sliding expiration natively, use absolute
        var expiration = absoluteExpiration ?? slidingExpiration;

        if (expiration.HasValue)
        {
            await _database.StringSetAsync(key, serializedData, expiration.Value);
        }
        else
        {
            await _database.StringSetAsync(key, serializedData);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string? pattern = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            // Clear all keys (use with caution in production!)
            var allEndpoints = _connectionMultiplexer.GetEndPoints();
            foreach (var endpoint in allEndpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
            return;
        }

        // Pattern-based deletion using SCAN and DEL
        var endpoints = _connectionMultiplexer.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                await _database.KeyDeleteAsync(key);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    public async Task<Dictionary<string, string>> GetStatsAsync()
    {
        var stats = new Dictionary<string, string>();
        var endpoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            var info = await server.InfoAsync("stats");

            foreach (var section in info)
            {
                foreach (var item in section)
                {
                    stats[$"{endpoint}:{item.Key}"] = item.Value;
                }
            }
        }

        return stats;
    }

    /// <summary>
    /// Gets the number of keys matching a pattern.
    /// </summary>
    public async Task<long> CountKeysAsync(string pattern = "*")
    {
        long count = 0;
        var endpoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                count++;
            }
        }

        return count;
    }
}
