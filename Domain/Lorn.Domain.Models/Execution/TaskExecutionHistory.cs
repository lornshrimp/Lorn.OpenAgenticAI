using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.UserManagement;

namespace Lorn.Domain.Models.Execution;

/// <summary>
/// Task execution history aggregate root
/// </summary>
public class TaskExecutionHistory : AggregateRoot
{
    private readonly List<ExecutionStepRecord> _executionSteps = new();
    private readonly List<ErrorEventRecord> _errorEvents = new();
    private readonly List<PerformanceMetricsRecord> _performanceMetrics = new();

    /// <summary>
    /// Gets the execution identifier
    /// </summary>
    public Guid ExecutionId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the request identifier
    /// </summary>
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user input
    /// </summary>
    public string UserInput { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the request type
    /// </summary>
    public string RequestType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the execution status
    /// </summary>
    public ExecutionStatus ExecutionStatus { get; private set; } = ExecutionStatus.Pending;

    /// <summary>
    /// Gets the start time
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Gets the end time
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Gets the total execution time in milliseconds
    /// </summary>
    public long TotalExecutionTime { get; private set; }

    /// <summary>
    /// Gets whether the execution was successful
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Gets the result summary
    /// </summary>
    public string? ResultSummary { get; private set; }

    /// <summary>
    /// Gets the error count
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets the LLM provider
    /// </summary>
    public string? LlmProvider { get; private set; }

    /// <summary>
    /// Gets the LLM model
    /// </summary>
    public string? LlmModel { get; private set; }

    /// <summary>
    /// Gets the token usage
    /// </summary>
    public int TokenUsage { get; private set; }

    /// <summary>
    /// Gets the estimated cost
    /// </summary>
    public decimal EstimatedCost { get; private set; }

    /// <summary>
    /// Gets the tags
    /// </summary>
    public List<string> Tags { get; private set; } = new();

    /// <summary>
    /// Gets the metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    /// <summary>
    /// Gets the user profile
    /// </summary>
    public UserProfile User { get; private set; } = null!;

    /// <summary>
    /// Gets the execution steps
    /// </summary>
    public IReadOnlyList<ExecutionStepRecord> ExecutionSteps => _executionSteps.AsReadOnly();

    /// <summary>
    /// Gets the error events
    /// </summary>
    public IReadOnlyList<ErrorEventRecord> ErrorEvents => _errorEvents.AsReadOnly();

    /// <summary>
    /// Gets the performance metrics
    /// </summary>
    public IReadOnlyList<PerformanceMetricsRecord> PerformanceMetrics => _performanceMetrics.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the TaskExecutionHistory class
    /// </summary>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="requestId">The request identifier</param>
    /// <param name="userInput">The user input</param>
    /// <param name="requestType">The request type</param>
    public TaskExecutionHistory(
        Guid executionId,
        Guid userId,
        string requestId,
        string userInput,
        string requestType)
    {
        ExecutionId = executionId;
        UserId = userId;
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        UserInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        StartTime = DateTime.UtcNow;
        ExecutionStatus = ExecutionStatus.Pending;
        
        Id = executionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TaskExecutionStartedEvent(executionId, userId, userInput));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private TaskExecutionHistory() { }

    /// <summary>
    /// Starts the execution
    /// </summary>
    /// <param name="llmProvider">The LLM provider</param>
    /// <param name="llmModel">The LLM model</param>
    public void StartExecution(string? llmProvider = null, string? llmModel = null)
    {
        if (!ExecutionStatus.CanTransitionTo(ExecutionStatus.Running))
            throw new InvalidOperationException($"Cannot transition from {ExecutionStatus} to Running");

        ExecutionStatus = ExecutionStatus.Running;
        LlmProvider = llmProvider;
        LlmModel = llmModel;
        StartTime = DateTime.UtcNow;
        UpdateVersion();
        
        AddDomainEvent(new TaskExecutionRunningEvent(ExecutionId, UserId));
    }

    /// <summary>
    /// Marks the execution as completed
    /// </summary>
    /// <param name="isSuccessful">Whether the execution was successful</param>
    /// <param name="resultSummary">The result summary</param>
    /// <param name="tokenUsage">The token usage</param>
    /// <param name="estimatedCost">The estimated cost</param>
    public void MarkAsCompleted(bool isSuccessful, string? resultSummary = null, int tokenUsage = 0, decimal estimatedCost = 0)
    {
        var targetStatus = isSuccessful ? ExecutionStatus.Completed : ExecutionStatus.Failed;
        
        if (!ExecutionStatus.CanTransitionTo(targetStatus))
            throw new InvalidOperationException($"Cannot transition from {ExecutionStatus} to {targetStatus}");

        ExecutionStatus = targetStatus;
        EndTime = DateTime.UtcNow;
        TotalExecutionTime = CalculateExecutionTime();
        IsSuccessful = isSuccessful;
        ResultSummary = resultSummary;
        TokenUsage = tokenUsage;
        EstimatedCost = estimatedCost;
        UpdateVersion();
        
        if (isSuccessful)
        {
            AddDomainEvent(new TaskExecutionCompletedEvent(ExecutionId, UserId, TotalExecutionTime, TokenUsage, EstimatedCost));
        }
        else
        {
            AddDomainEvent(new TaskExecutionFailedEvent(ExecutionId, UserId, ErrorCount, resultSummary ?? "Execution failed"));
        }
    }

    /// <summary>
    /// Cancels the execution
    /// </summary>
    /// <param name="reason">The cancellation reason</param>
    public void Cancel(string? reason = null)
    {
        if (!ExecutionStatus.CanTransitionTo(ExecutionStatus.Cancelled))
            throw new InvalidOperationException($"Cannot transition from {ExecutionStatus} to Cancelled");

        ExecutionStatus = ExecutionStatus.Cancelled;
        EndTime = DateTime.UtcNow;
        TotalExecutionTime = CalculateExecutionTime();
        ResultSummary = reason ?? "Execution cancelled";
        UpdateVersion();
        
        AddDomainEvent(new TaskExecutionCancelledEvent(ExecutionId, UserId, reason));
    }

    /// <summary>
    /// Marks the execution as timed out
    /// </summary>
    public void MarkAsTimedOut()
    {
        if (!ExecutionStatus.CanTransitionTo(ExecutionStatus.Timeout))
            throw new InvalidOperationException($"Cannot transition from {ExecutionStatus} to Timeout");

        ExecutionStatus = ExecutionStatus.Timeout;
        EndTime = DateTime.UtcNow;
        TotalExecutionTime = CalculateExecutionTime();
        ResultSummary = "Execution timed out";
        UpdateVersion();
        
        AddDomainEvent(new TaskExecutionTimedOutEvent(ExecutionId, UserId, TotalExecutionTime));
    }

    /// <summary>
    /// Calculates the execution time
    /// </summary>
    /// <returns>The execution time in milliseconds</returns>
    public long CalculateExecutionTime()
    {
        if (EndTime.HasValue)
            return (long)(EndTime.Value - StartTime).TotalMilliseconds;
        
        return (long)(DateTime.UtcNow - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Adds an execution step
    /// </summary>
    /// <param name="step">The execution step</param>
    public void AddExecutionStep(ExecutionStepRecord step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));

        _executionSteps.Add(step);
        UpdateVersion();
    }

    /// <summary>
    /// Adds an error event
    /// </summary>
    /// <param name="errorEvent">The error event</param>
    public void AddErrorEvent(ErrorEventRecord errorEvent)
    {
        if (errorEvent == null)
            throw new ArgumentNullException(nameof(errorEvent));

        _errorEvents.Add(errorEvent);
        ErrorCount = _errorEvents.Count;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a performance metric
    /// </summary>
    /// <param name="metric">The performance metric</param>
    public void AddPerformanceMetric(PerformanceMetricsRecord metric)
    {
        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        _performanceMetrics.Add(metric);
        UpdateVersion();
    }

    /// <summary>
    /// Adds a tag
    /// </summary>
    /// <param name="tag">The tag to add</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a tag
    /// </summary>
    /// <param name="tag">The tag to remove</param>
    public void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
            UpdateVersion();
    }

    /// <summary>
    /// Sets metadata
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    public void SetMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        Metadata[key] = value;
        UpdateVersion();
    }

