using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Capabilities;

/// <summary>
/// Model capability registry entity
/// </summary>
public class ModelCapabilityRegistry : BaseEntity
{
    private readonly List<ConnectionConfiguration> _connections = new();

    /// <summary>
    /// Gets the model identifier
    /// </summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the model name
    /// </summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the model version
    /// </summary>
    public new string Version { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the model is active
    /// </summary>
    public bool IsModelActive { get; private set; } = true;

    /// <summary>
    /// Gets the supported features
    /// </summary>
    public List<ModelFeature> SupportedFeatures { get; private set; } = new();

    /// <summary>
    /// Gets the maximum tokens
    /// </summary>
    public int MaxTokens { get; private set; }

    /// <summary>
    /// Gets the cost per input token
    /// </summary>
    public decimal CostPerInputToken { get; private set; }

    /// <summary>
    /// Gets the cost per output token
    /// </summary>
    public decimal CostPerOutputToken { get; private set; }

    /// <summary>
    /// Gets the average response time in milliseconds
    /// </summary>
    public long AverageResponseTime { get; private set; }

    /// <summary>
    /// Gets the reliability score (0-1)
    /// </summary>
    public double ReliabilityScore { get; private set; }

    /// <summary>
    /// Gets the last updated time
    /// </summary>
    public DateTime LastUpdatedTime { get; private set; }

    /// <summary>
    /// Gets the model configuration
    /// </summary>
    public ModelConfiguration ConfigurationOptions { get; private set; }

    /// <summary>
    /// Gets the usage restrictions
    /// </summary>
    public UsageRestrictions UsageRestrictions { get; private set; }

    /// <summary>
    /// Gets the documentation URL
    /// </summary>
    public string? DocumentationUrl { get; private set; }

    /// <summary>
    /// Gets the connections
    /// </summary>
    public IReadOnlyList<ConnectionConfiguration> Connections => _connections.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the ModelCapabilityRegistry class
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    /// <param name="modelName">The model name</param>
    /// <param name="provider">The provider</param>
    /// <param name="version">The version</param>
    /// <param name="maxTokens">The maximum tokens</param>
    /// <param name="costPerInputToken">The cost per input token</param>
    /// <param name="costPerOutputToken">The cost per output token</param>
    public ModelCapabilityRegistry(
        string modelId,
        string modelName,
        string provider,
        string version,
        int maxTokens,
        decimal costPerInputToken,
        decimal costPerOutputToken)
    {
        ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        MaxTokens = maxTokens;
        CostPerInputToken = costPerInputToken;
        CostPerOutputToken = costPerOutputToken;

        ReliabilityScore = 1.0;
        AverageResponseTime = 1000;
        LastUpdatedTime = DateTime.UtcNow;
        ConfigurationOptions = ModelConfiguration.Default();
        UsageRestrictions = UsageRestrictions.Default();

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ModelCapabilityRegistry()
    {
        ConfigurationOptions = ModelConfiguration.Default();
        UsageRestrictions = UsageRestrictions.Default();
    }

    /// <summary>
    /// Updates the model information
    /// </summary>
    /// <param name="modelName">The new model name</param>
    /// <param name="version">The new version</param>
    /// <param name="maxTokens">The new maximum tokens</param>
    public void UpdateModelInfo(string modelName, string version, int maxTokens)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));

