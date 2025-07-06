using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Capabilities;

/// <summary>
/// Agent action definition entity
/// </summary>
public class AgentActionDefinition : BaseEntity
{
    /// <summary>
    /// Gets the action identifier
    /// </summary>
    public Guid ActionId { get; private set; }

    /// <summary>
    /// Gets the agent identifier
    /// </summary>
    public string AgentId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the action name
    /// </summary>
    public string ActionName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the action description
    /// </summary>
    public string ActionDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the input parameters schema
    /// </summary>
    public JsonSchema InputParameters { get; private set; }

    /// <summary>
    /// Gets the output format schema
    /// </summary>
    public JsonSchema OutputFormat { get; private set; }

    /// <summary>
    /// Gets the estimated execution time in milliseconds
    /// </summary>
    public long EstimatedExecutionTime { get; private set; }

    /// <summary>
    /// Gets the reliability score (0-1)
    /// </summary>
    public double ReliabilityScore { get; private set; }

    /// <summary>
    /// Gets the usage count
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Gets the last used time
    /// </summary>
    public DateTime? LastUsedTime { get; private set; }

    /// <summary>
    /// Gets the example usage
    /// </summary>
    public ActionExample? ExampleUsage { get; private set; }

    /// <summary>
    /// Gets the documentation URL
    /// </summary>
    public string? DocumentationUrl { get; private set; }

