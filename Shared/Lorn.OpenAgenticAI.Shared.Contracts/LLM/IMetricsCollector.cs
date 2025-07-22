namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 指标收集器接口
/// 全面收集系统运行指标，支持性能监控和问题诊断
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// 记录请求开始
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="requestType">请求类型</param>
    /// <returns>追踪ID</returns>
    string StartRequest(string modelId, string requestType);

    /// <summary>
    /// 记录请求完成
    /// </summary>
    /// <param name="trackingId">追踪ID</param>
    /// <param name="success">是否成功</param>
    /// <param name="duration">持续时间</param>
    /// <param name="usage">使用统计</param>
    void EndRequest(string trackingId, bool success, TimeSpan duration, UsageStatistics? usage = null);

    /// <summary>
    /// 记录缓存命中
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cacheType">缓存类型</param>
    void RecordCacheHit(string modelId, string cacheType);

    /// <summary>
    /// 记录缓存未命中
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cacheType">缓存类型</param>
    void RecordCacheMiss(string modelId, string cacheType);

    /// <summary>
    /// 记录错误
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="errorType">错误类型</param>
    /// <param name="exception">异常信息</param>
    void RecordError(string modelId, string errorType, Exception? exception = null);

    /// <summary>
    /// 获取性能指标
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="timeRange">时间范围</param>
    /// <returns>性能指标</returns>
    Task<PerformanceMetrics> GetMetricsAsync(string modelId, TimeSpan timeRange);

    /// <summary>
    /// 获取健康状态
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <returns>健康状态</returns>
    Task<HealthStatus> GetHealthStatusAsync(string modelId);
}

/// <summary>
/// 健康状态
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// 性能指标
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// 请求总数
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// 成功请求数
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// 失败请求数
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// P95响应时间（毫秒）
    /// </summary>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// 总Token使用量
    /// </summary>
    public long TotalTokensUsed { get; set; }

    /// <summary>
    /// 总成本
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 错误率
    /// </summary>
    public double ErrorRate => TotalRequests > 0 ? (double)FailedRequests / TotalRequests : 0;

    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
}
