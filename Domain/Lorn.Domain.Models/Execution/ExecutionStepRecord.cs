using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Execution;

/// <summary>
/// Execution step record entity
/// </summary>
public class ExecutionStepRecord : BaseEntity
{
    /// <summary>
    /// Gets the step record identifier
    /// </summary>
    public Guid StepRecordId { get; private set; }

    /// <summary>
    /// Gets the execution identifier
    /// </summary>
    public Guid ExecutionId { get; private set; }

    /// <summary>
    /// Gets the step identifier
    /// </summary>
    public string StepId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the step order
    /// </summary>
    public int StepOrder { get; private set; }

    /// <summary>
    /// Gets the step description
    /// </summary>
    public string StepDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the agent identifier
    /// </summary>
    public string AgentId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the action name
    /// </summary>
    public string ActionName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the parameters
    /// </summary>
    public string Parameters { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the step status
    /// </summary>
    public ExecutionStatus StepStatus { get; private set; } = ExecutionStatus.Pending;

    /// <summary>
    /// Gets the start time
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Gets the end time
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Gets the execution time in milliseconds
    /// </summary>
    public long ExecutionTime { get; private set; }

    /// <summary>
    /// Gets whether the step was successful
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Gets the output data
    /// </summary>
    public string? OutputData { get; private set; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the retry count
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the resource usage
    /// </summary>
    public ResourceUsage? ResourceUsage { get; private set; }

    /// <summary>
    /// Gets the task execution
    /// </summary>
    public TaskExecutionHistory Execution { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the ExecutionStepRecord class
    /// </summary>
    /// <param name="stepRecordId">The step record identifier</param>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="stepId">The step identifier</param>
    /// <param name="stepOrder">The step order</param>
    /// <param name="stepDescription">The step description</param>
    /// <param name="agentId">The agent identifier</param>
    /// <param name="actionName">The action name</param>
    /// <param name="parameters">The parameters</param>
    public ExecutionStepRecord(
        Guid stepRecordId,
        Guid executionId,
        string stepId,
        int stepOrder,
        string stepDescription,
        string agentId,
        string actionName,
        string parameters)
    {
        StepRecordId = stepRecordId;
        ExecutionId = executionId;
        StepId = stepId ?? throw new ArgumentNullException(nameof(stepId));
        StepOrder = stepOrder;
        StepDescription = stepDescription ?? throw new ArgumentNullException(nameof(stepDescription));
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        StartTime = DateTime.UtcNow;
        StepStatus = ExecutionStatus.Pending;
        
        Id = stepRecordId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ExecutionStepRecord() { }

    /// <summary>
    /// Starts the step execution
    /// </summary>
    public void StartExecution()
    {
        if (!StepStatus.CanTransitionTo(ExecutionStatus.Running))
            throw new InvalidOperationException($"Cannot transition from {StepStatus} to Running");

        StepStatus = ExecutionStatus.Running;
        StartTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Marks the step as completed
    /// </summary>
    /// <param name="isSuccessful">Whether the step was successful</param>
    /// <param name="outputData">The output data</param>
    /// <param name="errorMessage">The error message (if failed)</param>
    /// <param name="resourceUsage">The resource usage</param>
    public void MarkAsCompleted(bool isSuccessful, string? outputData = null, string? errorMessage = null, ResourceUsage? resourceUsage = null)
    {
        var targetStatus = isSuccessful ? ExecutionStatus.Completed : ExecutionStatus.Failed;
        
        if (!StepStatus.CanTransitionTo(targetStatus))
            throw new InvalidOperationException($"Cannot transition from {StepStatus} to {targetStatus}");

        StepStatus = targetStatus;
        EndTime = DateTime.UtcNow;
        ExecutionTime = CalculateExecutionTime();
        IsSuccessful = isSuccessful;
        OutputData = outputData;
        ErrorMessage = errorMessage;
        ResourceUsage = resourceUsage;
        UpdateVersion();
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
    /// Increments the retry count
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdateVersion();
    }

    /// <summary>
    /// Resets the step for retry
    /// </summary>
    public void ResetForRetry()
    {
        StepStatus = ExecutionStatus.Pending;
        EndTime = null;
        ExecutionTime = 0;
        OutputData = null;
        ErrorMessage = null;
        IncrementRetryCount();
    }

    /// <summary>
    /// Cancels the step execution
    /// </summary>
    /// <param name="reason">The cancellation reason</param>
    public void Cancel(string? reason = null)
    {
        if (!StepStatus.CanTransitionTo(ExecutionStatus.Cancelled))
            throw new InvalidOperationException($"Cannot transition from {StepStatus} to Cancelled");

        StepStatus = ExecutionStatus.Cancelled;
        EndTime = DateTime.UtcNow;
        ExecutionTime = CalculateExecutionTime();
        ErrorMessage = reason ?? "Step cancelled";
        UpdateVersion();
    }

    /// <summary>
    /// Marks the step as timed out
    /// </summary>
    public void MarkAsTimedOut()
    {
        if (!StepStatus.CanTransitionTo(ExecutionStatus.Timeout))
            throw new InvalidOperationException($"Cannot transition from {StepStatus} to Timeout");

        StepStatus = ExecutionStatus.Timeout;
        EndTime = DateTime.UtcNow;
        ExecutionTime = CalculateExecutionTime();
        ErrorMessage = "Step timed out";
        UpdateVersion();
    }

    /// <summary>
    /// Gets the parameters as a dictionary
    /// </summary>
    /// <returns>The parameters dictionary</returns>
    public Dictionary<string, object> GetParametersAsDictionary()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Parameters) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Sets the parameters from a dictionary
    /// </summary>
    /// <param name="parameters">The parameters dictionary</param>
    public void SetParametersFromDictionary(Dictionary<string, object> parameters)
    {
        Parameters = System.Text.Json.JsonSerializer.Serialize(parameters);
        UpdateVersion();
    }

    /// <summary>
    /// Gets the output data as a dictionary
    /// </summary>
    /// <returns>The output data dictionary</returns>
    public Dictionary<string, object> GetOutputDataAsDictionary()
    {
        if (string.IsNullOrEmpty(OutputData))
            return new Dictionary<string, object>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(OutputData) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}