using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Resource usage value object
/// </summary>
public class ResourceUsage : ValueObject
{
    /// <summary>
    /// Gets the CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; }

    /// <summary>
    /// Gets the memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; }

    /// <summary>
    /// Gets the disk I/O in bytes
    /// </summary>
    public long DiskIOBytes { get; }

    /// <summary>
    /// Gets the network I/O in bytes
    /// </summary>
    public long NetworkIOBytes { get; }

    /// <summary>
    /// Gets custom metrics
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; }

    /// <summary>
    /// Initializes a new instance of the ResourceUsage class
    /// </summary>
    /// <param name="cpuUsagePercent">The CPU usage percentage</param>
    /// <param name="memoryUsageBytes">The memory usage in bytes</param>
    /// <param name="diskIOBytes">The disk I/O in bytes</param>
    /// <param name="networkIOBytes">The network I/O in bytes</param>
    /// <param name="customMetrics">Custom metrics</param>
    public ResourceUsage(
        double cpuUsagePercent,
        long memoryUsageBytes,
        long diskIOBytes,
        long networkIOBytes,
        Dictionary<string, double>? customMetrics = null)
    {
        CpuUsagePercent = cpuUsagePercent;
        MemoryUsageBytes = memoryUsageBytes;
        DiskIOBytes = diskIOBytes;
        NetworkIOBytes = networkIOBytes;
        CustomMetrics = customMetrics ?? new Dictionary<string, double>();
    }

    /// <summary>
    /// Checks if resource usage is within specified limits
    /// </summary>
    /// <param name="limits">The resource limits</param>
    /// <returns>True if within limits, false otherwise</returns>
    public bool IsWithinLimits(ResourceLimits limits)
    {
        return CpuUsagePercent <= limits.MaxCpuUsagePercent &&
               MemoryUsageBytes <= limits.MaxMemoryUsageBytes &&
               DiskIOBytes <= limits.MaxDiskIOBytes &&
               NetworkIOBytes <= limits.MaxNetworkIOBytes;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return CpuUsagePercent;
        yield return MemoryUsageBytes;
        yield return DiskIOBytes;
        yield return NetworkIOBytes;

        foreach (var metric in CustomMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}

/// <summary>
/// Resource limits value object
/// </summary>
public class ResourceLimits : ValueObject
{
    /// <summary>
    /// Gets the maximum CPU usage percentage
    /// </summary>
    public double MaxCpuUsagePercent { get; }

    /// <summary>
    /// Gets the maximum memory usage in bytes
    /// </summary>
    public long MaxMemoryUsageBytes { get; }

    /// <summary>
    /// Gets the maximum disk I/O in bytes
    /// </summary>
    public long MaxDiskIOBytes { get; }

    /// <summary>
    /// Gets the maximum network I/O in bytes
    /// </summary>
    public long MaxNetworkIOBytes { get; }

    /// <summary>
    /// Initializes a new instance of the ResourceLimits class
    /// </summary>
    /// <param name="maxCpuUsagePercent">The maximum CPU usage percentage</param>
    /// <param name="maxMemoryUsageBytes">The maximum memory usage in bytes</param>
    /// <param name="maxDiskIOBytes">The maximum disk I/O in bytes</param>
    /// <param name="maxNetworkIOBytes">The maximum network I/O in bytes</param>
    public ResourceLimits(
        double maxCpuUsagePercent,
        long maxMemoryUsageBytes,
        long maxDiskIOBytes,
        long maxNetworkIOBytes)
    {
        MaxCpuUsagePercent = maxCpuUsagePercent;
        MaxMemoryUsageBytes = maxMemoryUsageBytes;
        MaxDiskIOBytes = maxDiskIOBytes;
        MaxNetworkIOBytes = maxNetworkIOBytes;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxCpuUsagePercent;
        yield return MaxMemoryUsageBytes;
        yield return MaxDiskIOBytes;
        yield return MaxNetworkIOBytes;
    }
}