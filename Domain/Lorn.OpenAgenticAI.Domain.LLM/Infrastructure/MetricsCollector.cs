using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Infrastructure;

/// <summary>
/// 指标收集器实现
/// 全面收集系统运行指标，支持性能监控和问题诊断
/// </summary>
public class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly MetricsCollectorOptions _options;

    // 请求追踪
    private readonly ConcurrentDictionary<string, RequestTracking> _activeRequests = new();

    // 指标存储
    private readonly ConcurrentDictionary<string, ModelMetrics> _modelMetrics = new();

    // 计数器
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public MetricsCollector(
        ILogger<MetricsCollector> logger,
        IOptions<MetricsCollectorOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string StartRequest(string modelId, string requestType)
    {
        var trackingId = Guid.NewGuid().ToString();
        var tracking = new RequestTracking
        {
            TrackingId = trackingId,
            ModelId = modelId,
            RequestType = requestType,
            StartTime = DateTime.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        _activeRequests[trackingId] = tracking;

        // 增加请求计数
        IncrementCounter($"{modelId}:requests:total");
        IncrementCounter($"{modelId}:requests:{requestType}");

        _logger.LogDebug("开始请求追踪: {TrackingId}, 模型: {ModelId}, 类型: {RequestType}",
            trackingId, modelId, requestType);

        return trackingId;
    }

    /// <inheritdoc />
    public void EndRequest(string trackingId, bool success, TimeSpan duration, UsageStatistics? usage = null)
    {
        if (!_activeRequests.TryRemove(trackingId, out var tracking))
        {
            _logger.LogWarning("未找到请求追踪记录: {TrackingId}", trackingId);
            return;
        }

        tracking.Stopwatch.Stop();
        tracking.EndTime = DateTime.UtcNow;
        tracking.Success = success;
        tracking.Duration = duration;
        tracking.Usage = usage;

        // 更新模型指标
        var modelMetrics = _modelMetrics.GetOrAdd(tracking.ModelId, _ => new ModelMetrics
        {
            ModelId = tracking.ModelId
        });

        lock (modelMetrics)
        {
            modelMetrics.TotalRequests++;

            if (success)
            {
                modelMetrics.SuccessfulRequests++;
                modelMetrics.TotalResponseTime += duration.TotalMilliseconds;

                if (usage != null)
                {
                    modelMetrics.TotalTokensUsed += usage.TotalTokens;
                    if (usage.Cost.HasValue)
                    {
                        modelMetrics.TotalCost += usage.Cost.Value;
                    }
                }
            }
            else
            {
                modelMetrics.FailedRequests++;
            }

            // 更新响应时间分布
            modelMetrics.ResponseTimes.Add(duration.TotalMilliseconds);
            if (modelMetrics.ResponseTimes.Count > 1000) // 保持最近1000次请求的数据
            {
                modelMetrics.ResponseTimes.RemoveAt(0);
            }

            modelMetrics.LastUpdated = DateTime.UtcNow;
        }

        // 更新计数器
        if (success)
        {
            IncrementCounter($"{tracking.ModelId}:requests:success");
        }
        else
        {
            IncrementCounter($"{tracking.ModelId}:requests:failed");
        }

        _logger.LogDebug("结束请求追踪: {TrackingId}, 成功: {Success}, 耗时: {Duration}ms",
            trackingId, success, duration.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void RecordCacheHit(string modelId, string cacheType)
    {
        IncrementCounter($"{modelId}:cache:{cacheType}:hits");
        _logger.LogDebug("缓存命中: 模型={ModelId}, 类型={CacheType}", modelId, cacheType);
    }

    /// <inheritdoc />
    public void RecordCacheMiss(string modelId, string cacheType)
    {
        IncrementCounter($"{modelId}:cache:{cacheType}:misses");
        _logger.LogDebug("缓存未命中: 模型={ModelId}, 类型={CacheType}", modelId, cacheType);
    }

    /// <inheritdoc />
    public void RecordError(string modelId, string errorType, Exception? exception = null)
    {
        IncrementCounter($"{modelId}:errors:{errorType}");

        if (exception != null)
        {
            _logger.LogError(exception, "记录错误: 模型={ModelId}, 类型={ErrorType}", modelId, errorType);
        }
        else
        {
            _logger.LogWarning("记录错误: 模型={ModelId}, 类型={ErrorType}", modelId, errorType);
        }
    }

    /// <inheritdoc />
    public async Task<PerformanceMetrics> GetMetricsAsync(string modelId, TimeSpan timeRange)
    {
        await Task.CompletedTask; // 保持异步接口

        if (!_modelMetrics.TryGetValue(modelId, out var modelMetrics))
        {
            return new PerformanceMetrics();
        }

        lock (modelMetrics)
        {
            var metrics = new PerformanceMetrics
            {
                TotalRequests = modelMetrics.TotalRequests,
                SuccessfulRequests = modelMetrics.SuccessfulRequests,
                FailedRequests = modelMetrics.FailedRequests,
                TotalTokensUsed = modelMetrics.TotalTokensUsed,
                TotalCost = modelMetrics.TotalCost
            };

            if (modelMetrics.SuccessfulRequests > 0)
            {
                metrics.AverageResponseTime = modelMetrics.TotalResponseTime / modelMetrics.SuccessfulRequests;
            }

            if (modelMetrics.ResponseTimes.Count > 0)
            {
                var sortedTimes = modelMetrics.ResponseTimes.OrderBy(x => x).ToList();
                var p95Index = (int)(sortedTimes.Count * 0.95);
                if (p95Index < sortedTimes.Count)
                {
                    metrics.P95ResponseTime = sortedTimes[p95Index];
                }
            }

            // 计算缓存命中率
            var cacheHits = GetCounterValue($"{modelId}:cache:memory:hits") +
                           GetCounterValue($"{modelId}:cache:distributed:hits");
            var cacheMisses = GetCounterValue($"{modelId}:cache:memory:misses") +
                             GetCounterValue($"{modelId}:cache:distributed:misses");

            if (cacheHits + cacheMisses > 0)
            {
                metrics.CacheHitRate = (double)cacheHits / (cacheHits + cacheMisses);
            }

            return metrics;
        }
    }

    /// <inheritdoc />
    public async Task<HealthStatus> GetHealthStatusAsync(string modelId)
    {
        await Task.CompletedTask; // 保持异步接口

        var status = new HealthStatus
        {
            LastCheckTime = DateTime.UtcNow
        };

        if (_modelMetrics.TryGetValue(modelId, out var metrics))
        {
            lock (metrics)
            {
                var errorRate = metrics.TotalRequests > 0 ?
                    (double)metrics.FailedRequests / metrics.TotalRequests : 0;

                status.IsHealthy = errorRate < 0.1; // 错误率小于10%认为健康
                status.Status = status.IsHealthy ? "Healthy" : "Unhealthy";

                status.Details = new Dictionary<string, object>
                {
                    ["TotalRequests"] = metrics.TotalRequests,
                    ["ErrorRate"] = errorRate,
                    ["LastActivity"] = metrics.LastUpdated,
                    ["AverageResponseTime"] = metrics.SuccessfulRequests > 0 ?
                        metrics.TotalResponseTime / metrics.SuccessfulRequests : 0
                };
            }
        }
        else
        {
            status.IsHealthy = false;
            status.Status = "Unknown";
            status.Details["Reason"] = "No metrics available";
        }

        return status;
    }

    private void IncrementCounter(string key)
    {
        _counters.AddOrUpdate(key, 1, (k, v) => v + 1);
    }

    private long GetCounterValue(string key)
    {
        return _counters.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>
    /// 请求追踪信息
    /// </summary>
    private class RequestTracking
    {
        public string TrackingId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public UsageStatistics? Usage { get; set; }
        public Stopwatch Stopwatch { get; set; } = new();
    }

    /// <summary>
    /// 模型指标
    /// </summary>
    private class ModelMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public double TotalResponseTime { get; set; }
        public long TotalTokensUsed { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<double> ResponseTimes { get; set; } = new();
    }
}
