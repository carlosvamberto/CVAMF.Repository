using System.Linq.Expressions;
using CVAMF.Repository.Caching;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Interfaces;
using CVAMF.Repository.Models;
using CVAMF.Repository.MultiTenancy;
using CVAMF.Repository.QueryBuilder;
using CVAMF.Repository.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CVAMF.Repository.Repositories;

/// <summary>
/// Generic repository implementation
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type (Guid or int)</typeparam>
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ITenantProvider<string>? _tenantProvider;
    protected readonly ICacheService? _cacheService;
    protected readonly CacheOptions? _cacheOptions;

    public Repository(
        DbContext context, 
        ITenantProvider<string>? tenantProvider = null,
        ICacheService? cacheService = null,
        CacheOptions? cacheOptions = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
        _tenantProvider = tenantProvider;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions ?? new CacheOptions();
    }

    protected string BuildCacheKey(string operation, params object[] parameters)
    {
        var keyParts = new List<string> { _cacheOptions?.KeyPrefix ?? "CVAMF.Repository:" };

        if (_cacheOptions?.UseEntityTypeInKey ?? true)
        {
            keyParts.Add(typeof(TEntity).Name);
        }

        if (_cacheOptions?.UseTenantIdInKey ?? true)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                keyParts.Add($"Tenant:{tenantId}");
            }
        }

        keyParts.Add(operation);
        keyParts.AddRange(parameters.Select(p => p?.ToString() ?? "null"));

        return string.Join(":", keyParts);
    }

    protected async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheService == null || !(_cacheOptions?.AutoInvalidateOnWrite ?? true))
            return;

        var pattern = BuildCacheKey("*");
        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
    }

    protected IQueryable<TEntity> ApplyTenantFilter(IQueryable<TEntity> query)
    {
        if (_tenantProvider != null && _tenantProvider.HasTenantContext() && typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                var parameter = Expression.Parameter(typeof(TEntity), "e");
                var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var constant = Expression.Constant(tenantId);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<TEntity, bool>>(equality, parameter);

                query = query.Where(lambda);
            }
        }
        return query;
    }

    protected void SetTenantId(TEntity entity)
    {
        if (_tenantProvider != null && _tenantProvider.HasTenantContext() && entity is ITenantEntity tenantEntity)
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                tenantEntity.TenantId = tenantId;
            }
        }
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // Try cache first if enabled
        if (_cacheService != null && (_cacheOptions?.CacheGetById ?? true))
        {
            var cacheKey = BuildCacheKey("GetById", id);
            var cached = await _cacheService.GetAsync<TEntity>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }

        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        if (entity != null && _tenantProvider != null && _tenantProvider.HasTenantContext() && entity is ITenantEntity tenantEntity)
        {
            var currentTenantId = _tenantProvider.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(currentTenantId) && tenantEntity.TenantId != currentTenantId)
            {
                return null;
            }
        }

        // Cache the result if enabled
        if (entity != null && _cacheService != null && (_cacheOptions?.CacheGetById ?? true))
        {
            var cacheKey = BuildCacheKey("GetById", id);
            await _cacheService.SetAsync(
                cacheKey, 
                entity, 
                _cacheOptions?.DefaultExpiration,
                _cacheOptions?.SlidingExpiration,
                cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        TKey id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            query = includes(query);
        }

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;
        query = ApplyTenantFilter(query);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            query = includes(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            query = includes(query);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            query = includes(query);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;
        query = ApplyTenantFilter(query);
        return await query.FirstOrDefaultAsync(filter, cancellationToken);
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        query = ApplyTenantFilter(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            query = includes(query);
        }

        return await query.FirstOrDefaultAsync(filter, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;
        query = ApplyTenantFilter(query);
        return await query.AnyAsync(filter, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;
        query = ApplyTenantFilter(query);

        if (filter != null)
        {
            return await query.CountAsync(filter, cancellationToken);
        }

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        SetTenantId(entity);
        await _dbSet.AddAsync(entity, cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, string? createdBy, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        SetTenantId(entity);

        if (entity is IAuditable auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
            auditable.CreatedBy = createdBy;
        }

        await _dbSet.AddAsync(entity, cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        foreach (var entity in entities)
        {
            SetTenantId(entity);
        }

        await _dbSet.AddRangeAsync(entities, cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, string? createdBy, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            // Set audit fields if entity implements IAuditable
            if (entity is IAuditable auditable)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = createdBy;
            }
        }

        await _dbSet.AddRangeAsync(entities, cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, string? updatedBy, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Set audit fields if entity implements IAuditable
        if (entity is IAuditable auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
            auditable.UpdatedBy = updatedBy;
        }

        _dbSet.Update(entity);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        _dbSet.UpdateRange(entities);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? updatedBy, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            // Set audit fields if entity implements IAuditable
            if (entity is IAuditable auditable)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = updatedBy;
            }
        }

        _dbSet.UpdateRange(entities);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Remove(entity);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await DeleteAsync(entity, cancellationToken);
        }
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        _dbSet.RemoveRange(entities);
        await InvalidateCacheAsync(cancellationToken);
    }

    public virtual async Task<bool> SoftDeleteAsync(TEntity entity, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Try ISoftDeletable first (IsDeleted property)
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = DateTime.UtcNow;
            softDeletable.DeletedBy = deletedBy;
            _dbSet.Update(entity);
            await InvalidateCacheAsync(cancellationToken);
            return true;
        }

        // Try ISoftDeletableAlternative (Deleted property)
        if (entity is ISoftDeletableAlternative softDeletableAlt)
        {
            softDeletableAlt.Deleted = true;
            softDeletableAlt.DeletedAt = DateTime.UtcNow;
            softDeletableAlt.DeletedBy = deletedBy;
            _dbSet.Update(entity);
            await InvalidateCacheAsync(cancellationToken);
            return true;
        }

        // Entity doesn't support soft delete
        return false;
    }

    public virtual async Task<bool> SoftDeleteAsync(TKey id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        return await SoftDeleteAsync(entity, deletedBy, cancellationToken);
    }

    public virtual async Task<int> SoftDeleteRangeAsync(IEnumerable<TEntity> entities, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var count = 0;
        foreach (var entity in entities)
        {
            if (await SoftDeleteAsync(entity, deletedBy, cancellationToken))
            {
                count++;
            }
        }

        return count;
    }

    public virtual Task<bool> RestoreAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Try ISoftDeletable first (IsDeleted property)
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = false;
            softDeletable.DeletedAt = null;
            softDeletable.DeletedBy = null;
            _dbSet.Update(entity);
            return Task.FromResult(true);
        }

        // Try ISoftDeletableAlternative (Deleted property)
        if (entity is ISoftDeletableAlternative softDeletableAlt)
        {
            softDeletableAlt.Deleted = false;
            softDeletableAlt.DeletedAt = null;
            softDeletableAlt.DeletedBy = null;
            _dbSet.Update(entity);
            return Task.FromResult(true);
        }

        // Entity doesn't support soft delete
        return Task.FromResult(false);
    }

    public virtual async Task<bool> RestoreAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        return await RestoreAsync(entity, cancellationToken);
    }

    #region Specification Pattern (Optional)

    public virtual async Task<IEnumerable<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        var query = ApplyTenantFilter(_dbSet.AsQueryable());
        query = SpecificationEvaluator.GetQuery(query, specification);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        var query = ApplyTenantFilter(_dbSet.AsQueryable());
        query = SpecificationEvaluator.GetQuery(query, specification);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        if (!specification.Skip.HasValue || !specification.Take.HasValue)
            throw new ArgumentException("Specification must include Skip and Take for paging", nameof(specification));

        var query = ApplyTenantFilter(_dbSet.AsQueryable());

        // Get total count before applying pagination
        var countQuery = query;
        if (specification.Criteria != null)
            countQuery = countQuery.Where(specification.Criteria);

        var totalItems = await countQuery.CountAsync(cancellationToken);

        // Apply full specification including pagination
        query = SpecificationEvaluator.GetQuery(query, specification);
        var items = await query.ToListAsync(cancellationToken);

        var pageNumber = (specification.Skip.Value / specification.Take.Value) + 1;
        var pageSize = specification.Take.Value;

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        var query = ApplyTenantFilter(_dbSet.AsQueryable());

        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        var query = ApplyTenantFilter(_dbSet.AsQueryable());

        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        return await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region Fluent Query Builder (v1.7.0)

    public virtual IQueryBuilder<TEntity, TKey> Query()
    {
        return new QueryBuilder<TEntity, TKey>(_context, _tenantProvider, _cacheService, _cacheOptions);
    }

    #endregion

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
