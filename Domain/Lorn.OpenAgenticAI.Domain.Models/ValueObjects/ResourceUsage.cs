using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 资源使用情况值对象
/// </summary>
public class ResourceUsage : ValueObject
{
    public double CpuUsagePercent { get; private set; }
    public long MemoryUsageBytes { get; private set; }
    public long DiskIOBytes { get; private set; }
    public long NetworkIOBytes { get; private set; }
    public Dictionary<string, double> CustomMetrics { get; private set; } = new();

    // EF Core 需要的无参数构造函数
    private ResourceUsage()
    {
        CpuUsagePercent = 0;
        MemoryUsageBytes = 0;
        DiskIOBytes = 0;
        NetworkIOBytes = 0;
        CustomMetrics = new Dictionary<string, double>();
    }

    public ResourceUsage(
        double cpuUsagePercent,
        long memoryUsageBytes,
        long diskIOBytes = 0,
        long networkIOBytes = 0,
        Dictionary<string, double>? customMetrics = null)
    {
        CpuUsagePercent = cpuUsagePercent >= 0 ? cpuUsagePercent : 0;
        MemoryUsageBytes = memoryUsageBytes >= 0 ? memoryUsageBytes : 0;
        DiskIOBytes = diskIOBytes >= 0 ? diskIOBytes : 0;
        NetworkIOBytes = networkIOBytes >= 0 ? networkIOBytes : 0;
        CustomMetrics = customMetrics ?? new Dictionary<string, double>();
    }

    /// <summary>
    /// 检查资源使用是否在限制范围内
    /// </summary>
    public bool IsWithinLimits(ResourceLimits limits)
    {
        return CpuUsagePercent <= limits.MaxCpuPercent &&
               MemoryUsageBytes <= limits.MaxMemoryBytes &&
               DiskIOBytes <= limits.MaxDiskIOBytes &&
               NetworkIOBytes <= limits.MaxNetworkIOBytes;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return CpuUsagePercent;
        yield return MemoryUsageBytes;
        yield return DiskIOBytes;
        yield return NetworkIOBytes;

        foreach (var kvp in CustomMetrics)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}

/// <summary>
/// 资源限制值对象
/// </summary>
public class ResourceLimits : ValueObject
{
    public double MaxCpuPercent { get; private set; }
    public long MaxMemoryBytes { get; private set; }
    public long MaxDiskIOBytes { get; private set; }
    public long MaxNetworkIOBytes { get; private set; }

    public ResourceLimits(
        double maxCpuPercent = 100,
        long maxMemoryBytes = long.MaxValue,
        long maxDiskIOBytes = long.MaxValue,
        long maxNetworkIOBytes = long.MaxValue)
    {
        MaxCpuPercent = maxCpuPercent > 0 ? maxCpuPercent : 100;
        MaxMemoryBytes = maxMemoryBytes > 0 ? maxMemoryBytes : long.MaxValue;
        MaxDiskIOBytes = maxDiskIOBytes > 0 ? maxDiskIOBytes : long.MaxValue;
        MaxNetworkIOBytes = maxNetworkIOBytes > 0 ? maxNetworkIOBytes : long.MaxValue;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxCpuPercent;
        yield return MaxMemoryBytes;
        yield return MaxDiskIOBytes;
        yield return MaxNetworkIOBytes;
    }
}