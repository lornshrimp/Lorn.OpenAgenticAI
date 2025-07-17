using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 模型实体
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

    // 导航属性
    public virtual ModelProvider Provider { get; private set; } = null!;
    public virtual ICollection<ModelUserConfiguration> UserConfigurations { get; private set; } = new List<ModelUserConfiguration>();

    // 私有构造函数供EF Core
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
    /// 计算使用成本
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        return PricingInfo.CalculateCost(inputTokens, outputTokens);
    }

    /// <summary>
    /// 检查是否支持指定能力
    /// </summary>
    public bool SupportsCapability(ModelCapability capability)
    {
        return SupportedCapabilities.Contains(capability);
    }

    /// <summary>
    /// 更新性能指标
    /// </summary>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>
    /// 添加支持的能力
    /// </summary>
    public void AddCapability(ModelCapability capability)
    {
        if (capability != null && !SupportedCapabilities.Contains(capability))
        {
            SupportedCapabilities.Add(capability);
        }
    }

    /// <summary>
    /// 移除支持的能力
    /// </summary>
    public void RemoveCapability(ModelCapability capability)
    {
        SupportedCapabilities.Remove(capability);
    }

    /// <summary>
    /// 更新模型信息
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
    /// 检查模型是否活跃
    /// </summary>
    public bool IsActive()
    {
        // 模型活跃的条件：是最新版本且支持至少一种能力
        return IsLatestVersion && SupportedCapabilities.Any();
    }

    /// <summary>
    /// 获取模型摘要信息
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
    /// 预估任务成本
    /// </summary>
    public decimal EstimateTaskCost(int estimatedInputTokens, int estimatedOutputTokens)
    {
        return CalculateCost(estimatedInputTokens, estimatedOutputTokens);
    }

    /// <summary>
    /// 检查是否适合指定任务
    /// </summary>
    public bool IsSuitableForTask(List<ModelCapability> requiredCapabilities, int estimatedInputTokens)
    {
        // 能力匹配检查
        if (requiredCapabilities.Any(rc => !SupportsCapability(rc)))
            return false;

        // 检查输入上下文长度是否足够
        if (estimatedInputTokens > ContextLength)
            return false;

        // 检查总上下文长度
        if (MaxOutputTokens.HasValue && estimatedInputTokens + MaxOutputTokens.Value > ContextLength)
            return false;

        return true;
    }
}

/// <summary>
/// 模型摘要信息
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