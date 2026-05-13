namespace CVAMF.Repository.Entities;

/// <summary>
/// Base interface for soft delete functionality
/// Entities implementing this interface will be soft deleted instead of physically removed
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Date and time when the entity was soft deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who soft deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}

/// <summary>
/// Alternative interface for soft delete using "Deleted" property name
/// </summary>
public interface ISoftDeletableAlternative
{
    /// <summary>
    /// Indicates whether the entity has been deleted
    /// </summary>
    bool Deleted { get; set; }

    /// <summary>
    /// Date and time when the entity was deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}
