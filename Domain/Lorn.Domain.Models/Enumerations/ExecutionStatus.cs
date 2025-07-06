using System.Reflection;

namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Execution status enumeration
/// </summary>
public sealed class ExecutionStatus : Enumeration
{
    /// <summary>
    /// Pending execution
    /// </summary>
    public static readonly ExecutionStatus Pending = new(1, nameof(Pending));

    /// <summary>
    /// Currently running
    /// </summary>
    public static readonly ExecutionStatus Running = new(2, nameof(Running));

    /// <summary>
    /// Completed successfully
    /// </summary>
    public static readonly ExecutionStatus Completed = new(3, nameof(Completed));

    /// <summary>
    /// Failed execution
    /// </summary>
    public static readonly ExecutionStatus Failed = new(4, nameof(Failed));

    /// <summary>
    /// Cancelled execution
    /// </summary>
    public static readonly ExecutionStatus Cancelled = new(5, nameof(Cancelled));

    /// <summary>
    /// Timeout occurred
    /// </summary>
    public static readonly ExecutionStatus Timeout = new(6, nameof(Timeout));

    /// <summary>
    /// Initializes a new instance of the ExecutionStatus class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    private ExecutionStatus(int id, string name) : base(id, name) { }

    /// <summary>
    /// Checks if the current status can transition to the specified status
    /// </summary>
    /// <param name="newStatus">The new status</param>
    /// <returns>True if transition is valid, false otherwise</returns>
    public bool CanTransitionTo(ExecutionStatus newStatus)
    {
        return this switch
        {
            var s when s == Pending => newStatus == Running || newStatus == Cancelled,
            var s when s == Running => newStatus == Completed || newStatus == Failed || newStatus == Timeout || newStatus == Cancelled,
            var s when s == Completed => false,
            var s when s == Failed => false,
            var s when s == Cancelled => false,
            var s when s == Timeout => false,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the status represents a final state
    /// </summary>
    /// <returns>True if the status is final, false otherwise</returns>
    public bool IsFinal()
    {
        return this == Completed || this == Failed || this == Cancelled || this == Timeout;
    }

    /// <summary>
    /// Checks if the status represents a successful completion
    /// </summary>
    /// <returns>True if the status is successful, false otherwise</returns>
    public bool IsSuccess()
    {
        return this == Completed;
    }
}