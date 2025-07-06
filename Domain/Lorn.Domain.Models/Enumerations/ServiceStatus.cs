using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;

namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Service status enumeration
/// </summary>
public sealed class ServiceStatus : Enumeration
{
    /// <summary>
    /// Service is available
    /// </summary>
    public static readonly ServiceStatus Available = new(1, nameof(Available), "Service is available and operational");

    /// <summary>
    /// Service is under maintenance
    /// </summary>
    public static readonly ServiceStatus Maintenance = new(2, nameof(Maintenance), "Service is under maintenance");

    /// <summary>
    /// Service is deprecated
    /// </summary>
    public static readonly ServiceStatus Deprecated = new(3, nameof(Deprecated), "Service is deprecated and will be discontinued");

    /// <summary>
    /// Service is unavailable
    /// </summary>
    public static readonly ServiceStatus Unavailable = new(4, nameof(Unavailable), "Service is temporarily unavailable");

    /// <summary>
    /// Service status is unknown
    /// </summary>
    public static readonly ServiceStatus Unknown = new(5, nameof(Unknown), "Service status is unknown");

    /// <summary>
    /// Gets the description of the service status
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the ServiceStatus class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private ServiceStatus(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if the service is operational
    /// </summary>
    /// <returns>True if operational, false otherwise</returns>
    public bool IsOperational()
    {
        return this == Available;
    }

    /// <summary>
    /// Checks if the service requires attention
    /// </summary>
    /// <returns>True if attention is required, false otherwise</returns>
    public bool RequiresAttention()
    {
        return this == Unavailable || this == Unknown;
    }

    /// <summary>
    /// Gets the severity level (0-4, where 0 is best)
    /// </summary>
    /// <returns>The severity level</returns>
    public int GetSeverityLevel()
    {
        return this switch
        {
            var s when s == Available => 0,
            var s when s == Maintenance => 1,
            var s when s == Deprecated => 2,
            var s when s == Unavailable => 3,
            var s when s == Unknown => 4,
            _ => 4
        };
    }
}