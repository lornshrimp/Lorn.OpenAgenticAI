using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.Execution;

/// <summary>
/// Error event record entity
/// </summary>
public class ErrorEventRecord : BaseEntity
{
    /// <summary>
    /// Gets the error event identifier
    /// </summary>
    public Guid ErrorEventId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the execution identifier
    /// </summary>
    public Guid? ExecutionId { get; private set; }

    /// <summary>
    /// Gets the step identifier
    /// </summary>
    public string? StepId { get; private set; }

    /// <summary>
    /// Gets the error type
    /// </summary>
    public string ErrorType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the error code
    /// </summary>
    public string ErrorCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the stack trace
    /// </summary>
    public string? StackTrace { get; private set; }

    /// <summary>
    /// Gets the source component
    /// </summary>
    public string SourceComponent { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the severity level
    /// </summary>
    public string Severity { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Gets the environment information
    /// </summary>
    public string? Environment { get; private set; }

    /// <summary>
    /// Gets the user agent
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Gets whether the error is resolved
    /// </summary>
    public bool IsResolved { get; private set; }

    /// <summary>
    /// Gets the resolution time
    /// </summary>
    public DateTime? ResolutionTime { get; private set; }

    /// <summary>
    /// Gets the resolution notes
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// Gets the recurrence count
    /// </summary>
    public int RecurrenceCount { get; private set; } = 1;

    /// <summary>
    /// Gets the first occurrence time
    /// </summary>
    public DateTime FirstOccurrence { get; private set; }

    /// <summary>
    /// Gets the last occurrence time
    /// </summary>
    public DateTime LastOccurrence { get; private set; }

    /// <summary>
    /// Gets the task execution
    /// </summary>
    public TaskExecutionHistory? Execution { get; private set; }

    /// <summary>
    /// Initializes a new instance of the ErrorEventRecord class
    /// </summary>
    /// <param name="errorEventId">The error event identifier</param>
    /// <param name="errorType">The error type</param>
    /// <param name="errorCode">The error code</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="sourceComponent">The source component</param>
    /// <param name="severity">The severity level</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="stepId">The step identifier</param>
    public ErrorEventRecord(
        Guid errorEventId,
        string errorType,
        string errorCode,
        string errorMessage,
        string sourceComponent,
        string severity,
        Guid? userId = null,
        Guid? executionId = null,
        string? stepId = null)
    {
        ErrorEventId = errorEventId;
        ErrorType = errorType ?? throw new ArgumentNullException(nameof(errorType));
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        SourceComponent = sourceComponent ?? throw new ArgumentNullException(nameof(sourceComponent));
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        UserId = userId;
        ExecutionId = executionId;
        StepId = stepId;
        Timestamp = DateTime.UtcNow;
        FirstOccurrence = DateTime.UtcNow;
        LastOccurrence = DateTime.UtcNow;
        
        Id = errorEventId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ErrorEventRecord() { }

    /// <summary>
    /// Sets the stack trace
    /// </summary>
    /// <param name="stackTrace">The stack trace</param>
    public void SetStackTrace(string stackTrace)
    {
        StackTrace = stackTrace;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the environment information
    /// </summary>
    /// <param name="environment">The environment information</param>
    public void SetEnvironment(string environment)
    {
        Environment = environment;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the user agent
    /// </summary>
    /// <param name="userAgent">The user agent</param>
    public void SetUserAgent(string userAgent)
    {
        UserAgent = userAgent;
        UpdateVersion();
    }

    /// <summary>
    /// Marks the error as resolved
    /// </summary>
    /// <param name="resolutionNotes">The resolution notes</param>
    public void MarkAsResolved(string? resolutionNotes = null)
    {
        IsResolved = true;
        ResolutionTime = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;
        UpdateVersion();
    }

    /// <summary>
    /// Marks the error as unresolved
    /// </summary>
    public void MarkAsUnresolved()
    {
        IsResolved = false;
        ResolutionTime = null;
        ResolutionNotes = null;
        UpdateVersion();
    }

    /// <summary>
    /// Records another occurrence of this error
    /// </summary>
    public void RecordOccurrence()
    {
        RecurrenceCount++;
        LastOccurrence = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Gets the error severity as an enum
    /// </summary>
    /// <returns>The error severity</returns>
    public ErrorSeverity GetSeverityLevel()
    {
        return Severity.ToLower() switch
        {
            "low" => ErrorSeverity.Low,
            "medium" => ErrorSeverity.Medium,
            "high" => ErrorSeverity.High,
            "critical" => ErrorSeverity.Critical,
            _ => ErrorSeverity.Medium
        };
    }

    /// <summary>
    /// Checks if the error is critical
    /// </summary>
    /// <returns>True if critical, false otherwise</returns>
    public bool IsCritical()
    {
        return GetSeverityLevel() == ErrorSeverity.Critical;
    }

    /// <summary>
    /// Gets the time since first occurrence
    /// </summary>
    /// <returns>The time span since first occurrence</returns>
    public TimeSpan GetTimeSinceFirstOccurrence()
    {
        return DateTime.UtcNow - FirstOccurrence;
    }

    /// <summary>
    /// Gets the time since last occurrence
    /// </summary>
    /// <returns>The time span since last occurrence</returns>
    public TimeSpan GetTimeSinceLastOccurrence()
    {
        return DateTime.UtcNow - LastOccurrence;
    }
}

/// <summary>
/// Error severity enumeration
/// </summary>
public enum ErrorSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}