using System;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Workflow;

/// <summary>
/// 步骤参数条目实体 - 用于存储StepParameters中的Dictionary条目
/// </summary>
public class StepParameterEntry : IEntity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联的步骤参数ID
    /// </summary>
    public Guid StepParametersId { get; private set; }

    /// <summary>
    /// 参数类型：Input, Output, Mapping
    /// </summary>
    public string ParameterType { get; private set; } = string.Empty;

    /// <summary>
    /// 参数键
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// 参数值（JSON序列化）
    /// </summary>
    public string ValueJson { get; private set; } = string.Empty;

    /// <summary>
    /// 值的类型信息
    /// </summary>
    public string ValueType { get; private set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; private set; }

    // EF Core 需要的无参数构造函数
    private StepParameterEntry()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public StepParameterEntry(
        Guid stepParametersId,
        string parameterType,
        string key,
        object value)
    {
        Id = Guid.NewGuid();
        StepParametersId = stepParametersId;
        ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        SetValue(value);
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置参数值
    /// </summary>
    public void SetValue(object value)
    {
        if (value == null)
        {
            ValueJson = string.Empty;
            ValueType = "null";
            return;
        }

        ValueType = value.GetType().FullName ?? "unknown";
        ValueJson = JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    public T? GetValue<T>()
    {
        if (string.IsNullOrEmpty(ValueJson) || ValueType == "null")
            return default(T);

        try
        {
            return JsonSerializer.Deserialize<T>(ValueJson);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// 获取参数值（非泛型）
    /// </summary>
    public object? GetValue()
    {
        if (string.IsNullOrEmpty(ValueJson) || ValueType == "null")
            return null;

        try
        {
            // 根据ValueType信息尝试反序列化为正确的类型
            var type = Type.GetType(ValueType);
            if (type == null)
                return ValueJson; // 如果类型不能识别，返回原始JSON字符串

            return JsonSerializer.Deserialize(ValueJson, type);
        }
        catch
        {
            return ValueJson;
        }
    }

    /// <summary>
    /// 更新参数值
    /// </summary>
    public void UpdateValue(object value)
    {
        SetValue(value);
    }
}
