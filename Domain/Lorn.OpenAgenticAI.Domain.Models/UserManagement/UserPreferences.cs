using System;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户偏好设置实体
/// </summary>
public class UserPreferences
{
    public Guid PreferenceId { get; private set; }
    public Guid UserId { get; private set; }
    public string PreferenceCategory { get; set; } = string.Empty;
    public string PreferenceKey { get; set; } = string.Empty;
    public string PreferenceValue { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public DateTime LastUpdatedTime { get; private set; }
    public bool IsSystemDefault { get; private set; }
    public string Description { get; set; } = string.Empty;

    // 导航属性
    public virtual UserProfile User { get; set; } = null!;

    // 私有构造函数用于EF Core
    private UserPreferences() 
    {
        PreferenceId = Guid.NewGuid();
        LastUpdatedTime = DateTime.UtcNow;
    }

    public UserPreferences(
        Guid userId,
        string preferenceCategory,
        string preferenceKey,
        string preferenceValue,
        string valueType,
        bool isSystemDefault = false,
        string? description = null)
    {
        PreferenceId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        PreferenceCategory = !string.IsNullOrWhiteSpace(preferenceCategory) ? preferenceCategory : throw new ArgumentException("PreferenceCategory cannot be empty", nameof(preferenceCategory));
        PreferenceKey = !string.IsNullOrWhiteSpace(preferenceKey) ? preferenceKey : throw new ArgumentException("PreferenceKey cannot be empty", nameof(preferenceKey));
        PreferenceValue = preferenceValue ?? string.Empty;
        ValueType = !string.IsNullOrWhiteSpace(valueType) ? valueType : "String";
        LastUpdatedTime = DateTime.UtcNow;
        IsSystemDefault = isSystemDefault;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// 获取类型化的值
    /// </summary>
    public T? GetTypedValue<T>()
    {
        if (string.IsNullOrEmpty(PreferenceValue))
            return default(T);

        try
        {
            return ValueType switch
            {
                "Boolean" => (T)(object)bool.Parse(PreferenceValue),
                "Integer" => (T)(object)int.Parse(PreferenceValue),
                "Double" => (T)(object)double.Parse(PreferenceValue),
                "DateTime" => (T)(object)DateTime.Parse(PreferenceValue),
                "JSON" => System.Text.Json.JsonSerializer.Deserialize<T>(PreferenceValue),
                _ => (T)(object)PreferenceValue
            };
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// 设置类型化的值
    /// </summary>
    public void SetTypedValue<T>(T? value)
    {
        if (value == null)
        {
            PreferenceValue = string.Empty;
            return;
        }

        PreferenceValue = value switch
        {
            bool b => b.ToString(),
            int i => i.ToString(),
            double d => d.ToString(),
            DateTime dt => dt.ToString("O"), // ISO 8601 format
            string s => s,
            _ => System.Text.Json.JsonSerializer.Serialize(value)
        };

        ValueType = value switch
        {
            bool => "Boolean",
            int => "Integer",
            double => "Double",
            DateTime => "DateTime",
            string => "String",
            _ => "JSON"
        };

        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新偏好值
    /// </summary>
    public void UpdateValue(string? value, string? valueType = null)
    {
        PreferenceValue = value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(valueType))
        {
            ValueType = valueType;
        }
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新描述
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }
}