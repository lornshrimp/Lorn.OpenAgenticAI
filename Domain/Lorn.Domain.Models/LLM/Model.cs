using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.LLM;

/// <summary>
/// Model entity
/// </summary>
public class Model : BaseEntity
{
    private readonly List<ModelCapability> _supportedCapabilities = new();
    private readonly List<ModelUserConfiguration> _userConfigurations = new();

    /// <summary>
    /// Gets the model identifier
    /// </summary>
    public Guid ModelId { get; private set; }

    /// <summary>
    /// Gets the provider identifier
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Gets the model name
    /// </summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the display name
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the model group
    /// </summary>
    public string? ModelGroup { get; private set; }

    /// <summary>
    /// Gets the description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the context length in tokens
    /// </summary>
    public int ContextLength { get; private set; }

    /// <summary>
    /// Gets the maximum output tokens
    /// </summary>
    public int? MaxOutputTokens { get; private set; }

    /// <summary>
    /// Gets the pricing information
    /// </summary>
    public PricingInfo PricingInfo { get; private set; } = null!;

    /// <summary>
    /// Gets the performance metrics
    /// </summary>
    public PerformanceMetrics PerformanceMetrics { get; private set; } = null!;

    /// <summary>
    /// Gets the release date
    /// </summary>
    public DateTime ReleaseDate { get; private set; }

    /// <summary>
    /// Gets whether this is the latest version
    /// </summary>
    public bool IsLatestVersion { get; private set; }

    /// <summary>
    /// Gets whether this is a prebuilt model
    /// </summary>
    public bool IsPrebuilt { get; private set; }

    /// <summary>
    /// Gets the user who created this model configuration
    /// </summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>
    /// Gets the model provider
    /// </summary>
    public ModelProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Gets the supported capabilities
    /// </summary>
    public IReadOnlyList<ModelCapability> SupportedCapabilities => _supportedCapabilities.AsReadOnly();

    /// <summary>
    /// Gets the user configurations
    /// </summary>
    public IReadOnlyList<ModelUserConfiguration> UserConfigurations => _userConfigurations.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the Model class
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="modelName">The model name</param>
    /// <param name="displayName">The display name</param>
    /// <param name="description">The description</param>
    /// <param name="contextLength">The context length</param>
    /// <param name="pricingInfo">The pricing information</param>
    /// <param name="releaseDate">The release date</param>
    /// <param name="isPrebuilt">Whether this is a prebuilt model</param>
    /// <param name="createdBy">The user who created this model configuration</param>
    public Model(
        Guid modelId,
        Guid providerId,
        string modelName,
        string displayName,
        string description,
        int contextLength,
        PricingInfo pricingInfo,
        DateTime? releaseDate = null,
        bool isPrebuilt = false,
        Guid? createdBy = null)
    {
        ModelId = modelId;
        ProviderId = providerId;
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ContextLength = Math.Max(1, contextLength);
        PricingInfo = pricingInfo ?? throw new ArgumentNullException(nameof(pricingInfo));
        ReleaseDate = releaseDate ?? DateTime.UtcNow;
        IsPrebuilt = isPrebuilt;
        CreatedBy = createdBy;
        PerformanceMetrics = PerformanceMetrics.Default();
        IsLatestVersion = true;

        Id = modelId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ModelCreatedEvent(modelId, modelName, providerId));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Model()
    {
        PricingInfo = null!;
        PerformanceMetrics = PerformanceMetrics.Default();
        Provider = null!;
    }

    /// <summary>
    /// Updates the model information
    /// </summary>
    /// <param name="displayName">The new display name</param>
    /// <param name="description">The new description</param>
    /// <param name="modelGroup">The model group</param>
    /// <param name="maxOutputTokens">The maximum output tokens</param>
    public void UpdateModelInfo(
        string displayName,
        string description,
        string? modelGroup = null,
        int? maxOutputTokens = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName;
        Description = description ?? string.Empty;
        ModelGroup = modelGroup;
        MaxOutputTokens = maxOutputTokens.HasValue ? Math.Max(1, maxOutputTokens.Value) : null;

        UpdateVersion();
        AddDomainEvent(new ModelUpdatedEvent(ModelId, ModelName, displayName));
    }

