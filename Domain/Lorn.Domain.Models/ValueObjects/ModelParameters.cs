using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.ValueObjects;
using Lorn.Domain.Models.Capabilities;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Model parameters value object
/// </summary>
public class ModelParameters : ValueObject
{
    /// <summary>
    /// Gets the temperature setting (0-2)
    /// </summary>
    public double Temperature { get; }

    /// <summary>
    /// Gets the top-P setting (0-1)
    /// </summary>
    public double TopP { get; }

    /// <summary>
    /// Gets the top-K setting
    /// </summary>
    public int? TopK { get; }

    /// <summary>
    /// Gets the maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; }

    /// <summary>
    /// Gets the presence penalty (-2 to 2)
    /// </summary>
    public double PresencePenalty { get; }

    /// <summary>
    /// Gets the frequency penalty (-2 to 2)
    /// </summary>
    public double FrequencyPenalty { get; }

    /// <summary>
    /// Gets the stop sequences
    /// </summary>
    public List<string> StopSequences { get; }

    /// <summary>
    /// Gets additional model-specific parameters
    /// </summary>
    public Dictionary<string, object> AdditionalParameters { get; }

    /// <summary>
    /// Initializes a new instance of the ModelParameters class
    /// </summary>
    /// <param name="temperature">The temperature setting</param>
    /// <param name="topP">The top-P setting</param>
    /// <param name="topK">The top-K setting</param>
    /// <param name="maxTokens">The maximum tokens</param>
    /// <param name="presencePenalty">The presence penalty</param>
    /// <param name="frequencyPenalty">The frequency penalty</param>
    /// <param name="stopSequences">The stop sequences</param>
    /// <param name="additionalParameters">Additional parameters</param>
    public ModelParameters(
        double temperature = 0.7,
        double topP = 1.0,
        int? topK = null,
        int? maxTokens = null,
        double presencePenalty = 0.0,
        double frequencyPenalty = 0.0,
        List<string>? stopSequences = null,
        Dictionary<string, object>? additionalParameters = null)
    {
        Temperature = Math.Max(0, Math.Min(2, temperature));
        TopP = Math.Max(0, Math.Min(1, topP));
        TopK = topK.HasValue ? Math.Max(1, topK.Value) : null;
        MaxTokens = maxTokens.HasValue ? Math.Max(1, maxTokens.Value) : null;
        PresencePenalty = Math.Max(-2, Math.Min(2, presencePenalty));
        FrequencyPenalty = Math.Max(-2, Math.Min(2, frequencyPenalty));
        StopSequences = stopSequences ?? new List<string>();
        AdditionalParameters = additionalParameters ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Validates the parameters
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        if (Temperature < 0 || Temperature > 2)
            result.AddError("Temperature", "Temperature must be between 0 and 2");

        if (TopP < 0 || TopP > 1)
            result.AddError("TopP", "TopP must be between 0 and 1");

        if (TopK.HasValue && TopK.Value < 1)
            result.AddError("TopK", "TopK must be greater than 0");

        if (MaxTokens.HasValue && MaxTokens.Value < 1)
            result.AddError("MaxTokens", "MaxTokens must be greater than 0");

        if (PresencePenalty < -2 || PresencePenalty > 2)
            result.AddError("PresencePenalty", "PresencePenalty must be between -2 and 2");

        if (FrequencyPenalty < -2 || FrequencyPenalty > 2)
            result.AddError("FrequencyPenalty", "FrequencyPenalty must be between -2 and 2");

        return result;
    }

    /// <summary>
    /// Merges these parameters with override parameters
    /// </summary>
    /// <param name="overrides">The override parameters</param>
    /// <returns>New merged parameters</returns>
    public ModelParameters MergeWith(ModelParameters overrides)
    {
        if (overrides == null)
            return this;

        var mergedAdditionalParams = new Dictionary<string, object>(AdditionalParameters);
        foreach (var param in overrides.AdditionalParameters)
        {
            mergedAdditionalParams[param.Key] = param.Value;
        }

        var mergedStopSequences = new List<string>(StopSequences);
        foreach (var stop in overrides.StopSequences)
        {
            if (!mergedStopSequences.Contains(stop))
                mergedStopSequences.Add(stop);
        }

        return new ModelParameters(
            overrides.Temperature != 0.7 ? overrides.Temperature : Temperature,
            overrides.TopP != 1.0 ? overrides.TopP : TopP,
            overrides.TopK ?? TopK,
            overrides.MaxTokens ?? MaxTokens,
            overrides.PresencePenalty != 0.0 ? overrides.PresencePenalty : PresencePenalty,
            overrides.FrequencyPenalty != 0.0 ? overrides.FrequencyPenalty : FrequencyPenalty,
            mergedStopSequences,
            mergedAdditionalParams);
    }

