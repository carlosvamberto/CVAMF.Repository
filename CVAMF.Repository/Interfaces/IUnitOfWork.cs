using CVAMF.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace CVAMF.Repository.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions and repository coordination
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type (Guid or int)</typeparam>
    /// <returns>Repository instance</returns>
    /// <example>
    /// <code>
    /// var productRepo = _unitOfWork.Repository&lt;Product, Guid&gt;();
    /// var categoryRepo = _unitOfWork.Repository&lt;Category, int&gt;();
    /// </code>
    /// </example>
    IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : struct;

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database transaction</returns>
    /// <example>
    /// <code>
    /// await using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     await productRepo.AddAsync(product);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     await orderRepo.AddAsync(order);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </code>
    /// </example>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <example>
    /// <code>
    /// await _unitOfWork.BeginTransactionAsync();
    /// // ... perform operations
    /// await _unitOfWork.SaveChangesAsync();
    /// await _unitOfWork.CommitTransactionAsync();
    /// </code>
    /// </example>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     await _unitOfWork.BeginTransactionAsync();
    ///     // ... perform operations
    ///     await _unitOfWork.SaveChangesAsync();
    ///     await _unitOfWork.CommitTransactionAsync();
    /// }
    /// catch
    /// {
    ///     await _unitOfWork.RollbackTransactionAsync();
    ///     throw;
    /// }
    /// </code>
    /// </example>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes operations within a transaction scope
    /// </summary>
    /// <param name="action">Action to execute within transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of state entries written to database</returns>
    /// <example>
    /// <code>
    /// var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
    /// {
    ///     var productRepo = _unitOfWork.Repository&lt;Product, Guid&gt;();
    ///     var orderRepo = _unitOfWork.Repository&lt;Order, Guid&gt;();
    ///     
    ///     await productRepo.AddAsync(product);
    ///     await orderRepo.AddAsync(order);
    /// });
    /// </code>
    /// </example>
    Task<int> ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes operations within a transaction scope with a return value
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="func">Function to execute within transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the function</returns>
    /// <example>
    /// <code>
    /// var newProduct = await _unitOfWork.ExecuteInTransactionAsync(async () =>
    /// {
    ///     var repo = _unitOfWork.Repository&lt;Product, Guid&gt;();
    ///     var product = await repo.AddAsync(new Product { Name = "New Product" });
    ///     return product;
    /// });
    /// </code>
    /// </example>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of state entries written to database</returns>
    /// <example>
    /// <code>
    /// var productRepo = _unitOfWork.Repository&lt;Product, Guid&gt;();
    /// await productRepo.AddAsync(product1);
    /// await productRepo.AddAsync(product2);
    /// 
    /// var saved = await _unitOfWork.SaveChangesAsync();
    /// Console.WriteLine($"{saved} records saved");
    /// </code>
    /// </example>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active transaction, if any
    /// </summary>
    IDbContextTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Indicates whether there is an active transaction
    /// </summary>
    bool HasActiveTransaction { get; }
}
