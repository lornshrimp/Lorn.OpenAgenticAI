using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Workflow definition value object
/// </summary>
public class WorkflowDefinition : ValueObject
{
    /// <summary>
    /// Gets the workflow format (JSON, YAML, XML, etc.)
    /// </summary>
    public string WorkflowFormat { get; }

    /// <summary>
    /// Gets the serialized workflow definition
    /// </summary>
    public string SerializedDefinition { get; }

    /// <summary>
    /// Gets the workflow metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the workflow variables
    /// </summary>
    public List<WorkflowVariable> Variables { get; }

    /// <summary>
    /// Initializes a new instance of the WorkflowDefinition class
    /// </summary>
    /// <param name="workflowFormat">The workflow format</param>
    /// <param name="serializedDefinition">The serialized definition</param>
    /// <param name="metadata">The metadata</param>
    /// <param name="variables">The variables</param>
    public WorkflowDefinition(
        string workflowFormat,
        string serializedDefinition,
        Dictionary<string, object>? metadata = null,
        List<WorkflowVariable>? variables = null)
    {
        WorkflowFormat = workflowFormat ?? throw new ArgumentNullException(nameof(workflowFormat));
        SerializedDefinition = serializedDefinition ?? throw new ArgumentNullException(nameof(serializedDefinition));
        Metadata = metadata ?? new Dictionary<string, object>();
        Variables = variables ?? new List<WorkflowVariable>();
    }

    /// <summary>
    /// Deserializes the workflow definition to the specified type
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <returns>The deserialized object</returns>
    public T Deserialize<T>()
    {
        return WorkflowFormat.ToLower() switch
        {
            "json" => System.Text.Json.JsonSerializer.Deserialize<T>(SerializedDefinition)!,
            "yaml" => throw new NotSupportedException("YAML deserialization not implemented yet"),
            "xml" => throw new NotSupportedException("XML deserialization not implemented yet"),
            _ => throw new NotSupportedException($"Workflow format '{WorkflowFormat}' is not supported")
        };
    }

    /// <summary>
    /// Serializes an object to the workflow definition format
    /// </summary>
    /// <param name="definition">The object to serialize</param>
    public static WorkflowDefinition Serialize(object definition, string format = "json")
    {
        var serialized = format.ToLower() switch
        {
            "json" => System.Text.Json.JsonSerializer.Serialize(definition),
            "yaml" => throw new NotSupportedException("YAML serialization not implemented yet"),
            "xml" => throw new NotSupportedException("XML serialization not implemented yet"),
            _ => throw new NotSupportedException($"Workflow format '{format}' is not supported")
        };

        return new WorkflowDefinition(format, serialized);
    }

    /// <summary>
    /// Validates the workflow definition
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(WorkflowFormat))
        {
            result.AddError("WorkflowFormat", "Workflow format cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(SerializedDefinition))
        {
            result.AddError("SerializedDefinition", "Serialized definition cannot be empty");
        }

        // Try to parse the definition to ensure it's valid
        try
        {
            switch (WorkflowFormat.ToLower())
            {
                case "json":
                    System.Text.Json.JsonDocument.Parse(SerializedDefinition);
                    break;
                default:
                    break; // Other formats validation can be added later
            }
        }
        catch (Exception ex)
        {
            result.AddError("SerializedDefinition", $"Invalid workflow definition format: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return WorkflowFormat;
        yield return SerializedDefinition;
        
        foreach (var metadata in Metadata.OrderBy(x => x.Key))
        {
            yield return metadata.Key;
            yield return metadata.Value;
        }
        
        foreach (var variable in Variables.OrderBy(x => x.Name))
        {
            yield return variable;
        }
    }
}

/// <summary>
/// Workflow variable definition
/// </summary>
public class WorkflowVariable : ValueObject
{
    /// <summary>
    /// Gets the variable name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the variable type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the default value
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Gets whether the variable is required
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the variable description
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new instance of the WorkflowVariable class
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <param name="type">The variable type</param>
    /// <param name="defaultValue">The default value</param>
    /// <param name="isRequired">Whether the variable is required</param>
    /// <param name="description">The variable description</param>
    public WorkflowVariable(
        string name,
        string type,
        object? defaultValue = null,
        bool isRequired = false,
        string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
        IsRequired = isRequired;
        Description = description;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Name;
        yield return Type;
        yield return DefaultValue ?? "";
        yield return IsRequired;
        yield return Description ?? "";
    }
}

/// <summary>
/// Validation result class
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    /// <summary>
    /// Gets whether the validation is successful
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="errorMessage">The error message</param>
    public void AddError(string propertyName, string errorMessage)
    {
        _errors.Add(new ValidationError(propertyName, errorMessage));
    }

    /// <summary>
    /// Gets the error summary
    /// </summary>
    /// <returns>The error summary</returns>
    public string GetErrorSummary()
    {
        return string.Join("; ", _errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

/// <summary>
/// Validation error class
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets the property name
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationError class
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="errorMessage">The error message</param>
    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }
}