    /// <summary>
    /// Gets the agent capability registry
    /// </summary>
    public AgentCapabilityRegistry Agent { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the AgentActionDefinition class
    /// </summary>
    /// <param name="actionId">The action identifier</param>
    /// <param name="agentId">The agent identifier</param>
    /// <param name="actionName">The action name</param>
    /// <param name="actionDescription">The action description</param>
    /// <param name="inputParameters">The input parameters schema</param>
    /// <param name="outputFormat">The output format schema</param>
    /// <param name="estimatedExecutionTime">The estimated execution time in milliseconds</param>
    public AgentActionDefinition(
        Guid actionId,
        string agentId,
        string actionName,
        string actionDescription,
        JsonSchema inputParameters,
        JsonSchema outputFormat,
        long estimatedExecutionTime = 1000)
    {
        ActionId = actionId;
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
        ActionDescription = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
        InputParameters = inputParameters ?? throw new ArgumentNullException(nameof(inputParameters));
        OutputFormat = outputFormat ?? throw new ArgumentNullException(nameof(outputFormat));
        EstimatedExecutionTime = estimatedExecutionTime;
        ReliabilityScore = 1.0; // Start with perfect reliability
        UsageCount = 0;
        
        Id = actionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private AgentActionDefinition() 
    {
        InputParameters = JsonSchema.Empty();
        OutputFormat = JsonSchema.Empty();
    }

    /// <summary>
    /// Updates the action definition
    /// </summary>
    /// <param name="actionDescription">The new action description</param>
    /// <param name="inputParameters">The new input parameters schema</param>
    /// <param name="outputFormat">The new output format schema</param>
    /// <param name="estimatedExecutionTime">The new estimated execution time</param>
    public void UpdateAction(
        string actionDescription,
        JsonSchema inputParameters,
        JsonSchema outputFormat,
        long estimatedExecutionTime)
    {
        if (string.IsNullOrWhiteSpace(actionDescription))
            throw new ArgumentException("Action description cannot be empty", nameof(actionDescription));

        ActionDescription = actionDescription;
        InputParameters = inputParameters ?? throw new ArgumentNullException(nameof(inputParameters));
        OutputFormat = outputFormat ?? throw new ArgumentNullException(nameof(outputFormat));
        EstimatedExecutionTime = estimatedExecutionTime;
        UpdateVersion();
    }

    /// <summary>
    /// Increments the usage count
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
        LastUsedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Updates the reliability score
    /// </summary>
    /// <param name="score">The new reliability score (0-1)</param>
    public void UpdateReliabilityScore(double score)
    {
        if (score < 0 || score > 1)
            throw new ArgumentOutOfRangeException(nameof(score), "Reliability score must be between 0 and 1");

        ReliabilityScore = score;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the example usage
    /// </summary>
    /// <param name="exampleUsage">The example usage</param>
    public void SetExampleUsage(ActionExample exampleUsage)
    {
        ExampleUsage = exampleUsage;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the documentation URL
    /// </summary>
    /// <param name="documentationUrl">The documentation URL</param>
    public void SetDocumentationUrl(string documentationUrl)
    {
        DocumentationUrl = documentationUrl;
        UpdateVersion();
    }

    /// <summary>
    /// Validates the input parameters against the schema
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateInput(object input)
    {
        var result = new ValidationResult();
        
        try
        {
            if (input == null)
            {
                result.AddError("Input", "Input cannot be null");
                return result;
            }

            // Validate against JSON schema
            var validationResult = InputParameters.Validate(input);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    result.AddError(error.PropertyName, error.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            result.AddError("Input", $"Validation failed: {ex.Message}");
        }
        
        return result;
    }

    /// <summary>
    /// Gets the action category based on the action name
    /// </summary>
    /// <returns>The action category</returns>
    public string GetActionCategory()
    {
        return ActionName.ToLower() switch
        {
            var name when name.Contains("file") => "FileSystem",
            var name when name.Contains("email") => "Communication",
            var name when name.Contains("web") => "WebAutomation",
            var name when name.Contains("excel") || name.Contains("word") || name.Contains("powerpoint") => "OfficeAutomation",
            var name when name.Contains("database") || name.Contains("sql") => "DataAccess",
            var name when name.Contains("api") || name.Contains("http") => "WebServices",
            var name when name.Contains("script") || name.Contains("command") => "Scripting",
            _ => "General"
        };
    }

    /// <summary>
    /// Checks if the action requires elevated permissions
    /// </summary>
    /// <returns>True if elevated permissions are required, false otherwise</returns>
    public bool RequiresElevatedPermissions()
    {
        var category = GetActionCategory();
        return category switch
        {
            "FileSystem" => true,
            "Scripting" => true,
            "DataAccess" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the estimated cost of executing this action
    /// </summary>
    /// <returns>The estimated cost in arbitrary units</returns>
    public double GetEstimatedCost()
    {
        // Cost based on execution time and complexity
        var baseCost = EstimatedExecutionTime / 1000.0; // Convert to seconds
        
        // Add complexity multiplier based on category
        var categoryMultiplier = GetActionCategory() switch
        {
            "OfficeAutomation" => 1.5,
            "WebAutomation" => 1.3,
            "DataAccess" => 1.2,
            "WebServices" => 1.1,
            _ => 1.0
        };
        
        return baseCost * categoryMultiplier;
    }

    /// <summary>
    /// Creates a simplified version of the action definition for caching
    /// </summary>
    /// <returns>A simplified action definition</returns>
    public SimpleActionDefinition ToSimpleDefinition()
    {
        return new SimpleActionDefinition
        {
            ActionId = ActionId,
            AgentId = AgentId,
            ActionName = ActionName,
            ActionDescription = ActionDescription,
            EstimatedExecutionTime = EstimatedExecutionTime,
            ReliabilityScore = ReliabilityScore,
            UsageCount = UsageCount,
            Category = GetActionCategory(),
            RequiresElevatedPermissions = RequiresElevatedPermissions()
        };
    }
}

/// <summary>
/// JSON schema value object
/// </summary>
public class JsonSchema : ValueObject
{
    /// <summary>
    /// Gets the schema content
    /// </summary>
    public string SchemaContent { get; }

    /// <summary>
    /// Gets the schema version
    /// </summary>
    public string SchemaVersion { get; }

    /// <summary>
    /// Initializes a new instance of the JsonSchema class
    /// </summary>
    /// <param name="schemaContent">The schema content</param>
    /// <param name="schemaVersion">The schema version</param>
    public JsonSchema(string schemaContent, string schemaVersion = "draft-07")
    {
        SchemaContent = schemaContent ?? throw new ArgumentNullException(nameof(schemaContent));
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
    }

    /// <summary>
    /// Validates an object against this schema
    /// </summary>
    /// <param name="instance">The object to validate</param>
    /// <returns>A validation result</returns>
    public ValidationResult Validate(object instance)
    {
        var result = new ValidationResult();
        
        try
        {
            // Simple validation - in a real implementation, you would use a JSON schema validator
            // For now, just check that the instance can be serialized to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(instance);
            if (string.IsNullOrEmpty(json))
            {
                result.AddError("Instance", "Object cannot be serialized to JSON");
            }
        }
        catch (Exception ex)
        {
            result.AddError("Instance", $"Validation failed: {ex.Message}");
        }
        
        return result;
    }

    /// <summary>
    /// Creates an empty JSON schema
    /// </summary>
    /// <returns>An empty JSON schema</returns>
    public static JsonSchema Empty()
    {
        return new JsonSchema("{}");
    }

    /// <summary>
    /// Creates a JSON schema from a type
    /// </summary>
    /// <param name="type">The type to create schema for</param>
    /// <returns>A JSON schema</returns>
    public static JsonSchema FromType(Type type)
    {
        // Simple implementation - in a real system, you would generate the schema from the type
        var schema = $"{{\"type\": \"object\", \"title\": \"{type.Name}\"}}";
        return new JsonSchema(schema);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return SchemaContent;
        yield return SchemaVersion;
    }
}

/// <summary>
/// Action example value object
/// </summary>
public class ActionExample : ValueObject
{
    /// <summary>
    /// Gets the example title
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the example description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the example input
    /// </summary>
    public string ExampleInput { get; }

    /// <summary>
    /// Gets the expected output
    /// </summary>
    public string ExpectedOutput { get; }

    /// <summary>
    /// Initializes a new instance of the ActionExample class
    /// </summary>
    /// <param name="title">The example title</param>
    /// <param name="description">The example description</param>
    /// <param name="exampleInput">The example input</param>
    /// <param name="expectedOutput">The expected output</param>
    public ActionExample(string title, string description, string exampleInput, string expectedOutput)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ExampleInput = exampleInput ?? throw new ArgumentNullException(nameof(exampleInput));
        ExpectedOutput = expectedOutput ?? throw new ArgumentNullException(nameof(expectedOutput));
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Title;
        yield return Description;
        yield return ExampleInput;
        yield return ExpectedOutput;
    }
}

/// <summary>
/// Simple action definition for caching and quick access
/// </summary>
public class SimpleActionDefinition
{
    public Guid ActionId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public long EstimatedExecutionTime { get; set; }
    public double ReliabilityScore { get; set; }
    public int UsageCount { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool RequiresElevatedPermissions { get; set; }
}