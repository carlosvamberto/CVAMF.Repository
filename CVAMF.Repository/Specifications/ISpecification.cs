namespace CVAMF.Repository.Specifications;

/// <summary>
/// Specification pattern interface for building reusable query logic.
/// Allows encapsulating complex query criteria in a testable, composable way.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>
    /// The criteria expression for filtering entities.
    /// </summary>
    System.Linq.Expressions.Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// List of include expressions for eager loading related entities.
    /// </summary>
    List<System.Linq.Expressions.Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// List of include strings for eager loading related entities using string navigation.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression for ascending sort.
    /// </summary>
    System.Linq.Expressions.Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression for descending sort.
    /// </summary>
    System.Linq.Expressions.Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Group by expression for grouping results.
    /// </summary>
    System.Linq.Expressions.Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Number of records to skip (for pagination).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Number of records to take (for pagination).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Indicates whether to use AsNoTracking for read-only queries.
    /// Default is false (tracking enabled).
    /// </summary>
    bool IsNoTracking { get; }

    /// <summary>
    /// Indicates whether to ignore global query filters (e.g., tenant filters, soft delete).
    /// Default is false (filters applied).
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Indicates whether to use AsSplitQuery for related entity loading.
    /// Useful for avoiding cartesian explosion in complex includes.
    /// Default is false (single query).
    /// </summary>
    bool IsSplitQuery { get; }
}
