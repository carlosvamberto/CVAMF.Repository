namespace CVAMF.Repository.MultiTenancy;

/// <summary>
/// Simple implementation of ITenantProvider that can be used for testing or simple scenarios.
/// For production use, implement your own tenant provider based on HTTP context, claims, or other sources.
/// </summary>
/// <typeparam name="TTenantKey">The type of the tenant identifier</typeparam>
public class SimpleTenantProvider<TTenantKey> : ITenantProvider<TTenantKey>
{
    private TTenantKey? _currentTenantId;

    /// <summary>
    /// Sets the current tenant ID. This is typically done at the beginning of a request.
    /// </summary>
    public void SetCurrentTenantId(TTenantKey? tenantId)
    {
        _currentTenantId = tenantId;
    }

    /// <summary>
    /// Gets the identifier of the current tenant.
    /// </summary>
    public TTenantKey? GetCurrentTenantId()
    {
        return _currentTenantId;
    }

    /// <summary>
    /// Gets whether a tenant context is currently established.
    /// </summary>
    public bool HasTenantContext()
    {
        return _currentTenantId != null && !EqualityComparer<TTenantKey>.Default.Equals(_currentTenantId, default);
    }
}

/// <summary>
/// Simple implementation of ITenantProvider using string as tenant identifier.
/// </summary>
public class SimpleTenantProvider : SimpleTenantProvider<string>, ITenantProvider
{
}
