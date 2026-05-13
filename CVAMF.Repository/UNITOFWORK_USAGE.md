# UnitOfWork Usage Examples

## Table of Contents
- [Basic Setup](#basic-setup)
- [Simple Usage](#simple-usage)
- [Transaction Management](#transaction-management)
- [Advanced Scenarios](#advanced-scenarios)

## Basic Setup

### 1. Register in Dependency Injection

```csharp
// Program.cs or Startup.cs

// Option 1: Add only UnitOfWork
services.AddDbContext<MyDbContext>(options => 
    options.UseSqlServer(connectionString));
services.AddUnitOfWork<MyDbContext>();

// Option 2: Add both Repositories and UnitOfWork
services.AddDbContext<MyDbContext>(options => 
    options.UseSqlServer(connectionString));
services.AddRepositoriesWithUnitOfWork<MyDbContext>();
```

### 2. Define Your Entities

```csharp
public class Product : EntityBase<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class Order : EntityBase<Guid>
{
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem : EntityBase<int>
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## Simple Usage

### Basic CRUD Operations

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var repo = _unitOfWork.Repository<Product, Guid>();
        await repo.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> GetProductAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<Product, Guid>();
        return await repo.GetByIdAsync(id);
    }

    public async Task UpdateProductAsync(Product product)
    {
        var repo = _unitOfWork.Repository<Product, Guid>();
        await repo.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<Product, Guid>();
        await repo.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Working with Multiple Repositories

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Order> CreateOrderAsync(Order order, List<OrderItem> items)
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var itemRepo = _unitOfWork.Repository<OrderItem, int>();

        await orderRepo.AddAsync(order);
        await itemRepo.AddRangeAsync(items);

        await _unitOfWork.SaveChangesAsync();

        return order;
    }
}
```

## Transaction Management

### Manual Transaction Control

```csharp
public async Task<Order> CreateOrderWithManualTransactionAsync(Order order, List<OrderItem> items)
{
    var orderRepo = _unitOfWork.Repository<Order, Guid>();
    var itemRepo = _unitOfWork.Repository<OrderItem, int>();
    var productRepo = _unitOfWork.Repository<Product, Guid>();

    await using var transaction = await _unitOfWork.BeginTransactionAsync();

    try
    {
        // Step 1: Create the order
        await orderRepo.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Step 2: Add order items
        await itemRepo.AddRangeAsync(items);
        await _unitOfWork.SaveChangesAsync();

        // Step 3: Update product stock
        foreach (var item in items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.Stock -= item.Quantity;
                await productRepo.UpdateAsync(product);
            }
        }
        await _unitOfWork.SaveChangesAsync();

        // Commit if everything succeeded
        await transaction.CommitAsync();
        return order;
    }
    catch
    {
        // Rollback on any error
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Using ExecuteInTransactionAsync (Recommended)

```csharp
public async Task<Order> CreateOrderWithAutoTransactionAsync(Order order, List<OrderItem> items)
{
    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var itemRepo = _unitOfWork.Repository<OrderItem, int>();
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        // Create order
        await orderRepo.AddAsync(order);

        // Add items
        await itemRepo.AddRangeAsync(items);

        // Update stock
        foreach (var item in items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for {product.Name}");

            product.Stock -= item.Quantity;
            await productRepo.UpdateAsync(product);
        }

        return order;
    });
}
```

## Advanced Scenarios

### Nested Transactions

```csharp
public async Task<bool> ProcessBulkOrdersAsync(List<Order> orders)
{
    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();

        foreach (var order in orders)
        {
            // Each order is processed within the parent transaction
            await orderRepo.AddAsync(order);

            // You can call other services that use the same UnitOfWork
            await ProcessOrderItemsAsync(order.Id, order.Items);
        }

        return true;
    });
}

