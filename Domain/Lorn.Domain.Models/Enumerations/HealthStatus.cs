namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Health status enumeration
/// </summary>
public sealed class HealthStatus : Enumeration
{
    /// <summary>
    /// Healthy status
    /// </summary>
    public static readonly HealthStatus Healthy = new(1, nameof(Healthy), "System is operating normally");

    /// <summary>
    /// Warning status
    /// </summary>
    public static readonly HealthStatus Warning = new(2, nameof(Warning), "System is experiencing minor issues");

    /// <summary>
    /// Critical status
    /// </summary>
    public static readonly HealthStatus Critical = new(3, nameof(Critical), "System has critical issues requiring attention");

    /// <summary>
    /// Unknown status
    /// </summary>
    public static readonly HealthStatus Unknown = new(4, nameof(Unknown), "System health status is unknown");

    /// <summary>
    /// Offline status
    /// </summary>
    public static readonly HealthStatus Offline = new(5, nameof(Offline), "System is offline or unreachable");

    /// <summary>
    /// Gets the description of the health status
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the HealthStatus class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private HealthStatus(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if the status indicates the system is operational
    /// </summary>
    /// <returns>True if operational, false otherwise</returns>
    public bool IsOperational()
    {
        return this == Healthy || this == Warning;
    }

    /// <summary>
    /// Checks if the status requires immediate attention
    /// </summary>
    /// <returns>True if critical attention is needed, false otherwise</returns>
    public bool RequiresAttention()
    {
        return this == Critical || this == Offline;
    }

    /// <summary>
    /// Gets the severity level of the health status
    /// </summary>
    /// <returns>The severity level (0-4, where 0 is best)</returns>
    public int GetSeverityLevel()
    {
        return this switch
        {
            var s when s == Healthy => 0,
            var s when s == Warning => 1,
            var s when s == Unknown => 2,
            var s when s == Critical => 3,
            var s when s == Offline => 4,
            _ => 4
        };
    }

    /// <summary>
    /// Determines the appropriate action based on health status
    /// </summary>
    /// <returns>The recommended action</returns>
    public string GetRecommendedAction()
    {
        return this switch
        {
            var s when s == Healthy => "Continue normal operation",
            var s when s == Warning => "Monitor closely and investigate issues",
            var s when s == Critical => "Immediate investigation and remediation required",
            var s when s == Unknown => "Perform health check to determine status",
            var s when s == Offline => "Attempt reconnection or restart service",
            _ => "Unknown status - perform full system check"
        };
    }
}