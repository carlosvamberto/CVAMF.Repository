# Cache Support (Memory & Redis)

CVAMF.Repository provides built-in support for caching with both **in-memory** and **distributed Redis** cache implementations.

## 🎯 Overview

Caching can dramatically improve application performance by reducing database round-trips for frequently accessed data. This library provides:

- **Automatic caching** for GetById operations (configurable)
- **Optional caching** for GetAll, GetPaged, and custom queries
- **Automatic cache invalidation** on write operations (Add, Update, Delete)
- **Two implementations**: MemoryCache (single-server) and Redis (distributed)
- **Completely optional** - zero impact when not configured
- **Compatible with all features** (Multi-Tenancy, Soft Delete, Audit, etc.)

## 📦 Setup

### Option 1: Memory Cache (Single Server / Development)

**1. Install Package** (already included):
```bash
dotnet add package Microsoft.Extensions.Caching.Memory
```

**2. Register Services:**
```csharp
using CVAMF.Repository.Caching;

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// Optional: Configure cache options
builder.Services.AddScoped<CacheOptions>(sp => new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(10),
    CacheGetById = true,
    CacheGetAll = false, // Usually false - can consume too much memory
    AutoInvalidateOnWrite = true
});

// Register UnitOfWork with cache
builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<ApplicationDbContext>();
    var tenantProvider = sp.GetRequiredService<ITenantProvider>(); // optional
    var cacheService = sp.GetRequiredService<ICacheService>();
    var cacheOptions = sp.GetRequiredService<CacheOptions>();

    return new UnitOfWork(context, tenantProvider, cacheService, cacheOptions);
});
```

### Option 2: Redis Cache (Distributed / Production)

**1. Install Package** (already included):
```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

**2. Basic Redis Setup:**
```csharp
using CVAMF.Repository.Caching;

// Option A: Using IDistributedCache (simple)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // or from appsettings
    options.InstanceName = "CVAMFRepository:";
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();
```

**3. Advanced Redis Setup (recommended for production):**
```csharp
using StackExchange.Redis;
using CVAMF.Repository.Caching;

// Register Redis connection multiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse("localhost:6379");
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 3;
    configuration.ConnectTimeout = 5000;

    return ConnectionMultiplexer.Connect(configuration);
});

// Use advanced Redis cache service
builder.Services.AddScoped<ICacheService, RedisAdvancedCacheService>();

// Register cache options
builder.Services.AddScoped<CacheOptions>(sp => new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(30),
    KeyPrefix = "MyApp:Repo:",
    CacheGetById = true,
    AutoInvalidateOnWrite = true
});
```

**4. Configuration (appsettings.json):**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "CVAMFRepository:"
  },
  "CacheOptions": {
    "DefaultExpirationMinutes": 30,
    "CacheGetById": true,
    "CacheGetAll": false,
    "AutoInvalidateOnWrite": true
  }
}
```

## 🔧 Configuration Options

### CacheOptions Properties

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultExpiration` | 5 minutes | Default TTL for cached entries |
| `SlidingExpiration` | null | Sliding expiration window (optional) |
| `KeyPrefix` | "CVAMF.Repository:" | Prefix for all cache keys |
| `CacheGetById` | true | Enable cache for GetById operations |
| `CacheGetAll` | false | Enable cache for GetAll operations |
| `CacheGetPaged` | false | Enable cache for GetPaged operations |
| `CacheGetAsync` | false | Enable cache for custom GetAsync operations |
| `AutoInvalidateOnWrite` | true | Auto-invalidate cache on Add/Update/Delete |
| `UseEntityTypeInKey` | true | Include entity type name in cache key |
| `UseTenantIdInKey` | true | Include tenant ID in cache key (multi-tenancy) |

## 💡 Usage Examples

### Automatic Caching (GetById)

```csharp
// First call - hits database and caches result
var product = await _unitOfWork.Repository<Product, Guid>()
    .GetByIdAsync(productId);
// Cache key: "CVAMF.Repository:Product:GetById:{productId}"

// Second call - returns from cache (no database hit)
var cachedProduct = await _unitOfWork.Repository<Product, Guid>()
    .GetByIdAsync(productId);
