using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Domain.Models.Execution;

/// <summary>
/// ����ִ����ʷʵ�壨�ۺϸ���
/// </summary>
public class TaskExecutionHistory
{
    public Guid ExecutionId { get; private set; }
    public Guid UserId { get; private set; }
    public string RequestId { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public ExecutionStatus ExecutionStatus { get; private set; } = ExecutionStatus.Pending;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public long TotalExecutionTime { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string ResultSummary { get; set; } = string.Empty;
    public int ErrorCount { get; private set; }
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public int TokenUsage { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // ��������
    public virtual UserManagement.UserProfile User { get; set; } = null!;
    public virtual ICollection<ExecutionStepRecord> ExecutionSteps { get; private set; } = new List<ExecutionStepRecord>();
    public virtual ICollection<Monitoring.ErrorEventRecord> ErrorEvents { get; private set; } = new List<Monitoring.ErrorEventRecord>();
    public virtual ICollection<Monitoring.PerformanceMetricsRecord> PerformanceMetrics { get; private set; } = new List<Monitoring.PerformanceMetricsRecord>();

    // ˽�й��캯������EF Core
    private TaskExecutionHistory() 
    {
        ExecutionId = Guid.NewGuid();
        StartTime = DateTime.UtcNow;
    }

    public TaskExecutionHistory(
        Guid userId,
        string requestId,
        string userInput,
        string requestType,
        string? llmProvider = null,
        string? llmModel = null,
        List<string>? tags = null,
        Dictionary<string, object>? metadata = null)
    {
        ExecutionId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        RequestId = !string.IsNullOrWhiteSpace(requestId) ? requestId : throw new ArgumentException("RequestId cannot be empty", nameof(requestId));
        UserInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : throw new ArgumentException("UserInput cannot be empty", nameof(userInput));
        RequestType = !string.IsNullOrWhiteSpace(requestType) ? requestType : "General";
        ExecutionStatus = ExecutionStatus.Pending;
        StartTime = DateTime.UtcNow;
        IsSuccessful = false;
        ResultSummary = string.Empty;
        ErrorCount = 0;
        LlmProvider = llmProvider ?? string.Empty;
        LlmModel = llmModel ?? string.Empty;
        TokenUsage = 0;
        EstimatedCost = 0;
        Tags = tags ?? new List<string>();
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// ����ִ��ʱ��
    /// </summary>
    public long CalculateExecutionTime()
    {
        if (!EndTime.HasValue)
            return (long)(DateTime.UtcNow - StartTime).TotalMilliseconds;
        
        return (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// ���ִ�в���
    /// </summary>
    public void AddExecutionStep(ExecutionStepRecord step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));

        ExecutionSteps.Add(step);
    }

    /// <summary>
    /// ���Ϊ�����
    /// </summary>
    public void MarkAsCompleted(bool isSuccessful, string? resultSummary = null)
    {
        if (ExecutionStatus.CanTransitionTo(ExecutionStatus.Completed))
        {
            ExecutionStatus = ExecutionStatus.Completed;
            EndTime = DateTime.UtcNow;
            TotalExecutionTime = CalculateExecutionTime();
            IsSuccessful = isSuccessful;
            ResultSummary = resultSummary ?? string.Empty;
        }
    }

    /// <summary>
    /// ���Ϊʧ��
    /// </summary>
    public void MarkAsFailed(string? errorMessage = null)
    {
        if (ExecutionStatus.CanTransitionTo(ExecutionStatus.Failed))
        {
            ExecutionStatus = ExecutionStatus.Failed;
            EndTime = DateTime.UtcNow;
            TotalExecutionTime = CalculateExecutionTime();
            IsSuccessful = false;
            ResultSummary = errorMessage ?? "Execution failed";
            ErrorCount++;
        }
    }

    /// <summary>
    /// ��ʼִ��
    /// </summary>
    public void StartExecution()
    {
        if (ExecutionStatus.CanTransitionTo(ExecutionStatus.Running))
        {
            ExecutionStatus = ExecutionStatus.Running;
            StartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// ȡ��ִ��
    /// </summary>
    public void CancelExecution()
    {
        if (ExecutionStatus.CanTransitionTo(ExecutionStatus.Cancelled))
        {
            ExecutionStatus = ExecutionStatus.Cancelled;
            EndTime = DateTime.UtcNow;
            TotalExecutionTime = CalculateExecutionTime();
            IsSuccessful = false;
        }
    }

    /// <summary>
    /// ����LLMʹ�����
    /// </summary>
    public void UpdateLLMUsage(string? provider, string? model, int tokenUsage, decimal estimatedCost)
    {
        LlmProvider = provider ?? string.Empty;
        LlmModel = model ?? string.Empty;
        TokenUsage += tokenUsage;
        EstimatedCost += estimatedCost;
    }

    /// <summary>
    /// ��ӱ�ǩ
    /// </summary>
    public void AddTag(string? tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }

    /// <summary>
    /// ���Ԫ����
    /// </summary>
    public void AddMetadata(string key, object? value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata[key] = value ?? string.Empty;
        }
    }

    /// <summary>
    /// ��ȡִ��ͳ����Ϣ
    /// </summary>
    public ExecutionStatistics GetStatistics()
    {
        return new ExecutionStatistics
        {
            TotalSteps = ExecutionSteps.Count,
            CompletedSteps = ExecutionSteps.Count(s => s.IsSuccessful),
            FailedSteps = ExecutionSteps.Count(s => !s.IsSuccessful),
            TotalExecutionTime = TotalExecutionTime,
            AverageStepTime = ExecutionSteps.Any() ? ExecutionSteps.Average(s => s.ExecutionTime) : 0,
            TokenUsage = TokenUsage,
            EstimatedCost = EstimatedCost
        };
    }
}

/// <summary>
/// ִ��ͳ����Ϣ
/// </summary>
public class ExecutionStatistics
{
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int FailedSteps { get; set; }
    public long TotalExecutionTime { get; set; }
    public double AverageStepTime { get; set; }
    public int TokenUsage { get; set; }
    public decimal EstimatedCost { get; set; }
}