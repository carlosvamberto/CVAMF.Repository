using System.Linq.Expressions;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Models;

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
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filter
    /// </summary>
    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
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
    /// Gets the first entity matching the filter
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
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
    /// Updates an existing entity
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

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
    /// Saves all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
