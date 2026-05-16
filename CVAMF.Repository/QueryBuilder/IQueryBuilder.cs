using System.Linq.Expressions;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Models;

namespace CVAMF.Repository.QueryBuilder;

/// <summary>
/// Fluent query builder interface for constructing complex queries
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IQueryBuilder<TEntity, TKey> 
    where TEntity : class, IEntity<TKey> 
    where TKey : struct
{
    /// <summary>
    /// Adds a WHERE clause to the query
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Includes related entities (eager loading)
    /// </summary>
    /// <param name="navigationPropertyPath">Navigation property expression</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath);

    /// <summary>
    /// Includes related entities using a string path
    /// </summary>
    /// <param name="includePath">Navigation property path as string</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Include(string includePath);

    /// <summary>
    /// Orders the results in ascending order
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector);

    /// <summary>
    /// Orders the results in descending order
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector);

    /// <summary>
    /// Then orders the results in ascending order (for secondary ordering)
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector);

    /// <summary>
    /// Then orders the results in descending order (for secondary ordering)
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector);

    /// <summary>
    /// Projects the results to a DTO type
    /// </summary>
    /// <typeparam name="TDto">DTO type</typeparam>
    /// <param name="projection">Projection expression</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> ProjectTo<TDto>(Expression<Func<TEntity, TDto>> projection);

    /// <summary>
    /// Enables AsNoTracking for better read-only performance
    /// </summary>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> AsNoTracking();

    /// <summary>
    /// Enables split query for related entities (better performance for multiple includes)
    /// </summary>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> AsSplitQuery();

    /// <summary>
    /// Ignores query filters (e.g., soft delete, multi-tenancy)
    /// </summary>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> IgnoreQueryFilters();

    /// <summary>
    /// Takes only the first N results
    /// </summary>
    /// <param name="count">Number of results to take</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Take(int count);

    /// <summary>
    /// Skips the first N results
    /// </summary>
    /// <param name="count">Number of results to skip</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Skip(int count);

    /// <summary>
    /// Applies pagination to the query
    /// </summary>
    /// <param name="pageNumber">Page number (starts at 1)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Query builder for chaining</returns>
    IQueryBuilder<TEntity, TKey> Paginate(int pageNumber, int pageSize);

    /// <summary>
    /// Executes the query and returns all results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities</returns>
    Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns paged results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<TEntity>> ToPagedResultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the first result or null
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First entity or null</returns>
    Task<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the first result or throws an exception
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First entity</returns>
    Task<TEntity> FirstAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the single result or null
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single entity or null</returns>
    Task<TEntity?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the count
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the query
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fluent query builder interface for projected queries (DTOs)
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
public interface IProjectionQueryBuilder<TEntity, TKey, TDto> 
    where TEntity : class, IEntity<TKey> 
    where TKey : struct
{
    /// <summary>
    /// Orders the results in ascending order
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> OrderBy<TProperty>(Expression<Func<TDto, TProperty>> keySelector);

    /// <summary>
    /// Orders the results in descending order
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> OrderByDescending<TProperty>(Expression<Func<TDto, TProperty>> keySelector);

    /// <summary>
    /// Then orders the results in ascending order (for secondary ordering)
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> ThenBy<TProperty>(Expression<Func<TDto, TProperty>> keySelector);

    /// <summary>
    /// Then orders the results in descending order (for secondary ordering)
    /// </summary>
    /// <param name="keySelector">Property selector for ordering</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> ThenByDescending<TProperty>(Expression<Func<TDto, TProperty>> keySelector);

    /// <summary>
    /// Takes only the first N results
    /// </summary>
    /// <param name="count">Number of results to take</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> Take(int count);

    /// <summary>
    /// Skips the first N results
    /// </summary>
    /// <param name="count">Number of results to skip</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> Skip(int count);

    /// <summary>
    /// Applies pagination to the query
    /// </summary>
    /// <param name="pageNumber">Page number (starts at 1)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Projection query builder for chaining</returns>
    IProjectionQueryBuilder<TEntity, TKey, TDto> Paginate(int pageNumber, int pageSize);

    /// <summary>
    /// Executes the query and returns all projected results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of DTOs</returns>
    Task<List<TDto>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns paged projected results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of DTOs</returns>
    Task<PagedResult<TDto>> ToPagedResultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the first projected result or null
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First DTO or null</returns>
    Task<TDto?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the count
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
