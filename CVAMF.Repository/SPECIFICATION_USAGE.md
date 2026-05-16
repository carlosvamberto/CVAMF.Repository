# Specification Pattern Usage Guide

## Overview

The **Specification Pattern** is an optional feature that encapsulates query logic into reusable, testable classes. Instead of writing inline LINQ queries throughout your codebase, you create specification classes that define your query criteria, includes, ordering, and pagination.

## Benefits

✅ **Reusability**: Write query logic once, use it everywhere  
✅ **Testability**: Test specifications independently from repositories  
✅ **Maintainability**: Centralize complex query logic  
✅ **Composability**: Combine specifications for complex scenarios  
✅ **Type Safety**: Compile-time checking of query expressions  
✅ **Readability**: Self-documenting queries with descriptive class names

## Basic Usage

### 1. Create a Specification

```csharp
using CVAMF.Repository.Specifications;

public class ActiveProductsSpecification : Specification<Product>
{
    public ActiveProductsSpecification()
    {
        // Filter: Only active products
        AddCriteria(p => p.IsActive);

        // Include: Eager load category
        AddInclude(p => p.Category);

        // Order: Sort by name
        ApplyOrderBy(p => p.Name);

        // Performance: Use no tracking for read-only queries
        ApplyNoTracking();
    }
}
```

### 2. Use the Specification

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<IEnumerable<Product>> GetActiveProducts()
    {
        var spec = new ActiveProductsSpecification();
        return await _unitOfWork.Repository<Product, Guid>()
            .GetAsync(spec);
    }
}
```

## Advanced Specifications

### Parameterized Specification

```csharp
public class ProductsByCategorySpecification : Specification<Product>
{
    public ProductsByCategorySpecification(Guid categoryId, bool includeInactive = false)
    {
        // Dynamic criteria based on parameters
        if (includeInactive)
        {
            AddCriteria(p => p.CategoryId == categoryId);
        }
        else
        {
            AddCriteria(p => p.CategoryId == categoryId && p.IsActive);
        }

        AddInclude(p => p.Category);
        AddInclude(p => p.Reviews);
        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyNoTracking();
    }
}
```

### Paged Specification

```csharp
public class PagedProductsSpecification : Specification<Product>
{
    public PagedProductsSpecification(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        // Build dynamic criteria
        if (!string.IsNullOrEmpty(searchTerm))
        {
            AddCriteria(p => p.Name.Contains(searchTerm) || 
                           p.Description.Contains(searchTerm));
        }

        if (minPrice.HasValue)
        {
            AddCriteria(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            AddCriteria(p => p.Price <= maxPrice.Value);
        }

        // Always filter active
        AddCriteria(p => p.IsActive);

        // Includes
        AddInclude(p => p.Category);
        AddInclude("Reviews.User"); // String-based for nested includes

        // Ordering
        ApplyOrderByDescending(p => p.CreatedAt);

        // Pagination
        ApplyPaging(pageNumber, pageSize);

        // Performance
        ApplyNoTracking();
        ApplySplitQuery(); // Avoid cartesian explosion with multiple includes
    }
}
```

Usage:

```csharp
var spec = new PagedProductsSpecification(
    pageNumber: 1,
    pageSize: 20,
    searchTerm: "laptop",
    minPrice: 500,
    maxPrice: 2000);

var pagedResult = await _productRepository.GetPagedAsync(spec);

Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
Console.WriteLine($"Total items: {pagedResult.TotalItems}");
foreach (var product in pagedResult.Items)
{
    Console.WriteLine($"{product.Name} - ${product.Price}");
}
```

### Complex Specification with Multiple Includes

```csharp
public class OrderDetailsSpecification : Specification<Order>
{
    public OrderDetailsSpecification(Guid orderId)
    {
        AddCriteria(o => o.Id == orderId);

        // Multiple related entities
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        AddInclude("Items.Product"); // Nested include via string
        AddInclude("Items.Product.Category");
        AddInclude(o => o.ShippingAddress);

        // Use split query to avoid cartesian explosion
        ApplySplitQuery();

        // No tracking if read-only
        ApplyNoTracking();
    }
}
```

## Specification API Reference

### Filtering

```csharp
// Add criteria (WHERE clause)
AddCriteria(entity => entity.Property == value);
```

### Includes

```csharp
// Expression-based include
AddInclude(e => e.NavigationProperty);

// String-based include (for nested paths)
AddInclude("NavigationProperty.NestedProperty");
```

### Ordering

```csharp
// Ascending order
ApplyOrderBy(e => e.Property);

// Descending order
ApplyOrderByDescending(e => e.Property);
```

### Pagination

```csharp
// Manual skip/take
ApplySkip(20);
ApplyTake(10);

// Or use paging helper
ApplyPaging(pageNumber: 3, pageSize: 10); // Skip 20, Take 10
```

### Performance

```csharp
// Read-only queries (30-40% faster)
ApplyNoTracking();

// Split query for multiple includes
ApplySplitQuery();
```

### Advanced

```csharp
// Ignore global query filters (soft delete, tenant filtering)
// ⚠️ Use with caution!
ApplyIgnoreQueryFilters();

// Group by
ApplyGroupBy(e => e.Property);
```

## Repository Methods

```csharp
// Get collection
Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> spec);

