using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;
using Lorn.Domain.Models.UserManagement;
using Lorn.Domain.Models.Capabilities;

namespace Lorn.Domain.Models.LLM;

/// <summary>
/// Provider user configuration entity
/// </summary>
public class ProviderUserConfiguration : BaseEntity
{
    private readonly List<ModelUserConfiguration> _modelConfigurations = new();

    /// <summary>
    /// Gets the configuration identifier
    /// </summary>
    public Guid ConfigurationId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the provider identifier
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Gets the user API configuration
    /// </summary>
    public ApiConfiguration UserApiConfiguration { get; private set; } = null!;

    /// <summary>
    /// Gets whether this configuration is enabled
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Gets the priority (lower numbers = higher priority)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Gets the usage quota
    /// </summary>
    public UsageQuota UsageQuota { get; private set; } = null!;

    /// <summary>
    /// Gets the custom settings
    /// </summary>
    public CustomSettings CustomSettings { get; private set; } = null!;

    /// <summary>
    /// Gets the last used time
    /// </summary>
    public DateTime? LastUsedTime { get; private set; }

    /// <summary>
    /// Gets the user profile
    /// </summary>
    public UserProfile User { get; private set; } = null!;

    /// <summary>
    /// Gets the model provider
    /// </summary>
    public ModelProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Gets the model configurations
    /// </summary>
    public IReadOnlyList<ModelUserConfiguration> ModelConfigurations => _modelConfigurations.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the ProviderUserConfiguration class
    /// </summary>
    /// <param name="configurationId">The configuration identifier</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="userApiConfiguration">The user API configuration</param>
    /// <param name="priority">The priority</param>
    /// <param name="usageQuota">The usage quota</param>
    /// <param name="customSettings">The custom settings</param>
    public ProviderUserConfiguration(
        Guid configurationId,
        Guid userId,
        Guid providerId,
        ApiConfiguration userApiConfiguration,
        int priority = 0,
        UsageQuota? usageQuota = null,
        CustomSettings? customSettings = null)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ProviderId = providerId;
        UserApiConfiguration = userApiConfiguration ?? throw new ArgumentNullException(nameof(userApiConfiguration));
        Priority = priority;
        UsageQuota = usageQuota ?? UsageQuota.Unlimited();
        CustomSettings = customSettings ?? CustomSettings.Empty();
        
