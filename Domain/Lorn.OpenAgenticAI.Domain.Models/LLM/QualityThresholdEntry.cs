using System;
using System.ComponentModel.DataAnnotations;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 质量阈值条目实体 - 用于存储 QualitySettings 中的 CustomThresholds
/// </summary>
public class QualityThresholdEntry
{
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联的配置ID
    /// </summary>
    public Guid ConfigurationId { get; private set; }

    /// <summary>
    /// 阈值名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ThresholdName { get; private set; } = string.Empty;

    /// <summary>
    /// 阈值数值
    /// </summary>
    public double ThresholdValue { get; private set; }

    /// <summary>
    /// 阈值描述
    /// </summary>
    [MaxLength(500)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedTime { get; private set; }

    // EF Core 需要的无参构造函数
    private QualityThresholdEntry()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public QualityThresholdEntry(
        Guid configurationId,
        string thresholdName,
        double thresholdValue,
        string description = "") : this()
    {
        ConfigurationId = configurationId;
        ThresholdName = !string.IsNullOrWhiteSpace(thresholdName)
            ? thresholdName
            : throw new ArgumentException("ThresholdName cannot be empty", nameof(thresholdName));
        ThresholdValue = thresholdValue;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// 更新阈值
    /// </summary>
    public void UpdateThreshold(double newValue, string? newDescription = null)
    {
        ThresholdValue = newValue;
        if (newDescription != null)
        {
            Description = newDescription;
        }
        UpdatedTime = DateTime.UtcNow;
    }
}
