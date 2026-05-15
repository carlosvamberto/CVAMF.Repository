namespace CVAMF.Repository.Entities;

/// <summary>
/// Interface for entities that belong to a specific tenant in multi-tenant applications.
/// Implementing this interface enables automatic tenant filtering in queries.
/// </summary>
/// <typeparam name="TTenantKey">The type of the tenant identifier (string, Guid, int, etc.)</typeparam>
public interface ITenantEntity<TTenantKey>
{
    /// <summary>
    /// The identifier of the tenant that owns this entity.
    /// This field is used for automatic tenant isolation in multi-tenant applications.
    /// </summary>
    TTenantKey TenantId { get; set; }
}

/// <summary>
/// Interface for entities that belong to a tenant using string as tenant identifier.
/// This is the most common scenario for multi-tenant applications.
/// </summary>
public interface ITenantEntity : ITenantEntity<string>
{
}
