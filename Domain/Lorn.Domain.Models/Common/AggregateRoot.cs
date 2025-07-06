namespace Lorn.Domain.Models.Common;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    // AggregateRoot 现在继承了 BaseEntity 的领域事件功能
    // 不需要重复实现 AddDomainEvent 等方法
}

/// <summary>
/// Interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the date and time when the event occurred
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the unique identifier of the event
    /// </summary>
    Guid EventId { get; }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the DomainEvent class
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the date and time when the event occurred
    /// </summary>
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the unique identifier of the event
    /// </summary>
    public Guid EventId { get; }
}