```

### Automatic Cache Invalidation

```csharp
// Update product
product.Price = 99.99m;
await _unitOfWork.Repository<Product, Guid>().UpdateAsync(product);
await _unitOfWork.SaveChangesAsync();
// Cache is automatically invalidated - next GetByIdAsync will hit database

// Delete product
await _unitOfWork.Repository<Product, Guid>().DeleteAsync(productId);
await _unitOfWork.SaveChangesAsync();
// Cache is automatically invalidated
```

### Multi-Tenancy + Cache

```csharp
// Tenant 1's cache key: "CVAMF.Repository:Product:Tenant:tenant-1:GetById:abc-123"
// Tenant 2's cache key: "CVAMF.Repository:Product:Tenant:tenant-2:GetById:abc-123"

// Each tenant has isolated cache entries
var tenant1Product = await _unitOfWork.Repository<Product, Guid>()
    .GetByIdAsync(productId); // Cached separately per tenant
```

### Without Cache (Opt-out)

```csharp
// Don't provide ICacheService - repository works normally without caching
services.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<ApplicationDbContext>();
    return new UnitOfWork(context); // No cache service = no caching
});
```

## 🏗️ Advanced Scenarios

### Custom Cache Key Prefix

```csharp
var cacheOptions = new CacheOptions
{
    KeyPrefix = "MyApp:Products:",
    DefaultExpiration = TimeSpan.FromHours(1)
};
```

### Selective Caching

```csharp
// Cache only read-heavy entities
var cacheOptions = new CacheOptions
{
    CacheGetById = true,    // ✅ Cache individual lookups
    CacheGetAll = false,    // ❌ Don't cache full lists
    CacheGetPaged = false   // ❌ Don't cache paginated results
};
```

### Manual Cache Invalidation

```csharp
// Access cache service directly if needed
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Remove specific key
await cacheService.RemoveAsync("CVAMF.Repository:Product:GetById:abc-123");

// Remove all products from cache
await cacheService.RemoveByPatternAsync("CVAMF.Repository:Product:*");

// Clear all cache (use with caution!)
await cacheService.RemoveByPatternAsync();
```

### Redis Advanced Operations (RedisAdvancedCacheService only)

```csharp
var redisCache = (RedisAdvancedCacheService)cacheService;

// Get cache statistics
var stats = await redisCache.GetStatsAsync();
foreach (var stat in stats)
{
    Console.WriteLine($"{stat.Key}: {stat.Value}");
}

// Count keys matching pattern
var productCacheCount = await redisCache.CountKeysAsync("*:Product:*");
Console.WriteLine($"Cached products: {productCacheCount}");
```

## ⚡ Performance Comparison

### Without Cache
```csharp
// 100 GetById calls = 100 database queries
for (int i = 0; i < 100; i++)
{
    var product = await repository.GetByIdAsync(productId); // 100 x ~50ms = 5,000ms
}
// Total: ~5,000ms
```

### With Cache
```csharp
// 100 GetById calls = 1 database query + 99 cache hits
for (int i = 0; i < 100; i++)
{
    var product = await repository.GetByIdAsync(productId); 
    // First: 50ms (database)
    // Rest: 99 x ~1ms = 99ms (cache)
}
// Total: ~150ms (96% faster!)
```

## 🔄 Cache Lifecycle

```
┌─────────────┐
│  GetById()  │
└──────┬──────┘
       │
       ▼
  ┌─────────┐
  │ Cache?  │──Yes──► Return from cache (fast!)
  └────┬────┘
       │ No
       ▼
  ┌──────────┐
  │ Database │
  └────┬─────┘
       │
       ▼
  ┌───────────┐
  │ Set Cache │ (with TTL)
  └───────────┘
       │
       ▼
  ┌──────────┐
  │  Return  │
  └──────────┘

┌────────────────┐
│ Add/Update/Del │
└───────┬────────┘
        │
        ▼
   ┌────────────┐
   │  Database  │
   └─────┬──────┘
         │
         ▼
  ┌─────────────────┐
  │ Invalidate Cache│ (pattern-based)
  └─────────────────┘
