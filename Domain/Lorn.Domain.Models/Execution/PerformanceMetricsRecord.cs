using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;

namespace Lorn.Domain.Models.Execution;

/// <summary>
/// Performance metrics record entity
/// </summary>
public class PerformanceMetricsRecord : BaseEntity
{
    /// <summary>
    /// Gets the metric identifier
    /// </summary>
    public Guid MetricId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the execution identifier
    /// </summary>
    public Guid? ExecutionId { get; private set; }

    /// <summary>
    /// Gets the metric timestamp
    /// </summary>
    public DateTime MetricTimestamp { get; private set; }

    /// <summary>
    /// Gets the metric type
    /// </summary>
    public string MetricType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the metric name
    /// </summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the metric value
    /// </summary>
    public double MetricValue { get; private set; }

    /// <summary>
    /// Gets the metric unit
    /// </summary>
    public string MetricUnit { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tags
    /// </summary>
    public Dictionary<string, string> Tags { get; private set; } = new();

    /// <summary>
    /// Gets the context
    /// </summary>
    public Dictionary<string, object> Context { get; private set; } = new();

    /// <summary>
    /// Gets the aggregation period
    /// </summary>
    public string? AggregationPeriod { get; private set; }

    /// <summary>
    /// Gets the task execution
    /// </summary>
    public TaskExecutionHistory? Execution { get; private set; }

    /// <summary>
    /// Initializes a new instance of the PerformanceMetricsRecord class
    /// </summary>
    /// <param name="metricId">The metric identifier</param>
    /// <param name="metricType">The metric type</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="metricValue">The metric value</param>
    /// <param name="metricUnit">The metric unit</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="executionId">The execution identifier</param>
    public PerformanceMetricsRecord(
        Guid metricId,
        string metricType,
        string metricName,
        double metricValue,
        string metricUnit,
        Guid? userId = null,
        Guid? executionId = null)
    {
        MetricId = metricId;
        MetricType = metricType ?? throw new ArgumentNullException(nameof(metricType));
        MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
        MetricValue = metricValue;
        MetricUnit = metricUnit ?? throw new ArgumentNullException(nameof(metricUnit));
        UserId = userId;
        ExecutionId = executionId;
        MetricTimestamp = DateTime.UtcNow;

        Id = metricId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private PerformanceMetricsRecord() { }

    /// <summary>
    /// Adds a tag
    /// </summary>
    /// <param name="key">The tag key</param>
    /// <param name="value">The tag value</param>
    public void AddTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        Tags[key] = value ?? string.Empty;
        UpdateVersion();
    }

    /// <summary>
    /// Removes a tag
    /// </summary>
    /// <param name="key">The tag key</param>
    public void RemoveTag(string key)
    {
        if (Tags.Remove(key))
            UpdateVersion();
    }

    /// <summary>
    /// Sets context data
    /// </summary>
    /// <param name="key">The context key</param>
    /// <param name="value">The context value</param>
    public void SetContext(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        Context[key] = value;
        UpdateVersion();
    }

    /// <summary>
    /// Gets context data
    /// </summary>
    /// <param name="key">The context key</param>
    /// <returns>The context value or null if not found</returns>
    public object? GetContext(string key)
    {
        return Context.ContainsKey(key) ? Context[key] : null;
    }

    /// <summary>
    /// Sets the aggregation period
    /// </summary>
    /// <param name="aggregationPeriod">The aggregation period</param>
    public void SetAggregationPeriod(string aggregationPeriod)
    {
        AggregationPeriod = aggregationPeriod;
        UpdateVersion();
    }

    /// <summary>
    /// Gets the metric type as an enum
    /// </summary>
    /// <returns>The metric type</returns>
    public MetricType GetMetricType()
    {
        return MetricType.ToLower() switch
        {
            "executionperformance" => Enumerations.MetricType.ExecutionPerformance,
            "resourceusage" => Enumerations.MetricType.ResourceUsage,
            "businessmetric" => Enumerations.MetricType.BusinessMetric,
            "systemhealth" => Enumerations.MetricType.SystemHealth,
            "userbehavior" => Enumerations.MetricType.UserBehavior,
            _ => Enumerations.MetricType.BusinessMetric
        };
    }

    /// <summary>
    /// Checks if the metric is within normal range
    /// </summary>
    /// <param name="minValue">The minimum normal value</param>
    /// <param name="maxValue">The maximum normal value</param>
    /// <returns>True if within range, false otherwise</returns>
    public bool IsWithinNormalRange(double minValue, double maxValue)
    {
        return MetricValue >= minValue && MetricValue <= maxValue;
    }

    /// <summary>
    /// Calculates the percentage change from a baseline
    /// </summary>
    /// <param name="baseline">The baseline value</param>
    /// <returns>The percentage change</returns>
    public double CalculatePercentageChange(double baseline)
    {
        if (baseline == 0)
            return 0;

        return ((MetricValue - baseline) / baseline) * 100;
    }
}

