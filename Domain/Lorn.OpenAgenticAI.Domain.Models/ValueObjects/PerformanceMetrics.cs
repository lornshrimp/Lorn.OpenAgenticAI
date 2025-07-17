using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 性能指标值对象
/// </summary>
public class PerformanceMetrics : ValueObject
{
    public double AverageResponseTime { get; private set; }
    public double ThroughputPerSecond { get; private set; }
    public double ErrorRate { get; private set; }
    public long MemoryUsageBytes { get; private set; }
    public double CpuUsagePercent { get; private set; }
    public DateTime LastMeasuredTime { get; private set; }

    public PerformanceMetrics(
        double averageResponseTime = 0.0,
        double throughputPerSecond = 0.0,
        double errorRate = 0.0,
        long memoryUsageBytes = 0,
        double cpuUsagePercent = 0.0)
    {
        AverageResponseTime = averageResponseTime >= 0 ? averageResponseTime : 0.0;
        ThroughputPerSecond = throughputPerSecond >= 0 ? throughputPerSecond : 0.0;
        ErrorRate = errorRate >= 0 && errorRate <= 1 ? errorRate : 0.0;
        MemoryUsageBytes = memoryUsageBytes >= 0 ? memoryUsageBytes : 0;
        CpuUsagePercent = cpuUsagePercent >= 0 && cpuUsagePercent <= 100 ? cpuUsagePercent : 0.0;
        LastMeasuredTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新指标
    /// </summary>
    public PerformanceMetrics UpdateMetrics(double responseTime, bool isError)
    {
        var newErrorRate = isError ? 
            (ErrorRate * 0.9) + (1.0 * 0.1) : 
            (ErrorRate * 0.9) + (0.0 * 0.1);

        var newResponseTime = (AverageResponseTime * 0.9) + (responseTime * 0.1);

        return new PerformanceMetrics(
            newResponseTime,
            ThroughputPerSecond,
            newErrorRate,
            MemoryUsageBytes,
            CpuUsagePercent
        );
    }

    /// <summary>
    /// 检查是否在阈值范围内
    /// </summary>
    public bool IsWithinThresholds(PerformanceThresholds thresholds)
    {
        if (AverageResponseTime > thresholds.MaxResponseTimeMs)
            return false;

        if (ErrorRate > thresholds.MaxErrorRate)
            return false;

        if (CpuUsagePercent > thresholds.MaxCpuUsagePercent)
            return false;

        if (MemoryUsageBytes > thresholds.MaxMemoryUsageBytes)
            return false;

        return true;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AverageResponseTime;
        yield return ThroughputPerSecond;
        yield return ErrorRate;
        yield return MemoryUsageBytes;
        yield return CpuUsagePercent;
        yield return LastMeasuredTime.Date; // 只比较日期部分
    }
}

/// <summary>
/// 性能阈值
/// </summary>
public class PerformanceThresholds
{
    public double MaxResponseTimeMs { get; set; } = 5000;
    public double MaxErrorRate { get; set; } = 0.05;
    public double MaxCpuUsagePercent { get; set; } = 80;
    public long MaxMemoryUsageBytes { get; set; } = 1024 * 1024 * 1024; // 1GB
}