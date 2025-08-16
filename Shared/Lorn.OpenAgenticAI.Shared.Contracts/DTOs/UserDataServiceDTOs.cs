using System;
using System.Collections.Generic;

namespace Lorn.OpenAgenticAI.Shared.Contracts.DTOs;

/// <summary>
/// 偏好设置分类DTO
/// </summary>
public class PreferenceCategoryDto
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 偏好设置项
    /// </summary>
    public Dictionary<string, object?> Preferences { get; set; } = new();

    /// <summary>
    /// 设置项数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 分类描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 偏好设置统计信息DTO
/// </summary>
public class PreferenceStatisticsDto
{
    /// <summary>
    /// 总设置项数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 自定义设置项数量
    /// </summary>
    public int CustomCount { get; set; }

    /// <summary>
    /// 系统默认设置项数量
    /// </summary>
    public int SystemDefaultCount { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 各分类的设置项数量
    /// </summary>
    public Dictionary<string, int> CountByCategory { get; set; } = new();
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = new List<string>(errors)
    };

    /// <summary>
    /// 添加错误
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// 添加警告
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
