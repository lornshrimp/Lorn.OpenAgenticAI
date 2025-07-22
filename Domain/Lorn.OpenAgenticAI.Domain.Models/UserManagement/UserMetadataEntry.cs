using System;
using System.ComponentModel.DataAnnotations;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户元数据条目实体 - 用于存储 UserProfile 中的 Metadata
/// </summary>
public class UserMetadataEntry
{
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 元数据键
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// 元数据值（序列化为 JSON 字符串）
    /// </summary>
    [Required]
    public string ValueJson { get; private set; } = string.Empty;

    /// <summary>
    /// 元数据值的类型信息
    /// </summary>
    [MaxLength(200)]
    public string ValueType { get; private set; } = string.Empty;

    /// <summary>
    /// 元数据分类（可选）
    /// </summary>
    [MaxLength(50)]
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedTime { get; private set; }

    // 导航属性
    public virtual UserProfile User { get; private set; } = null!;

    // EF Core 需要的无参构造函数
    private UserMetadataEntry()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public UserMetadataEntry(
        Guid userId,
        string key,
        object value,
        string category = "") : this()
    {
        UserId = userId;
        Key = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentException("Key cannot be empty", nameof(key));
        Category = category ?? string.Empty;
        SetValue(value);
    }

    /// <summary>
    /// 设置元数据值
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
    /// 获取元数据值
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
    /// 获取元数据值（动态类型）
    /// </summary>
    public object? GetValue()
    {
        if (ValueJson == "null" || string.IsNullOrEmpty(ValueJson))
            return null;

        var type = Type.GetType(ValueType);
        if (type == null)
            return ValueJson;

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
