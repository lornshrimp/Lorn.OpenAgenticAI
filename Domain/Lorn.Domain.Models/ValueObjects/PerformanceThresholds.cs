using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Performance thresholds value object for monitoring and alerting
/// </summary>
public class PerformanceThresholds : ValueObject
{
    /// <summary>
    /// Gets the maximum acceptable average response time in milliseconds
    /// </summary>
    public double MaxAverageResponseTime { get; }

    /// <summary>
    /// Gets the maximum acceptable throughput per second
    /// </summary>
    public double MinThroughputPerSecond { get; }

    /// <summary>
    /// Gets the maximum acceptable error rate (0-1)
    /// </summary>
    public double MaxErrorRate { get; }

    /// <summary>
    /// Gets the maximum acceptable memory usage in bytes
    /// </summary>
    public long MaxMemoryUsageBytes { get; }

    /// <summary>
    /// Gets the maximum acceptable CPU usage percentage (0-100)
    /// </summary>
    public double MaxCpuUsagePercent { get; }

    /// <summary>
    /// Gets custom performance thresholds
    /// </summary>
    public Dictionary<string, double> CustomThresholds { get; }

    /// <summary>
    /// Initializes a new instance of the PerformanceThresholds class
    /// </summary>
    /// <param name="maxAverageResponseTime">Maximum acceptable average response time in milliseconds</param>
    /// <param name="minThroughputPerSecond">Minimum acceptable throughput per second</param>
    /// <param name="maxErrorRate">Maximum acceptable error rate (0-1)</param>
    /// <param name="maxMemoryUsageBytes">Maximum acceptable memory usage in bytes</param>
    /// <param name="maxCpuUsagePercent">Maximum acceptable CPU usage percentage (0-100)</param>
    /// <param name="customThresholds">Custom performance thresholds</param>
    public PerformanceThresholds(
        double maxAverageResponseTime = 5000.0, // 5 seconds
        double minThroughputPerSecond = 1.0,
        double maxErrorRate = 0.05, // 5%
        long maxMemoryUsageBytes = 1024 * 1024 * 512, // 512 MB
        double maxCpuUsagePercent = 80.0, // 80%
        Dictionary<string, double>? customThresholds = null)
    {
        MaxAverageResponseTime = Math.Max(0, maxAverageResponseTime);
        MinThroughputPerSecond = Math.Max(0, minThroughputPerSecond);
        MaxErrorRate = Math.Max(0, Math.Min(1, maxErrorRate));
        MaxMemoryUsageBytes = Math.Max(0, maxMemoryUsageBytes);
        MaxCpuUsagePercent = Math.Max(0, Math.Min(100, maxCpuUsagePercent));
        CustomThresholds = customThresholds ?? new Dictionary<string, double>();
    }

