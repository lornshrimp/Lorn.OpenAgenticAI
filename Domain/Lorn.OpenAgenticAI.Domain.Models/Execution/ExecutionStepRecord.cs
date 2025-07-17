using System;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Execution;

/// <summary>
/// 执行步骤记录实体
/// </summary>
public class ExecutionStepRecord
{
    public Guid StepRecordId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string StepId { get; set; } = string.Empty;
    public int StepOrder { get; private set; }
    public string StepDescription { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public ExecutionStatus StepStatus { get; private set; } = ExecutionStatus.Pending;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public long ExecutionTime { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string OutputData { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; private set; }
    public ResourceUsage ResourceUsage { get; private set; } = new(0, 0);

    // 导航属性
    public virtual TaskExecutionHistory Execution { get; set; } = null!;

    // 私有构造函数用于EF Core
    private ExecutionStepRecord() 
    {
        StepRecordId = Guid.NewGuid();
        StartTime = DateTime.UtcNow;
    }

    public ExecutionStepRecord(
        Guid executionId,
        string stepId,
        int stepOrder,
        string stepDescription,
        string agentId,
        string actionName,
        string? parameters = null)
    {
        StepRecordId = Guid.NewGuid();
        ExecutionId = executionId != Guid.Empty ? executionId : throw new ArgumentException("ExecutionId cannot be empty", nameof(executionId));
        StepId = !string.IsNullOrWhiteSpace(stepId) ? stepId : throw new ArgumentException("StepId cannot be empty", nameof(stepId));
        StepOrder = stepOrder >= 0 ? stepOrder : throw new ArgumentException("StepOrder must be non-negative", nameof(stepOrder));
        StepDescription = !string.IsNullOrWhiteSpace(stepDescription) ? stepDescription : throw new ArgumentException("StepDescription cannot be empty", nameof(stepDescription));
        AgentId = !string.IsNullOrWhiteSpace(agentId) ? agentId : throw new ArgumentException("AgentId cannot be empty", nameof(agentId));
        ActionName = !string.IsNullOrWhiteSpace(actionName) ? actionName : throw new ArgumentException("ActionName cannot be empty", nameof(actionName));
        Parameters = parameters ?? string.Empty;
        StepStatus = ExecutionStatus.Pending;
        StartTime = DateTime.UtcNow;
        IsSuccessful = false;
        RetryCount = 0;
        ResourceUsage = new ResourceUsage(0, 0);
        OutputData = string.Empty;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// 计算执行时间
    /// </summary>
    public long CalculateExecutionTime()
    {
        if (!EndTime.HasValue)
            return (long)(DateTime.UtcNow - StartTime).TotalMilliseconds;
        
        return (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// 增加重试次数
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
    }

    /// <summary>
    /// 开始执行步骤
    /// </summary>
    public void StartStep()
    {
        if (StepStatus.CanTransitionTo(ExecutionStatus.Running))
        {
            StepStatus = ExecutionStatus.Running;
            StartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 标记步骤为已完成
    /// </summary>
    public void MarkAsCompleted(bool isSuccessful, string? outputData = null, string? errorMessage = null)
    {
        if (StepStatus.CanTransitionTo(ExecutionStatus.Completed))
        {
            StepStatus = ExecutionStatus.Completed;
            EndTime = DateTime.UtcNow;
            ExecutionTime = CalculateExecutionTime();
            IsSuccessful = isSuccessful;
            OutputData = outputData ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// 标记步骤为失败
    /// </summary>
    public void MarkAsFailed(string? errorMessage)
    {
        if (StepStatus.CanTransitionTo(ExecutionStatus.Failed))
        {
            StepStatus = ExecutionStatus.Failed;
            EndTime = DateTime.UtcNow;
            ExecutionTime = CalculateExecutionTime();
            IsSuccessful = false;
            ErrorMessage = errorMessage ?? "Step execution failed";
        }
    }

    /// <summary>
    /// 更新资源使用情况
    /// </summary>
    public void UpdateResourceUsage(ResourceUsage resourceUsage)
    {
        ResourceUsage = resourceUsage ?? throw new ArgumentNullException(nameof(resourceUsage));
    }

    /// <summary>
    /// 更新输出数据
    /// </summary>
    public void UpdateOutputData(string? outputData)
    {
        OutputData = outputData ?? string.Empty;
    }

    /// <summary>
    /// 更新参数
    /// </summary>
    public void UpdateParameters(string? parameters)
    {
        Parameters = parameters ?? string.Empty;
    }
}