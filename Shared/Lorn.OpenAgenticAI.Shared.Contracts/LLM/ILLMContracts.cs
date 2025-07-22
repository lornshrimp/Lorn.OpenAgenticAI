namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 缓存序列化器接口
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    /// 序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] Serialize<T>(T value) where T : class;

    /// <summary>
    /// 反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="data">序列化的字节数组</param>
    /// <returns>反序列化后的对象</returns>
    T? Deserialize<T>(byte[] data) where T : class;
}

/// <summary>
/// 负载均衡策略接口
/// </summary>
public interface ILoadBalancingStrategy
{
    /// <summary>
    /// 选择下一个服务实例
    /// </summary>
    /// <param name="availableInstances">可用实例列表</param>
    /// <param name="criteria">选择条件</param>
    /// <returns>选择的实例</returns>
    T SelectNext<T>(IEnumerable<T> availableInstances, object? criteria = null);
}

/// <summary>
/// 路由结果
/// </summary>
public class RoutedRequest
{
    /// <summary>
    /// 原始请求
    /// </summary>
    public LLMRequest OriginalRequest { get; set; } = new();

    /// <summary>
    /// 选择的模型ID
    /// </summary>
    public string SelectedModelId { get; set; } = string.Empty;

    /// <summary>
    /// 路由原因
    /// </summary>
    public string RoutingReason { get; set; } = string.Empty;

    /// <summary>
    /// 路由时间
    /// </summary>
    public DateTime RoutingTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 路由元数据
    /// </summary>
    public Dictionary<string, object> RoutingMetadata { get; set; } = new();
}

/// <summary>
/// 路由条件
/// </summary>
public class RoutingCriteria
{
    /// <summary>
    /// 请求类型
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// 预期模型能力
    /// </summary>
    public List<ModelCapability> RequiredCapabilities { get; set; } = new();

    /// <summary>
    /// 性能要求
    /// </summary>
    public PerformanceRequirement? PerformanceRequirement { get; set; }

    /// <summary>
    /// 成本要求
    /// </summary>
    public CostRequirement? CostRequirement { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Kernel选择条件
/// </summary>
public class KernelSelectionCriteria
{
    /// <summary>
    /// 模型ID列表
    /// </summary>
    public List<string> PreferredModelIds { get; set; } = new();

    /// <summary>
    /// 最小性能要求
    /// </summary>
    public PerformanceRequirement? MinimumPerformance { get; set; }

    /// <summary>
    /// 最大成本要求
    /// </summary>
    public CostRequirement? MaximumCost { get; set; }

    /// <summary>
    /// 必需的能力
    /// </summary>
    public List<ModelCapability> RequiredCapabilities { get; set; } = new();
}

/// <summary>
/// 性能要求
/// </summary>
public class PerformanceRequirement
{
    /// <summary>
    /// 最大响应时间（毫秒）
    /// </summary>
    public int MaxResponseTimeMs { get; set; }

    /// <summary>
    /// 最小可用性
    /// </summary>
    public double MinAvailability { get; set; } = 0.99;

    /// <summary>
    /// 最大错误率
    /// </summary>
    public double MaxErrorRate { get; set; } = 0.01;
}

/// <summary>
/// 成本要求
/// </summary>
public class CostRequirement
{
    /// <summary>
    /// 最大每Token成本
    /// </summary>
    public decimal MaxCostPerToken { get; set; }

    /// <summary>
    /// 货币单位
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// 预算限制
    /// </summary>
    public decimal? BudgetLimit { get; set; }
}

/// <summary>
/// 模型选择条件
/// </summary>
public class ModelSelectionCriteria
{
    /// <summary>
    /// 所需能力
    /// </summary>
    public List<ModelCapability> RequiredCapabilities { get; set; } = new();

    /// <summary>
    /// 偏好的提供商
    /// </summary>
    public List<string> PreferredProviders { get; set; } = new();

    /// <summary>
    /// 性能优先级
    /// </summary>
    public PerformancePriority PerformancePriority { get; set; } = PerformancePriority.Balanced;

    /// <summary>
    /// 成本限制
    /// </summary>
    public decimal? MaxCostPerThousandTokens { get; set; }

    /// <summary>
    /// 最小上下文长度
    /// </summary>
    public int? MinContextLength { get; set; }
}
