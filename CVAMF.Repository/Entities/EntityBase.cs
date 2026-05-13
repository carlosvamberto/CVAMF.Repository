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

/// <summary>
/// Base class for entities with Guid primary key and audit fields
/// </summary>
public abstract class EntityBaseAuditable : EntityBase, IAuditable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key and audit fields
/// </summary>
public abstract class EntityBaseAuditableInt : EntityBaseInt, IAuditable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base class for entities with Guid primary key and soft delete support
/// </summary>
public abstract class EntityBaseSoftDelete : EntityBase, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key and soft delete support
/// </summary>
public abstract class EntityBaseSoftDeleteInt : EntityBaseInt, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Guid primary key and soft delete using "Deleted" property
/// </summary>
public abstract class EntityBaseSoftDeleteAlt : EntityBase, ISoftDeletableAlternative
{
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key and soft delete using "Deleted" property
/// </summary>
public abstract class EntityBaseSoftDeleteAltInt : EntityBaseInt, ISoftDeletableAlternative
{
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Guid primary key, audit fields, and soft delete support (IsDeleted)
/// </summary>
public abstract class EntityBaseAuditableSoftDelete : EntityBase, IAuditable, ISoftDeletable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key, audit fields, and soft delete support (IsDeleted)
/// </summary>
public abstract class EntityBaseAuditableSoftDeleteInt : EntityBaseInt, IAuditable, ISoftDeletable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Guid primary key, audit fields, and soft delete support (Deleted)
/// </summary>
public abstract class EntityBaseAuditableSoftDeleteAlt : EntityBase, IAuditable, ISoftDeletableAlternative
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with Int primary key, audit fields, and soft delete support (Deleted)
/// </summary>
public abstract class EntityBaseAuditableSoftDeleteAltInt : EntityBaseInt, IAuditable, ISoftDeletableAlternative
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
