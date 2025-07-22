using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Lorn.OpenAgenticAI.Domain.Models.Workflow;

/// <summary>
/// 工作流元数据条目实体
/// 用于存储WorkflowDefinition中的Metadata字典数据
/// </summary>
public class WorkflowMetadataEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 元数据键名
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MetadataKey { get; set; } = string.Empty;

    /// <summary>
    /// 元数据值（JSON序列化的对象）
    /// </summary>
    [Required]
    public string ValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 元数据值的类型信息
    /// </summary>
    [MaxLength(200)]
    public string? ValueType { get; set; }

    /// <summary>
    /// 元数据描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用此元数据
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
    /// 所属的WorkflowDefinition对象ID（外键关联）
    /// </summary>
    public int WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 空构造函数（EF Core需要）
    /// </summary>
    public WorkflowMetadataEntry()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="metadataKey">元数据键名</param>
    /// <param name="value">元数据值对象</param>
    /// <param name="description">描述</param>
    public WorkflowMetadataEntry(string metadataKey, object value, string? description = null)
    {
        MetadataKey = metadataKey ?? throw new ArgumentNullException(nameof(metadataKey));
        SetValue(value);
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置元数据值
    /// </summary>
    /// <param name="value">要设置的值</param>
    public void SetValue(object value)
    {
        if (value == null)
        {
            ValueJson = "null";
            ValueType = "null";
        }
        else
        {
            ValueJson = JsonSerializer.Serialize(value);
            ValueType = value.GetType().FullName;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取元数据值（泛型方法）
    /// </summary>
    /// <typeparam name="T">期望的值类型</typeparam>
    /// <returns>反序列化的值</returns>
    public T? GetValue<T>()
    {
        if (string.IsNullOrEmpty(ValueJson) || ValueJson == "null")
            return default(T);

        try
        {
            return JsonSerializer.Deserialize<T>(ValueJson);
        }
        catch (JsonException)
        {
            return default(T);
        }
    }

    /// <summary>
    /// 获取元数据值（object类型）
    /// </summary>
    /// <returns>反序列化的值</returns>
    public object? GetValue()
    {
        if (string.IsNullOrEmpty(ValueJson) || ValueJson == "null")
            return null;

        try
        {
            // 如果有类型信息，尝试反序列化为原始类型
            if (!string.IsNullOrEmpty(ValueType) && ValueType != "null")
            {
                var type = Type.GetType(ValueType);
                if (type != null)
                {
                    return JsonSerializer.Deserialize(ValueJson, type);
                }
            }

            // 否则反序列化为JsonElement
            return JsonSerializer.Deserialize<JsonElement>(ValueJson);
        }
        catch (JsonException)
        {
            return ValueJson; // 如果反序列化失败，返回原始字符串
        }
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
    /// 启用/禁用此元数据
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
