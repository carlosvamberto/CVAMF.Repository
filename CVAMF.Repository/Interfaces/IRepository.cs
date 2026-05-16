using System.Linq.Expressions;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Models;
using CVAMF.Repository.Specifications;

namespace CVAMF.Repository.Interfaces;

/// <summary>
/// Generic repository interface for CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type (Guid or int)</typeparam>
public interface IRepository<TEntity, TKey> 
    where TEntity : class, IEntity<TKey> 
    where TKey : struct
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its ID with related entities
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="includes">Related entities to include</param>
    /// <param name="asNoTracking">If true, entities are not tracked by the context (better performance for read-only queries)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity with related data or null</returns>
    /// <example>
    /// <code>
    /// // Load order with items and customer (read-only)
    /// var order = await _orderRepository.GetByIdAsync(
    ///     orderId,
    ///     includes: q => q.Include(o => o.Items)
    ///                     .Include(o => o.Customer),
    ///     asNoTracking: true); // 30-40% faster for read-only
    /// </code>
    /// </example>
    Task<TEntity?> GetByIdAsync(
        TKey id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities with related entities
    /// </summary>
    /// <param name="includes">Related entities to include</param>
    /// <param name="asNoTracking">If true, entities are not tracked by the context (better performance for read-only queries)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All entities with related data</returns>
    /// <example>
    /// <code>
    /// // Load all products with category (read-only for display)
    /// var products = await _productRepository.GetAllAsync(
    ///     includes: q => q.Include(p => p.Category),
    ///     asNoTracking: true);
    /// </code>
    /// </example>
    Task<IEnumerable<TEntity>> GetAllAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filter
    /// </summary>
    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filter and related entities
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Related entities to include</param>
    /// <param name="asNoTracking">If true, entities are not tracked by the context (better performance for read-only queries)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered entities with related data</returns>
    /// <example>
    /// <code>
    /// // Load active orders with items and customer (read-only)
    /// var orders = await _orderRepository.GetAsync(
    ///     filter: o => o.IsActive,
    ///     orderBy: q => q.OrderByDescending(o => o.OrderDate),
    ///     includes: q => q.Include(o => o.Items)
    ///                     .Include(o => o.Customer),
    ///     asNoTracking: true); // Much faster for display lists
    /// </code>
    /// </example>
    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filter and pagination
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filter, pagination and related entities
    /// </summary>
    /// <param name="pageNumber">Page number (starts at 1)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="filter">Filter expression</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Related entities to include</param>
    /// <param name="asNoTracking">If true, entities are not tracked by the context (better performance for read-only queries)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with related data</returns>
    /// <example>
    /// <code>
    /// // Load orders page with items (read-only for list display)
    /// var pagedOrders = await _orderRepository.GetPagedAsync(
    ///     pageNumber: 1,
    ///     pageSize: 10,
    ///     filter: o => o.Status == "Pending",
    ///     orderBy: q => q.OrderByDescending(o => o.OrderDate),
    ///     includes: q => q.Include(o => o.Items)
    ///                     .ThenInclude(i => i.Product),
    ///     asNoTracking: true); // Recommended for pagination
    /// </code>
    /// </example>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the filter
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the filter with related entities
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="includes">Related entities to include</param>
    /// <param name="asNoTracking">If true, entities are not tracked by the context (better performance for read-only queries)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First matching entity with related data or null</returns>
    /// <example>
    /// <code>
    /// // Find order by number with items (read-only)
    /// var order = await _orderRepository.GetFirstOrDefaultAsync(
    ///     filter: o => o.OrderNumber == "ORD-001",
    ///     includes: q => q.Include(o => o.Items)
    ///                     .Include(o => o.Customer),
    ///     asNoTracking: true);
    /// </code>
    /// </example>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the filter
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities matching the filter
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities matching the filter</returns>
    /// <example>
    /// <code>
    /// // Count all
    /// var totalProducts = await _productRepository.CountAsync();
    /// 
    /// // Count with filter
    /// var activeProducts = await _productRepository.CountAsync(
    ///     filter: p => p.IsActive);
    /// 
    /// Console.WriteLine($"Active: {activeProducts} of {totalProducts}");
    /// </code>
    /// </example>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity with audit information
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="createdBy">User who created the entity (optional, only applied if entity implements IAuditable)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <example>
    /// <code>
    /// var product = new Product { Name = "Laptop", Price = 999.99m };
    /// await _productRepository.AddAsync(product, "admin@example.com");
    /// await _productRepository.SaveChangesAsync();
    /// </code>
    /// </example>
    Task<TEntity> AddAsync(TEntity entity, string? createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    /// <param name="entities">Collection of entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <example>
    /// <code>
    /// var products = new List&lt;Product&gt;
    /// {
    ///     new Product { Id = Guid.NewGuid(), Name = "Mouse", Price = 29.99m },
    ///     new Product { Id = Guid.NewGuid(), Name = "Keyboard", Price = 79.99m },
    ///     new Product { Id = Guid.NewGuid(), Name = "Monitor", Price = 299.99m }
    /// };
    /// 
    /// await _productRepository.AddRangeAsync(products);
    /// await _productRepository.SaveChangesAsync();
    /// </code>
    /// </example>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities with audit information
    /// </summary>
    /// <param name="entities">Collection of entities to add</param>
    /// <param name="createdBy">User who created the entities (optional, only applied if entities implement IAuditable)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, string? createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity with audit information
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <param name="updatedBy">User who updated the entity (optional, only applied if entity implements IAuditable)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(TEntity entity, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities with audit information
    /// </summary>
    /// <param name="entities">Collection of entities to update</param>
    /// <param name="updatedBy">User who updated the entities (optional, only applied if entities implement IAuditable)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its ID
    /// </summary>
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity (marks as deleted without removing from database)
    /// </summary>
    /// <param name="entity">Entity to soft delete</param>
    /// <param name="deletedBy">User who deleted the entity (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity was soft deleted, false if entity doesn't support soft delete</returns>
    /// <example>
    /// <code>
    /// var product = await _productRepository.GetByIdAsync(productId);
    /// if (product != null)
    /// {
    ///     await _productRepository.SoftDeleteAsync(product, "admin@example.com");
    ///     await _productRepository.SaveChangesAsync();
    /// }
    /// </code>
    /// </example>
    Task<bool> SoftDeleteAsync(TEntity entity, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="deletedBy">User who deleted the entity (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity was soft deleted, false if entity not found or doesn't support soft delete</returns>
    /// <example>
    /// <code>
    /// var deleted = await _productRepository.SoftDeleteAsync(productId, "admin@example.com");
    /// if (deleted)
    /// {
    ///     await _productRepository.SaveChangesAsync();
    /// }
    /// </code>
    /// </example>
    Task<bool> SoftDeleteAsync(TKey id, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes multiple entities
    /// </summary>
    /// <param name="entities">Entities to soft delete</param>
    /// <param name="deletedBy">User who deleted the entities (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities soft deleted</returns>
    /// <example>
    /// <code>
    /// var oldProducts = await _productRepository.GetAsync(
    ///     filter: p => p.CreatedAt < DateTime.UtcNow.AddYears(-5));
    /// 
    /// var count = await _productRepository.SoftDeleteRangeAsync(oldProducts, "system");
    /// await _productRepository.SaveChangesAsync();
    /// Console.WriteLine($"{count} products soft deleted");
    /// </code>
    /// </example>
    Task<int> SoftDeleteRangeAsync(IEnumerable<TEntity> entities, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity was restored, false if entity doesn't support soft delete</returns>
    /// <example>
    /// <code>
    /// var product = await _productRepository.GetByIdAsync(productId, includeDeleted: true);
    /// if (product != null && product.IsDeleted)
    /// {
    ///     await _productRepository.RestoreAsync(product);
    ///     await _productRepository.SaveChangesAsync();
    /// }
    /// </code>
    /// </example>
    Task<bool> RestoreAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft deleted entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity was restored, false if entity not found or doesn't support soft delete</returns>
    Task<bool> RestoreAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities using a specification pattern (optional feature).
    /// Specifications encapsulate query logic in reusable, testable classes.
    /// </summary>
    /// <param name="specification">The specification containing query logic</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entities matching the specification</returns>
    /// <example>
    /// <code>
    /// // Define specification once
    /// public class ActiveProductsSpec : Specification&lt;Product&gt;
    /// {
    ///     public ActiveProductsSpec()
    ///     {
    ///         AddCriteria(p => p.IsActive);
    ///         AddInclude(p => p.Category);
    ///         ApplyOrderBy(p => p.Name);
    ///         ApplyNoTracking();
    ///     }
    /// }
    /// 
    /// // Reuse anywhere
    /// var products = await _productRepository.GetAsync(new ActiveProductsSpec());
    /// </code>
    /// </example>
    Task<IEnumerable<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity using a specification pattern (optional feature).
    /// </summary>
    /// <param name="specification">The specification containing query logic</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First entity matching the specification or null</returns>
    Task<TEntity?> GetFirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged entities using a specification pattern (optional feature).
    /// </summary>
    /// <param name="specification">The specification containing query logic (must include Skip/Take)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result matching the specification</returns>
    /// <example>
    /// <code>
    /// public class PagedActiveProductsSpec : Specification&lt;Product&gt;
    /// {
    ///     public PagedActiveProductsSpec(int pageNumber, int pageSize)
    ///     {
    ///         AddCriteria(p => p.IsActive && p.Stock > 0);
    ///         AddInclude(p => p.Category);
    ///         ApplyOrderByDescending(p => p.CreatedAt);
    ///         ApplyPaging(pageNumber, pageSize);
    ///         ApplyNoTracking();
    ///     }
    /// }
    /// 
    /// var pagedProducts = await _productRepository.GetPagedAsync(
    ///     new PagedActiveProductsSpec(1, 20));
    /// </code>
    /// </example>
    Task<PagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities using a specification pattern (optional feature).
    /// </summary>
    /// <param name="specification">The specification containing query logic</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of entities matching the specification</returns>
    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches a specification pattern (optional feature).
    /// </summary>
    /// <param name="specification">The specification containing query logic</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches the specification</returns>
    Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