    /// <summary>
    /// Converts to a dictionary for API calls
    /// </summary>
    /// <returns>Dictionary representation</returns>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            ["temperature"] = Temperature,
            ["top_p"] = TopP,
            ["presence_penalty"] = PresencePenalty,
            ["frequency_penalty"] = FrequencyPenalty
        };

        if (TopK.HasValue)
            dict["top_k"] = TopK.Value;

        if (MaxTokens.HasValue)
            dict["max_tokens"] = MaxTokens.Value;

        if (StopSequences.Any())
            dict["stop"] = StopSequences;

        foreach (var param in AdditionalParameters)
        {
            dict[param.Key] = param.Value;
        }

        return dict;
    }

    /// <summary>
    /// Creates default parameters
    /// </summary>
    /// <returns>Default model parameters</returns>
    public static ModelParameters Default()
    {
        return new ModelParameters();
    }

    /// <summary>
    /// Creates parameters optimized for creative tasks
    /// </summary>
    /// <returns>Creative model parameters</returns>
    public static ModelParameters Creative()
    {
        return new ModelParameters(
            temperature: 1.2,
            topP: 0.9,
            presencePenalty: 0.1,
            frequencyPenalty: 0.1);
    }

    /// <summary>
    /// Creates parameters optimized for factual tasks
    /// </summary>
    /// <returns>Factual model parameters</returns>
    public static ModelParameters Factual()
    {
        return new ModelParameters(
            temperature: 0.2,
            topP: 0.8,
            presencePenalty: 0.0,
            frequencyPenalty: 0.0);
    }

    /// <summary>
    /// Creates parameters optimized for coding tasks
    /// </summary>
    /// <returns>Coding model parameters</returns>
    public static ModelParameters Coding()
    {
        return new ModelParameters(
            temperature: 0.1,
            topP: 0.95,
            presencePenalty: 0.0,
            frequencyPenalty: 0.1,
            stopSequences: new List<string> { "\n\n", "```", "# End" });
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Temperature;
        yield return TopP;
        
        if (TopK.HasValue)
            yield return TopK.Value;
        
        if (MaxTokens.HasValue)
            yield return MaxTokens.Value;
        
        yield return PresencePenalty;
        yield return FrequencyPenalty;
        
        foreach (var stop in StopSequences.OrderBy(x => x))
        {
            yield return stop;
        }
        
        foreach (var param in AdditionalParameters.OrderBy(x => x.Key))
        {
            yield return param.Key;
            yield return param.Value;
        }
    }
}

/// <summary>
/// Quality settings value object
/// </summary>
public class QualitySettings : ValueObject
{
    /// <summary>
    /// Gets the response quality threshold (0-1)
    /// </summary>
    public double ResponseQualityThreshold { get; }

    /// <summary>
    /// Gets the latency threshold in milliseconds
    /// </summary>
    public int LatencyThresholdMs { get; }

    /// <summary>
    /// Gets the error rate threshold (0-1)
    /// </summary>
    public double ErrorRateThreshold { get; }

    /// <summary>
    /// Gets whether quality monitoring is enabled
    /// </summary>
    public bool EnableQualityMonitoring { get; }

    /// <summary>
    /// Gets custom quality thresholds
    /// </summary>
    public Dictionary<string, double> CustomThresholds { get; }

    /// <summary>
    /// Initializes a new instance of the QualitySettings class
    /// </summary>
    /// <param name="responseQualityThreshold">The response quality threshold</param>
    /// <param name="latencyThresholdMs">The latency threshold in milliseconds</param>
    /// <param name="errorRateThreshold">The error rate threshold</param>
    /// <param name="enableQualityMonitoring">Whether to enable quality monitoring</param>
    /// <param name="customThresholds">Custom quality thresholds</param>
    public QualitySettings(
        double responseQualityThreshold = 0.8,
        int latencyThresholdMs = 5000,
        double errorRateThreshold = 0.05,
        bool enableQualityMonitoring = true,
        Dictionary<string, double>? customThresholds = null)
    {
        ResponseQualityThreshold = Math.Max(0, Math.Min(1, responseQualityThreshold));
        LatencyThresholdMs = Math.Max(100, latencyThresholdMs);
        ErrorRateThreshold = Math.Max(0, Math.Min(1, errorRateThreshold));
        EnableQualityMonitoring = enableQualityMonitoring;
        CustomThresholds = customThresholds ?? new Dictionary<string, double>();
    }

