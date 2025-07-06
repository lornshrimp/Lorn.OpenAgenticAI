using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Fallback configuration value object
/// </summary>
public class FallbackConfig : ValueObject
{
    /// <summary>
    /// Gets the fallback model identifier
    /// </summary>
    public Guid? FallbackModelId { get; }

    /// <summary>
    /// Gets the fallback conditions
    /// </summary>
    public List<FallbackCondition> FallbackConditions { get; }

    /// <summary>
    /// Gets the fallback strategy
    /// </summary>
    public FallbackStrategy Strategy { get; }

    /// <summary>
    /// Gets the maximum fallback depth
    /// </summary>
    public int MaxFallbackDepth { get; }

    /// <summary>
    /// Gets whether auto fallback is enabled
    /// </summary>
    public bool AutoFallbackEnabled { get; }

    /// <summary>
    /// Gets the fallback chain (ordered by preference)
    /// </summary>
    public List<Guid> FallbackChain { get; }

    /// <summary>
    /// Initializes a new instance of the FallbackConfig class
    /// </summary>
    /// <param name="fallbackModelId">The fallback model identifier</param>
    /// <param name="fallbackConditions">The fallback conditions</param>
    /// <param name="strategy">The fallback strategy</param>
    /// <param name="maxFallbackDepth">The maximum fallback depth</param>
    /// <param name="autoFallbackEnabled">Whether auto fallback is enabled</param>
    /// <param name="fallbackChain">The fallback chain</param>
    public FallbackConfig(
        Guid? fallbackModelId = null,
        List<FallbackCondition>? fallbackConditions = null,
        FallbackStrategy? strategy = null,
        int maxFallbackDepth = 3,
        bool autoFallbackEnabled = true,
        List<Guid>? fallbackChain = null)
    {
        FallbackModelId = fallbackModelId;
        FallbackConditions = fallbackConditions ?? new List<FallbackCondition>();
        Strategy = strategy ?? FallbackStrategy.BestPerformance;
        MaxFallbackDepth = Math.Max(0, Math.Min(10, maxFallbackDepth));
        AutoFallbackEnabled = autoFallbackEnabled;
        FallbackChain = fallbackChain ?? new List<Guid>();
    }

    /// <summary>
    /// Determines whether fallback should be triggered
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>True if fallback should be triggered, false otherwise</returns>
    public bool ShouldFallback(ExecutionContext context)
    {
        if (!AutoFallbackEnabled || !FallbackConditions.Any())
            return false;

        return FallbackConditions.Any(condition => condition.Id switch
        {
            1 => context.ResponseTimeMs > context.LatencyThreshold, // HighLatency
            2 => context.QualityScore < context.QualityThreshold,   // LowQuality
            3 => context.ErrorRate > context.ErrorRateThreshold,    // ErrorRate
            4 => context.IsQuotaExceeded,                           // QuotaExceeded
            5 => context.IsServiceUnavailable,                      // ServiceUnavailable
            _ => false
        });
    }

    /// <summary>
    /// Gets the next fallback model from the chain
    /// </summary>
    /// <param name="currentDepth">The current fallback depth</param>
    /// <returns>The next fallback model identifier or null if none available</returns>
    public Guid? GetNextFallbackModel(int currentDepth = 0)
    {
        if (currentDepth >= MaxFallbackDepth)
            return null;

        // If single fallback model is specified, use it
        if (FallbackModelId.HasValue && currentDepth == 0)
            return FallbackModelId.Value;

        // Use fallback chain if available
        if (FallbackChain.Count > currentDepth)
            return FallbackChain[currentDepth];

        return null;
    }