private async Task ProcessOrderItemsAsync(Guid orderId, List<OrderItem> items)
{
    // This uses the same transaction if called within ExecuteInTransactionAsync
    var itemRepo = _unitOfWork.Repository<OrderItem, int>();
    await itemRepo.AddRangeAsync(items);
}
```

### Transaction with Return Value

```csharp
public async Task<(Order Order, decimal TotalSaved)> CreateOrderWithDiscountAsync(
    Order order, 
    List<OrderItem> items, 
    decimal discountPercentage)
{
    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var itemRepo = _unitOfWork.Repository<OrderItem, int>();

        decimal originalTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        decimal discount = originalTotal * (discountPercentage / 100);
        order.Total = originalTotal - discount;

        await orderRepo.AddAsync(order);
        await itemRepo.AddRangeAsync(items);

        return (order, discount);
    });
}
```

### Checking Active Transaction

```csharp
public async Task ProcessDataAsync()
{
    if (_unitOfWork.HasActiveTransaction)
    {
        // We're already in a transaction, just add operations
        var repo = _unitOfWork.Repository<Product, Guid>();
        // ... perform operations
    }
    else
    {
        // Start a new transaction
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repo = _unitOfWork.Repository<Product, Guid>();
            // ... perform operations
        });
    }
}
```

### Complex Business Logic Example

```csharp
public class OrderProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        IUnitOfWork unitOfWork, 
        ILogger<OrderProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Order> ProcessCompleteOrderAsync(
        Order order, 
        List<OrderItem> items,
        string? couponCode = null)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();
            var productRepo = _unitOfWork.Repository<Product, Guid>();

            _logger.LogInformation("Processing order {OrderId}", order.Id);

            // Validate and reserve stock
            foreach (var item in items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException(
                        $"Product {item.ProductId} not found");
                }

                if (product.Stock < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}");
                }

                // Reserve stock
                product.Stock -= item.Quantity;
                await productRepo.UpdateAsync(product);

                // Set current price
                item.UnitPrice = product.Price;
            }

            // Calculate totals
            decimal subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
            decimal discount = 0;

            // Apply coupon if provided
            if (!string.IsNullOrEmpty(couponCode))
            {
                discount = await ApplyCouponAsync(couponCode, subtotal);
            }

            order.Total = subtotal - discount;
            order.OrderDate = DateTime.UtcNow;

            // Save order and items
            await orderRepo.AddAsync(order);

            foreach (var item in items)
            {
                item.OrderId = order.Id;
            }
            await itemRepo.AddRangeAsync(items);

            _logger.LogInformation(
                "Order {OrderId} processed successfully. Total: {Total:C}", 
                order.Id, 
                order.Total);

            return order;
        });
    }

    private async Task<decimal> ApplyCouponAsync(string couponCode, decimal subtotal)
    {
        // Coupon validation logic here
        // This is just an example
        return subtotal * 0.1m; // 10% discount
    }
}
```

### Using in API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public OrdersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest request)
    {
        try
        {
            var order = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var orderRepo = _unitOfWork.Repository<Order, Guid>();
                var itemRepo = _unitOfWork.Repository<OrderItem, int>();

                var newOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderDate = DateTime.UtcNow
                };

                await orderRepo.AddAsync(newOrder);

                var items = request.Items.Select(i => new OrderItem
                {
                    OrderId = newOrder.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                await itemRepo.AddRangeAsync(items);

                newOrder.Total = items.Sum(i => i.UnitPrice * i.Quantity);
                await orderRepo.UpdateAsync(newOrder);

                return newOrder;
            });

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var order = await orderRepo.GetByIdAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }
}
```

## Best Practices

1. **Always use `ExecuteInTransactionAsync` for operations that need to be atomic**
2. **Dispose UnitOfWork properly** - Use dependency injection scopes or `await using`
3. **Don't hold transactions longer than necessary**
4. **Use the same UnitOfWork instance** throughout a single business operation
5. **Handle exceptions properly** and let transactions rollback automatically
6. **Log transaction operations** for debugging and auditing

## Error Handling

```csharp
public async Task<Result<Order>> CreateOrderSafelyAsync(Order order)
{
    try
    {
        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repo = _unitOfWork.Repository<Order, Guid>();
            await repo.AddAsync(order);
            return order;
        });

        return Result<Order>.Success(result);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error creating order");
        return Result<Order>.Failure("Failed to save order to database");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error creating order");
        return Result<Order>.Failure("An unexpected error occurred");
    }
}
```
