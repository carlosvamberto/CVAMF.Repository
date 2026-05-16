using Microsoft.EntityFrameworkCore;

namespace CVAMF.Repository.Specifications;

/// <summary>
/// Extension methods for applying specifications to IQueryable.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies a specification to a queryable, building the complete query.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="inputQuery">The input queryable</param>
    /// <param name="specification">The specification to apply</param>
    /// <returns>The queryable with specification applied</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification) where T : class
    {
        var query = inputQuery;

        // Apply ignore query filters if specified
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply criteria (where clause)
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply string-based includes
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply no tracking
        if (specification.IsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply split query
        if (specification.IsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        // Apply paging (must be after ordering)
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
