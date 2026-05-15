using System.Collections.Concurrent;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Interfaces;
using CVAMF.Repository.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CVAMF.Repository.UnitOfWork;

/// <summary>
/// Unit of Work implementation for managing transactions and repository coordination
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly ConcurrentDictionary<string, object> _repositories;
    private readonly ITenantProvider<string>? _tenantProvider;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of UnitOfWork
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="tenantProvider">Optional tenant provider for multi-tenancy support</param>
    public UnitOfWork(DbContext context, ITenantProvider<string>? tenantProvider = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new ConcurrentDictionary<string, object>();
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : struct
    {
        var key = $"{typeof(TEntity).Name}_{typeof(TKey).Name}";

        return (IRepository<TEntity, TKey>)_repositories.GetOrAdd(key, _ =>
        {
            var repositoryType = typeof(Repositories.Repository<,>);
            var repositoryInstance = Activator.CreateInstance(
                repositoryType.MakeGenericType(typeof(TEntity), typeof(TKey)),
                _context,
                _tenantProvider);

            return repositoryInstance ?? throw new InvalidOperationException(
                $"Could not create repository for {typeof(TEntity).Name}");
        });
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_currentTransaction != null)
        {
            await action();
            return await SaveChangesAsync(cancellationToken);
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                var result = await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        });
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        if (_currentTransaction != null)
        {
            var result = await func();
            await SaveChangesAsync(cancellationToken);
            return result;
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await func();
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        });
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IDbContextTransaction? CurrentTransaction => _currentTransaction;

    /// <inheritdoc />
    public bool HasActiveTransaction => _currentTransaction != null;

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Disposes the UnitOfWork and releases resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the UnitOfWork asynchronously
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _repositories.Clear();
            }

            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }

        _repositories.Clear();
    }
}
