using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.Execution;

/// <summary>
/// 资源利用率条目实体
/// 用于存储ExecutionMetrics中的ResourceUtilization字典数据
/// </summary>
public class ResourceUtilizationEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 资源名称/类型
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源利用率（百分比，0.0-1.0）
    /// </summary>
    [Column(TypeName = "decimal(5,4)")]
    public double UtilizationRate { get; set; }

    /// <summary>
    /// 资源单位
    /// </summary>
    [MaxLength(50)]
    public string? Unit { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用此记录
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属的ExecutionMetrics对象ID（外键关联）
    /// </summary>
    public int ExecutionMetricsId { get; set; }

    /// <summary>
    /// 空构造函数（EF Core需要）
    /// </summary>
    public ResourceUtilizationEntry()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="resourceName">资源名称</param>
    /// <param name="utilizationRate">利用率</param>
    /// <param name="unit">单位</param>
    /// <param name="description">描述</param>
    public ResourceUtilizationEntry(string resourceName, double utilizationRate, string? unit = null, string? description = null)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
        UtilizationRate = utilizationRate;
        Unit = unit;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新利用率
    /// </summary>
    /// <param name="newRate">新的利用率</param>
    public void UpdateUtilizationRate(double newRate)
    {
        UtilizationRate = newRate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新单位
    /// </summary>
    /// <param name="newUnit">新的单位</param>
    public void UpdateUnit(string? newUnit)
    {
        Unit = newUnit;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新描述
    /// </summary>
    /// <param name="newDescription">新的描述</param>
    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用/禁用此记录
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
