# CVAMF.Repository - Release Notes

## v1.7.2
- 🐛 **Bug Fix**: CancellationToken usage in caching services
  - Fixed `MemoryCacheService` to properly use `CancellationToken` in all async methods
  - Fixed `RedisAdvancedCacheService` to properly use `CancellationToken` in all async methods
  - Added `cancellationToken.ThrowIfCancellationRequested()` checks for proper cancellation support
  - Improved cancellation support in loops (RemoveByPatternAsync)
  - Enhanced application responsiveness and cancellation support
  - Note: `RedisCacheService` already had correct CancellationToken usage

## v1.7.1
- 🔍 **Improved NuGet Discoverability**: Enhanced package metadata for better search results
  - Added `Title` property: "Repository Framework for Entity Framework Core"
  - Expanded `Description` with key search terms (Repository Framework, Query Builder, Clean Architecture, DDD, CQRS)
  - Enhanced `PackageTags` with 50+ relevant tags including:
    - `repository-framework`, `ef-core-repository`, `fluent-query`
    - `clean-architecture`, `domain-driven-design`, `data-access`
    - `dotnet9`, `dotnet10`, `efcore-extensions`, `linq-extensions`
  - Added `Copyright` information
  - Added `RepositoryType` (git)
  - Improved metadata for NuGet.org search ranking

