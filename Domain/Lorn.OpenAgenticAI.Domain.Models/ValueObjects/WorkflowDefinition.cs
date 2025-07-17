using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// ����������ֵ����
/// </summary>
public class WorkflowDefinition : ValueObject
{
    public string WorkflowFormat { get; private set; } = string.Empty;
    public string SerializedDefinition { get; private set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public List<WorkflowVariable> Variables { get; private set; } = new();

    public WorkflowDefinition(
        string workflowFormat,
        string serializedDefinition,
        Dictionary<string, object>? metadata = null,
        List<WorkflowVariable>? variables = null)
    {
        WorkflowFormat = !string.IsNullOrWhiteSpace(workflowFormat) ? workflowFormat : throw new ArgumentException("WorkflowFormat cannot be empty", nameof(workflowFormat));
        SerializedDefinition = !string.IsNullOrWhiteSpace(serializedDefinition) ? serializedDefinition : throw new ArgumentException("SerializedDefinition cannot be empty", nameof(serializedDefinition));
        Metadata = metadata ?? new Dictionary<string, object>();
        Variables = variables ?? new List<WorkflowVariable>();
    }

    /// <summary>
    /// �����л�Ϊָ������
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
    /// ���л�����
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
    /// ��֤����������
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

        // ���Է����л�����֤��ʽ
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
        
        foreach (var kvp in Metadata)
        {
            yield return kvp.Key;
            yield return kvp.Value ?? "null";
        }
        
        foreach (var variable in Variables)
        {
            yield return variable;
        }
    }
}

/// <summary>
/// ����������
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