    /// <summary>
    /// Updates the pricing information
    /// </summary>
    /// <param name="pricingInfo">The new pricing information</param>
    public void UpdatePricingInfo(PricingInfo pricingInfo)
    {
        PricingInfo = pricingInfo ?? throw new ArgumentNullException(nameof(pricingInfo));
        UpdateVersion();

        AddDomainEvent(new ModelPricingUpdatedEvent(ModelId, ModelName));
    }

    /// <summary>
    /// Updates the performance metrics
    /// </summary>
    /// <param name="metrics">The new performance metrics</param>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        UpdateVersion();

        AddDomainEvent(new ModelPerformanceUpdatedEvent(ModelId, ModelName, metrics.GetHealthScore()));
    }

    /// <summary>
    /// Adds a supported capability
    /// </summary>
    /// <param name="capability">The capability to add</param>
    public void AddCapability(ModelCapability capability)
    {
        if (capability == null)
            throw new ArgumentNullException(nameof(capability));

        if (!_supportedCapabilities.Contains(capability))
        {
            _supportedCapabilities.Add(capability);
            UpdateVersion();

            AddDomainEvent(new ModelCapabilityAddedEvent(ModelId, ModelName, capability.Name));
        }
    }

    /// <summary>
    /// Removes a supported capability
    /// </summary>
    /// <param name="capability">The capability to remove</param>
    public void RemoveCapability(ModelCapability capability)
    {
        if (_supportedCapabilities.Remove(capability))
        {
            UpdateVersion();
            AddDomainEvent(new ModelCapabilityRemovedEvent(ModelId, ModelName, capability.Name));
        }
    }

    /// <summary>
    /// Checks if the model supports a specific capability
    /// </summary>
    /// <param name="capability">The capability to check</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool SupportsCapability(ModelCapability capability)
    {
        return _supportedCapabilities.Contains(capability);
    }

    /// <summary>
    /// Calculates the cost for token usage
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <returns>The calculated cost</returns>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        return PricingInfo.CalculateCost(inputTokens, outputTokens);
    }

    /// <summary>
    /// Gets the formatted cost for token usage
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <returns>The formatted cost string</returns>
    public string GetFormattedCost(int inputTokens, int outputTokens)
    {
        return PricingInfo.GetFormattedCost(inputTokens, outputTokens);
    }

    /// <summary>
    /// Sets the latest version status
    /// </summary>
    /// <param name="isLatest">Whether this is the latest version</param>
    public void SetLatestVersion(bool isLatest)
    {
        if (IsLatestVersion != isLatest)
        {
            IsLatestVersion = isLatest;
            UpdateVersion();

            AddDomainEvent(new ModelVersionStatusChangedEvent(ModelId, ModelName, isLatest));
        }
    }

    /// <summary>
    /// Validates the model configuration
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateModel()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ModelName))
            result.AddError("ModelName", "Model name is required");

        if (string.IsNullOrWhiteSpace(DisplayName))
            result.AddError("DisplayName", "Display name is required");

        if (ContextLength <= 0)
            result.AddError("ContextLength", "Context length must be greater than 0");

        if (MaxOutputTokens.HasValue && MaxOutputTokens.Value <= 0)
            result.AddError("MaxOutputTokens", "Max output tokens must be greater than 0");

        if (MaxOutputTokens.HasValue && MaxOutputTokens.Value > ContextLength)
            result.AddError("MaxOutputTokens", "Max output tokens cannot exceed context length");

        return result;
    }

    /// <summary>
    /// Gets the effective maximum output tokens
    /// </summary>
    /// <returns>The effective maximum output tokens</returns>
    public int GetEffectiveMaxOutputTokens()
    {
        return MaxOutputTokens ?? Math.Min(ContextLength / 2, 4096);
    }

    /// <summary>
    /// Checks if the model is suitable for the given task requirements
    /// </summary>
    /// <param name="requiredCapabilities">Required capabilities</param>
    /// <param name="estimatedTokens">Estimated token usage</param>
    /// <param name="maxCost">Maximum acceptable cost</param>
    /// <returns>True if suitable, false otherwise</returns>
    public bool IsSuitableFor(List<ModelCapability> requiredCapabilities, int estimatedTokens, decimal? maxCost = null)
    {
        // Check capabilities
        if (requiredCapabilities.Any(cap => !SupportsCapability(cap)))
            return false;

        // Check context length
        if (estimatedTokens > ContextLength)
            return false;

        // Check cost if specified
        if (maxCost.HasValue)
        {
            var estimatedCost = CalculateCost(estimatedTokens / 2, estimatedTokens / 2);
            if (estimatedCost > maxCost.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a recommendation score for the specified requirements
    /// </summary>
    /// <param name="requiredCapabilities">Required capabilities</param>
    /// <param name="estimatedTokens">Estimated token usage</param>
    /// <param name="prioritizeCost">Whether to prioritize cost</param>
    /// <param name="prioritizePerformance">Whether to prioritize performance</param>
    /// <returns>A recommendation score (0-100)</returns>
    public double GetRecommendationScore(
        List<ModelCapability> requiredCapabilities,
        int estimatedTokens,
        bool prioritizeCost = false,
        bool prioritizePerformance = false)
    {
        double score = 0;

        // Base suitability check
        if (!IsSuitableFor(requiredCapabilities, estimatedTokens))
            return 0;

        // Capability match score (40%)
        var capabilityScore = requiredCapabilities.Count > 0
            ? (double)requiredCapabilities.Count(SupportsCapability) / requiredCapabilities.Count
            : 1.0;
        score += capabilityScore * 40;

        // Performance score (30%)
        var performanceScore = PerformanceMetrics.GetHealthScore() / 100.0;
        score += performanceScore * 30;

        // Cost efficiency score (20%)
        var estimatedCost = CalculateCost(estimatedTokens / 2, estimatedTokens / 2);
        var costScore = estimatedCost == 0 ? 1.0 : Math.Max(0, 1.0 - (double)estimatedCost / 10.0);
        score += costScore * 20;

        // Context utilization score (10%)
        var utilizationScore = estimatedTokens <= ContextLength * 0.8 ? 1.0 : 0.5;
        score += utilizationScore * 10;

        // Apply priority adjustments
        if (prioritizeCost)
        {
            score = (score * 0.7) + (costScore * 30);
        }
        else if (prioritizePerformance)
        {
            score = (score * 0.7) + (performanceScore * 30);
        }

        return Math.Min(100, Math.Max(0, score));
    }
}

/// <summary>
/// Domain event raised when a model is created
/// </summary>
public class ModelCreatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public Guid ProviderId { get; }

    public ModelCreatedEvent(Guid modelId, string modelName, Guid providerId)
    {
        ModelId = modelId;
        ModelName = modelName;
        ProviderId = providerId;
    }
}

/// <summary>
/// Domain event raised when a model is updated
/// </summary>
public class ModelUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string DisplayName { get; }

    public ModelUpdatedEvent(Guid modelId, string modelName, string displayName)
    {
        ModelId = modelId;
        ModelName = modelName;
        DisplayName = displayName;
    }
}

