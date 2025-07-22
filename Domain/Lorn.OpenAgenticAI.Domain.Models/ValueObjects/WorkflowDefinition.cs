using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Workflow;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 工作流定义值对象
/// </summary>
public class WorkflowDefinition : ValueObject
{
    public string WorkflowFormat { get; private set; } = string.Empty;
    public string SerializedDefinition { get; private set; } = string.Empty;

    /// <summary>
    /// 工作流元数据条目（导航属性）
    /// </summary>
    [NotMapped]
    public virtual ICollection<WorkflowMetadataEntry> MetadataEntries { get; set; } = new List<WorkflowMetadataEntry>();

    public List<WorkflowVariable> Variables { get; private set; } = new();

    public WorkflowDefinition(
        string workflowFormat,
        string serializedDefinition,
        Dictionary<string, object>? metadata = null,
        List<WorkflowVariable>? variables = null)
    {
        WorkflowFormat = !string.IsNullOrWhiteSpace(workflowFormat) ? workflowFormat : throw new ArgumentException("WorkflowFormat cannot be empty", nameof(workflowFormat));
        SerializedDefinition = !string.IsNullOrWhiteSpace(serializedDefinition) ? serializedDefinition : throw new ArgumentException("SerializedDefinition cannot be empty", nameof(serializedDefinition));
        MetadataEntries = new List<WorkflowMetadataEntry>();
        Variables = variables ?? new List<WorkflowVariable>();

        // 如果提供了元数据字典，转换为实体对象
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                MetadataEntries.Add(new WorkflowMetadataEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 获取元数据字典（向后兼容）
    /// </summary>
    public Dictionary<string, object> GetMetadata()
    {
        return MetadataEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.MetadataKey, e => e.GetValue() ?? new object());
    }

    /// <summary>
    /// 设置元数据（向后兼容）
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        var existing = MetadataEntries.FirstOrDefault(e => e.MetadataKey == key);
        if (existing != null)
        {
            existing.SetValue(value);
            existing.SetEnabled(true);
        }
        else
        {
            MetadataEntries.Add(new WorkflowMetadataEntry(key, value));
        }
    }

    /// <summary>
    /// 反序列化为指定类型
    /// </summary>
    public T? Deserialize<T>()
    {
        try
        {
            return WorkflowFormat.ToLower() switch
            {
                "json" => System.Text.Json.JsonSerializer.Deserialize<T>(SerializedDefinition),
                "xml" => throw new NotImplementedException("XML deserialization not implemented"),
                "yaml" => throw new NotImplementedException("YAML deserialization not implemented"),
                _ => throw new NotSupportedException($"Workflow format '{WorkflowFormat}' is not supported")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize workflow definition: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 序列化对象
    /// </summary>
    public void Serialize(object definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        try
        {
            SerializedDefinition = WorkflowFormat.ToLower() switch
            {
                "json" => System.Text.Json.JsonSerializer.Serialize(definition, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                "xml" => throw new NotImplementedException("XML serialization not implemented"),
                "yaml" => throw new NotImplementedException("YAML serialization not implemented"),
                _ => throw new NotSupportedException($"Workflow format '{WorkflowFormat}' is not supported")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize workflow definition: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 验证工作流定义
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(WorkflowFormat))
        {
            result.AddError("WorkflowFormat", "Workflow format is required");
        }

        if (string.IsNullOrWhiteSpace(SerializedDefinition))
        {
            result.AddError("SerializedDefinition", "Serialized definition is required");
        }

        // 尝试反序列化以验证格式
        try
        {
            if (!string.IsNullOrWhiteSpace(SerializedDefinition))
            {
                Deserialize<object>();
            }
        }
        catch (Exception ex)
        {
            result.AddError("SerializedDefinition", $"Invalid workflow definition format: {ex.Message}");
        }

        return result;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return WorkflowFormat;
        yield return SerializedDefinition;

        foreach (var entry in MetadataEntries.Where(e => e.IsEnabled).OrderBy(e => e.MetadataKey))
        {
            yield return entry.MetadataKey;
            yield return entry.GetValue() ?? "null";
        }

        foreach (var variable in Variables)
        {
            yield return variable;
        }
    }
}

/// <summary>
/// 工作流变量
/// </summary>
public class WorkflowVariable : ValueObject
{
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public object? DefaultValue { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Description { get; private set; }

    public WorkflowVariable(string name, string type, object? defaultValue = null, bool isRequired = false, string? description = null)
    {
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty", nameof(name));
        Type = !string.IsNullOrWhiteSpace(type) ? type : "String";
        DefaultValue = defaultValue;
        IsRequired = isRequired;
        Description = description;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Name;
        yield return Type;
        yield return DefaultValue ?? "null";
        yield return IsRequired;
        yield return Description ?? "null";
    }
}