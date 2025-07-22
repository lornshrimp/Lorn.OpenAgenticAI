using System;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Monitoring;

/// <summary>
/// 性能指标标签条目实体
/// </summary>
public class MetricTagEntry : IEntity
{
    public Guid Id => EntryId; // IEntity.Id 实现
    public Guid EntryId { get; private set; }
    public Guid MetricId { get; private set; }
    public string TagKey { get; private set; } = string.Empty;
    public string TagValue { get; private set; } = string.Empty;
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }

    // 导航属性
    public virtual PerformanceMetricsRecord Metric { get; private set; } = null!;

    // EF Core 需要的无参数构造函数
    private MetricTagEntry()
    {
        EntryId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public MetricTagEntry(Guid metricId, string tagKey, string tagValue)
    {
        EntryId = Guid.NewGuid();
        MetricId = metricId;
        TagKey = !string.IsNullOrWhiteSpace(tagKey)
            ? tagKey
            : throw new ArgumentException("Tag key cannot be empty", nameof(tagKey));
        TagValue = tagValue ?? string.Empty;
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新标签值
    /// </summary>
    public void UpdateValue(string tagValue)
    {
        TagValue = tagValue ?? string.Empty;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证标签条目
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(TagKey))
        {
            result.AddError(nameof(TagKey), "Tag key is required");
        }

        if (MetricId == Guid.Empty)
        {
            result.AddError(nameof(MetricId), "Metric ID is required");
        }

        return result;
    }
}
