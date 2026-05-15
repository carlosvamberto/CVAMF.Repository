# Multi-Tenancy Support

CVAMF.Repository provides built-in support for multi-tenant applications where data must be isolated between different tenants (organizations, customers, etc.).

## 🎯 Overview

Multi-tenancy allows a single application instance to serve multiple tenants while keeping their data completely isolated. This library provides:

- **Automatic tenant filtering** on all queries
- **Automatic tenant assignment** on entity creation
- **Flexible tenant identification** (string, Guid, int, etc.)
- **Optional base classes** for quick implementation
- **Compatible with all features** (Soft Delete, Audit Fields, etc.)

## 📦 Basic Setup

### 1. Implement ITenantProvider

Create a service that provides the current tenant context:

```csharp
public class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentTenantId()
    {
        // Get tenant from claims, header, subdomain, etc.
        return _httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;
    }

    public bool HasTenantContext()
    {
        return !string.IsNullOrEmpty(GetCurrentTenantId());
    }
}
```

### 2. Register Services

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<ApplicationDbContext>();
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    return new UnitOfWork(context, tenantProvider);
});
```

### 3. Create Tenant Entities

Use base classes or implement `ITenantEntity`:

```csharp
// Option 1: Using base class
public class Product : EntityBaseTenant
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Option 2: Implement interface
public class Order : EntityBase, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}
```

## 🔧 Usage

### Automatic Filtering

All queries are automatically filtered by tenant:

```csharp
// Only returns products for the current tenant
var products = await _unitOfWork.GetRepository<Product, Guid>()
    .GetAllAsync();

// Filters are applied automatically
var expensiveProducts = await _unitOfWork.GetRepository<Product, Guid>()
    .GetAsync(filter: p => p.Price > 100);
```

### Automatic Tenant Assignment

Tenant ID is set automatically on entity creation:

```csharp
var product = new Product
{
    Name = "Laptop",
    Price = 999.99m
    // TenantId is set automatically
};

await _unitOfWork.GetRepository<Product, Guid>().AddAsync(product);
await _unitOfWork.CommitAsync();
```

### Paged Queries

Paging respects tenant isolation:

```csharp
var pagedProducts = await _unitOfWork.GetRepository<Product, Guid>()
    .GetPagedAsync(
        pageNumber: 1,
        pageSize: 20,
        filter: p => p.Price > 50,
        orderBy: q => q.OrderBy(p => p.Name)
    );

// TotalCount only includes current tenant's products
Console.WriteLine($"Total: {pagedProducts.TotalCount}");
```

## 🎨 Available Base Classes

### Simple Multi-Tenancy

```csharp
// Guid primary key + TenantId
public class MyEntity : EntityBaseTenant { }

// Int primary key + TenantId
public class MyEntity : EntityBaseTenantInt { }
```

### With Soft Delete

```csharp
// Guid + TenantId + IsDeleted
public class MyEntity : EntityBaseTenantSoftDelete { }

// Int + TenantId + IsDeleted
public class MyEntity : EntityBaseTenantSoftDeleteInt { }

// Guid + TenantId + Deleted
public class MyEntity : EntityBaseTenantSoftDeleteAlt { }

// Int + TenantId + Deleted
public class MyEntity : EntityBaseTenantSoftDeleteAltInt { }
```

### With Audit Fields

```csharp
// Guid + TenantId + Audit
public class MyEntity : EntityBaseTenantAuditable { }

// Int + TenantId + Audit
public class MyEntity : EntityBaseTenantAuditableInt { }
```

### Complete (Tenant + Audit + Soft Delete)

```csharp
// Guid + TenantId + Audit + IsDeleted
public class MyEntity : EntityBaseTenantAuditableSoftDelete { }

// Int + TenantId + Audit + IsDeleted
public class MyEntity : EntityBaseTenantAuditableSoftDeleteInt { }

// Guid + TenantId + Audit + Deleted
public class MyEntity : EntityBaseTenantAuditableSoftDeleteAlt { }

// Int + TenantId + Audit + Deleted
public class MyEntity : EntityBaseTenantAuditableSoftDeleteAltInt { }
```

## 🔐 Advanced Scenarios

### Custom Tenant Key Type

Use generic interfaces for custom tenant identifier types:

```csharp
public class Organization : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Custom provider
public class GuidTenantProvider : ITenantProvider<Guid>
{
    public Guid? GetCurrentTenantId()
    {
        // Your logic here
        return Guid.Parse("...");
    }

    public bool HasTenantContext()
    {
        var id = GetCurrentTenantId();
        return id.HasValue && id.Value != Guid.Empty;
    }
}
```

### Bypass Tenant Filtering

For admin scenarios, don't provide a tenant provider:

```csharp
// Repository without tenant provider - sees all data
var adminRepository = new Repository<Product, Guid>(_context);
var allProducts = await adminRepository.GetAllAsync();
```

### Testing with SimpleTenantProvider

Use the built-in simple provider for testing:

```csharp
[TestMethod]
public async Task Should_Filter_By_Tenant()
{
    var tenantProvider = new SimpleTenantProvider();
    tenantProvider.SetCurrentTenantId("tenant-1");

    var repository = new Repository<Product, Guid>(_context, tenantProvider);
    var products = await repository.GetAllAsync();

    Assert.IsTrue(products.All(p => p.TenantId == "tenant-1"));
}
```

## 🌐 Real-World Example

```csharp
// ASP.NET Core Web API
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Automatically filtered for current user's tenant
        var result = await _unitOfWork.GetRepository<Product, Guid>()
            .GetPagedAsync(
                pageNumber: page,
                pageSize: pageSize,
                orderBy: q => q.OrderBy(p => p.Name),
                asNoTracking: true
            );

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        // TenantId is set automatically from current user's context
        await _unitOfWork.GetRepository<Product, Guid>()
            .AddAsync(product, User.Identity?.Name);

        await _unitOfWork.CommitAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        // Only finds product if it belongs to current tenant
        var product = await _unitOfWork.GetRepository<Product, Guid>()
            .GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }
}
```

## ⚡ Performance Considerations

- Tenant filtering is applied at the database level (translated to SQL WHERE clause)
- No additional database queries are made for tenant validation
- Indexes on `TenantId` column are recommended for optimal performance

## 🔍 Database Indexing

Add indexes to tenant columns for better query performance:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.TenantId);

    modelBuilder.Entity<Order>()
        .HasIndex(o => o.TenantId);
}
```

## 🛡️ Security Notes

- **Never trust client input** for tenant identification
- Always get tenant ID from authenticated context (claims, session, etc.)
- Validate tenant access at the authentication/authorization level
- Consider using **Global Query Filters** in EF Core for defense-in-depth:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>().HasQueryFilter(p => 
        p.TenantId == _tenantProvider.GetCurrentTenantId());
}
```

## 📚 Integration with Other Features

Multi-tenancy works seamlessly with all library features:

```csharp
// Tenant + Soft Delete + Audit + Include
public class Invoice : EntityBaseTenantAuditableSoftDelete
{
    public string Number { get; set; } = string.Empty;
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

// Query with all features
var invoices = await _unitOfWork.GetRepository<Invoice, Guid>()
    .GetAsync(
        filter: i => !i.IsDeleted && i.CreatedAt >= DateTime.UtcNow.AddDays(-30),
        includes: q => q.Include(i => i.Items),
        asNoTracking: true,
        orderBy: q => q.OrderByDescending(i => i.CreatedAt)
    );
// Result: Only current tenant's non-deleted invoices from last 30 days with items
```
