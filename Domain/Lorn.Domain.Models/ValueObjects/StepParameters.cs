using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Step parameters value object
/// </summary>
public class StepParameters : ValueObject
{
    /// <summary>
    /// Gets the input parameters
    /// </summary>
    public Dictionary<string, object> InputParameters { get; }

    /// <summary>
    /// Gets the output parameters
    /// </summary>
    public Dictionary<string, object> OutputParameters { get; }

    /// <summary>
    /// Gets the parameter mappings
    /// </summary>
    public Dictionary<string, string> ParameterMappings { get; }

    /// <summary>
    /// Initializes a new instance of the StepParameters class
    /// </summary>
    /// <param name="inputParameters">The input parameters</param>
    /// <param name="outputParameters">The output parameters</param>
    /// <param name="parameterMappings">The parameter mappings</param>
    public StepParameters(
        Dictionary<string, object>? inputParameters = null,
        Dictionary<string, object>? outputParameters = null,
        Dictionary<string, string>? parameterMappings = null)
    {
        InputParameters = inputParameters ?? new Dictionary<string, object>();
        OutputParameters = outputParameters ?? new Dictionary<string, object>();
        ParameterMappings = parameterMappings ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets a parameter value of the specified type
    /// </summary>
    /// <typeparam name="T">The parameter type</typeparam>
    /// <param name="key">The parameter key</param>
    /// <returns>The parameter value</returns>
    public T GetParameter<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Parameter key cannot be empty", nameof(key));

        if (!InputParameters.ContainsKey(key))
            throw new KeyNotFoundException($"Parameter '{key}' not found");

        var value = InputParameters[key];
        
        if (value is T directValue)
            return directValue;

        // Try to convert the value
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert parameter '{key}' to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Sets a parameter value
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    public StepParameters SetParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Parameter key cannot be empty", nameof(key));

        var newInputParameters = new Dictionary<string, object>(InputParameters)
        {
            [key] = value
        };

        return new StepParameters(newInputParameters, OutputParameters, ParameterMappings);
    }

    /// <summary>
    /// Sets an output parameter value
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    public StepParameters SetOutputParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Parameter key cannot be empty", nameof(key));

        var newOutputParameters = new Dictionary<string, object>(OutputParameters)
        {
            [key] = value
        };

        return new StepParameters(InputParameters, newOutputParameters, ParameterMappings);
    }

    /// <summary>
    /// Adds a parameter mapping
    /// </summary>
    /// <param name="fromKey">The source parameter key</param>
    /// <param name="toKey">The target parameter key</param>
    public StepParameters AddMapping(string fromKey, string toKey)
    {
        if (string.IsNullOrWhiteSpace(fromKey))
            throw new ArgumentException("From key cannot be empty", nameof(fromKey));
        
        if (string.IsNullOrWhiteSpace(toKey))
            throw new ArgumentException("To key cannot be empty", nameof(toKey));

        var newMappings = new Dictionary<string, string>(ParameterMappings)
        {
            [fromKey] = toKey
        };

        return new StepParameters(InputParameters, OutputParameters, newMappings);
    }

    /// <summary>
    /// Validates the parameters
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        // Validate input parameters
        foreach (var param in InputParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Key))
            {
                result.AddError("InputParameters", "Parameter key cannot be empty");
            }
        }

        // Validate output parameters
        foreach (var param in OutputParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Key))
            {
                result.AddError("OutputParameters", "Parameter key cannot be empty");
            }
        }

        // Validate parameter mappings
        foreach (var mapping in ParameterMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key))
            {
                result.AddError("ParameterMappings", "Mapping source key cannot be empty");
            }
            
            if (string.IsNullOrWhiteSpace(mapping.Value))
            {
                result.AddError("ParameterMappings", "Mapping target key cannot be empty");
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a parameter exists
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <returns>True if the parameter exists, false otherwise</returns>
    public bool HasParameter(string key)
    {
        return InputParameters.ContainsKey(key);
    }

    /// <summary>
    /// Checks if an output parameter exists
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <returns>True if the output parameter exists, false otherwise</returns>
    public bool HasOutputParameter(string key)
    {
        return OutputParameters.ContainsKey(key);
    }

    /// <summary>
    /// Gets all parameter keys
    /// </summary>
    /// <returns>The parameter keys</returns>
    public IEnumerable<string> GetParameterKeys()
    {
        return InputParameters.Keys;
    }

    /// <summary>
    /// Gets all output parameter keys
    /// </summary>
    /// <returns>The output parameter keys</returns>
    public IEnumerable<string> GetOutputParameterKeys()
    {
        return OutputParameters.Keys;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        foreach (var param in InputParameters.OrderBy(x => x.Key))
        {
            yield return param.Key;
            yield return param.Value;
        }
        
        foreach (var param in OutputParameters.OrderBy(x => x.Key))
        {
            yield return param.Key;
            yield return param.Value;
        }
        
        foreach (var mapping in ParameterMappings.OrderBy(x => x.Key))
        {
            yield return mapping.Key;
            yield return mapping.Value;
        }
    }
}