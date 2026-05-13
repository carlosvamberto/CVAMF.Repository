namespace CVAMF.Repository.Entities;

/// <summary>
/// Interface for entities that support audit tracking
/// Tracks who created and who last modified the entity, along with timestamps
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// When the entity was created
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created the entity (username, email, or user ID)
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the entity (username, email, or user ID)
    /// </summary>
    string? UpdatedBy { get; set; }
}