    /// <summary>
    /// Gets the fallback model based on strategy
    /// </summary>
    /// <param name="availableModels">The available models to choose from</param>
    /// <param name="currentDepth">The current fallback depth</param>
    /// <returns>The selected fallback model identifier</returns>
    public Guid? GetFallbackModelByStrategy(List<FallbackModelOption> availableModels, int currentDepth = 0)
    {
        if (!availableModels.Any() || currentDepth >= MaxFallbackDepth)
            return null;

        if (Strategy.Equals(FallbackStrategy.BestPerformance))
        {
            return availableModels
                .OrderByDescending(m => m.PerformanceScore)
                .Skip(currentDepth)
                .FirstOrDefault()?.ModelId;
        }
        else if (Strategy.Equals(FallbackStrategy.LowestCost))
        {
            return availableModels
                .OrderBy(m => m.CostPerToken)
                .Skip(currentDepth)
                .FirstOrDefault()?.ModelId;
        }
        else if (Strategy.Equals(FallbackStrategy.HighestAvailability))
        {
            return availableModels
                .OrderByDescending(m => m.AvailabilityScore)
                .Skip(currentDepth)
                .FirstOrDefault()?.ModelId;
        }
        else if (Strategy.Equals(FallbackStrategy.CustomPriority))
        {
            return FallbackChain.Count > currentDepth
                ? FallbackChain[currentDepth]
                : availableModels.FirstOrDefault()?.ModelId;
        }
        else
        {
            return availableModels.FirstOrDefault()?.ModelId;
        }
    }

    /// <summary>
    /// Creates a fallback configuration with no fallback
    /// </summary>
    /// <returns>A fallback configuration with no fallback</returns>
    public static FallbackConfig None()
    {
        return new FallbackConfig(autoFallbackEnabled: false);
    }

    /// <summary>
    /// Creates a simple fallback configuration with a single fallback model
    /// </summary>
    /// <param name="fallbackModelId">The fallback model identifier</param>
    /// <param name="conditions">The fallback conditions</param>
    /// <returns>A simple fallback configuration</returns>
    public static FallbackConfig Simple(Guid fallbackModelId, params FallbackCondition[] conditions)
    {
        return new FallbackConfig(
            fallbackModelId: fallbackModelId,
            fallbackConditions: conditions.ToList(),
            autoFallbackEnabled: true);
    }

    /// <summary>
    /// Creates a chain-based fallback configuration
    /// </summary>
    /// <param name="fallbackChain">The ordered list of fallback models</param>
    /// <param name="strategy">The fallback strategy</param>
    /// <param name="conditions">The fallback conditions</param>
    /// <returns>A chain-based fallback configuration</returns>
    public static FallbackConfig Chain(
        List<Guid> fallbackChain,
        FallbackStrategy? strategy = null,
        params FallbackCondition[] conditions)
    {
        return new FallbackConfig(
            fallbackChain: fallbackChain,
            strategy: strategy,
            fallbackConditions: conditions.ToList(),
            autoFallbackEnabled: true);
    }

    /// <summary>
    /// Validates the fallback configuration
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateConfig()
    {
        var result = new ValidationResult();

        if (AutoFallbackEnabled && !FallbackConditions.Any())
        {
            result.AddError("FallbackConditions", "At least one fallback condition must be specified when auto fallback is enabled");
        }

        if (FallbackModelId.HasValue && FallbackChain.Contains(FallbackModelId.Value))
        {
            result.AddError("FallbackModelId", "Fallback model ID should not appear in the fallback chain");
        }

        if (FallbackChain.Count > MaxFallbackDepth)
        {
            result.AddError("FallbackChain", $"Fallback chain cannot exceed maximum depth of {MaxFallbackDepth}");
        }

        // Check for circular references in fallback chain
        if (FallbackChain.Count != FallbackChain.Distinct().Count())
        {
            result.AddError("FallbackChain", "Fallback chain cannot contain duplicate model IDs");
        }

        return result;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        if (FallbackModelId.HasValue)
            yield return FallbackModelId.Value;

        foreach (var condition in FallbackConditions.OrderBy(x => x.Name))
        {
            yield return condition;
        }

        yield return Strategy;
        yield return MaxFallbackDepth;
        yield return AutoFallbackEnabled;

        foreach (var modelId in FallbackChain)
        {
            yield return modelId;
        }
    }
}

/// <summary>
/// Fallback condition enumeration
/// </summary>
public sealed class FallbackCondition : Enumeration
{
    /// <summary>
    /// High latency condition
    /// </summary>
    public static readonly FallbackCondition HighLatency = new(1, nameof(HighLatency), "Response time exceeds threshold");