        Id = configurationId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ProviderUserConfigurationCreatedEvent(configurationId, userId, providerId));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ProviderUserConfiguration() 
    {
        UserApiConfiguration = null!;
        UsageQuota = null!;
        CustomSettings = null!;
        User = null!;
        Provider = null!;
    }

    /// <summary>
    /// Updates the API configuration
    /// </summary>
    /// <param name="apiConfiguration">The new API configuration</param>
    public void UpdateApiConfiguration(ApiConfiguration apiConfiguration)
    {
        UserApiConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
        UpdateVersion();
        
        AddDomainEvent(new ProviderUserConfigurationUpdatedEvent(ConfigurationId, UserId, ProviderId));
    }

    /// <summary>
    /// Updates the usage quota
    /// </summary>
    /// <param name="usageQuota">The new usage quota</param>
    public void UpdateUsageQuota(UsageQuota usageQuota)
    {
        UsageQuota = usageQuota ?? throw new ArgumentNullException(nameof(usageQuota));
        UpdateVersion();
    }

    /// <summary>
    /// Updates the priority
    /// </summary>
    /// <param name="priority">The new priority</param>
    public void UpdatePriority(int priority)
    {
        Priority = priority;
        UpdateVersion();
    }

    /// <summary>
    /// Enables or disables the configuration
    /// </summary>
    /// <param name="enabled">Whether to enable the configuration</param>
    public void SetEnabled(bool enabled)
    {
        if (IsEnabled != enabled)
        {
            IsEnabled = enabled;
            UpdateVersion();
            
            var eventType = enabled ? "Enabled" : "Disabled";
            AddDomainEvent(new ProviderUserConfigurationStatusChangedEvent(ConfigurationId, UserId, ProviderId, eventType));
        }
    }

    /// <summary>
    /// Updates the last used time
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();

        if (UserId == Guid.Empty)
            result.AddError("UserId", "User identifier is required");

        if (ProviderId == Guid.Empty)
            result.AddError("ProviderId", "Provider identifier is required");

        var apiValidation = UserApiConfiguration?.ValidateConfiguration();
        if (apiValidation != null && !apiValidation.IsValid)
        {
            foreach (var error in apiValidation.Errors)
            {
                result.AddError($"UserApiConfiguration.{error.PropertyName}", error.ErrorMessage);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if the usage is within quota limits
    /// </summary>
    /// <param name="tokenUsage">The token usage to check</param>
    /// <returns>True if within quota, false otherwise</returns>
    public bool IsWithinQuota(int tokenUsage)
    {
        // This would typically check against actual usage data
        // For now, we'll use the quota limits directly
        return UsageQuota.IsWithinLimits(tokenUsage, 0);
    }

    /// <summary>
    /// Records usage for quota tracking
    /// </summary>
    /// <param name="tokenUsage">The token usage</param>
    /// <param name="cost">The cost</param>
    public void RecordUsage(int tokenUsage, decimal cost)
    {
        UpdateLastUsed();
        
        // In a real implementation, this would update usage tracking
        AddDomainEvent(new ProviderUsageRecordedEvent(ConfigurationId, UserId, ProviderId, tokenUsage, cost));
    }
}

/// <summary>
/// Model user configuration entity
/// </summary>
public class ModelUserConfiguration : BaseEntity
{
    /// <summary>
    /// Gets the configuration identifier
    /// </summary>
    public Guid ConfigurationId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the model identifier
    /// </summary>
    public Guid ModelId { get; private set; }

    /// <summary>
    /// Gets the provider identifier
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Gets whether this configuration is enabled
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Gets the priority (lower numbers = higher priority)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Gets the default model parameters
    /// </summary>
    public ModelParameters DefaultParameters { get; private set; } = null!;

    /// <summary>
    /// Gets the usage settings
    /// </summary>
    public UsageSettings UsageSettings { get; private set; } = null!;

    /// <summary>
    /// Gets the quality settings
    /// </summary>
    public QualitySettings QualitySettings { get; private set; } = null!;

    /// <summary>
    /// Gets the fallback configuration
    /// </summary>
    public FallbackConfig FallbackConfig { get; private set; } = null!;

    /// <summary>
    /// Gets the last used time
    /// </summary>
    public DateTime? LastUsedTime { get; private set; }

    /// <summary>
    /// Gets the user profile
    /// </summary>
    public UserProfile User { get; private set; } = null!;

    /// <summary>
    /// Gets the model
    /// </summary>
    public Model Model { get; private set; } = null!;

    /// <summary>
    /// Gets the model provider
    /// </summary>
    public ModelProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the ModelUserConfiguration class
    /// </summary>
    /// <param name="configurationId">The configuration identifier</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="modelId">The model identifier</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="defaultParameters">The default model parameters</param>
    /// <param name="priority">The priority</param>
    /// <param name="usageSettings">The usage settings</param>
    /// <param name="qualitySettings">The quality settings</param>
    /// <param name="fallbackConfig">The fallback configuration</param>
    public ModelUserConfiguration(
        Guid configurationId,
        Guid userId,
        Guid modelId,
        Guid providerId,
        ModelParameters? defaultParameters = null,
        int priority = 0,
        UsageSettings? usageSettings = null,
        QualitySettings? qualitySettings = null,
        FallbackConfig? fallbackConfig = null)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ModelId = modelId;
        ProviderId = providerId;
        DefaultParameters = defaultParameters ?? ModelParameters.Default();
        Priority = priority;
        UsageSettings = usageSettings ?? UsageSettings.Default();
        QualitySettings = qualitySettings ?? QualitySettings.Default();
        FallbackConfig = fallbackConfig ?? FallbackConfig.None();
        
        Id = configurationId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ModelUserConfigurationCreatedEvent(configurationId, userId, modelId, providerId));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ModelUserConfiguration() 
    {
        DefaultParameters = null!;
        UsageSettings = null!;
        QualitySettings = null!;
        FallbackConfig = null!;
        User = null!;
        Model = null!;
        Provider = null!;
    }

    /// <summary>
    /// Updates the model parameters
    /// </summary>
    /// <param name="parameters">The new model parameters</param>
    public void UpdateModelParameters(ModelParameters parameters)
    {
        DefaultParameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        UpdateVersion();
        
        AddDomainEvent(new ModelUserConfigurationUpdatedEvent(ConfigurationId, UserId, ModelId));
    }

    /// <summary>
    /// Updates the quality settings
    /// </summary>
    /// <param name="qualitySettings">The new quality settings</param>
    public void UpdateQualitySettings(QualitySettings qualitySettings)
    {
        QualitySettings = qualitySettings ?? throw new ArgumentNullException(nameof(qualitySettings));
        UpdateVersion();
    }

    /// <summary>
    /// Updates the fallback configuration
    /// </summary>
    /// <param name="fallbackConfig">The new fallback configuration</param>
    public void UpdateFallbackConfig(FallbackConfig fallbackConfig)
    {
        FallbackConfig = fallbackConfig ?? throw new ArgumentNullException(nameof(fallbackConfig));
        UpdateVersion();
    }

    /// <summary>
    /// Builds request parameters with optional overrides
    /// </summary>
    /// <param name="overrides">Parameter overrides</param>
    /// <returns>The merged model parameters</returns>
    public ModelParameters BuildRequestParameters(Dictionary<string, object>? overrides = null)
    {
        var parameters = DefaultParameters;
        
        if (overrides != null && overrides.Any())
        {
            // Convert overrides to ModelParameters and merge
            var overrideParams = ConvertToModelParameters(overrides);
            parameters = parameters.MergeWith(overrideParams);
        }
        
        return parameters;
    }

    /// <summary>
    /// Validates the model parameters
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateParameters()
    {
        return DefaultParameters.ValidateParameters();
    }

    /// <summary>
    /// Records model usage
    /// </summary>
    /// <param name="responseTime">The response time</param>
    /// <param name="tokenUsage">The token usage</param>
    /// <param name="isSuccess">Whether the request was successful</param>
    public void RecordUsage(double responseTime, int tokenUsage, bool isSuccess)
    {
        LastUsedTime = DateTime.UtcNow;
        UpdateVersion();
        
        AddDomainEvent(new ModelUsageRecordedEvent(ConfigurationId, UserId, ModelId, responseTime, tokenUsage, isSuccess));
    }

    /// <summary>
    /// Enables or disables the configuration
    /// </summary>
    /// <param name="enabled">Whether to enable the configuration</param>
    public void SetEnabled(bool enabled)
    {
        if (IsEnabled != enabled)
        {
            IsEnabled = enabled;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Updates the priority
    /// </summary>
    /// <param name="priority">The new priority</param>
    public void UpdatePriority(int priority)
    {
        Priority = priority;
        UpdateVersion();
    }

    /// <summary>
    /// Converts dictionary overrides to ModelParameters
    /// </summary>
    /// <param name="overrides">The override dictionary</param>
    /// <returns>ModelParameters object</returns>
    private static ModelParameters ConvertToModelParameters(Dictionary<string, object> overrides)
    {
        var temperature = GetDoubleValue(overrides, "temperature", 0.7);
        var topP = GetDoubleValue(overrides, "top_p", 1.0);
        var topK = GetIntValue(overrides, "top_k");
        var maxTokens = GetIntValue(overrides, "max_tokens");
        var presencePenalty = GetDoubleValue(overrides, "presence_penalty", 0.0);
        var frequencyPenalty = GetDoubleValue(overrides, "frequency_penalty", 0.0);
        
        var stopSequences = new List<string>();
        if (overrides.ContainsKey("stop") && overrides["stop"] is IEnumerable<string> stops)
        {
            stopSequences.AddRange(stops);
        }
        
        var additionalParams = overrides
            .Where(kvp => !new[] { "temperature", "top_p", "top_k", "max_tokens", "presence_penalty", "frequency_penalty", "stop" }.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        return new ModelParameters(
            temperature,
            topP,
            topK,
            maxTokens,
            presencePenalty,
            frequencyPenalty,
            stopSequences,
            additionalParams);
    }

    private static double GetDoubleValue(Dictionary<string, object> dict, string key, double defaultValue = 0.0)
    {
        if (dict.ContainsKey(key) && dict[key] is double value)
            return value;
        if (dict.ContainsKey(key) && double.TryParse(dict[key]?.ToString(), out var parsed))
            return parsed;
        return defaultValue;
    }

    private static int? GetIntValue(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key) && dict[key] is int value)
            return value;
        if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out var parsed))
            return parsed;
        return null;
    }
}