## v1.7.0
- 🔍 **Fluent Query Builder**: Modern, fluent API for building complex queries
  - `Query()` method for starting fluent queries
  - Method chaining: `Where`, `Include`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`
  - Projection support: `ProjectTo<TDto>()` for mapping to DTOs
  - Pagination: `Paginate(pageNumber, pageSize)` or `Skip/Take`
  - Performance options: `AsNoTracking()`, `AsSplitQuery()`, `IgnoreQueryFilters()`
  - Multiple execution methods: `ToListAsync()`, `ToPagedResultAsync()`, `FirstOrDefaultAsync()`, `FirstAsync()`, `SingleOrDefaultAsync()`, `CountAsync()`, `AnyAsync()`
  - Example:
    ```csharp
    var customers = await repository.Query<Customer>()
        .Where(x => x.Active)
        .Include(x => x.Orders)
        .ProjectTo(x => new CustomerDto { Id = x.Id, Name = x.Name })
        .OrderBy(x => x.Name)
        .Paginate(1, 20)
        .ToPagedResultAsync();
    ```
  - Integrates with Multi-Tenancy (automatic tenant filtering)
  - Works with Caching (when configured)
  - Type-safe, IntelliSense-friendly
  - Completely optional - use alongside existing methods
  - Documentation: QUERYBUILDER_USAGE.md

- 📄 **Improved Package Documentation**: Release notes moved to separate file (RELEASE_NOTES.md)

## v1.6.0
- ⚡ **Caching Support**: Dramatic performance improvements with automatic caching
  - `ICacheService` interface for cache abstraction
  - `MemoryCacheService` for single-server/development scenarios
  - `RedisCacheService` for distributed caching via IDistributedCache
  - `RedisAdvancedCacheService` for advanced Redis features (pattern deletion, stats)
  - Automatic cache invalidation on Add, Update, Delete operations
  - Configurable caching via `CacheOptions`:
    - Selective caching (GetById, GetAll, GetPaged, GetAsync)
    - Custom TTL and sliding expiration
    - Cache key prefix customization
    - Multi-tenant cache isolation
  - Performance boost: 96% faster for repeated queries (100 calls: 5s → 150ms)
  - Completely optional - zero impact when not configured
  - Thread-safe implementations for all cache providers
  - Integration with Multi-Tenancy (automatic tenant-based cache keys)
  - Dependencies: Microsoft.Extensions.Caching.Memory, Microsoft.Extensions.Caching.StackExchangeRedis
  - Documentation: CACHE_USAGE.md

- 🎯 **Specification Pattern**: Reusable, testable query logic
  - ISpecification&lt;T&gt; and Specification&lt;T&gt; base class
  - SpecificationEvaluator for applying specifications to queries
  - Encapsulate complex query logic in classes instead of inline LINQ
  - Benefits: Reusability, testability, maintainability, type safety
  - Features:
    - Criteria (WHERE), Includes (Eager Loading), OrderBy, Pagination
    - AsNoTracking, AsSplitQuery, IgnoreQueryFilters support
    - Parameterized specifications for dynamic queries
  - Repository methods:
    - GetAsync(specification)
    - GetFirstOrDefaultAsync(specification)
    - GetPagedAsync(specification)
    - CountAsync(specification)
    - AnyAsync(specification)
  - Perfect for complex queries, business rules, and multi-criteria searches
  - Completely optional - use inline LINQ or specifications as needed
  - Integrates seamlessly with existing features (Multi-Tenancy, Caching, etc.)
  - Documentation: SPECIFICATION_USAGE.md

## v1.5.0
- 🏢 **Multi-Tenancy Support**: Complete isolation for multi-tenant applications
  - `ITenantEntity` interface and `ITenantProvider` for tenant context
  - Automatic tenant filtering on all queries (GetAll, GetById, GetPaged, etc.)
  - Automatic tenant assignment on entity creation (Add, AddRange)
  - 12 new base classes combining tenant support with other features:
    - Simple: EntityBaseTenant, EntityBaseTenantInt
    - With Soft Delete: EntityBaseTenantSoftDelete[Alt][Int]
    - With Audit: EntityBaseTenantAuditable[Int]
    - Complete: EntityBaseTenantAuditableSoftDelete[Alt][Int]
  - SimpleTenantProvider for testing scenarios
  - Custom tenant identifier support (string, Guid, int, etc.)
  - Zero-configuration for tenant filtering - just implement ITenantProvider
  - UnitOfWork integration with tenant context propagation
  - Performance optimized with expression-based filters
  - Documentation: MULTITENANCY_USAGE.md

## v1.4.3
- 📖 **Fixed Documentation Links**: Updated all documentation links in README to use absolute GitHub URLs
  - Links now work correctly when viewing README on NuGet.org
  - Documentation files: MULTITARGETING.md, UNITOFWORK_USAGE.md, INCLUDE_USAGE.md, ASNOTRACKING_USAGE.md, SOFTDELETE_USAGE.md, AUDIT_USAGE.md
  - Improved user experience for package documentation

## v1.4.2
- 🎨 **Package Icon**: Added custom icon for better visibility on NuGet.org
  - 128x128 pixels, optimized to 23 KB
  - Displays on NuGet package listing and Visual Studio package manager
  - Professional branding for the package

## v1.4.1
- 🎯 **Multi-Targeting Support**: Now supports .NET 9.0 and 10.0
  - Single package works across both .NET versions
  - Automatic version selection based on your project's target framework
  - EF Core 9.x for .NET 9.0, EF Core 10.x for .NET 10.0
  - Zero configuration required - NuGet handles everything automatically
  - Package contains optimized builds for each framework
  - Documentation: MULTITARGETING.md

## v1.4.0
- 📝 **Audit Fields Support**: Optional automatic tracking (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
  - `IAuditable` interface for entities requiring audit tracking
  - Optional parameters in AddAsync, AddRangeAsync, UpdateAsync, UpdateRangeAsync
  - Base classes: EntityBaseAuditable, EntityBaseAuditableInt
  - Combined classes: EntityBaseAuditableSoftDelete, EntityBaseAuditableSoftDeleteInt, EntityBaseAuditableSoftDeleteAlt, EntityBaseAuditableSoftDeleteAltInt
  - Fully optional - use only when needed
  - Automatic timestamp and user tracking
  - ASP.NET Core integration examples
  - Documentation: AUDIT_USAGE.md

## v1.3.0
- ⚡ **AsNoTracking Support**: 30-40% performance improvement for read-only queries
  - All query methods (GetByIdAsync, GetAllAsync, GetAsync, GetPagedAsync, GetFirstOrDefaultAsync) support `asNoTracking` parameter
  - Ideal for lists, pagination, API GET endpoints, and display-only scenarios
  - Documentation: ASNOTRACKING_USAGE.md

- 🗑️ **Soft Delete Support**: Flexible field naming (IsDeleted or Deleted)
  - `ISoftDeletable` interface for entities using `IsDeleted` property
  - `ISoftDeletableAlternative` interface for entities using `Deleted` property
  - Base classes: EntityBaseSoftDelete, EntityBaseSoftDeleteInt, EntityBaseSoftDeleteAlt, EntityBaseSoftDeleteAltInt
  - Methods: SoftDeleteAsync, SoftDeleteRangeAsync, RestoreAsync
  - DeletedAt and DeletedBy tracking for audit purposes
  - Global Query Filter examples for automatic filtering
  - Documentation: SOFTDELETE_USAGE.md

## v1.2.1
- Improved release notes formatting with Markdown support
- Better documentation structure on NuGet.org

## v1.2.0
- Added Include (Eager Loading) support to all query methods
- GetByIdAsync, GetAllAsync, GetAsync, GetPagedAsync, and GetFirstOrDefaultAsync now support includes
- Support for ThenInclude for nested relationships
- Comprehensive documentation in INCLUDE_USAGE.md

## v1.1.1
- Added README.md to package for better documentation on NuGet.org

## v1.1.0
- Added Unit of Work pattern with comprehensive transaction support
- ExecuteInTransactionAsync for automatic transaction management
- Manual transaction control with BeginTransactionAsync, CommitAsync, and RollbackAsync
- Multiple repository coordination
- Comprehensive documentation in UNITOFWORK_USAGE.md

## v1.0.0
- Initial release with generic repository pattern
- Support for Guid and Int primary keys
- Filtering with Expression Functions
- Pagination support
- Full CRUD operations
