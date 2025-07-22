namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 负载均衡策略类型
/// </summary>
public enum LoadBalancingStrategyType
{
    /// <summary>
    /// 轮询
    /// </summary>
    RoundRobin = 0,

    /// <summary>
    /// 随机
    /// </summary>
    Random = 1,

    /// <summary>
    /// 基于性能
    /// </summary>
    PerformanceBased = 2,

    /// <summary>
    /// 加权轮询
    /// </summary>
    WeightedRoundRobin = 3,

    /// <summary>
    /// 最少连接
    /// </summary>
    LeastConnections = 4,

    /// <summary>
    /// 最少响应时间
    /// </summary>
    LeastResponseTime = 5
}

/// <summary>
/// 负载均衡策略类型扩展方法
/// </summary>
public static class LoadBalancingStrategyTypeExtensions
{
    /// <summary>
    /// 获取策略描述
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>描述</returns>
    public static string GetDescription(this LoadBalancingStrategyType strategyType)
    {
        return strategyType switch
        {
            LoadBalancingStrategyType.RoundRobin => "轮询策略：按顺序分发请求",
            LoadBalancingStrategyType.Random => "随机策略：随机选择模型",
            LoadBalancingStrategyType.PerformanceBased => "性能策略：基于历史性能选择最佳模型",
            LoadBalancingStrategyType.WeightedRoundRobin => "加权轮询：根据权重分发请求",
            LoadBalancingStrategyType.LeastConnections => "最少连接：选择当前连接数最少的模型",
            LoadBalancingStrategyType.LeastResponseTime => "最少响应时间：选择响应时间最短的模型",
            _ => "未知策略"
        };
    }

    /// <summary>
    /// 判断是否需要性能指标
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>是否需要性能指标</returns>
    public static bool RequiresMetrics(this LoadBalancingStrategyType strategyType)
    {
        return strategyType is
            LoadBalancingStrategyType.PerformanceBased or
            LoadBalancingStrategyType.LeastConnections or
            LoadBalancingStrategyType.LeastResponseTime;
    }

    /// <summary>
    /// 判断是否支持权重
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>是否支持权重</returns>
    public static bool SupportsWeights(this LoadBalancingStrategyType strategyType)
    {
        return strategyType is
            LoadBalancingStrategyType.WeightedRoundRobin or
            LoadBalancingStrategyType.PerformanceBased;
    }
}
