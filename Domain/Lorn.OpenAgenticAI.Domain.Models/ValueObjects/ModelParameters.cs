using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 模型参数值对象
/// </summary>
[ValueObject]
public class ModelParameters : ValueObject
{
    public double Temperature { get; private set; }
    public double TopP { get; private set; }
    public int? TopK { get; private set; }
    public int? MaxTokens { get; private set; }
    public double PresencePenalty { get; private set; }
    public double FrequencyPenalty { get; private set; }
    public List<string> StopSequences { get; private set; } = new();

    // 注意：AdditionalParameters 不再直接存储在这里
    // 它将通过 ModelParameterEntry 实体在数据库中单独存储
    // 在运行时可以通过 GetAdditionalParameters/SetAdditionalParameters 方法访问

    public ModelParameters(
        double temperature = 0.7,
        double topP = 1.0,
        int? topK = null,
        int? maxTokens = null,
        double presencePenalty = 0.0,
        double frequencyPenalty = 0.0,
        List<string>? stopSequences = null)
    {
        Temperature = Math.Max(0.0, Math.Min(2.0, temperature));
        TopP = Math.Max(0.0, Math.Min(1.0, topP));
        TopK = topK > 0 ? topK : null;
        MaxTokens = maxTokens > 0 ? maxTokens : null;
        PresencePenalty = Math.Max(-2.0, Math.Min(2.0, presencePenalty));
        FrequencyPenalty = Math.Max(-2.0, Math.Min(2.0, frequencyPenalty));
        StopSequences = stopSequences ?? new List<string>();
    }

    /// <summary>
    /// 验证参数
    /// </summary>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        if (Temperature < 0 || Temperature > 2)
        {
            result.AddError("Temperature", "Temperature must be between 0 and 2");
        }

        if (TopP < 0 || TopP > 1)
        {
            result.AddError("TopP", "TopP must be between 0 and 1");
        }

        if (TopK.HasValue && TopK.Value <= 0)
        {
            result.AddError("TopK", "TopK must be positive");
        }

        if (MaxTokens.HasValue && MaxTokens.Value <= 0)
        {
            result.AddError("MaxTokens", "MaxTokens must be positive");
        }

        if (PresencePenalty < -2 || PresencePenalty > 2)
        {
            result.AddError("PresencePenalty", "PresencePenalty must be between -2 and 2");
        }

        if (FrequencyPenalty < -2 || FrequencyPenalty > 2)
        {
            result.AddError("FrequencyPenalty", "FrequencyPenalty must be between -2 and 2");
        }

        return result;
    }

    /// <summary>
    /// 合并参数（覆盖的参数将替换现有参数）
    /// 注意：由于 AdditionalParameters 现在存储在单独的实体中，
    /// 此方法只合并基本参数，附加参数需要在应用层处理
    /// </summary>
    public ModelParameters MergeWith(ModelParameters? overrides)
    {
        if (overrides == null)
            return this;

        return new ModelParameters(
            overrides.Temperature != 0.7 ? overrides.Temperature : Temperature,
            overrides.TopP != 1.0 ? overrides.TopP : TopP,
            overrides.TopK ?? TopK,
            overrides.MaxTokens ?? MaxTokens,
            overrides.PresencePenalty != 0.0 ? overrides.PresencePenalty : PresencePenalty,
            overrides.FrequencyPenalty != 0.0 ? overrides.FrequencyPenalty : FrequencyPenalty,
            overrides.StopSequences.Any() ? overrides.StopSequences : StopSequences
        );
    }

    /// <summary>
    /// 创建保守参数（更稳定的输出）
    /// </summary>
    public static ModelParameters CreateConservative()
    {
        return new ModelParameters(
            temperature: 0.3,
            topP: 0.8,
            presencePenalty: 0.1,
            frequencyPenalty: 0.1
        );
    }

    /// <summary>
    /// 创建创造性参数（更有创意的输出）
    /// </summary>
    public static ModelParameters CreateCreative()
    {
        return new ModelParameters(
            temperature: 1.2,
            topP: 0.95,
            presencePenalty: 0.6,
            frequencyPenalty: 0.6
        );
    }

    /// <summary>
    /// 创建平衡参数（默认设置）
    /// </summary>
    public static ModelParameters CreateBalanced()
    {
        return new ModelParameters();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Temperature;
        yield return TopP;
        yield return TopK ?? (object)"null";
        yield return MaxTokens ?? (object)"null";
        yield return PresencePenalty;
        yield return FrequencyPenalty;

        foreach (var seq in StopSequences.OrderBy(s => s))
        {
            yield return seq;
        }

        // 注意：AdditionalParameters 现在不包含在值对象比较中
        // 因为它们存储在单独的实体中，需要在应用层进行比较
    }
}

/// <summary>
/// 质量设置值对象
/// </summary>
[ValueObject]
public class QualitySettings : ValueObject
{
    public double ResponseQualityThreshold { get; private set; }
    public int LatencyThresholdMs { get; private set; }
    public double ErrorRateThreshold { get; private set; }
    public bool EnableQualityMonitoring { get; private set; }

    // 注意：CustomThresholds 不再直接存储在这里
    // 它将通过 QualityThresholdEntry 实体在数据库中单独存储

    public QualitySettings(
        double responseQualityThreshold = 0.8,
        int latencyThresholdMs = 5000,
        double errorRateThreshold = 0.05,
        bool enableQualityMonitoring = true)
    {
        ResponseQualityThreshold = Math.Max(0.0, Math.Min(1.0, responseQualityThreshold));
        LatencyThresholdMs = Math.Max(100, latencyThresholdMs);
        ErrorRateThreshold = Math.Max(0.0, Math.Min(1.0, errorRateThreshold));
        EnableQualityMonitoring = enableQualityMonitoring;
    }

    /// <summary>
    /// 检查是否符合质量标准
    /// 注意：自定义阈值检查需要在应用层通过 QualityThresholdEntry 实体进行
    /// </summary>
    public bool IsWithinQualityStandards(QualityMetrics metrics)
    {
        if (!EnableQualityMonitoring)
            return true;

        if (metrics.ResponseQuality < ResponseQualityThreshold)
            return false;

        if (metrics.LatencyMs > LatencyThresholdMs)
            return false;

        if (metrics.ErrorRate > ErrorRateThreshold)
            return false;

        // 自定义阈值检查现在需要在应用层处理
        return true;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ResponseQualityThreshold;
        yield return LatencyThresholdMs;
        yield return ErrorRateThreshold;
        yield return EnableQualityMonitoring;

        // 注意：CustomThresholds 现在不包含在值对象比较中
        // 因为它们存储在单独的实体中，需要在应用层进行比较
    }
}

/// <summary>
/// 质量指标
/// </summary>
public class QualityMetrics
{
    public double ResponseQuality { get; set; }
    public int LatencyMs { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
}