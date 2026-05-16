# CVAMF.Repository

Generic Repository Pattern implementation for Entity Framework Core with support for filters, pagination, and multiple primary key types.

## Features

- ✅ Generic Repository Pattern for EF Core
- ✅ **Unit of Work pattern with transaction support**
- ✅ **Include (Eager Loading) support for related entities**
- ✅ **AsNoTracking for 30-40% faster read-only queries**
- ✅ **Soft Delete with flexible field naming (IsDeleted or Deleted)**
- ✅ **Audit Fields (optional CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)**
- ✅ **Multi-Tenancy support with automatic tenant isolation**
- ✅ **Caching support (Memory & Redis) with automatic invalidation**
- ✅ **Specification Pattern for reusable, testable query logic**
- ✅ **Multi-Targeting: Compatible with .NET 9.0 and 10.0**
- ✅ Support for Guid and Int primary keys
- ✅ Filtering with Expression Functions
- ✅ Optional pagination
- ✅ Full CRUD operations
- ✅ **Automatic transaction management**
- ✅ Async/await support
- ✅ Easy dependency injection integration

## Installation

```bash
dotnet add package CVAMF.Repository
```

Or via NuGet Package Manager (NPM):

```
Install-Package CVAMF.Repository
```

### Compatibility

This package supports **multiple .NET versions** through multi-targeting:

- ✅ **.NET 9.0** (with EF Core 9.x)
- ✅ **.NET 10.0** (with EF Core 10.x)

The correct version is **automatically selected** based on your project's target framework. No additional configuration needed!

📖 For more details on multi-targeting, see **[MULTITARGETING.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/MULTITARGETING.md)**.

## Quick Start

### 1. Create your entities

**For Guid primary key (recommended):**

```csharp
using CVAMF.Repository.Entities;

public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**For Int primary key:**

```csharp
using CVAMF.Repository.Entities;

public class Category : EntityBaseInt
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

**Custom entity implementing IEntity<T>:**

```csharp
using CVAMF.Repository.Entities;

public class Order : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

### 2. Configure your DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Your entity configurations here
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
```

### 3. Register services in Program.cs

**Option 1: Using Repositories only**

```csharp
using CVAMF.Repository.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add repositories - This registers IRepository<,> for all entities!
builder.Services.AddRepositories();

// Add your services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();

var app = builder.Build();
app.Run();
```

**Option 2: Using Unit of Work (Recommended for complex scenarios)**

```csharp
using CVAMF.Repository.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add both Repositories and Unit of Work
builder.Services.AddRepositoriesWithUnitOfWork<ApplicationDbContext>();

// Add your services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();
app.Run();
```

### 4. Use in your services

```csharp
using CVAMF.Repository.Interfaces;
using CVAMF.Repository.Models;

public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IRepository<Product, Guid> productRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    // CRUD operations examples below...
}
```

## Complete Usage Examples

### 📖 Read Operations

#### Get by ID

```csharp
public async Task<Product?> GetProductById(Guid productId)
{
    var product = await _productRepository.GetByIdAsync(productId);

    if (product == null)
    {
        _logger.LogWarning("Product {ProductId} not found", productId);
        return null;
    }

    return product;
}
```

#### Get All

```csharp
public async Task<IEnumerable<Product>> GetAllProducts()
{
    return await _productRepository.GetAllAsync();
}
```

#### Simple Filter

```csharp
public async Task<IEnumerable<Product>> GetActiveProducts()
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive);
}
```

#### Filter with Ordering

```csharp
public async Task<IEnumerable<Product>> GetProductsByCategory(string category)
{
    return await _productRepository.GetAsync(
        filter: p => p.Category == category && p.IsActive,
        orderBy: q => q.OrderBy(p => p.Name));
}
```

#### Complex Filters

```csharp
public async Task<IEnumerable<Product>> GetProductsInPriceRange(
    decimal minPrice, 
    decimal maxPrice, 
    string? searchTerm = null)
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive 
                  && p.Price >= minPrice 
                  && p.Price <= maxPrice
                  && (searchTerm == null || p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)),
        orderBy: q => q.OrderBy(p => p.Price).ThenBy(p => p.Name));
}
```

#### Multiple Sorting

```csharp
public async Task<IEnumerable<Product>> GetProductsSorted()
{
    return await _productRepository.GetAsync(
        filter: p => p.Stock > 0,
        orderBy: q => q.OrderBy(p => p.Category)
                       .ThenByDescending(p => p.Price)
                       .ThenBy(p => p.Name));
}
```

### 📄 Pagination Examples

#### Basic Pagination

```csharp
public async Task<PagedResult<Product>> GetProductsPaged(int pageNumber, int pageSize)
{
    return await _productRepository.GetPagedAsync(
        pageNumber: pageNumber,
        pageSize: pageSize,
        filter: p => p.IsActive,
        orderBy: q => q.OrderBy(p => p.Name));
}
```

#### Advanced Pagination with Search

