using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Performance metrics value object
/// </summary>
public class PerformanceMetrics : ValueObject
{
    /// <summary>
    /// Gets the average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; }

    /// <summary>
    /// Gets the throughput per second
    /// </summary>
    public double ThroughputPerSecond { get; }

    /// <summary>
    /// Gets the error rate (0-1)
    /// </summary>
    public double ErrorRate { get; }

    /// <summary>
    /// Gets the memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; }

    /// <summary>
    /// Gets the CPU usage percentage (0-100)
    /// </summary>
    public double CpuUsagePercent { get; }

    /// <summary>
    /// Gets the last measured time
    /// </summary>
    public DateTime LastMeasuredTime { get; }

    /// <summary>
    /// Initializes a new instance of the PerformanceMetrics class
    /// </summary>
    /// <param name="averageResponseTime">The average response time in milliseconds</param>
    /// <param name="throughputPerSecond">The throughput per second</param>
    /// <param name="errorRate">The error rate (0-1)</param>
    /// <param name="memoryUsageBytes">The memory usage in bytes</param>
    /// <param name="cpuUsagePercent">The CPU usage percentage (0-100)</param>
    /// <param name="lastMeasuredTime">The last measured time</param>
    public PerformanceMetrics(
        double averageResponseTime,
        double throughputPerSecond,
        double errorRate,
        long memoryUsageBytes,
        double cpuUsagePercent,
        DateTime? lastMeasuredTime = null)
    {
        if (averageResponseTime < 0)
            throw new ArgumentException("Average response time cannot be negative", nameof(averageResponseTime));

        if (throughputPerSecond < 0)
            throw new ArgumentException("Throughput cannot be negative", nameof(throughputPerSecond));

        if (errorRate < 0 || errorRate > 1)
            throw new ArgumentException("Error rate must be between 0 and 1", nameof(errorRate));

        if (memoryUsageBytes < 0)
            throw new ArgumentException("Memory usage cannot be negative", nameof(memoryUsageBytes));

        if (cpuUsagePercent < 0 || cpuUsagePercent > 100)
            throw new ArgumentException("CPU usage must be between 0 and 100", nameof(cpuUsagePercent));

        AverageResponseTime = averageResponseTime;
        ThroughputPerSecond = throughputPerSecond;
        ErrorRate = errorRate;
        MemoryUsageBytes = memoryUsageBytes;
        CpuUsagePercent = cpuUsagePercent;
        LastMeasuredTime = lastMeasuredTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the performance metrics with new measurement data
    /// </summary>
    /// <param name="responseTime">The new response time</param>
    /// <param name="isError">Whether the operation resulted in an error</param>
    /// <param name="memoryUsage">The current memory usage</param>
    /// <param name="cpuUsage">The current CPU usage</param>
    /// <returns>New performance metrics with updated values</returns>
    public PerformanceMetrics UpdateMetrics(
        double responseTime,
        bool isError,
        long? memoryUsage = null,
        double? cpuUsage = null)
    {
        // Calculate new average response time (simple moving average)
        var newAverageResponseTime = (AverageResponseTime + responseTime) / 2;

        // Update throughput (simplified calculation)
        var timeSinceLastMeasurement = DateTime.UtcNow - LastMeasuredTime;
        var newThroughput = timeSinceLastMeasurement.TotalSeconds > 0
            ? 1.0 / timeSinceLastMeasurement.TotalSeconds
            : ThroughputPerSecond;

        // Calculate new error rate (exponential moving average)
        var alpha = 0.1; // Smoothing factor
        var newErrorRate = ErrorRate * (1 - alpha) + (isError ? 1.0 : 0.0) * alpha;

        return new PerformanceMetrics(
            newAverageResponseTime,
            newThroughput,
            newErrorRate,
            memoryUsage ?? MemoryUsageBytes,
            cpuUsage ?? CpuUsagePercent,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Checks if the metrics are within specified thresholds
    /// </summary>
    /// <param name="thresholds">The performance thresholds</param>
    /// <returns>True if within thresholds, false otherwise</returns>
    public bool IsWithinThresholds(PerformanceThresholds thresholds)
    {
        if (thresholds == null)
            return true;

        return AverageResponseTime <= thresholds.MaxAverageResponseTime &&
               ErrorRate <= thresholds.MaxErrorRate &&
               MemoryUsageBytes <= thresholds.MaxMemoryUsageBytes &&
               CpuUsagePercent <= thresholds.MaxCpuUsagePercent &&
               ThroughputPerSecond >= thresholds.MinThroughputPerSecond;
    }

    /// <summary>
    /// Gets the overall health score based on metrics (0-100)
    /// </summary>
    /// <returns>The health score</returns>
    public double GetHealthScore()
    {
        var responseTimeScore = Math.Max(0, 100 - (AverageResponseTime / 10)); // Penalize high response times
        var errorRateScore = (1 - ErrorRate) * 100; // Convert error rate to score
        var memoryScore = Math.Max(0, 100 - (MemoryUsageBytes / (1024 * 1024 * 10))); // Penalize high memory usage (>10MB penalty starts)
        var cpuScore = Math.Max(0, 100 - CpuUsagePercent); // Penalize high CPU usage

        return (responseTimeScore + errorRateScore + memoryScore + cpuScore) / 4;
    }

    /// <summary>
    /// Gets a performance summary string
    /// </summary>
    /// <returns>The performance summary</returns>
    public string GetSummary()
    {
        return $"Avg Response: {AverageResponseTime:F2}ms, " +
               $"Throughput: {ThroughputPerSecond:F2}/s, " +
               $"Error Rate: {ErrorRate:P2}, " +
               $"Memory: {MemoryUsageBytes / (1024 * 1024):F1}MB, " +
               $"CPU: {CpuUsagePercent:F1}%";
    }

    /// <summary>
    /// Creates default performance metrics
    /// </summary>
    /// <returns>Default performance metrics</returns>
    public static PerformanceMetrics Default()
    {
        return new PerformanceMetrics(
            averageResponseTime: 0,
            throughputPerSecond: 0,
            errorRate: 0,
            memoryUsageBytes: 0,
            cpuUsagePercent: 0);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AverageResponseTime;
        yield return ThroughputPerSecond;
        yield return ErrorRate;
        yield return MemoryUsageBytes;
        yield return CpuUsagePercent;
        yield return LastMeasuredTime;
    }
}