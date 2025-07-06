namespace Lorn.Domain.Models.Common;

/// <summary>
/// Base entity class with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets or sets the entity identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the entity version for optimistic concurrency control
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the entity is active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets the domain events associated with this entity
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Updates the entity version and timestamp
    /// </summary>
    public virtual void UpdateVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the entity as inactive (soft delete)
    /// </summary>
    public virtual void Deactivate()
    {
        IsActive = false;
        UpdateVersion();
    }

    /// <summary>
    /// Marks the entity as active
    /// </summary>
    public virtual void Activate()
    {
        IsActive = true;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a domain event to the entity
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the entity
    /// </summary>
    /// <param name="domainEvent">The domain event to remove</param>
    protected void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}