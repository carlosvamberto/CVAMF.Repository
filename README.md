# CVAMF.Repository

Generic Repository Pattern implementation for Entity Framework Core with support for filters, pagination, and multiple primary key types.

## Features

- ✅ Generic Repository Pattern for EF Core
- ✅ **Unit of Work pattern with transaction support**
- ✅ Support for Guid and Int primary keys
- ✅ Filtering with Expression Functions
- ✅ Optional pagination
- ✅ Full CRUD operations
- ✅ **Automatic transaction management**
- ✅ Async/await support
- ✅ Easy dependency injection integration
- ✅ .NET 10 compatible

## Installation

```bash
dotnet add package CVAMF.Repository
```

Or via NuGet Package Manager:

```
Install-Package CVAMF.Repository
```

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

For detailed examples and advanced usage patterns, see **[UNITOFWORK_USAGE.md](UNITOFWORK_USAGE.md)** which includes:

- ✅ Basic setup and configuration
- ✅ Transaction management strategies
- ✅ Error handling patterns
- ✅ Nested transactions
- ✅ API controller examples
- ✅ Best practices and performance tips

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

## Requirements

- .NET 10.0 or higher
- Entity Framework Core 10.0 or higher

## License

MIT

## Author

Carlos Filho

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

If you encounter any issues or have questions, please file an issue on the GitHub repository.