```

## 🎯 Best Practices

### ✅ DO

- **Cache read-heavy entities** (products, categories, settings)
- **Use Redis for production** multi-server environments
- **Set appropriate TTL** based on data volatility
- **Monitor cache hit ratio** to measure effectiveness
- **Use pattern-based invalidation** for related entities
- **Cache with multi-tenancy** to isolate tenant data

### ❌ DON'T

- **Don't cache rapidly changing data** (stock prices, real-time data)
- **Don't cache large collections** (GetAll) unless necessary
- **Don't forget to invalidate** on updates
- **Don't cache sensitive data** without encryption
- **Don't use Memory Cache** in multi-server scenarios (use Redis)

## 🌐 Real-World Example

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GetById - automatically cached
    public async Task<Product?> GetProductAsync(Guid id)
    {
        // First call: Database + Cache
        // Subsequent calls: Cache only (5-30min TTL)
        return await _unitOfWork.Repository<Product, Guid>()
            .GetByIdAsync(id);
    }

    // Update - automatically invalidates cache
    public async Task<bool> UpdatePriceAsync(Guid id, decimal newPrice)
    {
        var product = await _unitOfWork.Repository<Product, Guid>()
            .GetByIdAsync(id);

        if (product == null)
            return false;

        product.Price = newPrice;
        await _unitOfWork.Repository<Product, Guid>().UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
        // Cache automatically invalidated here

        return true;
    }

    // Bulk operation - single invalidation
    public async Task<int> BulkUpdateAsync(List<Product> products)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repo = _unitOfWork.Repository<Product, Guid>();

            foreach (var product in products)
            {
                await repo.UpdateAsync(product);
            }

            // All updates committed together
            // Cache invalidated once at the end
            return products.Count;
        });
    }
}
```

## 🔍 Monitoring & Debugging

### Check if cache is working

```csharp
// Log cache hits/misses
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

var key = "test-key";
var exists = await cacheService.ExistsAsync(key);
Console.WriteLine($"Cache hit: {exists}");
```

### Redis monitoring

```bash
# Connect to Redis CLI
redis-cli

# Monitor all commands
MONITOR

# Count keys
KEYS CVAMF.Repository:*

# Get key TTL
TTL CVAMF.Repository:Product:GetById:abc-123

# Clear all cache
FLUSHDB
```

## 🛡️ Thread Safety

- **MemoryCacheService**: Thread-safe via `IMemoryCache`
- **RedisCacheService**: Thread-safe via `IDistributedCache`
- **RedisAdvancedCacheService**: Thread-safe via `StackExchange.Redis`

## 📊 Cache Key Format

```
{KeyPrefix}{EntityType}[Tenant:{TenantId}]:{Operation}:{Parameters}

Examples:
CVAMF.Repository:Product:GetById:abc-123-def-456
CVAMF.Repository:Product:Tenant:tenant-1:GetById:abc-123
MyApp:Orders:Tenant:company-xyz:GetById:999
```

## 🚀 Performance Tips

1. **Tune TTL based on data volatility**
   - Hot data (rarely changes): 30-60 minutes
   - Warm data (occasional changes): 10-30 minutes
   - Cold data (frequent changes): 1-5 minutes

2. **Use Redis connection pooling**
   ```csharp
   configuration.ConnectRetry = 3;
   configuration.KeepAlive = 60;
   ```

3. **Monitor memory usage**
   - Memory Cache: Set size limits
   - Redis: Monitor memory with `INFO memory`

4. **Batch invalidations**
   - Use pattern-based removal for related entities
   - Example: `Product:*` invalidates all products

## ✅ Integration with Other Features

Cache works seamlessly with all library features:

```csharp
// Multi-Tenancy + Cache + Soft Delete + Audit
public class Invoice : EntityBaseTenantAuditableSoftDelete
{
    public string Number { get; set; } = string.Empty;
    // TenantId, Audit fields, Soft Delete inherited
}

// Query with cache (automatic tenant isolation)
var invoice = await _unitOfWork.Repository<Invoice, Guid>()
    .GetByIdAsync(invoiceId);
// Cache key includes tenant ID automatically
// Result respects soft delete (IsDeleted = false)
```

## 🔗 See Also

- [Multi-Tenancy Documentation](MULTITENANCY_USAGE.md)
- [Soft Delete Documentation](SOFTDELETE_USAGE.md)
- [Audit Fields Documentation](AUDIT_USAGE.md)
- [Unit of Work Documentation](UNITOFWORK_USAGE.md)