// Get single entity
Task<TEntity?> GetFirstOrDefaultAsync(ISpecification<TEntity> spec);

// Get paged result
Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> spec);

// Get count
Task<int> CountAsync(ISpecification<TEntity> spec);

// Check existence
Task<bool> AnyAsync(ISpecification<TEntity> spec);
```

## Real-World Examples

### E-commerce Product Search

```csharp
public class ProductSearchSpecification : Specification<Product>
{
    public ProductSearchSpecification(ProductSearchCriteria criteria)
    {
        // Base filter
        AddCriteria(p => p.IsActive && p.Stock > 0);

        // Search term
        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            AddCriteria(p => p.Name.Contains(criteria.SearchTerm) ||
                           p.Description.Contains(criteria.SearchTerm) ||
                           p.SKU.Contains(criteria.SearchTerm));
        }

        // Category filter
        if (criteria.CategoryId.HasValue)
        {
            AddCriteria(p => p.CategoryId == criteria.CategoryId.Value);
        }

        // Price range
        if (criteria.MinPrice.HasValue)
        {
            AddCriteria(p => p.Price >= criteria.MinPrice.Value);
        }
        if (criteria.MaxPrice.HasValue)
        {
            AddCriteria(p => p.Price <= criteria.MaxPrice.Value);
        }

        // In stock only
        if (criteria.InStockOnly)
        {
            AddCriteria(p => p.Stock > 0);
        }

        // Includes
        AddInclude(p => p.Category);
        AddInclude(p => p.Images);

        // Sorting
        switch (criteria.SortBy)
        {
            case "price_asc":
                ApplyOrderBy(p => p.Price);
                break;
            case "price_desc":
                ApplyOrderByDescending(p => p.Price);
                break;
            case "name":
                ApplyOrderBy(p => p.Name);
                break;
            default:
                ApplyOrderByDescending(p => p.CreatedAt);
                break;
        }

        // Pagination
        ApplyPaging(criteria.PageNumber, criteria.PageSize);

        // Performance
        ApplyNoTracking();
    }
}
```

### Multi-Tenant Order Report

```csharp
public class TenantOrdersReportSpecification : Specification<Order>
{
    public TenantOrdersReportSpecification(
        DateTime startDate,
        DateTime endDate,
        OrderStatus? status = null)
    {
        // Date range
        AddCriteria(o => o.OrderDate >= startDate && o.OrderDate <= endDate);

        // Optional status filter
        if (status.HasValue)
        {
            AddCriteria(o => o.Status == status.Value);
        }

        // Includes for reporting
        AddInclude(o => o.Customer);
        AddInclude(o => o.Items);
        AddInclude("Items.Product");

        // Order by date
        ApplyOrderByDescending(o => o.OrderDate);

        // Read-only
        ApplyNoTracking();

        // Split query for performance
        ApplySplitQuery();

        // ⚠️ Tenant filtering is applied automatically by the repository
        // No need to add tenant criteria manually!
    }
}
```

### Soft-Deleted Items (Admin View)

```csharp
public class AllProductsIncludingDeletedSpecification : Specification<Product>
{
    public AllProductsIncludingDeletedSpecification(bool includeDeleted = false)
    {
        if (includeDeleted)
        {
            // Ignore soft delete global filter
            ApplyIgnoreQueryFilters();
        }
        else
        {
            // Only active (soft delete filter applied automatically)
            AddCriteria(p => p.IsActive);
        }

        AddInclude(p => p.Category);
        ApplyOrderBy(p => p.Name);
        ApplyNoTracking();
    }
}
```

## Testing Specifications

```csharp
[Fact]
public void ActiveProductsSpecification_Should_Have_Correct_Criteria()
{
    // Arrange
    var spec = new ActiveProductsSpecification();

    // Assert
    Assert.NotNull(spec.Criteria);
    Assert.True(spec.IsNoTracking);
    Assert.Single(spec.Includes);
    Assert.NotNull(spec.OrderBy);
}

