using System;
using System.Collections.Generic;

namespace Lorn.OpenAgenticAI.Shared.Contracts.DTOs;

/// <summary>
/// 数据完整性检查结果
/// </summary>
public class DataIntegrityCheckResult
{
    /// <summary>
    /// 检查是否通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 检查开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 检查结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 检查耗时
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 检查的项目列表
    /// </summary>
    public List<DataIntegrityCheckItem> CheckItems { get; set; } = new();

    /// <summary>
    /// 错误数量
    /// </summary>
    public int ErrorCount => CheckItems.Count(item => item.Status == CheckItemStatus.Failed);

    /// <summary>
    /// 警告数量
    /// </summary>
    public int WarningCount => CheckItems.Count(item => item.Status == CheckItemStatus.Warning);

    /// <summary>
    /// 通过数量
    /// </summary>
    public int PassedCount => CheckItems.Count(item => item.Status == CheckItemStatus.Passed);

    /// <summary>
    /// 总体统计信息
    /// </summary>
    public DataIntegrityStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 添加检查项
    /// </summary>
    public void AddCheckItem(string name, CheckItemStatus status, string message, object? details = null)
    {
        CheckItems.Add(new DataIntegrityCheckItem
        {
            Name = name,
            Status = status,
            Message = message,
            Details = details,
            CheckedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取失败的检查项
    /// </summary>
    public IEnumerable<DataIntegrityCheckItem> GetFailedItems()
    {
        return CheckItems.Where(item => item.Status == CheckItemStatus.Failed);
    }

    /// <summary>
    /// 获取警告的检查项
    /// </summary>
    public IEnumerable<DataIntegrityCheckItem> GetWarningItems()
    {
        return CheckItems.Where(item => item.Status == CheckItemStatus.Warning);
    }

    /// <summary>
    /// 生成摘要报告
    /// </summary>
    public string GenerateSummaryReport()
    {
        var lines = new List<string>
        {
            $"数据完整性检查报告",
            $"检查时间: {StartTime:yyyy-MM-dd HH:mm:ss} - {EndTime:yyyy-MM-dd HH:mm:ss}",
            $"耗时: {Duration.TotalSeconds:F2} 秒",
            $"总体状态: {(IsValid ? "通过" : "失败")}",
            "",
            $"检查结果统计:",
            $"  通过: {PassedCount}",
            $"  警告: {WarningCount}",
            $"  失败: {ErrorCount}",
            ""
        };

        if (ErrorCount > 0)
        {
            lines.Add("失败项目:");
            foreach (var item in GetFailedItems())
            {
                lines.Add($"  - {item.Name}: {item.Message}");
            }
            lines.Add("");
        }

        if (WarningCount > 0)
        {
            lines.Add("警告项目:");
            foreach (var item in GetWarningItems())
            {
                lines.Add($"  - {item.Name}: {item.Message}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 数据完整性检查项
/// </summary>
public class DataIntegrityCheckItem
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 检查状态
    /// </summary>
    public CheckItemStatus Status { get; set; }

    /// <summary>
    /// 检查消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细信息
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// 检查项状态
/// </summary>
public enum CheckItemStatus
{
    /// <summary>
    /// 通过
    /// </summary>
    Passed,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// 失败
    /// </summary>
    Failed
}

/// <summary>
/// 数据完整性统计信息
/// </summary>
public class DataIntegrityStatistics
{
    /// <summary>
    /// 用户档案总数
    /// </summary>
    public int TotalUserProfiles { get; set; }

    /// <summary>
    /// 活跃用户档案数
    /// </summary>
    public int ActiveUserProfiles { get; set; }

    /// <summary>
    /// 偏好设置总数
    /// </summary>
    public int TotalPreferences { get; set; }

    /// <summary>
    /// 孤立的偏好设置数（没有对应用户档案）
    /// </summary>
    public int OrphanedPreferences { get; set; }

    /// <summary>
    /// 元数据条目总数
    /// </summary>
    public int TotalMetadataEntries { get; set; }

    /// <summary>
    /// 孤立的元数据条目数
    /// </summary>
    public int OrphanedMetadataEntries { get; set; }

    /// <summary>
    /// 重复的机器ID数量
    /// </summary>
    public int DuplicateMachineIds { get; set; }

    /// <summary>
    /// 无效的用户档案数量
    /// </summary>
    public int InvalidUserProfiles { get; set; }

    /// <summary>
    /// 数据库大小（字节）
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastCheckTime { get; set; }
}
