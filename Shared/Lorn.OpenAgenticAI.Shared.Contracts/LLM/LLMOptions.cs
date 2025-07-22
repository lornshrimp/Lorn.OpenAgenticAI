namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 响应缓存配置选项
/// </summary>
public class ResponseCacheOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ResponseCache";

    /// <summary>
    /// 启用内存缓存
    /// </summary>
    public bool EnableMemoryCache { get; set; } = true;

    /// <summary>
    /// 启用分布式缓存
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;

    /// <summary>
    /// 默认缓存过期时间（秒）
    /// </summary>
    public int DefaultExpirationSeconds { get; set; } = 3600; // 1小时

    /// <summary>
    /// 内存缓存过期时间（分钟）
    /// </summary>
    public int MemoryCacheExpiryMinutes { get; set; } = 60; // 1小时

    /// <summary>
    /// 分布式缓存过期时间（分钟）
    /// </summary>
    public int DistributedCacheExpiryMinutes { get; set; } = 1440; // 24小时

    /// <summary>
    /// 最大缓存项数量
    /// </summary>
    public int MaxCacheItems { get; set; } = 10000;

    /// <summary>
    /// 缓存压缩阈值（字节）
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// 启用缓存压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "llm:";

    /// <summary>
    /// 根据模型类型的缓存时间配置
    /// </summary>
    public Dictionary<string, int> ModelTypeExpirations { get; set; } = new();
}

/// <summary>
/// LLM服务配置选项
/// </summary>
public class LLMServiceOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "LLMService";

    /// <summary>
    /// 默认超时时间（秒）
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5分钟

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// 启用请求去重
    /// </summary>
    public bool EnableRequestDeduplication { get; set; } = true;

    /// <summary>
    /// 启用指标收集
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 启用健康检查
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// 健康检查间隔（秒）
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 默认模型ID
    /// </summary>
    public string? DefaultModelId { get; set; }

    /// <summary>
    /// 启用故障转移
    /// </summary>
    public bool EnableFailover { get; set; } = true;

    /// <summary>
    /// 故障转移阈值
    /// </summary>
    public double FailoverThreshold { get; set; } = 0.8; // 80%错误率触发故障转移
}

/// <summary>
/// 指标收集配置选项
/// </summary>
public class MetricsCollectorOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "MetricsCollector";

    /// <summary>
    /// 启用详细指标
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// 指标保留天数
    /// </summary>
    public int MetricsRetentionDays { get; set; } = 30;

    /// <summary>
    /// 指标聚合间隔（秒）
    /// </summary>
    public int AggregationIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 最大指标项数量
    /// </summary>
    public int MaxMetricsItems { get; set; } = 100000;

    /// <summary>
    /// 启用性能分析
    /// </summary>
    public bool EnablePerformanceProfiling { get; set; } = false;

    /// <summary>
    /// 导出器配置
    /// </summary>
    public Dictionary<string, object> ExporterConfig { get; set; } = new();
}
