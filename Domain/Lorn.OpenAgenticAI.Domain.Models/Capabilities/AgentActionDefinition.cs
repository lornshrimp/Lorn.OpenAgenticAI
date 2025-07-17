using System;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Capabilities;

/// <summary>
/// Agent��������ʵ��
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

    // ��������
    public virtual AgentCapabilityRegistry Agent { get; private set; } = null!;

    // ˽�й��캯����EF Core
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
        ReliabilityScore = 1.0; // ��ʼ�ɿ��Է���
        UsageCount = 0;
        LastUsedTime = DateTime.UtcNow;
        ExampleUsage = exampleUsage ?? "{}";
        DocumentationUrl = documentationUrl;
    }

    /// <summary>
    /// ����ʹ�ô���
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
        LastUsedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���¿ɿ��Է���
    /// </summary>
    public void UpdateReliabilityScore(double score)
    {
        if (score < 0 || score > 1)
            throw new ArgumentException("Reliability score must be between 0 and 1", nameof(score));

        // ʹ�ü�Ȩƽ�����¿ɿ��Է���
        var weight = Math.Min(UsageCount, 100) / 100.0; // ��࿼��100��ʹ�õ�Ȩ��
        ReliabilityScore = (ReliabilityScore * weight) + (score * (1 - weight));
    }

    /// <summary>
    /// ��֤�������
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
            // ����Ӧ��ʹ��JSON Schema��֤�����ڼ򻯴���
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
    /// ���²�����Ϣ
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
    /// ��¼ִ�н���Ը��¿ɿ���
    /// </summary>
    public void RecordExecution(bool isSuccessful, long executionTime)
    {
        IncrementUsageCount();

        // ����Ԥ��ִ��ʱ�� (ʹ���ƶ�ƽ��)
        var alpha = 0.1; // ƽ������
        EstimatedExecutionTime = (long)(alpha * executionTime + (1 - alpha) * EstimatedExecutionTime);

        // ���¿ɿ��Է���
        var successScore = isSuccessful ? 1.0 : 0.0;
        UpdateReliabilityScore(successScore);
    }

    /// <summary>
    /// ��鶯���Ƿ����
    /// </summary>
    public bool IsAvailable()
    {
        return !string.IsNullOrWhiteSpace(AgentId) && 
               !string.IsNullOrWhiteSpace(ActionName) && 
               ReliabilityScore > 0.1; // �ɿ��Է�������0.1��Ϊ������
    }

    /// <summary>
    /// ��ȡ����ժҪ��Ϣ
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
/// ����ժҪ��Ϣ
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