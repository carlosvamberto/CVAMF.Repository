namespace CVAMF.Repository.MultiTenancy;

/// <summary>
/// Service that provides information about the current tenant context.
/// This interface should be implemented by the application to provide tenant information.
/// </summary>
/// <typeparam name="TTenantKey">The type of the tenant identifier</typeparam>
public interface ITenantProvider<TTenantKey>
{
    /// <summary>
    /// Gets the identifier of the current tenant.
    /// Returns null if no tenant context is established (e.g., during migrations, background jobs).
    /// </summary>
    TTenantKey? GetCurrentTenantId();

    /// <summary>
    /// Gets whether a tenant context is currently established.
    /// </summary>
    bool HasTenantContext();
}

/// <summary>
/// Service that provides information about the current tenant using string as identifier.
/// </summary>
public interface ITenantProvider : ITenantProvider<string>
{
}
