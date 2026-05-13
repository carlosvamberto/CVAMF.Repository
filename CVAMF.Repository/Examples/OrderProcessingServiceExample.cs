using CVAMF.Repository.Entities;
using CVAMF.Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace CVAMF.Repository.Examples;

// Example entities for demonstration purposes
public class Order : EntityBase
{
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OrderItem : EntityBaseInt
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class Inventory : EntityBase
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class InventoryLog : EntityBaseInt
{
    public Guid ProductId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public int Quantity { get; set; }
    public DateTime TransferDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Example service demonstrating Unit of Work usage patterns
/// This is a complete working example you can use as reference
/// </summary>
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

    /// <summary>
    /// Example 1: Simple transaction with automatic rollback on error
    /// </summary>
    public async Task<Order> CreateSimpleOrderAsync(Order order, List<OrderItem> items)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();

            // Add order
            await orderRepo.AddAsync(order);

            // Add items
            foreach (var item in items)
            {
                item.OrderId = order.Id;
            }
            await itemRepo.AddRangeAsync(items);

            _logger.LogInformation("Order {OrderId} created with {ItemCount} items", 
                order.Id, items.Count);

            return order;
        });
    }

    /// <summary>
    /// Example 2: Complex transaction with stock management
    /// </summary>
    public async Task<Order> CreateOrderWithStockManagementAsync(
        Order order, 
        List<OrderItem> items)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();
            var productRepo = _unitOfWork.Repository<Product, Guid>();

            // Validate and reserve stock
            foreach (var item in items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, includes: null);

                if (product == null)
                    throw new InvalidOperationException(
                        $"Product {item.ProductId} not found");

                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.Name}. " +
                        $"Available: {product.Stock}, Requested: {item.Quantity}");

                // Deduct stock
                product.Stock -= item.Quantity;
                await productRepo.UpdateAsync(product);

                // Set current price
                item.UnitPrice = product.Price;
                item.OrderId = order.Id;
            }

            // Calculate order total
            order.Total = items.Sum(i => i.UnitPrice * i.Quantity);
            order.OrderDate = DateTime.UtcNow;
            order.Status = "Confirmed";

            // Save order
            await orderRepo.AddAsync(order);

            // Save items
            await itemRepo.AddRangeAsync(items);

            _logger.LogInformation(
                "Order {OrderId} created successfully. Total: {Total:C}", 
                order.Id, 
                order.Total);

            return order;
        });
    }

    /// <summary>
    /// Example 3: Manual transaction control for step-by-step processing
    /// </summary>
    public async Task<Order> CreateOrderManualTransactionAsync(
        Order order, 
        List<OrderItem> items)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();

            // Step 1: Create order
            await orderRepo.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} created", order.Id);

            // Step 2: Add items
            foreach (var item in items)
            {
                item.OrderId = order.Id;
            }
            await itemRepo.AddRangeAsync(items);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added {Count} items to order", items.Count);

            // Step 3: Update order total
            order.Total = items.Sum(i => i.UnitPrice * i.Quantity);
            await orderRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order total updated: {Total:C}", order.Total);

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation("Transaction committed successfully");

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order. Rolling back transaction");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Example 4: Batch processing with transaction
    /// </summary>
    public async Task<List<Order>> ProcessBulkOrdersAsync(List<Order> orders)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var processedOrders = new List<Order>();

            foreach (var order in orders)
            {
                order.Status = "Processing";
                order.OrderDate = DateTime.UtcNow;

                await orderRepo.AddAsync(order);
                processedOrders.Add(order);
            }

            _logger.LogInformation(
                "Processed {Count} orders in bulk transaction", 
                processedOrders.Count);

            return processedOrders;
        });
    }

    /// <summary>
    /// Example 5: Cancel order with stock restoration
    /// </summary>
    public async Task<bool> CancelOrderAsync(Guid orderId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            var itemRepo = _unitOfWork.Repository<OrderItem, int>();
            var productRepo = _unitOfWork.Repository<Product, Guid>();

            // Get order
            var order = await orderRepo.GetByIdAsync(orderId, includes: null);
            if (order == null)
                throw new InvalidOperationException("Order not found");

            if (order.Status == "Cancelled")
                throw new InvalidOperationException("Order already cancelled");

            // Get order items
            var items = await itemRepo.GetAsync(
                filter: i => i.OrderId == orderId,
                orderBy: null,
                includes: null);

            // Restore stock
            foreach (var item in items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, includes: null);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    await productRepo.UpdateAsync(product);
                }
            }

            // Update order status
            order.Status = "Cancelled";
            await orderRepo.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} cancelled and stock restored", orderId);

            return true;
        });
    }

    /// <summary>
    /// Example 6: Multiple repository coordination
    /// </summary>
    public async Task<bool> TransferProductBetweenWarehousesAsync(
        Guid productId, 
        Guid fromWarehouseId, 
        Guid toWarehouseId, 
        int quantity)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var inventoryRepo = _unitOfWork.Repository<Inventory, Guid>();
            var logRepo = _unitOfWork.Repository<InventoryLog, int>();

            // Get source inventory
            var sourceInventory = await inventoryRepo.GetFirstOrDefaultAsync(
                i => i.WarehouseId == fromWarehouseId && i.ProductId == productId,
                includes: null);

            if (sourceInventory == null || sourceInventory.Quantity < quantity)
                throw new InvalidOperationException("Insufficient inventory in source warehouse");

            // Deduct from source
            sourceInventory.Quantity -= quantity;
            await inventoryRepo.UpdateAsync(sourceInventory);

            // Get or create destination inventory
            var destInventory = await inventoryRepo.GetFirstOrDefaultAsync(
                i => i.WarehouseId == toWarehouseId && i.ProductId == productId,
                includes: null);

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
            var log = new InventoryLog
            {
                ProductId = productId,
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                Quantity = quantity,
                TransferDate = DateTime.UtcNow,
                Notes = $"Transfer of {quantity} units"
            };
            await logRepo.AddAsync(log);

            _logger.LogInformation(
                "Transferred {Quantity} units of product {ProductId} from warehouse {From} to {To}",
                quantity, productId, fromWarehouseId, toWarehouseId);

            return true;
        });
    }

    /// <summary>
    /// Example 7: Checking for active transaction
    /// </summary>
    public async Task ProcessWithTransactionCheckAsync()
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            // We're already in a transaction, just add operations
            _logger.LogInformation("Using existing transaction");
            var repo = _unitOfWork.Repository<Product, Guid>();
            // ... perform operations
        }
        else
        {
            // Start a new transaction
            _logger.LogInformation("Starting new transaction");
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var repo = _unitOfWork.Repository<Product, Guid>();
                // ... perform operations
            });
        }
    }
}

