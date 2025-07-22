using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.Execution;

/// <summary>
/// 步骤执行时间条目实体
/// 用于存储ExecutionMetrics中的StepExecutionTimes字典数据
/// </summary>
public class StepExecutionTimeEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 步骤名称/标识符
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 步骤描述
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
    public StepExecutionTimeEntry()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="stepName">步骤名称</param>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    /// <param name="description">描述</param>
    public StepExecutionTimeEntry(string stepName, long executionTimeMs, string? description = null)
    {
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        ExecutionTimeMs = executionTimeMs;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新执行时间
    /// </summary>
    /// <param name="newExecutionTime">新的执行时间</param>
    public void UpdateExecutionTime(long newExecutionTime)
    {
        ExecutionTimeMs = newExecutionTime;
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
