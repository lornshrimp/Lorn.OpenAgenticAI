using System;
using System.Collections.Generic;

namespace Lorn.OpenAgenticAI.Shared.Contracts.DTOs;

/// <summary>
/// 用户偏好设置配置文件DTO
/// 用于传输用户的完整偏好设置信息
/// </summary>
public class UserPreferenceProfileDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 偏好设置分组
    /// Key: 偏好类别名称
    /// Value: 该类别下的偏好设置列表
    /// </summary>
    public Dictionary<string, List<UserPreferenceItemDto>> PreferenceCategories { get; set; } = new();

    /// <summary>
    /// 配置文件版本
    /// </summary>
    public int ProfileVersion { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 偏好设置总数
    /// </summary>
    public int TotalPreferences => PreferenceCategories.Values.Sum(list => list.Count);

    /// <summary>
    /// 获取指定类别的偏好设置
    /// </summary>
    public List<UserPreferenceItemDto> GetCategoryPreferences(string category)
    {
        return PreferenceCategories.TryGetValue(category, out var preferences)
            ? preferences
            : new List<UserPreferenceItemDto>();
    }

    /// <summary>
    /// 添加偏好设置项
    /// </summary>
    public void AddPreference(string category, UserPreferenceItemDto preference)
    {
        if (!PreferenceCategories.ContainsKey(category))
        {
            PreferenceCategories[category] = new List<UserPreferenceItemDto>();
        }
        PreferenceCategories[category].Add(preference);
    }
}

/// <summary>
/// 用户偏好设置项DTO
/// </summary>
public class UserPreferenceItemDto
{
    /// <summary>
    /// 偏好设置ID
    /// </summary>
    public Guid PreferenceId { get; set; }

    /// <summary>
    /// 偏好类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 偏好键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 偏好值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 值类型
    /// </summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否为默认值
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 获取强类型值
    /// </summary>
    public T? GetValue<T>()
    {
        try
        {
            if (string.IsNullOrEmpty(Value))
                return default;

            if (typeof(T) == typeof(string))
                return (T)(object)Value;

            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
