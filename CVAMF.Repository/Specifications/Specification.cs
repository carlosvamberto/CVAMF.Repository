using System.Linq.Expressions;

namespace CVAMF.Repository.Specifications;

/// <summary>
/// Base class for implementing the Specification pattern.
/// Provides a fluent API for building complex, reusable query logic.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to</typeparam>
public abstract class Specification<T> : ISpecification<T> where T : class
{
    /// <inheritdoc/>
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc/>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <inheritdoc/>
    public List<string> IncludeStrings { get; } = new();

    /// <inheritdoc/>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc/>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc/>
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <inheritdoc/>
    public int? Skip { get; private set; }

    /// <inheritdoc/>
    public int? Take { get; private set; }

    /// <inheritdoc/>
    public bool IsNoTracking { get; private set; }

    /// <inheritdoc/>
    public bool IgnoreQueryFilters { get; private set; }

    /// <inheritdoc/>
    public bool IsSplitQuery { get; private set; }

    /// <summary>
    /// Adds a criteria expression to filter entities.
    /// </summary>
    /// <param name="criteria">The filter expression</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading a related entity.
    /// </summary>
    /// <param name="includeExpression">The navigation property expression</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include using a string navigation path.
    /// Useful for nested includes like "Order.OrderItems.Product".
    /// </summary>
    /// <param name="includeString">The navigation path as string</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds ascending order by expression.
    /// </summary>
    /// <param name="orderByExpression">The property to order by</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Adds descending order by expression.
    /// </summary>
    /// <param name="orderByDescendingExpression">The property to order by descending</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Adds group by expression.
    /// </summary>
    /// <param name="groupByExpression">The property to group by</param>
    protected void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }

    /// <summary>
    /// Applies pagination by skipping a number of records.
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    protected void ApplySkip(int skip)
    {
        Skip = skip;
    }

    /// <summary>
    /// Applies pagination by taking a number of records.
    /// </summary>
    /// <param name="take">Number of records to take</param>
    protected void ApplyTake(int take)
    {
        Take = take;
    }

    /// <summary>
    /// Applies pagination using page number and page size.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    protected void ApplyPaging(int pageNumber, int pageSize)
    {
        Skip = (pageNumber - 1) * pageSize;
        Take = pageSize;
    }

    /// <summary>
    /// Enables AsNoTracking for read-only queries (better performance).
    /// </summary>
    protected void ApplyNoTracking()
    {
        IsNoTracking = true;
    }

    /// <summary>
    /// Ignores global query filters (e.g., soft delete, tenant filtering).
    /// Use with caution - may expose deleted or cross-tenant data.
    /// </summary>
    protected void ApplyIgnoreQueryFilters()
    {
        IgnoreQueryFilters = true;
    }

    /// <summary>
    /// Enables AsSplitQuery to avoid cartesian explosion with multiple includes.
    /// </summary>
    protected void ApplySplitQuery()
    {
        IsSplitQuery = true;
    }
}