    /// <summary>
    /// Gets metadata
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <returns>The metadata value or null if not found</returns>
    public object? GetMetadata(string key)
    {
        return Metadata.ContainsKey(key) ? Metadata[key] : null;
    }
}

/// <summary>
/// Domain event raised when task execution starts
/// </summary>
public class TaskExecutionStartedEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }
    public string UserInput { get; }

    public TaskExecutionStartedEvent(Guid executionId, Guid userId, string userInput)
    {
        ExecutionId = executionId;
        UserId = userId;
        UserInput = userInput;
    }
}

/// <summary>
/// Domain event raised when task execution is running
/// </summary>
public class TaskExecutionRunningEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }

    public TaskExecutionRunningEvent(Guid executionId, Guid userId)
    {
        ExecutionId = executionId;
        UserId = userId;
    }
}

/// <summary>
/// Domain event raised when task execution completes successfully
/// </summary>
public class TaskExecutionCompletedEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }
    public long ExecutionTime { get; }
    public int TokenUsage { get; }
    public decimal EstimatedCost { get; }

    public TaskExecutionCompletedEvent(Guid executionId, Guid userId, long executionTime, int tokenUsage, decimal estimatedCost)
    {
        ExecutionId = executionId;
        UserId = userId;
        ExecutionTime = executionTime;
        TokenUsage = tokenUsage;
        EstimatedCost = estimatedCost;
    }
}

/// <summary>
/// Domain event raised when task execution fails
/// </summary>
public class TaskExecutionFailedEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }
    public int ErrorCount { get; }
    public string ErrorMessage { get; }

    public TaskExecutionFailedEvent(Guid executionId, Guid userId, int errorCount, string errorMessage)
    {
        ExecutionId = executionId;
        UserId = userId;
        ErrorCount = errorCount;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Domain event raised when task execution is cancelled
/// </summary>
public class TaskExecutionCancelledEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }
    public string? Reason { get; }

    public TaskExecutionCancelledEvent(Guid executionId, Guid userId, string? reason)
    {
        ExecutionId = executionId;
        UserId = userId;
        Reason = reason;
    }
}

/// <summary>
/// Domain event raised when task execution times out
/// </summary>
public class TaskExecutionTimedOutEvent : DomainEvent
{
    public Guid ExecutionId { get; }
    public Guid UserId { get; }
    public long ExecutionTime { get; }

    public TaskExecutionTimedOutEvent(Guid executionId, Guid userId, long executionTime)
    {
        ExecutionId = executionId;
        UserId = userId;
        ExecutionTime = executionTime;
    }
}