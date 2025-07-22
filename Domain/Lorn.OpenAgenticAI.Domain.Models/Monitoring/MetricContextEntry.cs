using System;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Monitoring;

/// <summary>
/// 性能指标上下文条目实体
/// </summary>
public class MetricContextEntry : IEntity
{
    public Guid Id => EntryId; // IEntity.Id 实现
    public Guid EntryId { get; private set; }
    public Guid MetricId { get; private set; }
    public string ContextKey { get; private set; } = string.Empty;
    public string ContextValue { get; private set; } = string.Empty;
    public string ValueType { get; private set; } = string.Empty;
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }

    // 导航属性
    public virtual PerformanceMetricsRecord Metric { get; private set; } = null!;

    // EF Core 需要的无参数构造函数
    private MetricContextEntry()
    {
        EntryId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public MetricContextEntry(Guid metricId, string contextKey, object contextValue)
    {
        EntryId = Guid.NewGuid();
        MetricId = metricId;
        ContextKey = !string.IsNullOrWhiteSpace(contextKey)
            ? contextKey
            : throw new ArgumentException("Context key cannot be empty", nameof(contextKey));

        SetValue(contextValue);

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
            ContextValue = string.Empty;
            ValueType = "null";
        }
        else
        {
            ValueType = value.GetType().FullName ?? "object";

            // 基本类型直接转换
            if (value is string str)
            {
                ContextValue = str;
            }
            else if (value is int || value is long || value is float || value is double || value is decimal)
            {
                ContextValue = value.ToString() ?? string.Empty;
            }
            else if (value is bool boolValue)
            {
                ContextValue = boolValue.ToString().ToLowerInvariant();
            }
            else if (value is DateTime dateTime)
            {
                ContextValue = dateTime.ToString("O"); // ISO 8601 格式
            }
            else
            {
                // 复杂对象使用JSON序列化
                ContextValue = JsonSerializer.Serialize(value);
            }
        }

        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取强类型值
    /// </summary>
    public T? GetValue<T>()
    {
        if (string.IsNullOrEmpty(ContextValue) || ValueType == "null")
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
                return (T)(object)ContextValue;
            }
            else if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(ContextValue);
            }
            else if (targetType == typeof(long))
            {
                return (T)(object)long.Parse(ContextValue);
            }
            else if (targetType == typeof(float))
            {
                return (T)(object)float.Parse(ContextValue);
            }
            else if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(ContextValue);
            }
            else if (targetType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(ContextValue);
            }
            else if (targetType == typeof(bool))
            {
                return (T)(object)bool.Parse(ContextValue);
            }
            else if (targetType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(ContextValue);
            }
            else
            {
                // 复杂对象使用JSON反序列化
                return JsonSerializer.Deserialize<T>(ContextValue);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot convert context value '{ContextValue}' to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// 获取object类型的值
    /// </summary>
    public object? GetObjectValue()
    {
        if (string.IsNullOrEmpty(ContextValue) || ValueType == "null")
        {
            return null;
        }

        try
        {
            // 根据存储的类型信息尝试转换
            return ValueType switch
            {
                "System.String" => ContextValue,
                "System.Int32" => int.Parse(ContextValue),
                "System.Int64" => long.Parse(ContextValue),
                "System.Single" => float.Parse(ContextValue),
                "System.Double" => double.Parse(ContextValue),
                "System.Decimal" => decimal.Parse(ContextValue),
                "System.Boolean" => bool.Parse(ContextValue),
                "System.DateTime" => DateTime.Parse(ContextValue),
                _ => JsonSerializer.Deserialize<object>(ContextValue)
            };
        }
        catch
        {
            // 如果转换失败，返回原始字符串
            return ContextValue;
        }
    }

    /// <summary>
    /// 更新上下文值
    /// </summary>
    public void UpdateValue(object value)
    {
        SetValue(value);
    }

    /// <summary>
    /// 验证上下文条目
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ContextKey))
        {
            result.AddError(nameof(ContextKey), "Context key is required");
        }

        if (MetricId == Guid.Empty)
        {
            result.AddError(nameof(MetricId), "Metric ID is required");
        }

        return result;
    }
}
