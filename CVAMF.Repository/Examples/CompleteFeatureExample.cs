using CVAMF.Repository.Entities;
using CVAMF.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CVAMF.Repository.Examples;

/// <summary>
/// Complete example demonstrating all features: Audit, Soft Delete, Include, AsNoTracking, UnitOfWork
/// </summary>
public class CompleteFeatureExample
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteFeatureExample(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Example: Create product with audit tracking
    /// </summary>
    public async Task<ProductComplete> CreateProductAsync(string name, decimal price, string userName)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        var product = new ProductComplete
        {
            Name = name,
            Price = price,
            Stock = 100,
            Category = "Electronics"
        };

        // Audit fields (CreatedAt, CreatedBy) are set automatically
        await productRepo.AddAsync(product, userName);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    /// <summary>
    /// Example: Update product with audit tracking
    /// </summary>
    public async Task<bool> UpdateProductPriceAsync(Guid productId, decimal newPrice, string userName)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // Get without tracking is not suitable for updates
        var product = await productRepo.GetByIdAsync(productId, includes: null, asNoTracking: false);
        if (product == null || product.IsDeleted)
            return false;

        product.Price = newPrice;

        // Audit fields (UpdatedAt, UpdatedBy) are set automatically
        await productRepo.UpdateAsync(product, userName);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Example: Soft delete with audit
    /// </summary>
    public async Task<bool> DeleteProductAsync(Guid productId, string userName)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // Soft delete (IsDeleted, DeletedAt, DeletedBy) are set automatically
        var deleted = await productRepo.SoftDeleteAsync(productId, userName);
        if (deleted)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return deleted;
    }

    /// <summary>
    /// Example: List active products with AsNoTracking for performance
    /// </summary>
    public async Task<IEnumerable<ProductComplete>> GetActiveProductsAsync()
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // AsNoTracking for read-only queries (30-40% faster)
        // Filter out soft-deleted products
        return await productRepo.GetAsync(
            filter: p => !p.IsDeleted && p.Stock > 0,
            orderBy: q => q.OrderBy(p => p.Name),
            includes: null,
            asNoTracking: true);
    }

    /// <summary>
    /// Example: Get product with related data using Include
    /// </summary>
    public async Task<ProductComplete?> GetProductWithDetailsAsync(Guid productId)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // Include related entities + AsNoTracking for read-only
        return await productRepo.GetByIdAsync(
            productId,
            includes: q => q.Include(p => p.CategoryRef)
                            .Include(p => p.Reviews),
            asNoTracking: true);
    }

    /// <summary>
    /// Example: Audit report showing all changes to a product
    /// </summary>
    public async Task<ProductAuditReport> GetProductAuditReportAsync(Guid productId)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        var product = await productRepo.GetByIdAsync(id: productId, cancellationToken: default);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        var report = new ProductAuditReport
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Created = new AuditEntry
            {
                Timestamp = product.CreatedAt,
                PerformedBy = product.CreatedBy,
                Action = "Created"
            }
        };

        if (product.UpdatedAt.HasValue)
        {
            report.LastUpdate = new AuditEntry
            {
                Timestamp = product.UpdatedAt.Value,
                PerformedBy = product.UpdatedBy,
                Action = "Updated"
            };
        }

        if (product.IsDeleted)
        {
            report.Deletion = new AuditEntry
            {
                Timestamp = product.DeletedAt!.Value,
                PerformedBy = product.DeletedBy,
                Action = "Soft Deleted"
            };
        }

        return report;
    }

    /// <summary>
    /// Example: Complex business operation with transaction, audit, and soft delete
    /// </summary>
    public async Task<bool> TransferStockAsync(Guid fromProductId, Guid toProductId, int quantity, string userName)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

            // Get both products (with tracking for updates)
            var fromProduct = await productRepo.GetByIdAsync(id: fromProductId, cancellationToken: default);
            var toProduct = await productRepo.GetByIdAsync(id: toProductId, cancellationToken: default);

            if (fromProduct == null || toProduct == null)
                throw new InvalidOperationException("Product not found");

            if (fromProduct.IsDeleted || toProduct.IsDeleted)
                throw new InvalidOperationException("Cannot transfer to/from deleted product");

            if (fromProduct.Stock < quantity)
                throw new InvalidOperationException("Insufficient stock");

            // Update stock
            fromProduct.Stock -= quantity;
            toProduct.Stock += quantity;

            // Audit tracking happens automatically
            await productRepo.UpdateAsync(fromProduct, userName);
            await productRepo.UpdateAsync(toProduct, userName);

            // If source product is now empty, soft delete it
            if (fromProduct.Stock == 0)
            {
                await productRepo.SoftDeleteAsync(fromProduct, userName);
            }

            await _unitOfWork.SaveChangesAsync();

            return true;
        });
    }

    /// <summary>
    /// Example: Batch update with audit
    /// </summary>
    public async Task<int> ApplyDiscountToCategoryAsync(string category, decimal discountPercent, string userName)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // Get products (with tracking)
        var products = await productRepo.GetAsync(
            filter: p => p.Category == category && !p.IsDeleted,
            orderBy: null,
            includes: null,
            asNoTracking: false); // Need tracking for updates

        foreach (var product in products)
        {
            product.Price *= (1 - discountPercent / 100);
        }

        // Batch update with audit
        await productRepo.UpdateRangeAsync(products, userName);
        await _unitOfWork.SaveChangesAsync();

        return products.Count();
    }

    /// <summary>
    /// Example: Restore soft deleted product
    /// </summary>
    public async Task<bool> RestoreProductAsync(Guid productId, string userName)
    {
        var productRepo = _unitOfWork.Repository<ProductComplete, Guid>();

        // Restore the product
        var restored = await productRepo.RestoreAsync(productId);
        if (restored)
        {
            // Update to register who restored it
            var product = await productRepo.GetByIdAsync(id: productId, cancellationToken: default);
            if (product != null)
            {
                await productRepo.UpdateAsync(product, userName);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        return restored;
    }
}

#region Supporting Classes

/// <summary>
/// Complete entity with all features: Audit + Soft Delete
/// Uses EntityBaseAuditableSoftDelete which includes:
/// - Id (Guid)
/// - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (Audit)
/// - IsDeleted, DeletedAt, DeletedBy (Soft Delete)
/// </summary>
public class ProductComplete : EntityBaseAuditableSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;

    // Navigation properties for Include examples
    public CategoryComplete? CategoryRef { get; set; }
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}

public class CategoryComplete : EntityBaseAuditableInt
{
    public string Name { get; set; } = string.Empty;
    public ICollection<ProductComplete> Products { get; set; } = new List<ProductComplete>();
}

public class ProductReview : EntityBaseAuditable
{
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class ProductAuditReport
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public AuditEntry Created { get; set; } = new();
    public AuditEntry? LastUpdate { get; set; }
    public AuditEntry? Deletion { get; set; }
}

public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public string? PerformedBy { get; set; }
    public string Action { get; set; } = string.Empty;
}

#endregion