```csharp
public async Task<PagedResult<Product>> SearchProducts(
    string searchTerm,
    int page = 1,
    int pageSize = 10,
    string sortBy = "name",
    bool descending = false)
{
    var pagedResult = await _productRepository.GetPagedAsync(
        pageNumber: page,
        pageSize: pageSize,
        filter: p => p.IsActive && (
            p.Name.Contains(searchTerm) ||
            p.Description.Contains(searchTerm) ||
            p.Category.Contains(searchTerm)
        ),
        orderBy: sortBy.ToLower() switch
        {
            "price" => descending 
                ? q => q.OrderByDescending(p => p.Price)
                : q => q.OrderBy(p => p.Price),
            "date" => descending
                ? q => q.OrderByDescending(p => p.CreatedAt)
                : q => q.OrderBy(p => p.CreatedAt),
            _ => descending
                ? q => q.OrderByDescending(p => p.Name)
                : q => q.OrderBy(p => p.Name)
        });

    _logger.LogInformation(
        "Found {TotalCount} products. Showing page {Page} of {TotalPages}",
        pagedResult.TotalCount,
        pagedResult.PageNumber,
        pagedResult.TotalPages);

    return pagedResult;
}
```

#### Display Pagination Info

```csharp
public async Task DisplayProductsPaged()
{
    var pagedResult = await _productRepository.GetPagedAsync(
        pageNumber: 1,
        pageSize: 10,
        filter: p => p.IsActive);

    Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
    Console.WriteLine($"Total items: {pagedResult.TotalCount}");
    Console.WriteLine($"Items per page: {pagedResult.PageSize}");
    Console.WriteLine($"Has previous: {pagedResult.HasPreviousPage}");
    Console.WriteLine($"Has next: {pagedResult.HasNextPage}");
    Console.WriteLine();

    foreach (var product in pagedResult.Items)
    {
        Console.WriteLine($"- {product.Name} (${product.Price})");
    }
}
```

### ✏️ Create Operations

#### Add Single Entity

```csharp
public async Task<Product> CreateProduct(string name, decimal price, string category)
{
    var product = new Product
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
        Category = category,
        IsActive = true,
        Stock = 0,
        CreatedAt = DateTime.UtcNow
    };

    await _productRepository.AddAsync(product);
    await _productRepository.SaveChangesAsync();

    _logger.LogInformation("Product {ProductName} created with ID {ProductId}", 
        product.Name, product.Id);

    return product;
}
```

#### Add Multiple Entities

```csharp
public async Task<int> ImportProducts(List<ProductDto> productDtos)
{
    var products = productDtos.Select(dto => new Product
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Price = dto.Price,
        Category = dto.Category,
        IsActive = true,
        Stock = dto.Stock,
        CreatedAt = DateTime.UtcNow
    }).ToList();

    await _productRepository.AddRangeAsync(products);
    var savedCount = await _productRepository.SaveChangesAsync();

    _logger.LogInformation("Imported {Count} products", savedCount);

    return savedCount;
}
```

### 🔄 Update Operations

#### Update Single Entity

```csharp
public async Task<bool> UpdateProductPrice(Guid productId, decimal newPrice)
{
    var product = await _productRepository.GetByIdAsync(productId);

    if (product == null)
    {
        _logger.LogWarning("Product {ProductId} not found", productId);
        return false;
    }

    product.Price = newPrice;

    await _productRepository.UpdateAsync(product);
    await _productRepository.SaveChangesAsync();

    _logger.LogInformation("Product {ProductId} price updated to {Price}", 
        productId, newPrice);

    return true;
}
```

#### Update Multiple Entities

```csharp
public async Task<int> ApplyDiscountToCategory(string category, decimal discountPercent)
{
    var products = await _productRepository.GetAsync(
        filter: p => p.Category == category && p.IsActive);

    foreach (var product in products)
    {
        product.Price = product.Price * (1 - discountPercent / 100);
    }

    await _productRepository.UpdateRangeAsync(products);
    var updatedCount = await _productRepository.SaveChangesAsync();

    _logger.LogInformation(
        "Applied {Discount}% discount to {Count} products in category {Category}",
        discountPercent, updatedCount, category);

    return updatedCount;
}
```

#### Conditional Update

```csharp
public async Task<int> DeactivateLowStockProducts()
{
    var lowStockProducts = await _productRepository.GetAsync(
        filter: p => p.Stock < 5 && p.IsActive);

    foreach (var product in lowStockProducts)
    {
        product.IsActive = false;
    }

    await _productRepository.UpdateRangeAsync(lowStockProducts);
    return await _productRepository.SaveChangesAsync();
}
```

### 🗑️ Delete Operations

#### Delete by ID

```csharp
public async Task<bool> DeleteProduct(Guid productId)
{
    var product = await _productRepository.GetByIdAsync(productId);

    if (product == null)
    {
        return false;
    }

    await _productRepository.DeleteAsync(productId);
    await _productRepository.SaveChangesAsync();

    _logger.LogInformation("Product {ProductId} deleted", productId);

    return true;
}
```