    /// <summary>
    /// Checks if the quality metrics meet the standards
    /// </summary>
    /// <param name="metrics">The quality metrics to check</param>
    /// <returns>True if within standards, false otherwise</returns>
    public bool IsWithinQualityStandards(QualityMetrics metrics)
    {
        if (!EnableQualityMonitoring)
            return true;

        if (metrics.QualityScore < ResponseQualityThreshold)
            return false;

        if (metrics.ResponseTimeMs > LatencyThresholdMs)
            return false;

        if (metrics.ErrorRate > ErrorRateThreshold)
            return false;

        // Check custom thresholds
        foreach (var threshold in CustomThresholds)
        {
            if (metrics.CustomMetrics.ContainsKey(threshold.Key))
            {
                var value = metrics.CustomMetrics[threshold.Key];
                if (value < threshold.Value)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the quality score for given metrics
    /// </summary>
    /// <param name="metrics">The quality metrics</param>
    /// <returns>A composite quality score (0-100)</returns>
    public double GetQualityScore(QualityMetrics metrics)
    {
        if (!EnableQualityMonitoring)
            return 100;

        double score = 0;

        // Quality score component (40%)
        score += (metrics.QualityScore / ResponseQualityThreshold) * 40;

        // Latency component (30%)
        var latencyScore = Math.Max(0, 1.0 - (double)metrics.ResponseTimeMs / LatencyThresholdMs);
        score += latencyScore * 30;

        // Error rate component (30%)
        var errorScore = Math.Max(0, 1.0 - (metrics.ErrorRate / ErrorRateThreshold));
        score += errorScore * 30;

        return Math.Min(100, Math.Max(0, score));
    }

    /// <summary>
    /// Creates default quality settings
    /// </summary>
    /// <returns>Default quality settings</returns>
    public static QualitySettings Default()
    {
        return new QualitySettings();
    }

    /// <summary>
    /// Creates strict quality settings
    /// </summary>
    /// <returns>Strict quality settings</returns>
    public static QualitySettings Strict()
    {
        return new QualitySettings(
            responseQualityThreshold: 0.9,
            latencyThresholdMs: 3000,
            errorRateThreshold: 0.02);
    }

    /// <summary>
    /// Creates relaxed quality settings
    /// </summary>
    /// <returns>Relaxed quality settings</returns>
    public static QualitySettings Relaxed()
    {
        return new QualitySettings(
            responseQualityThreshold: 0.6,
            latencyThresholdMs: 10000,
            errorRateThreshold: 0.1);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ResponseQualityThreshold;
        yield return LatencyThresholdMs;
        yield return ErrorRateThreshold;
        yield return EnableQualityMonitoring;
        
        foreach (var threshold in CustomThresholds.OrderBy(x => x.Key))
        {
            yield return threshold.Key;
            yield return threshold.Value;
        }
    }
}

/// <summary>
/// Quality metrics class
/// </summary>
public class QualityMetrics
{
    /// <summary>
    /// Gets the quality score (0-1)
    /// </summary>
    public double QualityScore { get; }

    /// <summary>
    /// Gets the response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; }

    /// <summary>
    /// Gets the error rate (0-1)
    /// </summary>
    public double ErrorRate { get; }

    /// <summary>
    /// Gets custom metrics
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; }

    /// <summary>
    /// Initializes a new instance of the QualityMetrics class
    /// </summary>
    /// <param name="qualityScore">The quality score</param>
    /// <param name="responseTimeMs">The response time in milliseconds</param>
    /// <param name="errorRate">The error rate</param>
    /// <param name="customMetrics">Custom metrics</param>
    public QualityMetrics(
        double qualityScore,
        int responseTimeMs,
        double errorRate,
        Dictionary<string, double>? customMetrics = null)
    {
        QualityScore = Math.Max(0, Math.Min(1, qualityScore));
        ResponseTimeMs = Math.Max(0, responseTimeMs);
        ErrorRate = Math.Max(0, Math.Min(1, errorRate));
        CustomMetrics = customMetrics ?? new Dictionary<string, double>();
    }
}