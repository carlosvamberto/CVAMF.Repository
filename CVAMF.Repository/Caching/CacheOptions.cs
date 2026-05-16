namespace CVAMF.Repository.Caching;

/// <summary>
/// Configuration options for cache behavior in repositories.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Default time-to-live for cached entries.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Sliding expiration time - cache entry is refreshed if accessed within this window.
    /// Default: null (disabled)
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Prefix for all cache keys to avoid collisions.
    /// Default: "CVAMF.Repository:"
    /// </summary>
    public string KeyPrefix { get; set; } = "CVAMF.Repository:";

    /// <summary>
    /// Enable cache for GetById operations.
    /// Default: true
    /// </summary>
    public bool CacheGetById { get; set; } = true;

    /// <summary>
    /// Enable cache for GetAll operations.
    /// Default: false (can consume too much memory)
    /// </summary>
    public bool CacheGetAll { get; set; } = false;

    /// <summary>
    /// Enable cache for GetPaged operations.
    /// Default: false (highly dynamic data)
    /// </summary>
    public bool CacheGetPaged { get; set; } = false;

    /// <summary>
    /// Enable cache for custom GetAsync operations.
    /// Default: false (custom filters are unpredictable)
    /// </summary>
    public bool CacheGetAsync { get; set; } = false;

    /// <summary>
    /// Automatically invalidate cache on Add, Update, Delete operations.
    /// Default: true
    /// </summary>
    public bool AutoInvalidateOnWrite { get; set; } = true;

    /// <summary>
    /// Use entity type name in cache key.
    /// Default: true
    /// </summary>
    public bool UseEntityTypeInKey { get; set; } = true;

    /// <summary>
    /// Use tenant ID in cache key for multi-tenant scenarios.
    /// Default: true
    /// </summary>
    public bool UseTenantIdInKey { get; set; } = true;
}