    /// <summary>
    /// Checks if the given metrics are within acceptable thresholds
    /// </summary>
    /// <param name="metrics">The performance metrics to check</param>
    /// <returns>True if within thresholds, false otherwise</returns>
    public bool IsWithinThresholds(PerformanceMetrics metrics)
    {
        if (metrics.AverageResponseTime > MaxAverageResponseTime)
            return false;

        if (metrics.ThroughputPerSecond < MinThroughputPerSecond)
            return false;

        if (metrics.ErrorRate > MaxErrorRate)
            return false;

        if (metrics.MemoryUsageBytes > MaxMemoryUsageBytes)
            return false;

        if (metrics.CpuUsagePercent > MaxCpuUsagePercent)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the severity level based on how far metrics are from thresholds
    /// </summary>
    /// <param name="metrics">The performance metrics to evaluate</param>
    /// <returns>Severity level (0-4, where 0 is within thresholds and 4 is critical)</returns>
    public int GetSeverityLevel(PerformanceMetrics metrics)
    {
        if (IsWithinThresholds(metrics))
            return 0; // Normal

        var violations = 0;
        var criticalViolations = 0;

        // Check response time
        if (metrics.AverageResponseTime > MaxAverageResponseTime)
        {
            violations++;
            if (metrics.AverageResponseTime > MaxAverageResponseTime * 2)
                criticalViolations++;
        }

        // Check throughput
        if (metrics.ThroughputPerSecond < MinThroughputPerSecond)
        {
            violations++;
            if (metrics.ThroughputPerSecond < MinThroughputPerSecond * 0.5)
                criticalViolations++;
        }

        // Check error rate
        if (metrics.ErrorRate > MaxErrorRate)
        {
            violations++;
            if (metrics.ErrorRate > MaxErrorRate * 2)
                criticalViolations++;
        }

        // Check memory usage
        if (metrics.MemoryUsageBytes > MaxMemoryUsageBytes)
        {
            violations++;
            if (metrics.MemoryUsageBytes > MaxMemoryUsageBytes * 1.5)
                criticalViolations++;
        }

        // Check CPU usage
        if (metrics.CpuUsagePercent > MaxCpuUsagePercent)
        {
            violations++;
            if (metrics.CpuUsagePercent > Math.Min(MaxCpuUsagePercent * 1.2, 95))
                criticalViolations++;
        }

        return criticalViolations > 0 ? 4 : Math.Min(violations, 3);
    }

    /// <summary>
    /// Gets the health score based on how well metrics align with thresholds
    /// </summary>
    /// <param name="metrics">The performance metrics to evaluate</param>
    /// <returns>Health score (0-100, where 100 is perfect health)</returns>
    public double GetHealthScore(PerformanceMetrics metrics)
    {
        double score = 100.0;

        // Response time score (25%)
        var responseScore = Math.Max(0, 1.0 - (metrics.AverageResponseTime / MaxAverageResponseTime));
        score *= 0.75 + (responseScore * 0.25);

        // Throughput score (20%)
        var throughputScore = Math.Min(1.0, metrics.ThroughputPerSecond / MinThroughputPerSecond);
        score *= 0.8 + (throughputScore * 0.2);

        // Error rate score (25%)
        var errorScore = Math.Max(0, 1.0 - (metrics.ErrorRate / MaxErrorRate));
        score *= 0.75 + (errorScore * 0.25);

        // Memory usage score (15%)
        var memoryScore = Math.Max(0, 1.0 - ((double)metrics.MemoryUsageBytes / MaxMemoryUsageBytes));
        score *= 0.85 + (memoryScore * 0.15);

        // CPU usage score (15%)
        var cpuScore = Math.Max(0, 1.0 - (metrics.CpuUsagePercent / MaxCpuUsagePercent));
        score *= 0.85 + (cpuScore * 0.15);

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Creates default performance thresholds
    /// </summary>
    /// <returns>Default performance thresholds</returns>
    public static PerformanceThresholds Default()
    {
        return new PerformanceThresholds();
    }

    /// <summary>
    /// Creates strict performance thresholds for high-performance requirements
    /// </summary>
    /// <returns>Strict performance thresholds</returns>
    public static PerformanceThresholds Strict()
    {
        return new PerformanceThresholds(
            maxAverageResponseTime: 2000.0, // 2 seconds
            minThroughputPerSecond: 5.0,
            maxErrorRate: 0.01, // 1%
            maxMemoryUsageBytes: 1024 * 1024 * 256, // 256 MB
            maxCpuUsagePercent: 60.0 // 60%
        );
    }

    /// <summary>
    /// Creates relaxed performance thresholds for non-critical applications
    /// </summary>
    /// <returns>Relaxed performance thresholds</returns>
    public static PerformanceThresholds Relaxed()
    {
        return new PerformanceThresholds(
            maxAverageResponseTime: 10000.0, // 10 seconds
            minThroughputPerSecond: 0.5,
            maxErrorRate: 0.1, // 10%
            maxMemoryUsageBytes: 1024 * 1024 * 1024, // 1 GB
            maxCpuUsagePercent: 90.0 // 90%
        );
    }

    /// <summary>
    /// Creates performance thresholds for agent-specific requirements
    /// </summary>
    /// <param name="agentType">The agent type to create thresholds for</param>
    /// <returns>Agent-specific performance thresholds</returns>
    public static PerformanceThresholds ForAgentType(string agentType)
    {
        return agentType.ToLower() switch
        {
            "officeautomation" => new PerformanceThresholds(
                maxAverageResponseTime: 8000.0, // Office operations can be slow
                minThroughputPerSecond: 0.5,
                maxErrorRate: 0.02,
                maxMemoryUsageBytes: 1024 * 1024 * 512, // 512 MB
                maxCpuUsagePercent: 70.0
            ),
            "webautomation" => new PerformanceThresholds(
                maxAverageResponseTime: 15000.0, // Web operations depend on network
                minThroughputPerSecond: 1.0,
                maxErrorRate: 0.05,
                maxMemoryUsageBytes: 1024 * 1024 * 768, // 768 MB (browser overhead)
                maxCpuUsagePercent: 80.0
            ),
            "dataprocessing" => new PerformanceThresholds(
                maxAverageResponseTime: 30000.0, // Data processing can take time
                minThroughputPerSecond: 0.1,
                maxErrorRate: 0.01,
                maxMemoryUsageBytes: 1024 * 1024 * 1024, // 1 GB
                maxCpuUsagePercent: 85.0
            ),
            "filesystem" => new PerformanceThresholds(
                maxAverageResponseTime: 5000.0,
                minThroughputPerSecond: 2.0,
                maxErrorRate: 0.02,
                maxMemoryUsageBytes: 1024 * 1024 * 256, // 256 MB
                maxCpuUsagePercent: 60.0
            ),
            _ => Default()
        };
    }

    /// <summary>
    /// Gets threshold violations for the given metrics
    /// </summary>
    /// <param name="metrics">The performance metrics to check</param>
    /// <returns>List of threshold violations</returns>
    public List<ThresholdViolation> GetViolations(PerformanceMetrics metrics)
    {
        var violations = new List<ThresholdViolation>();

        if (metrics.AverageResponseTime > MaxAverageResponseTime)
        {
            violations.Add(new ThresholdViolation(
                "AverageResponseTime",
                metrics.AverageResponseTime,
                MaxAverageResponseTime,
                "Average response time exceeds threshold"));
        }

        if (metrics.ThroughputPerSecond < MinThroughputPerSecond)
        {
            violations.Add(new ThresholdViolation(
                "ThroughputPerSecond",
                metrics.ThroughputPerSecond,
                MinThroughputPerSecond,
                "Throughput is below minimum threshold"));
        }

        if (metrics.ErrorRate > MaxErrorRate)
        {
            violations.Add(new ThresholdViolation(
                "ErrorRate",
                metrics.ErrorRate,
                MaxErrorRate,
                "Error rate exceeds maximum threshold"));
        }

        if (metrics.MemoryUsageBytes > MaxMemoryUsageBytes)
        {
            violations.Add(new ThresholdViolation(
                "MemoryUsageBytes",
                metrics.MemoryUsageBytes,
                MaxMemoryUsageBytes,
                "Memory usage exceeds maximum threshold"));
        }

        if (metrics.CpuUsagePercent > MaxCpuUsagePercent)
        {
            violations.Add(new ThresholdViolation(
                "CpuUsagePercent",
                metrics.CpuUsagePercent,
                MaxCpuUsagePercent,
                "CPU usage exceeds maximum threshold"));
        }

        return violations;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxAverageResponseTime;
        yield return MinThroughputPerSecond;
        yield return MaxErrorRate;
        yield return MaxMemoryUsageBytes;
        yield return MaxCpuUsagePercent;

        foreach (var threshold in CustomThresholds.OrderBy(x => x.Key))
        {
            yield return threshold.Key;
            yield return threshold.Value;
        }
    }
}

/// <summary>
/// Represents a threshold violation
/// </summary>
public class ThresholdViolation
{
    /// <summary>
    /// Gets the metric name that violated the threshold
    /// </summary>
    public string MetricName { get; }

    /// <summary>
    /// Gets the actual value
    /// </summary>
    public double ActualValue { get; }

    /// <summary>
    /// Gets the threshold value
    /// </summary>
    public double ThresholdValue { get; }

    /// <summary>
    /// Gets the violation message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the severity of the violation
    /// </summary>
    public ViolationSeverity Severity { get; }

    /// <summary>
    /// Initializes a new instance of the ThresholdViolation class
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="actualValue">The actual value</param>
    /// <param name="thresholdValue">The threshold value</param>
    /// <param name="message">The violation message</param>
    public ThresholdViolation(string metricName, double actualValue, double thresholdValue, string message)
    {
        MetricName = metricName;
        ActualValue = actualValue;
        ThresholdValue = thresholdValue;
        Message = message;
        Severity = CalculateSeverity(actualValue, thresholdValue);
    }

    private static ViolationSeverity CalculateSeverity(double actualValue, double thresholdValue)
    {
        var ratio = actualValue / thresholdValue;
        return ratio switch
        {
            <= 1.2 => ViolationSeverity.Minor,
            <= 1.5 => ViolationSeverity.Moderate,
            <= 2.0 => ViolationSeverity.Major,
            _ => ViolationSeverity.Critical
        };
    }
}

/// <summary>
/// Threshold violation severity enumeration
/// </summary>
public enum ViolationSeverity
{
    Minor = 1,
    Moderate = 2,
    Major = 3,
    Critical = 4
}