#### Delete by Entity

```csharp
public async Task DeleteInactiveProduct(string productName)
{
    var product = await _productRepository.GetFirstOrDefaultAsync(
        filter: p => p.Name == productName && !p.IsActive);

    if (product != null)
    {
        await _productRepository.DeleteAsync(product);
        await _productRepository.SaveChangesAsync();
    }
}
```

#### Delete Multiple Entities

```csharp
public async Task<int> CleanupOldInactiveProducts(int daysOld)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

    var oldProducts = await _productRepository.GetAsync(
        filter: p => !p.IsActive && p.CreatedAt < cutoffDate);

    if (oldProducts.Any())
    {
        await _productRepository.DeleteRangeAsync(oldProducts);
        var deletedCount = await _productRepository.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} old inactive products", deletedCount);
        return deletedCount;
    }

    return 0;
}
```

### 🔍 Query Helper Methods

#### Check Existence

```csharp
public async Task<bool> ProductExists(string name)
{
    return await _productRepository.AnyAsync(
        filter: p => p.Name == name);
}

public async Task<bool> HasProductsInCategory(string category)
{
    return await _productRepository.AnyAsync(
        filter: p => p.Category == category && p.IsActive);
}
```

#### Count

```csharp
public async Task<int> GetTotalProducts()
{
    return await _productRepository.CountAsync();
}

public async Task<int> GetActiveProductCount()
{
    return await _productRepository.CountAsync(
        filter: p => p.IsActive);
}

public async Task<Dictionary<string, int>> GetProductCountByCategory()
{
    var categories = await _productRepository.GetAsync(
        filter: p => p.IsActive);

    return categories
        .GroupBy(p => p.Category)
        .ToDictionary(g => g.Key, g => g.Count());
}
```

#### First or Default

```csharp
public async Task<Product?> GetMostExpensiveProduct()
{
    var products = await _productRepository.GetAsync(
        filter: p => p.IsActive,
        orderBy: q => q.OrderByDescending(p => p.Price));

    return products.FirstOrDefault();
}

public async Task<Product?> FindProductByName(string name)
{
    return await _productRepository.GetFirstOrDefaultAsync(
        filter: p => p.Name == name && p.IsActive);
}
```

## Using with Int Primary Keys

```csharp
public class CategoryService
{
    private readonly IRepository<Category, int> _categoryRepository;

    public CategoryService(IRepository<Category, int> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Category> CreateCategory(string name)
    {
        var category = new Category
        {
            // No need to set Id for int (auto-increment)
            Name = name,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return category;
    }

    public async Task<Category?> GetCategoryById(int categoryId)
    {
        return await _categoryRepository.GetByIdAsync(categoryId);
    }
}
```

## Advanced Scenarios

### Using Unit of Work for Complex Transactions

The **Unit of Work** pattern is ideal when you need to coordinate multiple repositories in a single transaction. This ensures data consistency across multiple operations.