[Fact]
public async Task ProductRepository_Should_Return_Active_Products_Only()
{
    // Arrange
    var spec = new ActiveProductsSpecification();

    // Act
    var products = await _productRepository.GetAsync(spec);

    // Assert
    Assert.All(products, p => Assert.True(p.IsActive));
}
```

## Best Practices

### ✅ DO

- Use descriptive specification class names (e.g., `ActiveProductsWithCategorySpecification`)
- Apply `ApplyNoTracking()` for read-only queries
- Use `ApplySplitQuery()` when loading multiple collections
- Keep specifications focused on a single use case
- Parameterize specifications for flexibility
- Test specifications independently

### ❌ DON'T

- Don't add tenant filtering manually (handled automatically by repository)
- Don't use `IgnoreQueryFilters()` unless absolutely necessary
- Don't create overly generic "god specifications"
- Don't forget pagination for large datasets
- Don't mix specification pattern with inline LINQ in the same method

## Performance Considerations

```csharp
// ❌ BAD: Cartesian explosion with multiple includes
public class SlowOrderSpecification : Specification<Order>
{
    public SlowOrderSpecification()
    {
        AddInclude(o => o.Items);
        AddInclude(o => o.Payments);
        AddInclude(o => o.Shipments);
        // Results in HUGE result set with duplicated data
    }
}

// ✅ GOOD: Split query avoids cartesian explosion
public class FastOrderSpecification : Specification<Order>
{
    public FastOrderSpecification()
    {
        AddInclude(o => o.Items);
        AddInclude(o => o.Payments);
        AddInclude(o => o.Shipments);
        ApplySplitQuery(); // Executes multiple optimized queries
        ApplyNoTracking(); // No change tracking overhead
    }
}
```

## Integration with Caching

Specifications work seamlessly with the caching feature:

```csharp
// Cache configuration
var cacheOptions = new CacheOptions
{
    CacheGetById = true,
    AutoInvalidateOnWrite = true
};

// GetByIdAsync uses cache automatically
var product = await _productRepository.GetByIdAsync(productId);

// Specification queries don't use cache by default
// (because they can be very dynamic)
var spec = new ActiveProductsSpecification();
var products = await _productRepository.GetAsync(spec);
```

## Migration Path

### Before (Inline LINQ)

```csharp
public async Task<IEnumerable<Product>> GetActiveProductsByCategory(Guid categoryId)
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive && p.CategoryId == categoryId,
        orderBy: q => q.OrderBy(p => p.Name),
        includes: q => q.Include(p => p.Category)
                       .Include(p => p.Reviews),
        asNoTracking: true);
}
```

### After (Specification Pattern)

```csharp
// Specification class (reusable)
public class ActiveProductsByCategorySpecification : Specification<Product>
{
    public ActiveProductsByCategorySpecification(Guid categoryId)
    {
        AddCriteria(p => p.IsActive && p.CategoryId == categoryId);
        AddInclude(p => p.Category);
        AddInclude(p => p.Reviews);
        ApplyOrderBy(p => p.Name);
        ApplyNoTracking();
    }
}

// Usage (clean and testable)
public async Task<IEnumerable<Product>> GetActiveProductsByCategory(Guid categoryId)
{
    var spec = new ActiveProductsByCategorySpecification(categoryId);
    return await _productRepository.GetAsync(spec);
}
```

## Summary

The Specification Pattern is **completely optional** but highly recommended for:

- Complex query logic
- Reusable queries across services
- Unit testing query logic
- Centralizing business rules
- Improving code maintainability

You can mix and match - use specifications where they add value, and use inline LINQ for simple one-off queries.
