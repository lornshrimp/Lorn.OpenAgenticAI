using System;
using System.ComponentModel.DataAnnotations;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 模型参数条目实体 - 用于存储 ModelParameters 中的 AdditionalParameters
/// </summary>
public class ModelParameterEntry
{
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联的配置ID（可能是 ModelUserConfiguration 或其他使用 ModelParameters 的实体）
    /// </summary>
    public Guid ConfigurationId { get; private set; }

    /// <summary>
    /// 参数键
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// 参数值（序列化为 JSON 字符串）
    /// </summary>
    [Required]
    public string ValueJson { get; private set; } = string.Empty;

    /// <summary>
    /// 参数值的类型信息
    /// </summary>
    [MaxLength(200)]
    public string ValueType { get; private set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedTime { get; private set; }

    // EF Core 需要的无参构造函数
    private ModelParameterEntry()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public ModelParameterEntry(
        Guid configurationId,
        string key,
        object value) : this()
    {
        ConfigurationId = configurationId;
        Key = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentException("Key cannot be empty", nameof(key));
        SetValue(value);
    }

    /// <summary>
    /// 设置参数值
    /// </summary>
    public void SetValue(object value)
    {
        if (value == null)
        {
            ValueJson = "null";
            ValueType = "null";
        }
        else
        {
            ValueType = value.GetType().FullName ?? "object";
            ValueJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    public T? GetValue<T>()
    {
        if (ValueJson == "null" || string.IsNullOrEmpty(ValueJson))
            return default(T);

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(ValueJson);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// 获取参数值（动态类型）
    /// </summary>
    public object? GetValue()
    {
        if (ValueJson == "null" || string.IsNullOrEmpty(ValueJson))
            return null;

        // 根据类型信息反序列化
        var type = Type.GetType(ValueType);
        if (type == null)
            return ValueJson; // 如果无法确定类型，返回原始JSON字符串

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize(ValueJson, type);
        }
        catch
        {
            return ValueJson;
        }
    }
}