#### Simple Transaction Example

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(Order order, List<OrderItem> items)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Get repositories
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();
            var productRepo = _unitOfWork.Repository<Product, Guid>();

            // Create order
            await orderRepo.AddAsync(order);

            // Add order items and update stock
            foreach (var item in items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId);

                if (product == null)
                    throw new InvalidOperationException($"Product {item.ProductId} not found");

                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}");

                // Update stock
                product.Stock -= item.Quantity;
                await productRepo.UpdateAsync(product);

                // Add order item
                item.OrderId = order.Id;
                await itemRepo.AddAsync(item);
            }

            // All operations are committed together
            // If any fails, all are rolled back automatically
            return order;
        });
    }
}
```

#### Manual Transaction Control

```csharp
public async Task<bool> ProcessComplexOrderAsync(Order order)
{
    await using var transaction = await _unitOfWork.BeginTransactionAsync();

    try
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var inventoryRepo = _unitOfWork.Repository<Inventory, Guid>();

        // Step 1: Create order
        await orderRepo.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Step 2: Update inventory
        // ... inventory operations
        await _unitOfWork.SaveChangesAsync();

        // Step 3: Process payment
        // ... payment operations
        await _unitOfWork.SaveChangesAsync();

        // Commit if everything succeeded
        await transaction.CommitAsync();
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing order");
        await transaction.RollbackAsync();
        return false;
    }
}
```

#### Multiple Repositories Coordination

```csharp
public async Task<bool> TransferInventoryAsync(Guid fromWarehouseId, Guid toWarehouseId, Guid productId, int quantity)
{
    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var warehouseRepo = _unitOfWork.Repository<Warehouse, Guid>();
        var inventoryRepo = _unitOfWork.Repository<Inventory, Guid>();
        var logRepo = _unitOfWork.Repository<InventoryLog, int>();

        var fromWarehouse = await warehouseRepo.GetByIdAsync(fromWarehouseId);
        var toWarehouse = await warehouseRepo.GetByIdAsync(toWarehouseId);

        if (fromWarehouse == null || toWarehouse == null)
            throw new InvalidOperationException("Warehouse not found");

        // Deduct from source
        var sourceInventory = await inventoryRepo.GetFirstOrDefaultAsync(
            i => i.WarehouseId == fromWarehouseId && i.ProductId == productId);

        if (sourceInventory == null || sourceInventory.Quantity < quantity)
            throw new InvalidOperationException("Insufficient inventory");

        sourceInventory.Quantity -= quantity;
        await inventoryRepo.UpdateAsync(sourceInventory);

        // Add to destination
        var destInventory = await inventoryRepo.GetFirstOrDefaultAsync(
            i => i.WarehouseId == toWarehouseId && i.ProductId == productId);

        if (destInventory == null)
        {
            destInventory = new Inventory
            {
                Id = Guid.NewGuid(),
                WarehouseId = toWarehouseId,
                ProductId = productId,
                Quantity = quantity
            };
            await inventoryRepo.AddAsync(destInventory);
        }
        else
        {
            destInventory.Quantity += quantity;
            await inventoryRepo.UpdateAsync(destInventory);
        }

        // Log the transfer
        await logRepo.AddAsync(new InventoryLog
        {
            ProductId = productId,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Quantity = quantity,
            TransferDate = DateTime.UtcNow
        });

        return true;
    });
}
```

### 📚 Complete Unit of Work Documentation

For detailed examples and advanced usage patterns, see **[UNITOFWORK_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/UNITOFWORK_USAGE.md)** which includes:

- ✅ Basic setup and configuration
- ✅ Transaction management strategies
- ✅ Error handling patterns
- ✅ Nested transactions
- ✅ API controller examples
- ✅ Best practices and performance tips

### 🔗 Loading Related Entities with Include

All repository methods support **Include (Eager Loading)** to load related entities and avoid N+1 query problems.

#### GetByIdAsync with Include

```csharp
// Load Order with Items and Customer
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .Include(o => o.Customer));
```

#### GetAsync with Include

```csharp
// Load active orders with all details
var activeOrders = await _orderRepository.GetAsync(
    filter: o => o.Status == "Active",
    orderBy: q => q.OrderByDescending(o => o.OrderDate),
    includes: q => q.Include(o => o.Items)
                    .Include(o => o.Customer));
```

#### GetPagedAsync with Include

```csharp
// Paginated orders with related data
var pagedOrders = await _orderRepository.GetPagedAsync(
    pageNumber: 1,
    pageSize: 10,
    filter: o => o.Status == "Pending",
    orderBy: q => q.OrderByDescending(o => o.OrderDate),
    includes: q => q.Include(o => o.Items)
                    .ThenInclude(i => i.Product));
```

#### Multiple Includes

```csharp
// Load product with all related entities
var product = await _productRepository.GetByIdAsync(
    productId,
    includes: q => q.Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Include(p => p.Reviews)
                    .Include(p => p.Images));
```

#### 📖 Complete Include Documentation

For comprehensive examples, performance tips, and best practices, see **[INCLUDE_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/INCLUDE_USAGE.md)** which includes:

- ✅ All methods with Include support
- ✅ ThenInclude for nested relationships
- ✅ Multiple includes examples
- ✅ Performance optimization
- ✅ Avoiding N+1 queries
- ✅ Real-world scenarios

### ⚡ Performance Optimization with AsNoTracking

All query methods support **AsNoTracking** for **30-40% faster** read-only queries. When entities don't need to be tracked for changes, use `asNoTracking: true`.

#### When to Use AsNoTracking

```csharp
// ✅ RECOMMENDED: Lists and grids (read-only display)
var products = await _productRepository.GetAsync(
    filter: p => p.IsActive,
    orderBy: q => q.OrderBy(p => p.Name),
    includes: q => q.Include(p => p.Category),
    asNoTracking: true); // 30-40% faster!

// ✅ RECOMMENDED: Pagination (large result sets)
var pagedOrders = await _orderRepository.GetPagedAsync(
    pageNumber: 1,
    pageSize: 10,
    filter: o => o.Status == "Pending",
    orderBy: q => q.OrderByDescending(o => o.OrderDate),
    includes: q => q.Include(o => o.Items),
    asNoTracking: true); // Much faster for lists

// ✅ RECOMMENDED: API GET endpoints returning DTOs
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items)
                    .Include(o => o.Customer),
    asNoTracking: true);

var dto = new OrderDto
{
    Id = order.Id,
    OrderNumber = order.OrderNumber,
    CustomerName = order.Customer.Name
    // ... map to DTO
};
```

#### When NOT to Use AsNoTracking

```csharp
// ❌ DON'T USE: When you need to update the entity
var product = await _productRepository.GetByIdAsync(
    productId,
    asNoTracking: false); // or omit parameter (default is false)

