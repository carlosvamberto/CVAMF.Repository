using System.Linq.Expressions;
using CVAMF.Repository.Caching;
using CVAMF.Repository.Entities;
using CVAMF.Repository.Models;
using CVAMF.Repository.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CVAMF.Repository.QueryBuilder;

/// <summary>
/// Fluent query builder implementation for constructing complex queries
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public class QueryBuilder<TEntity, TKey> : IQueryBuilder<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    private readonly DbContext _context;
    private readonly ITenantProvider<string>? _tenantProvider;
    private readonly ICacheService? _cacheService;
    private readonly CacheOptions? _cacheOptions;
    private IQueryable<TEntity> _query;
    private List<Expression<Func<TEntity, bool>>> _whereExpressions = new();
    private List<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> _orderByExpressions = new();
    private bool _asNoTracking = false;
    private bool _asSplitQuery = false;
    private bool _ignoreQueryFilters = false;
    private int? _take = null;
    private int? _skip = null;
    private int? _pageNumber = null;
    private int? _pageSize = null;

    public QueryBuilder(
        DbContext context,
        ITenantProvider<string>? tenantProvider = null,
        ICacheService? cacheService = null,
        CacheOptions? cacheOptions = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantProvider = tenantProvider;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions;
        _query = _context.Set<TEntity>();
    }

    public IQueryBuilder<TEntity, TKey> Where(Expression<Func<TEntity, bool>> predicate)
    {
        _whereExpressions.Add(predicate);
        return this;
    }

    public IQueryBuilder<TEntity, TKey> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
    {
        _query = _query.Include(navigationPropertyPath);
        return this;
    }

    public IQueryBuilder<TEntity, TKey> Include(string includePath)
    {
        _query = _query.Include(includePath);
        return this;
    }

    public IQueryBuilder<TEntity, TKey> OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector)
    {
        _orderByExpressions.Add(query => query.OrderBy(keySelector));
        return this;
    }

    public IQueryBuilder<TEntity, TKey> OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector)
    {
        _orderByExpressions.Add(query => query.OrderByDescending(keySelector));
        return this;
    }

    public IQueryBuilder<TEntity, TKey> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector)
    {
        if (_orderByExpressions.Count == 0)
            throw new InvalidOperationException("ThenBy requires a previous OrderBy or OrderByDescending call");

        var lastOrderBy = _orderByExpressions[^1];
        _orderByExpressions[^1] = query => ((IOrderedQueryable<TEntity>)lastOrderBy(query)).ThenBy(keySelector);
        return this;
    }

    public IQueryBuilder<TEntity, TKey> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector)
    {
        if (_orderByExpressions.Count == 0)
            throw new InvalidOperationException("ThenByDescending requires a previous OrderBy or OrderByDescending call");

        var lastOrderBy = _orderByExpressions[^1];
        _orderByExpressions[^1] = query => ((IOrderedQueryable<TEntity>)lastOrderBy(query)).ThenByDescending(keySelector);
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> ProjectTo<TDto>(Expression<Func<TEntity, TDto>> projection)
    {
        return new ProjectionQueryBuilder<TEntity, TKey, TDto>(
            BuildQuery(),
            projection,
            _pageNumber,
            _pageSize);
    }

    public IQueryBuilder<TEntity, TKey> AsNoTracking()
    {
        _asNoTracking = true;
        return this;
    }

    public IQueryBuilder<TEntity, TKey> AsSplitQuery()
    {
        _asSplitQuery = true;
        return this;
    }

    public IQueryBuilder<TEntity, TKey> IgnoreQueryFilters()
    {
        _ignoreQueryFilters = true;
        return this;
    }

    public IQueryBuilder<TEntity, TKey> Take(int count)
    {
        _take = count;
        return this;
    }

    public IQueryBuilder<TEntity, TKey> Skip(int count)
    {
        _skip = count;
        return this;
    }

    public IQueryBuilder<TEntity, TKey> Paginate(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        _pageNumber = pageNumber;
        _pageSize = pageSize;
        return this;
    }

    private IQueryable<TEntity> BuildQuery()
    {
        var query = _query;

        // Apply tenant filter (unless explicitly ignored)
        if (!_ignoreQueryFilters)
        {
            query = ApplyTenantFilter(query);
        }

        // Apply AsNoTracking
        if (_asNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply AsSplitQuery
        if (_asSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        // Apply IgnoreQueryFilters
        if (_ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply WHERE clauses
        foreach (var where in _whereExpressions)
        {
            query = query.Where(where);
        }

        // Apply OrderBy
        if (_orderByExpressions.Count > 0)
        {
            query = _orderByExpressions[0](query);
        }

        // Apply Skip and Take (for pagination or manual paging)
        if (_pageNumber.HasValue && _pageSize.HasValue)
        {
            query = query.Skip((_pageNumber.Value - 1) * _pageSize.Value).Take(_pageSize.Value);
        }
        else
        {
            if (_skip.HasValue)
            {
                query = query.Skip(_skip.Value);
            }

            if (_take.HasValue)
            {
                query = query.Take(_take.Value);
            }
        }

        return query;
    }

    private IQueryable<TEntity> ApplyTenantFilter(IQueryable<TEntity> query)
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

    public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TEntity>> ToPagedResultAsync(CancellationToken cancellationToken = default)
    {
        if (!_pageNumber.HasValue || !_pageSize.HasValue)
            throw new InvalidOperationException("Paginate must be called before ToPagedResultAsync");

        // Build query without pagination for total count
        var countQuery = _query;

        if (!_ignoreQueryFilters)
        {
            countQuery = ApplyTenantFilter(countQuery);
        }

        if (_ignoreQueryFilters)
        {
            countQuery = countQuery.IgnoreQueryFilters();
        }

        foreach (var where in _whereExpressions)
        {
            countQuery = countQuery.Where(where);
        }

        var totalCount = await countQuery.CountAsync(cancellationToken);

        // Build query with pagination for items
        var items = await ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = _pageNumber.Value,
            PageSize = _pageSize.Value,
            TotalCount = totalCount
        };
    }

    public async Task<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity> FirstAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.FirstAsync(cancellationToken);
    }

    public async Task<TEntity?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        // Build query without pagination for count
        var countQuery = _query;

        if (!_ignoreQueryFilters)
        {
            countQuery = ApplyTenantFilter(countQuery);
        }

        if (_ignoreQueryFilters)
        {
            countQuery = countQuery.IgnoreQueryFilters();
        }

        foreach (var where in _whereExpressions)
        {
            countQuery = countQuery.Where(where);
        }

        return await countQuery.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.AnyAsync(cancellationToken);
    }
}

