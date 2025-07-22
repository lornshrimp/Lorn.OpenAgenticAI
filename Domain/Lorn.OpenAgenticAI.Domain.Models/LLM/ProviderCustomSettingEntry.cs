using System;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 提供商用户配置自定义设置条目实体
/// </summary>
public class ProviderCustomSettingEntry : IEntity
{
    public Guid Id => EntryId; // IEntity.Id 实现
    public Guid EntryId { get; private set; }
    public Guid ConfigurationId { get; private set; }
    public string SettingKey { get; private set; } = string.Empty;
    public string SettingValue { get; private set; } = string.Empty;
    public string ValueType { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }

    // 导航属性
    public virtual ProviderUserConfiguration Configuration { get; private set; } = null!;

    // EF Core 需要的无参数构造函数
    private ProviderCustomSettingEntry()
    {
        EntryId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
        IsEnabled = true;
    }

    public ProviderCustomSettingEntry(Guid configurationId, string settingKey, object settingValue)
    {
        EntryId = Guid.NewGuid();
        ConfigurationId = configurationId;
        SettingKey = !string.IsNullOrWhiteSpace(settingKey)
            ? settingKey
            : throw new ArgumentException("Setting key cannot be empty", nameof(settingKey));

        SetValue(settingValue);

        IsEnabled = true;
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置值（支持复杂对象的JSON序列化）
    /// </summary>
    public void SetValue(object value)
    {
        if (value == null)
        {
            SettingValue = string.Empty;
            ValueType = "null";
        }
        else
        {
            ValueType = value.GetType().FullName ?? "object";

            // 基本类型直接转换
            if (value is string str)
            {
                SettingValue = str;
            }
            else if (value is int || value is long || value is float || value is double || value is decimal)
            {
                SettingValue = value.ToString() ?? string.Empty;
            }
            else if (value is bool boolValue)
            {
                SettingValue = boolValue.ToString().ToLowerInvariant();
            }
            else if (value is DateTime dateTime)
            {
                SettingValue = dateTime.ToString("O"); // ISO 8601 格式
            }
            else
            {
                // 复杂对象使用JSON序列化
                SettingValue = JsonSerializer.Serialize(value);
            }
        }

        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取强类型值
    /// </summary>
    public T? GetValue<T>()
    {
        if (string.IsNullOrEmpty(SettingValue) || ValueType == "null")
        {
            return default;
        }

        var targetType = typeof(T);

        // 处理可空类型
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            targetType = Nullable.GetUnderlyingType(targetType)!;
        }

        try
        {
            // 基本类型转换
            if (targetType == typeof(string))
            {
                return (T)(object)SettingValue;
            }
            else if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(SettingValue);
            }
            else if (targetType == typeof(long))
            {
                return (T)(object)long.Parse(SettingValue);
            }
            else if (targetType == typeof(float))
            {
                return (T)(object)float.Parse(SettingValue);
            }
            else if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(SettingValue);
            }
            else if (targetType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(SettingValue);
            }
            else if (targetType == typeof(bool))
            {
                return (T)(object)bool.Parse(SettingValue);
            }
            else if (targetType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(SettingValue);
            }
            else
            {
                // 复杂对象使用JSON反序列化
                return JsonSerializer.Deserialize<T>(SettingValue);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot convert setting value '{SettingValue}' to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// 获取object类型的值
    /// </summary>
    public object? GetObjectValue()
    {
        if (string.IsNullOrEmpty(SettingValue) || ValueType == "null")
        {
            return null;
        }

        try
        {
            // 根据存储的类型信息尝试转换
            return ValueType switch
            {
                "System.String" => SettingValue,
                "System.Int32" => int.Parse(SettingValue),
                "System.Int64" => long.Parse(SettingValue),
                "System.Single" => float.Parse(SettingValue),
                "System.Double" => double.Parse(SettingValue),
                "System.Decimal" => decimal.Parse(SettingValue),
                "System.Boolean" => bool.Parse(SettingValue),
                "System.DateTime" => DateTime.Parse(SettingValue),
                _ => JsonSerializer.Deserialize<object>(SettingValue)
            };
        }
        catch
        {
            // 如果转换失败，返回原始字符串
            return SettingValue;
        }
    }

    /// <summary>
    /// 更新设置值
    /// </summary>
    public void UpdateValue(object value)
    {
        SetValue(value);
    }

    /// <summary>
    /// 启用/禁用设置
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (IsEnabled != enabled)
        {
            IsEnabled = enabled;
            UpdatedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 验证设置条目
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(SettingKey))
        {
            result.AddError(nameof(SettingKey), "Setting key is required");
        }

        if (ConfigurationId == Guid.Empty)
        {
            result.AddError(nameof(ConfigurationId), "Configuration ID is required");
        }

        return result;
    }
}
