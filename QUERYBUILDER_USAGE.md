# Fluent Query Builder - Usage Guide

The Fluent Query Builder provides a modern, fluent API for constructing complex queries with CVAMF.Repository. It offers a type-safe, IntelliSense-friendly way to build queries using method chaining.

## 📋 Table of Contents
- [Basic Usage](#basic-usage)
- [Features](#features)
- [Where Clauses](#where-clauses)
- [Include (Eager Loading)](#include-eager-loading)
- [Ordering](#ordering)
- [Projection (DTOs)](#projection-dtos)
- [Pagination](#pagination)
- [Performance Options](#performance-options)
- [Execution Methods](#execution-methods)
- [Integration with Other Features](#integration-with-other-features)
- [Complete Examples](#complete-examples)

## Basic Usage

```csharp
// Simple query
var customers = await _customerRepository.Query()
    .Where(x => x.Active)
    .ToListAsync();

// Query with ordering
var products = await _productRepository.Query()
    .Where(x => x.InStock)
    .OrderBy(x => x.Name)
    .ToListAsync();
```

## Features

The Query Builder supports:
- ✅ Multiple WHERE clauses
- ✅ Include (Eager Loading) with ThenInclude
- ✅ OrderBy, OrderByDescending, ThenBy, ThenByDescending
- ✅ Projection to DTOs
- ✅ Pagination (Paginate or Skip/Take)
- ✅ AsNoTracking for read-only queries
- ✅ AsSplitQuery for multiple includes
- ✅ IgnoreQueryFilters (bypass soft delete, multi-tenancy)
- ✅ Integration with Multi-Tenancy (automatic tenant filtering)
- ✅ Integration with Caching (when configured)

## Where Clauses

You can chain multiple WHERE clauses - they will be combined with AND:

```csharp
var filteredCustomers = await _customerRepository.Query()
    .Where(x => x.Active)
    .Where(x => x.Country == "USA")
    .Where(x => x.CreatedAt > DateTime.UtcNow.AddMonths(-6))
    .ToListAsync();

// Equivalent to:
// WHERE Active = true AND Country = 'USA' AND CreatedAt > [6 months ago]
```

## Include (Eager Loading)

Load related entities using Include:

```csharp
// Single include
var orders = await _orderRepository.Query()
    .Include(x => x.Customer)
    .ToListAsync();

// Multiple includes
var products = await _productRepository.Query()
    .Include(x => x.Category)
    .Include(x => x.Supplier)
    .Include(x => x.Reviews)
    .ToListAsync();

// Nested includes (ThenInclude)
var orders = await _orderRepository.Query()
    .Include(x => x.Items)
    .Include("Items.Product") // String-based path for complex navigation
    .ToListAsync();
```

## Ordering

Order your results with primary and secondary sorting:

```csharp
// Single ordering
var customers = await _customerRepository.Query()
    .OrderBy(x => x.Name)
    .ToListAsync();

// Descending order
var products = await _productRepository.Query()
    .OrderByDescending(x => x.Price)
    .ToListAsync();

// Multiple ordering (ThenBy)
var customers = await _customerRepository.Query()
    .OrderBy(x => x.Country)
    .ThenBy(x => x.City)
    .ThenBy(x => x.Name)
    .ToListAsync();

// Mixed ordering
var products = await _productRepository.Query()
    .OrderByDescending(x => x.CreatedAt)
    .ThenBy(x => x.Name)
    .ToListAsync();
```

## Projection (DTOs)

Project entity data to DTOs for optimized queries and clean separation:

```csharp
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}

// Project to DTO
var customerDtos = await _customerRepository.Query()
    .Where(x => x.Active)
    .Include(x => x.Orders)
    .ProjectTo(x => new CustomerDto
    {
        Id = x.Id,
        Name = x.Name,
        OrderCount = x.Orders.Count
    })
    .ToListAsync();

// Projection with ordering and pagination
var pagedDtos = await _customerRepository.Query()
    .Where(x => x.Active)
    .Include(x => x.Orders)
    .ProjectTo(x => new CustomerDto
    {
        Id = x.Id,
        Name = x.Name,
        OrderCount = x.Orders.Count
    })
    .OrderByDescending(x => x.OrderCount)
    .Paginate(1, 20)
    .ToPagedResultAsync();
```

**Note:** After calling `ProjectTo()`, you work with the DTO type, so ordering must be done on DTO properties.

## Pagination

Two ways to paginate: `Paginate()` or `Skip/Take`:

### Option 1: Paginate

```csharp
var pagedResult = await _productRepository.Query()
    .Where(x => x.InStock)
    .OrderBy(x => x.Name)
    .Paginate(pageNumber: 1, pageSize: 20)
    .ToPagedResultAsync();

Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
Console.WriteLine($"Total items: {pagedResult.TotalCount}");

foreach (var product in pagedResult.Items)
{
    Console.WriteLine(product.Name);
}
```

### Option 2: Skip/Take

```csharp
// Get items 11-20
var products = await _productRepository.Query()
    .Where(x => x.InStock)
    .OrderBy(x => x.Name)
    .Skip(10)
    .Take(10)
    .ToListAsync();

// Get top 5
var topProducts = await _productRepository.Query()
    .OrderByDescending(x => x.Rating)
    .Take(5)
    .ToListAsync();
```

## Performance Options

### AsNoTracking

For read-only queries (lists, DTOs, display purposes):

```csharp
var products = await _productRepository.Query()
    .Where(x => x.InStock)
    .AsNoTracking() // 30-40% faster for read-only
    .ToListAsync();
```

### AsSplitQuery

For queries with multiple includes to avoid cartesian explosion:

```csharp
var orders = await _orderRepository.Query()
    .Include(x => x.Items)
    .Include(x => x.Customer)
    .Include(x => x.Shipment)
    .AsSplitQuery() // Executes multiple SQL queries instead of one large JOIN
    .ToListAsync();
```

### IgnoreQueryFilters

Bypass global query filters (soft delete, multi-tenancy):

```csharp
// Include soft-deleted items
var allProducts = await _productRepository.Query()
    .IgnoreQueryFilters()
    .ToListAsync();
```

## Execution Methods

Different ways to execute your query:

### ToListAsync
Returns all matching entities:

```csharp
var customers = await _customerRepository.Query()
    .Where(x => x.Active)
    .ToListAsync();
```

### ToPagedResultAsync
Returns paginated results (requires `Paginate()`):

```csharp
var pagedResult = await _productRepository.Query()
    .Where(x => x.InStock)
    .Paginate(1, 20)
    .ToPagedResultAsync();
```

### FirstOrDefaultAsync
Returns the first matching entity or null:

```csharp
var customer = await _customerRepository.Query()
    .Where(x => x.Email == "john@example.com")
    .FirstOrDefaultAsync();
```

### FirstAsync
Returns the first matching entity or throws exception:

```csharp
var customer = await _customerRepository.Query()
    .Where(x => x.Id == customerId)
    .FirstAsync(); // Throws if not found
```

### SingleOrDefaultAsync
Returns the single matching entity or null (throws if multiple):

```csharp
var customer = await _customerRepository.Query()
    .Where(x => x.Email == "unique@example.com")
    .SingleOrDefaultAsync(); // Throws if > 1 result
```

### CountAsync
Returns the count of matching entities:

```csharp
var activeCount = await _customerRepository.Query()
    .Where(x => x.Active)
    .CountAsync();
```

### AnyAsync
Checks if any entity matches:

```csharp
var hasActiveCustomers = await _customerRepository.Query()
    .Where(x => x.Active)
    .AnyAsync();
```

## Integration with Other Features

### Multi-Tenancy

Query Builder automatically applies tenant filtering:

```csharp
// Only returns customers for current tenant
var customers = await _customerRepository.Query()
    .Where(x => x.Active)
    .ToListAsync();

// Bypass tenant filter if needed
var allTenantCustomers = await _customerRepository.Query()
    .Where(x => x.Active)
    .IgnoreQueryFilters()
    .ToListAsync();
```

### Caching

Works seamlessly with configured caching:

```csharp
// Configure caching in Startup.cs
services.AddCacheService(options =>
{
    options.CacheGetAll = true;
    options.DefaultExpiration = TimeSpan.FromMinutes(10);
});

// Query Builder respects cache configuration
var products = await _productRepository.Query()
    .Where(x => x.InStock)
    .ToListAsync(); // Cached if enabled
```

## Complete Examples

### Example 1: Customer Orders Dashboard

```csharp
public class CustomerOrderSummaryDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public async Task<PagedResult<CustomerOrderSummaryDto>> GetCustomerSummaries(
    int pageNumber,
    int pageSize,
    string? country = null)
{
    var query = _customerRepository.Query()
        .Where(x => x.Active)
        .Include(x => x.Orders);

    if (!string.IsNullOrEmpty(country))
    {
        query = query.Where(x => x.Country == country);
    }

    return await query
        .ProjectTo(x => new CustomerOrderSummaryDto
        {
            CustomerId = x.Id,
            CustomerName = x.Name,
            Email = x.Email,
            TotalOrders = x.Orders.Count,
            TotalSpent = x.Orders.Sum(o => o.TotalAmount),
            LastOrderDate = x.Orders.Max(o => (DateTime?)o.OrderDate)
        })
        .OrderByDescending(x => x.TotalSpent)
        .Paginate(pageNumber, pageSize)
        .AsNoTracking()
        .ToPagedResultAsync();
}
```

### Example 2: Product Search

```csharp
public async Task<List<Product>> SearchProducts(
    string? searchTerm,
    Guid? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    bool onlyInStock = true)
{
    var query = _productRepository.Query();

    if (onlyInStock)
    {
        query = query.Where(x => x.Stock > 0);
    }

    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where(x => x.Name.Contains(searchTerm) || 
                                 x.Description.Contains(searchTerm));
    }

    if (categoryId.HasValue)
    {
        query = query.Where(x => x.CategoryId == categoryId.Value);
    }

    if (minPrice.HasValue)
    {
        query = query.Where(x => x.Price >= minPrice.Value);
    }

    if (maxPrice.HasValue)
    {
        query = query.Where(x => x.Price <= maxPrice.Value);
    }

    return await query
        .Include(x => x.Category)
        .Include(x => x.Supplier)
        .OrderBy(x => x.Name)
        .AsNoTracking()
        .ToListAsync();
}
```

### Example 3: Order Analytics

```csharp
public class OrderAnalyticsDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public async Task<PagedResult<OrderAnalyticsDto>> GetRecentOrders(
    DateTime startDate,
    DateTime endDate,
    int pageNumber,
    int pageSize)
{
    return await _orderRepository.Query()
        .Where(x => x.OrderDate >= startDate && x.OrderDate <= endDate)
        .Include(x => x.Customer)
        .Include(x => x.Items)
        .ProjectTo(x => new OrderAnalyticsDto
        {
            OrderId = x.Id,
            OrderNumber = x.OrderNumber,
            CustomerName = x.Customer.Name,
            OrderDate = x.OrderDate,
            TotalAmount = x.TotalAmount,
            ItemCount = x.Items.Count,
            Status = x.Status
        })
        .OrderByDescending(x => x.OrderDate)
        .Paginate(pageNumber, pageSize)
        .AsNoTracking()
        .ToPagedResultAsync();
}
```

### Example 4: Combined with Specifications

You can still use Specifications alongside Query Builder:

```csharp
// Use Specification for complex reusable logic
var spec = new ActiveCustomersWithOrdersSpec();
var specResults = await _customerRepository.GetAsync(spec);

// Use Query Builder for ad-hoc queries
var queryResults = await _customerRepository.Query()
    .Where(x => x.Active)
    .Include(x => x.Orders)
    .ProjectTo(x => new CustomerDto { ... })
    .Paginate(1, 20)
    .ToPagedResultAsync();
```

## When to Use Query Builder vs Specifications

### Use Query Builder when:
- ✅ Building ad-hoc queries in controllers/services
- ✅ Query logic is specific to one use case
- ✅ You need projection to DTOs
- ✅ You want fluent, readable query construction
- ✅ You're building dynamic queries (optional filters)

### Use Specifications when:
- ✅ Query logic is reusable across multiple places
- ✅ You need to unit test query logic
- ✅ Query represents a business rule
- ✅ You want to encapsulate complex query logic
- ✅ You need to compose specifications together

## Best Practices

1. **Always use AsNoTracking for read-only queries:**
   ```csharp
   var products = await _productRepository.Query()
       .AsNoTracking()
       .ToListAsync();
   ```

2. **Use projection for DTOs to avoid over-fetching:**
   ```csharp
   var dtos = await _repository.Query()
       .ProjectTo(x => new Dto { Id = x.Id, Name = x.Name })
       .ToListAsync();
   ```

3. **Apply filters before includes for better performance:**
   ```csharp
   // Good: Filter first
   var orders = await _repository.Query()
       .Where(x => x.Status == "Pending")
       .Include(x => x.Items)
       .ToListAsync();
   ```

4. **Use AsSplitQuery for multiple includes:**
   ```csharp
   var orders = await _repository.Query()
       .Include(x => x.Items)
       .Include(x => x.Customer)
       .Include(x => x.Shipment)
       .AsSplitQuery()
       .ToListAsync();
   ```

5. **Chain Where clauses for readability:**
   ```csharp
   var filtered = await _repository.Query()
       .Where(x => x.Active)
       .Where(x => x.Country == "USA")
       .Where(x => x.CreatedAt > cutoffDate)
       .ToListAsync();
   ```

## Summary

The Fluent Query Builder provides:
- 🎯 Type-safe query construction
- 📖 Readable, fluent API
- 🚀 Full feature support (includes, ordering, pagination, projection)
- ⚡ Performance options (AsNoTracking, AsSplitQuery)
- 🏢 Seamless integration with Multi-Tenancy and Caching
- 💪 IntelliSense support for better developer experience

It's a modern alternative to the traditional repository methods while maintaining full compatibility with all existing features!