product.Price = newPrice;
await _productRepository.UpdateAsync(product);
await _productRepository.SaveChangesAsync();
```

#### Performance Comparison

|    Operation       | With Tracking  | AsNoTracking       |
|--------------------|----------------|--------------------|
| Query 1000 records | 250ms          | 150ms (40% faster) |
| Memory usage       | 15MB           | 9MB (40% less)     |
| Query 10 records   | 25ms           | 18ms (28% faster)  |

#### 📖 Complete AsNoTracking Documentation

For detailed examples and best practices, see **[ASNOTRACKING_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/ASNOTRACKING_USAGE.md)** which includes:

- ✅ When to use vs when NOT to use
- ✅ API controller examples
- ✅ CQRS patterns
- ✅ Performance benchmarks
- ✅ Common pitfalls and solutions

### 🗑️ Soft Delete Support

**Soft Delete** allows marking records as deleted without physically removing them from the database. You can choose between **two field naming conventions**: `IsDeleted` or `Deleted`.

#### Quick Start

**Option 1: Using `IsDeleted` (recommended):**

```csharp
using CVAMF.Repository.Entities;

public class Product : EntityBaseSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // IsDeleted, DeletedAt, DeletedBy are inherited
}
```

**Option 2: Using `Deleted` (alternative):**

```csharp
using CVAMF.Repository.Entities;

public class Product : EntityBaseSoftDeleteAlt
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Deleted, DeletedAt, DeletedBy are inherited
}
```

#### Basic Usage

```csharp
// Soft delete a product
await _productRepository.SoftDeleteAsync(productId, "admin@example.com");
await _productRepository.SaveChangesAsync();

// Restore a soft deleted product
await _productRepository.RestoreAsync(productId);
await _productRepository.SaveChangesAsync();

// Soft delete multiple entities
var oldProducts = await _productRepository.GetAsync(
    filter: p => p.CreatedAt < DateTime.UtcNow.AddYears(-5));

var count = await _productRepository.SoftDeleteRangeAsync(oldProducts, "system");
await _productRepository.SaveChangesAsync();
```

#### Global Query Filter (Recommended)

Configure automatic filtering of soft deleted records:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // For ISoftDeletable (IsDeleted)
    modelBuilder.Entity<Product>()
        .HasQueryFilter(p => !p.IsDeleted);

    // For ISoftDeletableAlternative (Deleted)
    // modelBuilder.Entity<Product>()
    //     .HasQueryFilter(p => !p.Deleted);
}
```

#### 📖 Complete Soft Delete Documentation

For comprehensive examples and migration guide, see **[SOFTDELETE_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/SOFTDELETE_USAGE.md)** which includes:

- ✅ Choosing field names (`IsDeleted` vs `Deleted`)
- ✅ Base classes and interfaces available
- ✅ Soft delete, restore, and bulk operations
- ✅ Global query filters setup
- ✅ When to use soft delete vs physical delete
- ✅ Migration guide from physical to soft delete
- ✅ Performance optimization with indexes
- ✅ Complete examples with UnitOfWork

### 📝 Audit Fields Support (Optional)

**Audit Fields** automatically track **who** and **when** created or modified entities. This is completely **optional** and only applied when you use the overloads with audit parameters.

#### Quick Start

**Option 1: Using base class with audit:**

```csharp
using CVAMF.Repository.Entities;

public class Product : EntityBaseAuditable
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy are inherited
}
```

**Option 2: Audit + Soft Delete (all together):**

```csharp
public class Product : EntityBaseAuditableSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Audit: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    // Soft Delete: IsDeleted, DeletedAt, DeletedBy
}
```

#### Basic Usage

```csharp
// Create with audit
var product = new Product { Name = "Laptop", Price = 1299.99m };
await _productRepository.AddAsync(product, "admin@example.com");
await _productRepository.SaveChangesAsync();
// Result: product.CreatedAt and product.CreatedBy are set automatically

// Update with audit
product.Price = 999.99m;
await _productRepository.UpdateAsync(product, "manager@example.com");
await _productRepository.SaveChangesAsync();
// Result: product.UpdatedAt and product.UpdatedBy are set automatically

// Without audit (optional)
await _productRepository.AddAsync(product); // No audit info filled
```

#### Available Base Classes

- `EntityBaseAuditable` (Guid) / `EntityBaseAuditableInt` (int) - Only audit fields
- `EntityBaseAuditableSoftDelete` (Guid) / `EntityBaseAuditableSoftDeleteInt` (int) - Audit + Soft Delete (IsDeleted)
- `EntityBaseAuditableSoftDeleteAlt` (Guid) / `EntityBaseAuditableSoftDeleteAltInt` (int) - Audit + Soft Delete (Deleted)

#### 📖 Complete Audit Documentation

For comprehensive examples and ASP.NET Core integration, see **[AUDIT_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/AUDIT_USAGE.md)** which includes:

- ✅ All available base classes and interfaces
- ✅ Automatic vs manual audit field population
- ✅ Combining with Soft Delete and other features
- ✅ Audit queries and reporting
- ✅ ASP.NET Core integration (getting current user)
- ✅ DbContext configuration and indexes
- ✅ Complete real-world examples

### 🏢 Multi-Tenancy Support