        ModelName = modelName;
        Version = version ?? string.Empty;
        MaxTokens = maxTokens;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Updates the pricing information
    /// </summary>
    /// <param name="costPerInputToken">The cost per input token</param>
    /// <param name="costPerOutputToken">The cost per output token</param>
    public void UpdatePricing(decimal costPerInputToken, decimal costPerOutputToken)
    {
        CostPerInputToken = costPerInputToken;
        CostPerOutputToken = costPerOutputToken;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a supported feature
    /// </summary>
    /// <param name="feature">The feature to add</param>
    public void AddSupportedFeature(ModelFeature feature)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        if (!SupportedFeatures.Contains(feature))
        {
            SupportedFeatures.Add(feature);
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a supported feature
    /// </summary>
    /// <param name="feature">The feature to remove</param>
    public void RemoveSupportedFeature(ModelFeature feature)
    {
        if (SupportedFeatures.Remove(feature))
        {
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Updates the performance metrics
    /// </summary>
    /// <param name="averageResponseTime">The new average response time</param>
    /// <param name="reliabilityScore">The new reliability score</param>
    public void UpdatePerformanceMetrics(long averageResponseTime, double reliabilityScore)
    {
        if (reliabilityScore < 0 || reliabilityScore > 1)
            throw new ArgumentOutOfRangeException(nameof(reliabilityScore), "Reliability score must be between 0 and 1");

        AverageResponseTime = averageResponseTime;
        ReliabilityScore = reliabilityScore;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the model configuration
    /// </summary>
    /// <param name="configuration">The model configuration</param>
    public void SetConfiguration(ModelConfiguration configuration)
    {
        ConfigurationOptions = configuration ?? throw new ArgumentNullException(nameof(configuration));
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the usage restrictions
    /// </summary>
    /// <param name="restrictions">The usage restrictions</param>
    public void SetUsageRestrictions(UsageRestrictions restrictions)
    {
        UsageRestrictions = restrictions ?? throw new ArgumentNullException(nameof(restrictions));
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the documentation URL
    /// </summary>
    /// <param name="documentationUrl">The documentation URL</param>
    public void SetDocumentationUrl(string documentationUrl)
    {
        DocumentationUrl = documentationUrl;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a connection configuration
    /// </summary>
    /// <param name="connection">The connection configuration</param>
    public void AddConnection(ConnectionConfiguration connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        _connections.Add(connection);
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Removes a connection configuration
    /// </summary>
    /// <param name="connectionId">The connection identifier</param>
    public void RemoveConnection(Guid connectionId)
    {
        var connection = _connections.FirstOrDefault(c => c.ConnectionId == connectionId);
        if (connection != null)
        {
            _connections.Remove(connection);
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Calculates the cost for token usage
    /// </summary>
    /// <param name="inputTokens">The number of input tokens</param>
    /// <param name="outputTokens">The number of output tokens</param>
    /// <returns>The calculated cost</returns>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        return (inputTokens * CostPerInputToken) + (outputTokens * CostPerOutputToken);
    }

    /// <summary>
    /// Checks if a feature is supported
    /// </summary>
    /// <param name="feature">The feature to check</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool IsFeatureSupported(ModelFeature feature)
    {
        return SupportedFeatures.Contains(feature);
    }

    /// <summary>
    /// Updates the reliability score based on operation success
    /// </summary>
    /// <param name="isSuccessful">Whether the operation was successful</param>
    public void UpdateReliabilityScore(bool isSuccessful)
    {
        // Use exponential moving average with alpha = 0.1
        var alpha = 0.1;
        var newScore = isSuccessful ? 1.0 : 0.0;
        ReliabilityScore = ReliabilityScore * (1 - alpha) + newScore * alpha;

        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Activates the model
    /// </summary>
    public void ActivateModel()
    {
        IsModelActive = true;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Deactivates the model
    /// </summary>
    public void DeactivateModel()
    {
        IsModelActive = false;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Gets the model efficiency score based on cost and performance
    /// </summary>
    /// <returns>The efficiency score (higher is better)</returns>
    public double GetEfficiencyScore()
    {
        // Simple efficiency calculation: reliability / (cost per token + response time factor)
        var avgCostPerToken = (CostPerInputToken + CostPerOutputToken) / 2;
        var responseTimeFactor = AverageResponseTime / 1000.0; // Convert to seconds

        var costFactor = (double)avgCostPerToken * 1000; // Scale up for calculation
        var denominator = costFactor + responseTimeFactor;

        return denominator > 0 ? ReliabilityScore / denominator : 0;
    }

    /// <summary>
    /// Checks if the model can handle the specified token count
    /// </summary>
    /// <param name="tokenCount">The token count to check</param>
    /// <returns>True if the model can handle the token count, false otherwise</returns>
    public bool CanHandleTokenCount(int tokenCount)
    {
        return tokenCount <= MaxTokens;
    }

    /// <summary>
    /// Gets a summary of the model capabilities
    /// </summary>
    /// <returns>The capability summary</returns>
    public string GetCapabilitySummary()
    {
        var features = string.Join(", ", SupportedFeatures.Select(f => f.Name));
        return $"{ModelName} v{Version} - Features: {features}, Max Tokens: {MaxTokens:N0}, " +
               $"Reliability: {ReliabilityScore:P1}, Avg Response: {AverageResponseTime}ms";
    }
}

/// <summary>
/// Model feature enumeration
/// </summary>
public sealed class ModelFeature : Enumeration
{
    /// <summary>
    /// Text generation feature
    /// </summary>
    public static readonly ModelFeature TextGeneration = new(1, nameof(TextGeneration), "Generate human-like text");

    /// <summary>
    /// Embedding feature
    /// </summary>
    public static readonly ModelFeature Embedding = new(2, nameof(Embedding), "Convert text to vector embeddings");

    /// <summary>
    /// Image generation feature
    /// </summary>
    public static readonly ModelFeature ImageGeneration = new(3, nameof(ImageGeneration), "Generate images from text descriptions");

    /// <summary>
    /// Image analysis feature
    /// </summary>
    public static readonly ModelFeature ImageAnalysis = new(4, nameof(ImageAnalysis), "Analyze and describe images");

    /// <summary>
    /// Code generation feature
    /// </summary>
    public static readonly ModelFeature CodeGeneration = new(5, nameof(CodeGeneration), "Generate programming code");

    /// <summary>
    /// Function calling feature
    /// </summary>
    public static readonly ModelFeature FunctionCalling = new(6, nameof(FunctionCalling), "Call external functions based on context");

    /// <summary>
    /// Streaming response feature
    /// </summary>
    public static readonly ModelFeature StreamingResponse = new(7, nameof(StreamingResponse), "Stream responses in real-time");

    /// <summary>
    /// Fine-tuning feature
    /// </summary>
    public static readonly ModelFeature FineTuning = new(8, nameof(FineTuning), "Support for custom fine-tuning");

    /// <summary>
    /// Gets the feature description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the ModelFeature class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private ModelFeature(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if this feature requires additional configuration
    /// </summary>
    /// <returns>True if additional configuration is required, false otherwise</returns>
    public bool RequiresConfiguration()
    {
        return this switch
        {
            var f when f == FunctionCalling => true,
            var f when f == FineTuning => true,
            var f when f == StreamingResponse => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the typical use cases for this feature
    /// </summary>
    /// <returns>List of use cases</returns>
    public List<string> GetUseCases()
    {
        return this switch
        {
            var f when f == TextGeneration => new List<string> { "Content Creation", "Summarization", "Q&A", "Translation" },
            var f when f == Embedding => new List<string> { "Semantic Search", "Similarity Matching", "Clustering" },
            var f when f == ImageGeneration => new List<string> { "Art Creation", "Product Design", "Marketing Materials" },
            var f when f == ImageAnalysis => new List<string> { "Content Moderation", "Medical Diagnosis", "Quality Control" },
            var f when f == CodeGeneration => new List<string> { "Software Development", "Code Review", "Documentation" },
            var f when f == FunctionCalling => new List<string> { "Tool Integration", "API Automation", "Workflow Execution" },
            var f when f == StreamingResponse => new List<string> { "Real-time Chat", "Live Updates", "Progressive Loading" },
            var f when f == FineTuning => new List<string> { "Domain Specialization", "Performance Optimization", "Custom Behavior" },
            _ => new List<string> { "General Purpose" }
        };
    }
}

/// <summary>
/// Model configuration value object
/// </summary>
public class ModelConfiguration : ValueObject
{
    /// <summary>
    /// Gets the configuration parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// Gets the custom headers
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the API key name
    /// </summary>
    public string? ApiKeyName { get; }

    /// <summary>
    /// Gets the endpoint template
    /// </summary>
    public string? EndpointTemplate { get; }

    /// <summary>
    /// Gets the default timeout in seconds
    /// </summary>
    public int DefaultTimeout { get; }

    /// <summary>
    /// Gets the maximum retries
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Initializes a new instance of the ModelConfiguration class
    /// </summary>
    /// <param name="parameters">The configuration parameters</param>
    /// <param name="headers">The custom headers</param>
    /// <param name="apiKeyName">The API key name</param>
    /// <param name="endpointTemplate">The endpoint template</param>
    /// <param name="defaultTimeout">The default timeout in seconds</param>
    /// <param name="maxRetries">The maximum retries</param>
    public ModelConfiguration(
        Dictionary<string, object>? parameters = null,
        Dictionary<string, string>? headers = null,
        string? apiKeyName = null,
        string? endpointTemplate = null,
        int defaultTimeout = 30,
        int maxRetries = 3)
    {
        Parameters = parameters ?? new Dictionary<string, object>();
        Headers = headers ?? new Dictionary<string, string>();
        ApiKeyName = apiKeyName;
        EndpointTemplate = endpointTemplate;
        DefaultTimeout = defaultTimeout;
        MaxRetries = maxRetries;
    }

    /// <summary>
    /// Gets a parameter value
    /// </summary>
    /// <typeparam name="T">The parameter type</typeparam>
    /// <param name="key">The parameter key</param>
    /// <returns>The parameter value</returns>
    public T GetParameter<T>(string key)
    {
        if (!Parameters.ContainsKey(key))
            throw new KeyNotFoundException($"Parameter '{key}' not found");

        var value = Parameters[key];
        if (value is T typedValue)
            return typedValue;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// Sets a parameter value
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>A new configuration with the updated parameter</returns>
    public ModelConfiguration SetParameter(string key, object value)
    {
        var newParameters = new Dictionary<string, object>(Parameters)
        {
            [key] = value
        };

        return new ModelConfiguration(newParameters, Headers, ApiKeyName, EndpointTemplate, DefaultTimeout, MaxRetries);
    }

    /// <summary>
    /// Builds the endpoint URL with variable substitution
    /// </summary>
    /// <param name="variables">The variables to substitute</param>
    /// <returns>The built endpoint URL</returns>
    public string BuildEndpoint(Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(EndpointTemplate))
            return string.Empty;

        var endpoint = EndpointTemplate;
        foreach (var variable in variables)
        {
            endpoint = endpoint.Replace($"{{{variable.Key}}}", variable.Value);
        }

        return endpoint;
    }

    /// <summary>
    /// Creates a default model configuration
    /// </summary>
    /// <returns>A default model configuration</returns>
    public static ModelConfiguration Default()
    {
        return new ModelConfiguration();
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        foreach (var param in Parameters.OrderBy(p => p.Key))
        {
            yield return param.Key;
            yield return param.Value;
        }

        foreach (var header in Headers.OrderBy(h => h.Key))
        {
            yield return header.Key;
            yield return header.Value;
        }

        yield return ApiKeyName ?? string.Empty;
        yield return EndpointTemplate ?? string.Empty;
        yield return DefaultTimeout;
        yield return MaxRetries;
    }
}

/// <summary>
/// Usage restrictions value object
/// </summary>
public class UsageRestrictions : ValueObject
{
    /// <summary>
    /// Gets the maximum requests per hour
    /// </summary>
    public int MaxRequestsPerHour { get; }

    /// <summary>
    /// Gets the maximum tokens per request
    /// </summary>
    public int MaxTokensPerRequest { get; }

    /// <summary>
    /// Gets the allowed regions
    /// </summary>
    public List<string> AllowedRegions { get; }

    /// <summary>
    /// Gets the restricted content types
    /// </summary>
    public List<string> RestrictedContent { get; }

    /// <summary>
    /// Gets whether authentication is required
    /// </summary>
    public bool RequiresAuthentication { get; }

    /// <summary>
    /// Gets the custom restrictions
    /// </summary>
    public Dictionary<string, object> CustomRestrictions { get; }

    /// <summary>
    /// Initializes a new instance of the UsageRestrictions class
    /// </summary>
    /// <param name="maxRequestsPerHour">The maximum requests per hour</param>
    /// <param name="maxTokensPerRequest">The maximum tokens per request</param>
    /// <param name="allowedRegions">The allowed regions</param>
    /// <param name="restrictedContent">The restricted content types</param>
    /// <param name="requiresAuthentication">Whether authentication is required</param>
    /// <param name="customRestrictions">The custom restrictions</param>
    public UsageRestrictions(
        int maxRequestsPerHour = 3600,
        int maxTokensPerRequest = 4096,
        List<string>? allowedRegions = null,
        List<string>? restrictedContent = null,
        bool requiresAuthentication = true,
        Dictionary<string, object>? customRestrictions = null)
    {
        MaxRequestsPerHour = maxRequestsPerHour;
        MaxTokensPerRequest = maxTokensPerRequest;
        AllowedRegions = allowedRegions ?? new List<string>();
        RestrictedContent = restrictedContent ?? new List<string>();
        RequiresAuthentication = requiresAuthentication;
        CustomRestrictions = customRestrictions ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Checks if a usage request is within limits
    /// </summary>
    /// <param name="request">The usage request</param>
    /// <returns>True if within limits, false otherwise</returns>
    public bool IsWithinLimits(UsageRequest request)
    {
        // Check token limit
        if (request.TokenCount > MaxTokensPerRequest)
            return false;

        // Check hourly limit (would need rate limiting service in real implementation)
        // For now, just return true

        // Check region restrictions
        if (AllowedRegions.Count > 0 && !string.IsNullOrEmpty(request.Region))
        {
            if (!AllowedRegions.Contains(request.Region))
                return false;
        }

        // Check content restrictions
        foreach (var restrictedType in RestrictedContent)
        {
            if (request.Content.Contains(restrictedType, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the remaining quota for a user
    /// </summary>
    /// <returns>The remaining quota</returns>
    public UsageQuota GetRemainingQuota()
    {
        // In a real implementation, this would check against actual usage
        return new UsageQuota(
            dailyLimit: MaxRequestsPerHour * 24,
            monthlyLimit: MaxRequestsPerHour * 24 * 30,
            remainingRequests: MaxRequestsPerHour,
            remainingTokens: MaxTokensPerRequest,
            resetTime: DateTime.UtcNow.AddHours(1));
    }

    /// <summary>
    /// Creates default usage restrictions
    /// </summary>
    /// <returns>Default usage restrictions</returns>
    public static UsageRestrictions Default()
    {
        return new UsageRestrictions();
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxRequestsPerHour;
        yield return MaxTokensPerRequest;

        foreach (var region in AllowedRegions.OrderBy(r => r))
        {
            yield return region;
        }

        foreach (var content in RestrictedContent.OrderBy(c => c))
        {
            yield return content;
        }

        yield return RequiresAuthentication;

        foreach (var restriction in CustomRestrictions.OrderBy(r => r.Key))
        {
            yield return restriction.Key;
            yield return restriction.Value;
        }
    }
}

/// <summary>
/// Usage request for validation
/// </summary>
public class UsageRequest
{
    public int TokenCount { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Connection configuration entity
/// </summary>
public class ConnectionConfiguration : BaseEntity
{
    /// <summary>
    /// Gets the connection identifier
    /// </summary>
    public Guid ConnectionId { get; private set; }

    /// <summary>
    /// Gets the connection name
    /// </summary>
    public string ConnectionName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the connection type
    /// </summary>
    public string ConnectionType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the connection parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; private set; } = new();

    /// <summary>
    /// Gets whether the connection is active
    /// </summary>
    public bool IsConnectionActive { get; private set; } = true;

    /// <summary>
    /// Initializes a new instance of the ConnectionConfiguration class
    /// </summary>
    /// <param name="connectionId">The connection identifier</param>
    /// <param name="connectionName">The connection name</param>
    /// <param name="connectionType">The connection type</param>
    /// <param name="parameters">The connection parameters</param>
    public ConnectionConfiguration(
        Guid connectionId,
        string connectionName,
        string connectionType,
        Dictionary<string, object>? parameters = null)
    {
        ConnectionId = connectionId;
        ConnectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        ConnectionType = connectionType ?? throw new ArgumentNullException(nameof(connectionType));
        Parameters = parameters ?? new Dictionary<string, object>();

        Id = connectionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ConnectionConfiguration() { }

    /// <summary>
    /// Updates the connection parameters
    /// </summary>
    /// <param name="parameters">The new parameters</param>
    public void UpdateParameters(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        UpdateVersion();
    }

    /// <summary>
    /// Activates the connection
    /// </summary>
    public void ActivateConnection()
    {
        IsConnectionActive = true;
        UpdateVersion();
    }

    /// <summary>
    /// Deactivates the connection
    /// </summary>
    public void DeactivateConnection()
    {
        IsConnectionActive = false;
        UpdateVersion();
    }
}
