using System;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Capabilities;

/// <summary>
/// Agent动作定义实体
/// </summary>
public class AgentActionDefinition
{
    public Guid ActionId { get; private set; }
    public string AgentId { get; private set; } = string.Empty;
    public string ActionName { get; private set; } = string.Empty;
    public string ActionDescription { get; private set; } = string.Empty;
    public string InputParameters { get; private set; } = string.Empty; // JSON Schema
    public string OutputFormat { get; private set; } = string.Empty; // JSON Schema
    public long EstimatedExecutionTime { get; private set; }
    public double ReliabilityScore { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime LastUsedTime { get; private set; }
    public string ExampleUsage { get; private set; } = string.Empty; // JSON
    public string? DocumentationUrl { get; private set; }

    // 导航属性
    public virtual AgentCapabilityRegistry Agent { get; private set; } = null!;

    // 私有构造函数供EF Core
    private AgentActionDefinition() { }

    public AgentActionDefinition(
        string agentId,
        string actionName,
        string actionDescription,
        string? inputParameters = null,
        string? outputFormat = null,
        long estimatedExecutionTime = 1000,
        string? exampleUsage = null,
        string? documentationUrl = null)
    {
        ActionId = Guid.NewGuid();
        AgentId = !string.IsNullOrWhiteSpace(agentId) ? agentId : throw new ArgumentException("AgentId cannot be empty", nameof(agentId));
        ActionName = !string.IsNullOrWhiteSpace(actionName) ? actionName : throw new ArgumentException("ActionName cannot be empty", nameof(actionName));
        ActionDescription = !string.IsNullOrWhiteSpace(actionDescription) ? actionDescription : throw new ArgumentException("ActionDescription cannot be empty", nameof(actionDescription));
        InputParameters = inputParameters ?? "{}";
        OutputFormat = outputFormat ?? "{}";
        EstimatedExecutionTime = estimatedExecutionTime > 0 ? estimatedExecutionTime : 1000;
        ReliabilityScore = 1.0; // 初始可靠性分数
        UsageCount = 0;
        LastUsedTime = DateTime.UtcNow;
        ExampleUsage = exampleUsage ?? "{}";
        DocumentationUrl = documentationUrl;
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
        LastUsedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新可靠性分数
    /// </summary>
    public void UpdateReliabilityScore(double score)
    {
        if (score < 0 || score > 1)
            throw new ArgumentException("Reliability score must be between 0 and 1", nameof(score));

        // 使用加权平均更新可靠性分数
        var weight = Math.Min(UsageCount, 100) / 100.0; // 最多考虑100次使用的权重
        ReliabilityScore = (ReliabilityScore * weight) + (score * (1 - weight));
    }

    /// <summary>
    /// 验证输入参数
    /// </summary>
    public ValidationResult ValidateInput(object input)
    {
        var result = new ValidationResult();

        if (input == null)
        {
            result.AddError("Input", "Input cannot be null");
            return result;
        }

        try
        {
            // 这里应该使用JSON Schema验证，现在简化处理
            var inputJson = System.Text.Json.JsonSerializer.Serialize(input);
            if (string.IsNullOrWhiteSpace(inputJson))
            {
                result.AddError("Input", "Input cannot be serialized to JSON");
            }
        }
        catch (Exception ex)
        {
            result.AddError("Input", $"Input validation failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 更新参数信息
    /// </summary>
    public void UpdateAction(
        string? actionDescription = null,
        string? inputParameters = null,
        string? outputFormat = null,
        long? estimatedExecutionTime = null,
        string? exampleUsage = null,
        string? documentationUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(actionDescription))
            ActionDescription = actionDescription;

        if (inputParameters != null)
            InputParameters = inputParameters;

        if (outputFormat != null)
            OutputFormat = outputFormat;

        if (estimatedExecutionTime.HasValue && estimatedExecutionTime.Value > 0)
            EstimatedExecutionTime = estimatedExecutionTime.Value;

        if (exampleUsage != null)
            ExampleUsage = exampleUsage;

        DocumentationUrl = documentationUrl;
    }

    /// <summary>
    /// 记录执行结果以更新可靠性
    /// </summary>
    public void RecordExecution(bool isSuccessful, long executionTime)
    {
        IncrementUsageCount();

        // 更新预估执行时间 (使用移动平均)
        var alpha = 0.1; // 平滑因子
        EstimatedExecutionTime = (long)(alpha * executionTime + (1 - alpha) * EstimatedExecutionTime);

        // 更新可靠性分数
        var successScore = isSuccessful ? 1.0 : 0.0;
        UpdateReliabilityScore(successScore);
    }

    /// <summary>
    /// 检查动作是否可用
    /// </summary>
    public bool IsAvailable()
    {
        return !string.IsNullOrWhiteSpace(AgentId) && 
               !string.IsNullOrWhiteSpace(ActionName) && 
               ReliabilityScore > 0.1; // 可靠性分数低于0.1视为不可用
    }

    /// <summary>
    /// 获取动作摘要信息
    /// </summary>
    public ActionSummary GetSummary()
    {
        return new ActionSummary
        {
            ActionId = ActionId,
            AgentId = AgentId,
            ActionName = ActionName,
            Description = ActionDescription,
            EstimatedExecutionTime = EstimatedExecutionTime,
            ReliabilityScore = ReliabilityScore,
            UsageCount = UsageCount,
            LastUsedTime = LastUsedTime,
            IsAvailable = IsAvailable()
        };
    }
}

/// <summary>
/// 动作摘要信息
/// </summary>
public class ActionSummary
{
    public Guid ActionId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long EstimatedExecutionTime { get; set; }
    public double ReliabilityScore { get; set; }
    public int UsageCount { get; set; }
    public DateTime LastUsedTime { get; set; }
    public bool IsAvailable { get; set; }
}