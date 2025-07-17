using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// ģ��ʵ��
/// </summary>
public class Model
{
    public Guid ModelId { get; private set; }
    public Guid ProviderId { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string ModelGroup { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ContextLength { get; private set; }
    public int? MaxOutputTokens { get; private set; }
    public List<ModelCapability> SupportedCapabilities { get; private set; } = new();
    public PricingInfo PricingInfo { get; private set; } = null!;
    public PerformanceMetrics PerformanceMetrics { get; private set; } = null!;
    public DateTime ReleaseDate { get; private set; }
    public bool IsLatestVersion { get; private set; }
    public bool IsPrebuilt { get; private set; }
    public DateTime CreatedTime { get; private set; }
    public Guid? CreatedBy { get; private set; }

    // ��������
    public virtual ModelProvider Provider { get; private set; } = null!;
    public virtual ICollection<ModelUserConfiguration> UserConfigurations { get; private set; } = new List<ModelUserConfiguration>();

    // ˽�й��캯����EF Core
    private Model() { }

    public Model(
        Guid providerId,
        string modelName,
        string displayName,
        int contextLength,
        PricingInfo pricingInfo,
        List<ModelCapability>? supportedCapabilities = null,
        string? modelGroup = null,
        string? description = null,
        int? maxOutputTokens = null,
        DateTime? releaseDate = null,
        bool isLatestVersion = true,
        bool isPrebuilt = false,
        Guid? createdBy = null)
    {
        ModelId = Guid.NewGuid();
        ProviderId = providerId != Guid.Empty ? providerId : throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName : throw new ArgumentException("ModelName cannot be empty", nameof(modelName));
        DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : modelName;
        ModelGroup = modelGroup ?? "Default";
        Description = description ?? string.Empty;
        ContextLength = contextLength > 0 ? contextLength : throw new ArgumentException("ContextLength must be positive", nameof(contextLength));
        MaxOutputTokens = maxOutputTokens > 0 ? maxOutputTokens : null;
        SupportedCapabilities = supportedCapabilities ?? new List<ModelCapability> { ModelCapability.TextGeneration };
        PricingInfo = pricingInfo ?? throw new ArgumentNullException(nameof(pricingInfo));
        PerformanceMetrics = new PerformanceMetrics();
        ReleaseDate = releaseDate ?? DateTime.UtcNow;
        IsLatestVersion = isLatestVersion;
        IsPrebuilt = isPrebuilt;
        CreatedTime = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// ����ʹ�óɱ�
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        return PricingInfo.CalculateCost(inputTokens, outputTokens);
    }

    /// <summary>
    /// ����Ƿ�֧��ָ������
    /// </summary>
    public bool SupportsCapability(ModelCapability capability)
    {
        return SupportedCapabilities.Contains(capability);
    }

    /// <summary>
    /// ��������ָ��
    /// </summary>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>
    /// ���֧�ֵ�����
    /// </summary>
    public void AddCapability(ModelCapability capability)
    {
        if (capability != null && !SupportedCapabilities.Contains(capability))
        {
            SupportedCapabilities.Add(capability);
        }
    }

    /// <summary>
    /// �Ƴ�֧�ֵ�����
    /// </summary>
    public void RemoveCapability(ModelCapability capability)
    {
        SupportedCapabilities.Remove(capability);
    }

    /// <summary>
    /// ����ģ����Ϣ
    /// </summary>
    public void UpdateModel(
        string? displayName = null,
        string? description = null,
        string? modelGroup = null,
        int? contextLength = null,
        int? maxOutputTokens = null,
        PricingInfo? pricingInfo = null,
        bool? isLatestVersion = null)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName;

        if (description != null)
            Description = description;

        if (!string.IsNullOrWhiteSpace(modelGroup))
            ModelGroup = modelGroup;

        if (contextLength.HasValue && contextLength.Value > 0)
            ContextLength = contextLength.Value;

        if (maxOutputTokens.HasValue && maxOutputTokens.Value > 0)
            MaxOutputTokens = maxOutputTokens.Value;

        if (pricingInfo != null)
            PricingInfo = pricingInfo;

        if (isLatestVersion.HasValue)
            IsLatestVersion = isLatestVersion.Value;
    }

    /// <summary>
    /// ���ģ���Ƿ��Ծ
    /// </summary>
    public bool IsActive()
    {
        // ģ�ͻ�Ծ�������������°汾��֧������һ������
        return IsLatestVersion && SupportedCapabilities.Any();
    }

    /// <summary>
    /// ��ȡģ��ժҪ��Ϣ
    /// </summary>
    public ModelSummary GetSummary()
    {
        return new ModelSummary
        {
            ModelId = ModelId,
            ModelName = ModelName,
            DisplayName = DisplayName,
            ModelGroup = ModelGroup,
            ContextLength = ContextLength,
            MaxOutputTokens = MaxOutputTokens,
            SupportedCapabilities = SupportedCapabilities.Select(c => c.Name).ToList(),
            InputPrice = PricingInfo.InputPrice,
            OutputPrice = PricingInfo.OutputPrice,
            Currency = PricingInfo.Currency.Name,
            IsLatestVersion = IsLatestVersion,
            IsActive = IsActive(),
            AverageResponseTime = PerformanceMetrics.AverageResponseTime,
            ReliabilityScore = 1.0 - PerformanceMetrics.ErrorRate
        };
    }

    /// <summary>
    /// Ԥ������ɱ�
    /// </summary>
    public decimal EstimateTaskCost(int estimatedInputTokens, int estimatedOutputTokens)
    {
        return CalculateCost(estimatedInputTokens, estimatedOutputTokens);
    }

    /// <summary>
    /// ����Ƿ��ʺ�ָ������
    /// </summary>
    public bool IsSuitableForTask(List<ModelCapability> requiredCapabilities, int estimatedInputTokens)
    {
        // ����ƥ����
        if (requiredCapabilities.Any(rc => !SupportsCapability(rc)))
            return false;

        // ������������ĳ����Ƿ��㹻
        if (estimatedInputTokens > ContextLength)
            return false;

        // ����������ĳ���
        if (MaxOutputTokens.HasValue && estimatedInputTokens + MaxOutputTokens.Value > ContextLength)
            return false;

        return true;
    }
}

/// <summary>
/// ģ��ժҪ��Ϣ
/// </summary>
public class ModelSummary
{
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ModelGroup { get; set; } = string.Empty;
    public int ContextLength { get; set; }
    public int? MaxOutputTokens { get; set; }
    public List<string> SupportedCapabilities { get; set; } = new();
    public decimal InputPrice { get; set; }
    public decimal OutputPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsLatestVersion { get; set; }
    public bool IsActive { get; set; }
    public double AverageResponseTime { get; set; }
    public double ReliabilityScore { get; set; }
}