/// <summary>
/// Custom settings value object
/// </summary>
public class CustomSettings : ValueObject
{
    /// <summary>
    /// Gets the settings dictionary
    /// </summary>
    public Dictionary<string, object> Settings { get; }

    /// <summary>
    /// Initializes a new instance of the CustomSettings class
    /// </summary>
    /// <param name="settings">The settings dictionary</param>
    public CustomSettings(Dictionary<string, object>? settings = null)
    {
        Settings = settings ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets a setting value
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <returns>The setting value or null if not found</returns>
    public object? GetSetting(string key)
    {
        return Settings.ContainsKey(key) ? Settings[key] : null;
    }

    /// <summary>
    /// Gets a typed setting value
    /// </summary>
    /// <typeparam name="T">The type to cast to</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">The default value</param>
    /// <returns>The typed setting value</returns>
    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        if (Settings.ContainsKey(key) && Settings[key] is T value)
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Sets a setting value
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    /// <returns>New CustomSettings with the updated value</returns>
    public CustomSettings SetSetting(string key, object value)
    {
        var newSettings = new Dictionary<string, object>(Settings)
        {
            [key] = value
        };
        return new CustomSettings(newSettings);
    }

    /// <summary>
    /// Creates empty custom settings
    /// </summary>
    /// <returns>Empty custom settings</returns>
    public static CustomSettings Empty()
    {
        return new CustomSettings();
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        foreach (var setting in Settings.OrderBy(x => x.Key))
        {
            yield return setting.Key;
            yield return setting.Value;
        }
    }
}

/// <summary>
/// Usage settings value object
/// </summary>
public class UsageSettings : ValueObject
{
    /// <summary>
    /// Gets the maximum requests per minute
    /// </summary>
    public int MaxRequestsPerMinute { get; }