**Multi-Tenancy** enables your application to serve multiple tenants (organizations, customers) with complete data isolation. The library provides **automatic tenant filtering** and **tenant assignment** without any manual work.

#### Quick Start

**1. Implement ITenantProvider:**

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
        return _httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;
    }

    public bool HasTenantContext()
    {
        return !string.IsNullOrEmpty(GetCurrentTenantId());
    }
}
```

**2. Create tenant entities:**

```csharp
public class Product : EntityBaseTenant
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    // TenantId is inherited
}
```

**3. Register services:**

```csharp
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<ApplicationDbContext>();
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    return new UnitOfWork(context, tenantProvider);
});
```

**4. Use normally - automatic tenant isolation:**

```csharp
// Only returns current tenant's products
var products = await _unitOfWork.Repository<Product, Guid>().GetAllAsync();

// TenantId is set automatically
var product = new Product { Name = "Laptop", Price = 999 };
await _unitOfWork.Repository<Product, Guid>().AddAsync(product);
await _unitOfWork.CommitAsync();
```

#### Available Multi-Tenant Base Classes

```csharp
// Simple tenant support
EntityBaseTenant / EntityBaseTenantInt

// With Soft Delete
EntityBaseTenantSoftDelete / EntityBaseTenantSoftDeleteInt
EntityBaseTenantSoftDeleteAlt / EntityBaseTenantSoftDeleteAltInt

// With Audit
EntityBaseTenantAuditable / EntityBaseTenantAuditableInt

// Complete (Tenant + Audit + Soft Delete)
EntityBaseTenantAuditableSoftDelete / EntityBaseTenantAuditableSoftDeleteInt
EntityBaseTenantAuditableSoftDeleteAlt / EntityBaseTenantAuditableSoftDeleteAltInt
```

#### 📖 Complete Multi-Tenancy Documentation

For comprehensive examples, advanced scenarios, and security best practices, see **[MULTITENANCY_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/MULTITENANCY_USAGE.md)** which includes:

- ✅ Automatic tenant filtering in all queries
- ✅ Automatic tenant assignment on entity creation
- ✅ Custom tenant identifier types (string, Guid, int)
- ✅ Integration with authentication/authorization
- ✅ Testing strategies
- ✅ Performance optimization and indexing
- ✅ Security best practices
- ✅ Real-world API examples

### ⚡ Caching Support (Memory & Redis)

**Caching** dramatically improves performance by reducing database round-trips for frequently accessed data. The library provides **automatic caching** with both in-memory and distributed Redis implementations.

#### Quick Start

**1. Memory Cache (Development / Single Server):**

```csharp
using CVAMF.Repository.Caching;

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

builder.Services.AddScoped<CacheOptions>(sp => new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(10),
    CacheGetById = true,
    AutoInvalidateOnWrite = true
});

builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var context = sp.GetRequiredService<ApplicationDbContext>();
    var cacheService = sp.GetRequiredService<ICacheService>();
    var cacheOptions = sp.GetRequiredService<CacheOptions>();

    return new UnitOfWork(context, null, cacheService, cacheOptions);
});
```

**2. Redis Cache (Production / Distributed):**

```csharp
using CVAMF.Repository.Caching;
using StackExchange.Redis;

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse("localhost:6379");
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddScoped<ICacheService, RedisAdvancedCacheService>();

builder.Services.AddScoped<CacheOptions>(sp => new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(30),
    KeyPrefix = "MyApp:Repo:",
    CacheGetById = true
});
```

**3. Use normally - automatic caching:**

```csharp
// First call - hits database and caches result
var product = await _unitOfWork.Repository<Product, Guid>()
    .GetByIdAsync(productId);

// Second call - returns from cache (no database hit!)
var cachedProduct = await _unitOfWork.Repository<Product, Guid>()
    .GetByIdAsync(productId);