    /// <summary>
    /// Low quality condition
    /// </summary>
    public static readonly FallbackCondition LowQuality = new(2, nameof(LowQuality), "Response quality below threshold");

    /// <summary>
    /// High error rate condition
    /// </summary>
    public static readonly FallbackCondition ErrorRate = new(3, nameof(ErrorRate), "Error rate exceeds threshold");

    /// <summary>
    /// Quota exceeded condition
    /// </summary>
    public static readonly FallbackCondition QuotaExceeded = new(4, nameof(QuotaExceeded), "Usage quota has been exceeded");

    /// <summary>
    /// Service unavailable condition
    /// </summary>
    public static readonly FallbackCondition ServiceUnavailable = new(5, nameof(ServiceUnavailable), "Primary service is unavailable");

    /// <summary>
    /// Gets the description of the fallback condition
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the FallbackCondition class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private FallbackCondition(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Gets the severity level of the condition (1-5, where 5 is most severe)
    /// </summary>
    /// <returns>The severity level</returns>
    public int GetSeverityLevel()
    {
        return this switch
        {
            var c when c == HighLatency => 2,
            var c when c == LowQuality => 3,
            var c when c == ErrorRate => 4,
            var c when c == QuotaExceeded => 4,
            var c when c == ServiceUnavailable => 5,
            _ => 1
        };
    }
}

/// <summary>
/// Fallback strategy enumeration
/// </summary>
public sealed class FallbackStrategy : Enumeration
{
    /// <summary>
    /// Best performance strategy
    /// </summary>
    public static readonly FallbackStrategy BestPerformance = new(1, nameof(BestPerformance), "Select model with best performance metrics");

    /// <summary>
    /// Lowest cost strategy
    /// </summary>
    public static readonly FallbackStrategy LowestCost = new(2, nameof(LowestCost), "Select model with lowest cost per token");

    /// <summary>
    /// Highest availability strategy
    /// </summary>
    public static readonly FallbackStrategy HighestAvailability = new(3, nameof(HighestAvailability), "Select model with highest availability");

    /// <summary>
    /// Custom priority strategy
    /// </summary>
    public static readonly FallbackStrategy CustomPriority = new(4, nameof(CustomPriority), "Use predefined priority order");

    /// <summary>
    /// Gets the description of the fallback strategy
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the FallbackStrategy class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private FallbackStrategy(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if this strategy requires predefined model order
    /// </summary>
    /// <returns>True if predefined order is required, false otherwise</returns>
    public bool RequiresPredefinedOrder()
    {
        return this == CustomPriority;
    }
}

/// <summary>
/// Execution context for fallback decisions
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Gets the response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the quality score (0-1)
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Gets the error rate (0-1)
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Gets whether quota is exceeded
    /// </summary>
    public bool IsQuotaExceeded { get; init; }

    /// <summary>
    /// Gets whether service is unavailable
    /// </summary>
    public bool IsServiceUnavailable { get; init; }

    /// <summary>
    /// Gets the latency threshold
    /// </summary>
    public int LatencyThreshold { get; init; } = 5000;

    /// <summary>
    /// Gets the quality threshold
    /// </summary>
    public double QualityThreshold { get; init; } = 0.8;

    /// <summary>
    /// Gets the error rate threshold
    /// </summary>
    public double ErrorRateThreshold { get; init; } = 0.05;
}

/// <summary>
/// Fallback model option
/// </summary>
public class FallbackModelOption
{
    /// <summary>
    /// Gets the model identifier
    /// </summary>
    public Guid ModelId { get; init; }

    /// <summary>
    /// Gets the performance score (0-100)
    /// </summary>
    public double PerformanceScore { get; init; }

    /// <summary>
    /// Gets the cost per token
    /// </summary>
    public decimal CostPerToken { get; init; }

    /// <summary>
    /// Gets the availability score (0-100)
    /// </summary>
    public double AvailabilityScore { get; init; }

    /// <summary>
    /// Gets whether the model is currently available
    /// </summary>
    public bool IsAvailable { get; init; }
}