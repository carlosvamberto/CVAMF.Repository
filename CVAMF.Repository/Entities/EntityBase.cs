namespace CVAMF.Repository.Entities;

/// <summary>
/// Base class for entities with Guid primary key
/// </summary>
public abstract class EntityBase : IEntity<Guid>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key
/// </summary>
public abstract class EntityBaseInt : IEntity<int>
{
    public int Id { get; set; }
}