// Update - automatically invalidates cache
product.Price = 99.99m;
await _unitOfWork.Repository<Product, Guid>().UpdateAsync(product);
await _unitOfWork.SaveChangesAsync();
// Next GetById will hit database again
```

#### Performance Comparison

```csharp
// Without cache: 100 calls = 100 database queries (~5,000ms)
// With cache: 100 calls = 1 database query + 99 cache hits (~150ms)
// Result: 96% faster!
```

#### Cache Configuration Options

```csharp
var cacheOptions = new CacheOptions
{
    DefaultExpiration = TimeSpan.FromMinutes(10),     // Cache TTL
    SlidingExpiration = null,                         // Optional sliding window
    KeyPrefix = "CVAMF.Repository:",                  // Cache key prefix
    CacheGetById = true,                              // ✅ Cache GetById (recommended)
    CacheGetAll = false,                              // ❌ Usually false
    CacheGetPaged = false,                            // ❌ Usually false
    CacheGetAsync = false,                            // ❌ Usually false
    AutoInvalidateOnWrite = true,                     // ✅ Auto-invalidate on write
    UseEntityTypeInKey = true,                        // ✅ Include entity type
    UseTenantIdInKey = true                           // ✅ Include tenant ID
};
```

#### Available Implementations

```csharp
// MemoryCacheService - In-memory cache (single server)
// RedisCacheService - Redis via IDistributedCache (simple)
// RedisAdvancedCacheService - Redis via IConnectionMultiplexer (advanced features)
```

#### 📖 Complete Cache Documentation

For detailed setup, performance tips, and advanced scenarios, see **[CACHE_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/CACHE_USAGE.md)** which includes:

- ✅ Memory Cache and Redis setup
- ✅ Configuration options and best practices
- ✅ Automatic cache invalidation
- ✅ Multi-tenancy cache isolation
- ✅ Performance benchmarks
- ✅ Manual cache operations
- ✅ Redis monitoring and debugging
- ✅ Real-world examples

### 🎯 Specification Pattern (Optional)

The **Specification Pattern** encapsulates query logic into reusable, testable classes, making your codebase cleaner and more maintainable.

#### Why Use Specifications?

✅ **Reusable**: Write query logic once, use it everywhere  
✅ **Testable**: Test specifications independently  
✅ **Maintainable**: Centralize complex query logic  
✅ **Type-Safe**: Compile-time checking  
✅ **Self-Documenting**: Descriptive class names explain intent

#### Quick Example

**1. Create a Specification:**

```csharp
using CVAMF.Repository.Specifications;

public class ActiveProductsSpecification : Specification<Product>
{
    public ActiveProductsSpecification()
    {
        AddCriteria(p => p.IsActive);
        AddInclude(p => p.Category);
        ApplyOrderBy(p => p.Name);
        ApplyNoTracking();
    }
}
```

**2. Use the Specification:**

```csharp
// Clean, readable, and reusable!
var spec = new ActiveProductsSpecification();
var products = await _productRepository.GetAsync(spec);
```

#### Parameterized Specification

```csharp
public class PagedProductsSpecification : Specification<Product>
{
    public PagedProductsSpecification(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        decimal? minPrice = null)
    {
        // Dynamic criteria
        AddCriteria(p => p.IsActive);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            AddCriteria(p => p.Name.Contains(searchTerm));
        }

        if (minPrice.HasValue)
        {
            AddCriteria(p => p.Price >= minPrice.Value);
        }

        // Includes and ordering
        AddInclude(p => p.Category);
        ApplyOrderByDescending(p => p.CreatedAt);

        // Pagination
        ApplyPaging(pageNumber, pageSize);

        // Performance
        ApplyNoTracking();
    }
}

// Usage
var spec = new PagedProductsSpecification(1, 20, "laptop", 500);
var pagedProducts = await _productRepository.GetPagedAsync(spec);
```

#### Available Specification Methods

```csharp
AddCriteria(e => e.Property == value);           // WHERE clause
AddInclude(e => e.NavigationProperty);           // Eager loading
AddInclude("Nav.Nested");                        // String-based include
ApplyOrderBy(e => e.Property);                   // ORDER BY ASC
ApplyOrderByDescending(e => e.Property);         // ORDER BY DESC
ApplyPaging(pageNumber, pageSize);               // Skip/Take
ApplyNoTracking();                               // Read-only (faster)
ApplySplitQuery();                               // Avoid cartesian explosion
ApplyIgnoreQueryFilters();                       // Ignore soft delete/tenant filters
```

#### Repository Methods

```csharp
// Get collection
await _repository.GetAsync(specification);

// Get single
await _repository.GetFirstOrDefaultAsync(specification);

// Get paged
await _repository.GetPagedAsync(specification);

// Get count
await _repository.CountAsync(specification);

// Check existence
await _repository.AnyAsync(specification);
```

#### Real-World Example: E-commerce Search

```csharp
public class ProductSearchSpecification : Specification<Product>
{
    public ProductSearchSpecification(ProductSearchCriteria criteria)
    {
        // Base filters
        AddCriteria(p => p.IsActive && p.Stock > 0);

        // Search
        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            AddCriteria(p => p.Name.Contains(criteria.SearchTerm) ||
                           p.Description.Contains(criteria.SearchTerm));
        }

        // Category
        if (criteria.CategoryId.HasValue)
        {
            AddCriteria(p => p.CategoryId == criteria.CategoryId.Value);
        }