/// <summary>
/// Fluent query builder implementation for projected queries (DTOs)
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
public class ProjectionQueryBuilder<TEntity, TKey, TDto> : IProjectionQueryBuilder<TEntity, TKey, TDto>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    private IQueryable<TEntity> _entityQuery;
    private readonly Expression<Func<TEntity, TDto>> _projection;
    private IQueryable<TDto>? _projectedQuery;
    private List<Func<IQueryable<TDto>, IOrderedQueryable<TDto>>> _orderByExpressions = new();
    private int? _take = null;
    private int? _skip = null;
    private int? _pageNumber = null;
    private int? _pageSize = null;

    public ProjectionQueryBuilder(
        IQueryable<TEntity> entityQuery,
        Expression<Func<TEntity, TDto>> projection,
        int? pageNumber = null,
        int? pageSize = null)
    {
        _entityQuery = entityQuery;
        _projection = projection;
        _pageNumber = pageNumber;
        _pageSize = pageSize;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> OrderBy<TProperty>(Expression<Func<TDto, TProperty>> keySelector)
    {
        _orderByExpressions.Add(query => query.OrderBy(keySelector));
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> OrderByDescending<TProperty>(Expression<Func<TDto, TProperty>> keySelector)
    {
        _orderByExpressions.Add(query => query.OrderByDescending(keySelector));
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> ThenBy<TProperty>(Expression<Func<TDto, TProperty>> keySelector)
    {
        if (_orderByExpressions.Count == 0)
            throw new InvalidOperationException("ThenBy requires a previous OrderBy or OrderByDescending call");

        var lastOrderBy = _orderByExpressions[^1];
        _orderByExpressions[^1] = query => ((IOrderedQueryable<TDto>)lastOrderBy(query)).ThenBy(keySelector);
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> ThenByDescending<TProperty>(Expression<Func<TDto, TProperty>> keySelector)
    {
        if (_orderByExpressions.Count == 0)
            throw new InvalidOperationException("ThenByDescending requires a previous OrderBy or OrderByDescending call");

        var lastOrderBy = _orderByExpressions[^1];
        _orderByExpressions[^1] = query => ((IOrderedQueryable<TDto>)lastOrderBy(query)).ThenByDescending(keySelector);
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> Take(int count)
    {
        _take = count;
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> Skip(int count)
    {
        _skip = count;
        return this;
    }

    public IProjectionQueryBuilder<TEntity, TKey, TDto> Paginate(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        _pageNumber = pageNumber;
        _pageSize = pageSize;
        return this;
    }

    private IQueryable<TDto> BuildQuery()
    {
        // Apply projection
        var query = _entityQuery.Select(_projection);

        // Apply OrderBy
        if (_orderByExpressions.Count > 0)
        {
            query = _orderByExpressions[0](query);
        }

        // Apply Skip and Take (for pagination or manual paging)
        if (_pageNumber.HasValue && _pageSize.HasValue)
        {
            query = query.Skip((_pageNumber.Value - 1) * _pageSize.Value).Take(_pageSize.Value);
        }
        else
        {
            if (_skip.HasValue)
            {
                query = query.Skip(_skip.Value);
            }

            if (_take.HasValue)
            {
                query = query.Take(_take.Value);
            }
        }

        return query;
    }

    public async Task<List<TDto>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TDto>> ToPagedResultAsync(CancellationToken cancellationToken = default)
    {
        if (!_pageNumber.HasValue || !_pageSize.HasValue)
            throw new InvalidOperationException("Paginate must be called before ToPagedResultAsync");

        // Count total before projection
        var totalCount = await _entityQuery.CountAsync();

        // Build query with pagination for items
        var items = await ToListAsync(cancellationToken);

        return new PagedResult<TDto>
        {
            Items = items,
            PageNumber = _pageNumber.Value,
            PageSize = _pageSize.Value,
            TotalCount = totalCount
        };
    }

    public async Task<TDto?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var query = BuildQuery();
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        // Count on entity query before projection
        return await _entityQuery.CountAsync(cancellationToken);
    }
}
