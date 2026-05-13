namespace CVAMF.Repository.Entities;

/// <summary>
/// Base interface for entities with a generic ID type
/// </summary>
/// <typeparam name="TKey">The type of the primary key (Guid or int)</typeparam>
public interface IEntity<TKey> where TKey : struct
{
    /// <summary>
    /// Primary key of the entity
    /// </summary>
    TKey Id { get; set; }
}