        // Price range
        if (criteria.MinPrice.HasValue)
            AddCriteria(p => p.Price >= criteria.MinPrice.Value);
        if (criteria.MaxPrice.HasValue)
            AddCriteria(p => p.Price <= criteria.MaxPrice.Value);

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

#### Before vs After

**❌ Before (Inline LINQ everywhere):**

```csharp
public async Task<IEnumerable<Product>> GetActiveProductsByCategory(Guid categoryId)
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive && p.CategoryId == categoryId,
        orderBy: q => q.OrderBy(p => p.Name),
        includes: q => q.Include(p => p.Category).Include(p => p.Reviews),
        asNoTracking: true);
}
```

**✅ After (Specification Pattern):**

```csharp
// Reusable specification
public class ActiveProductsByCategorySpec : Specification<Product>
{
    public ActiveProductsByCategorySpec(Guid categoryId)
    {
        AddCriteria(p => p.IsActive && p.CategoryId == categoryId);
        AddInclude(p => p.Category);
        AddInclude(p => p.Reviews);
        ApplyOrderBy(p => p.Name);
        ApplyNoTracking();
    }
}

// Clean usage
public async Task<IEnumerable<Product>> GetActiveProductsByCategory(Guid categoryId)
{
    return await _productRepository.GetAsync(
        new ActiveProductsByCategorySpec(categoryId));
}
```

#### 📖 Complete Specification Documentation

For comprehensive examples, testing strategies, and advanced patterns, see **[SPECIFICATION_USAGE.md](https://github.com/carlosvamberto/CVAMF.Repository/blob/master/CVAMF.Repository/SPECIFICATION_USAGE.md)** which includes:

- ✅ Basic and advanced specifications
- ✅ Parameterized specifications
- ✅ Pagination and filtering
- ✅ Performance optimization (NoTracking, SplitQuery)
- ✅ Testing specifications
- ✅ Real-world e-commerce examples
- ✅ Best practices and anti-patterns
- ✅ Integration with caching and multi-tenancy

> **Note:** Specification Pattern is **completely optional**. Use it for complex queries and when you need reusability. For simple one-off queries, inline LINQ is perfectly fine!

### Transaction Example (Without Unit of Work)

```csharp
public async Task<bool> TransferStock(Guid fromProductId, Guid toProductId, int quantity)
{
    var fromProduct = await _productRepository.GetByIdAsync(fromProductId);
    var toProduct = await _productRepository.GetByIdAsync(toProductId);

    if (fromProduct == null || toProduct == null || fromProduct.Stock < quantity)
    {
        return false;
    }

    try
    {
        fromProduct.Stock -= quantity;
        toProduct.Stock += quantity;

        await _productRepository.UpdateAsync(fromProduct);
        await _productRepository.UpdateAsync(toProduct);

        // Both updates are saved in a single transaction
        await _productRepository.SaveChangesAsync();

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error transferring stock");
        return false;
    }
}
```

### Working with DTOs

```csharp
public async Task<List<ProductListDto>> GetProductList(string category)
{
    var products = await _productRepository.GetAsync(
        filter: p => p.Category == category && p.IsActive,
        orderBy: q => q.OrderBy(p => p.Name));

    return products.Select(p => new ProductListDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        InStock = p.Stock > 0
    }).ToList();
}
```

### Soft Delete Pattern

```csharp
// Add DeletedAt property to your entity
public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// Implement soft delete
public async Task SoftDeleteProduct(Guid productId)
{
    var product = await _productRepository.GetByIdAsync(productId);

    if (product != null)
    {
        product.DeletedAt = DateTime.UtcNow;
        product.IsActive = false;

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
    }
}

// Get only non-deleted products
public async Task<IEnumerable<Product>> GetActiveNonDeletedProducts()
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive && p.DeletedAt == null);
}
```

## Best Practices

### ✅ DO

- Always call `SaveChangesAsync()` after Add, Update, or Delete operations
- Use pagination for large datasets
- Use specific filters to reduce database load
- Handle null returns from `GetByIdAsync` and `GetFirstOrDefaultAsync`
- Use `CancellationToken` for long-running operations
- Inject `IRepository<TEntity, TKey>` instead of concrete implementations

### ❌ DON'T

- Don't forget to call `SaveChangesAsync()` - changes won't persist!
- Don't load all data without filters unless necessary
- Don't use magic strings - use constants or enums
- Don't ignore exceptions - always log and handle errors

## API Reference

### Query Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `GetByIdAsync(id)` | Get entity by primary key | `TEntity?` |
| `GetAllAsync()` | Get all entities | `IEnumerable<TEntity>` |
| `GetAsync(filter, orderBy)` | Get filtered/ordered entities | `IEnumerable<TEntity>` |
| `GetPagedAsync(page, size, filter, orderBy)` | Get paginated results | `PagedResult<TEntity>` |
| `GetFirstOrDefaultAsync(filter)` | Get first matching entity | `TEntity?` |
| `AnyAsync(filter)` | Check if any matches | `bool` |
| `CountAsync(filter)` | Count matching entities | `int` |

### Command Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `AddAsync(entity)` | Add new entity | `TEntity` |
| `AddRangeAsync(entities)` | Add multiple entities | `Task` |
| `UpdateAsync(entity)` | Update entity | `Task` |
| `UpdateRangeAsync(entities)` | Update multiple entities | `Task` |
| `DeleteAsync(entity)` | Delete entity | `Task` |
| `DeleteAsync(id)` | Delete by ID | `Task` |
| `DeleteRangeAsync(entities)` | Delete multiple entities | `Task` |
| `SaveChangesAsync()` | Persist changes to database | `int` (affected rows) |

## License

MIT

## Author

Carlos Vamberto Filho

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

If you encounter any issues or have questions, please file an issue on the GitHub repository.