/// <summary>
/// Domain event raised when model pricing is updated
/// </summary>
public class ModelPricingUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }

    public ModelPricingUpdatedEvent(Guid modelId, string modelName)
    {
        ModelId = modelId;
        ModelName = modelName;
    }
}

/// <summary>
/// Domain event raised when model performance is updated
/// </summary>
public class ModelPerformanceUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public double HealthScore { get; }

    public ModelPerformanceUpdatedEvent(Guid modelId, string modelName, double healthScore)
    {
        ModelId = modelId;
        ModelName = modelName;
        HealthScore = healthScore;
    }
}

/// <summary>
/// Domain event raised when a model capability is added
/// </summary>
public class ModelCapabilityAddedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string CapabilityName { get; }

    public ModelCapabilityAddedEvent(Guid modelId, string modelName, string capabilityName)
    {
        ModelId = modelId;
        ModelName = modelName;
        CapabilityName = capabilityName;
    }
}

/// <summary>
/// Domain event raised when a model capability is removed
/// </summary>
public class ModelCapabilityRemovedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string CapabilityName { get; }

    public ModelCapabilityRemovedEvent(Guid modelId, string modelName, string capabilityName)
    {
        ModelId = modelId;
        ModelName = modelName;
        CapabilityName = capabilityName;
    }
}

/// <summary>
/// Domain event raised when model version status changes
/// </summary>
public class ModelVersionStatusChangedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public bool IsLatestVersion { get; }

    public ModelVersionStatusChangedEvent(Guid modelId, string modelName, bool isLatestVersion)
    {
        ModelId = modelId;
        ModelName = modelName;
        IsLatestVersion = isLatestVersion;
    }
}