    /// <summary>
    /// Gets the maximum concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; }

    /// <summary>
    /// Gets the timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Gets whether streaming is enabled
    /// </summary>
    public bool EnableStreaming { get; }

    /// <summary>
    /// Initializes a new instance of the UsageSettings class
    /// </summary>
    /// <param name="maxRequestsPerMinute">The maximum requests per minute</param>
    /// <param name="maxConcurrentRequests">The maximum concurrent requests</param>
    /// <param name="timeoutSeconds">The timeout in seconds</param>
    /// <param name="enableStreaming">Whether to enable streaming</param>
    public UsageSettings(
        int maxRequestsPerMinute = 60,
        int maxConcurrentRequests = 5,
        int timeoutSeconds = 30,
        bool enableStreaming = true)
    {
        MaxRequestsPerMinute = Math.Max(1, maxRequestsPerMinute);
        MaxConcurrentRequests = Math.Max(1, maxConcurrentRequests);
        TimeoutSeconds = Math.Max(5, timeoutSeconds);
        EnableStreaming = enableStreaming;
    }

    /// <summary>
    /// Creates default usage settings
    /// </summary>
    /// <returns>Default usage settings</returns>
    public static UsageSettings Default()
    {
        return new UsageSettings();
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxRequestsPerMinute;
        yield return MaxConcurrentRequests;
        yield return TimeoutSeconds;
        yield return EnableStreaming;
    }
}

/// <summary>
/// Domain event raised when a provider user configuration is created
/// </summary>
public class ProviderUserConfigurationCreatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ProviderId { get; }

    public ProviderUserConfigurationCreatedEvent(Guid configurationId, Guid userId, Guid providerId)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ProviderId = providerId;
    }
}

/// <summary>
/// Domain event raised when a provider user configuration is updated
/// </summary>
public class ProviderUserConfigurationUpdatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ProviderId { get; }

    public ProviderUserConfigurationUpdatedEvent(Guid configurationId, Guid userId, Guid providerId)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ProviderId = providerId;
    }
}

/// <summary>
/// Domain event raised when a provider user configuration status changes
/// </summary>
public class ProviderUserConfigurationStatusChangedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ProviderId { get; }
    public string Status { get; }

    public ProviderUserConfigurationStatusChangedEvent(Guid configurationId, Guid userId, Guid providerId, string status)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ProviderId = providerId;
        Status = status;
    }
}

/// <summary>
/// Domain event raised when provider usage is recorded
/// </summary>
public class ProviderUsageRecordedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ProviderId { get; }
    public int TokenUsage { get; }
    public decimal Cost { get; }

    public ProviderUsageRecordedEvent(Guid configurationId, Guid userId, Guid providerId, int tokenUsage, decimal cost)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ProviderId = providerId;
        TokenUsage = tokenUsage;
        Cost = cost;
    }
}

/// <summary>
/// Domain event raised when a model user configuration is created
/// </summary>
public class ModelUserConfigurationCreatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ModelId { get; }
    public Guid ProviderId { get; }

    public ModelUserConfigurationCreatedEvent(Guid configurationId, Guid userId, Guid modelId, Guid providerId)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ModelId = modelId;
        ProviderId = providerId;
    }
}

/// <summary>
/// Domain event raised when a model user configuration is updated
/// </summary>
public class ModelUserConfigurationUpdatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ModelId { get; }

    public ModelUserConfigurationUpdatedEvent(Guid configurationId, Guid userId, Guid modelId)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ModelId = modelId;
    }
}

/// <summary>
/// Domain event raised when model usage is recorded
/// </summary>
public class ModelUsageRecordedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public Guid UserId { get; }
    public Guid ModelId { get; }
    public double ResponseTime { get; }
    public int TokenUsage { get; }
    public bool IsSuccess { get; }

    public ModelUsageRecordedEvent(Guid configurationId, Guid userId, Guid modelId, double responseTime, int tokenUsage, bool isSuccess)
    {
        ConfigurationId = configurationId;
        UserId = userId;
        ModelId = modelId;
        ResponseTime = responseTime;
        TokenUsage = tokenUsage;
        IsSuccess = isSuccess;
    }
}