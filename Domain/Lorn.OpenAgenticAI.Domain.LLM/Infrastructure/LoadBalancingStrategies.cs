using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Infrastructure;

/// <summary>
/// 轮询负载均衡策略
/// </summary>
public class RoundRobinLoadBalancingStrategy : ILoadBalancingStrategy
{
    private long _counter = 0;

    /// <inheritdoc />
    public T SelectNext<T>(IEnumerable<T> availableInstances, object? criteria = null)
    {
        var instances = availableInstances.ToList();
        if (!instances.Any())
            throw new InvalidOperationException("没有可用的实例");

        var index = Interlocked.Increment(ref _counter) % instances.Count;
        return instances[(int)index];
    }
}

/// <summary>
/// 随机负载均衡策略
/// </summary>
public class RandomLoadBalancingStrategy : ILoadBalancingStrategy
{
    private readonly Random _random = new();

    /// <inheritdoc />
    public T SelectNext<T>(IEnumerable<T> availableInstances, object? criteria = null)
    {
        var instances = availableInstances.ToList();
        if (!instances.Any())
            throw new InvalidOperationException("没有可用的实例");

        var index = _random.Next(instances.Count);
        return instances[index];
    }
}

/// <summary>
/// 性能优先负载均衡策略
/// </summary>
public class PerformanceBasedLoadBalancingStrategy : ILoadBalancingStrategy
{
    private readonly IMetricsCollector _metricsCollector;

    public PerformanceBasedLoadBalancingStrategy(IMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

    /// <inheritdoc />
    public T SelectNext<T>(IEnumerable<T> availableInstances, object? criteria = null)
    {
        var instances = availableInstances.ToList();
        if (!instances.Any())
            throw new InvalidOperationException("没有可用的实例");

        if (instances.Count == 1)
            return instances[0];

        // 这里需要根据实际的性能指标来选择
        // 为了简化，暂时使用随机策略
        var random = new Random();
        var index = random.Next(instances.Count);
        return instances[index];
    }
}

/// <summary>
/// 加权轮询负载均衡策略
/// </summary>
public class WeightedRoundRobinLoadBalancingStrategy : ILoadBalancingStrategy
{
    private readonly Dictionary<object, int> _weights = new();
    private readonly Dictionary<object, int> _currentWeights = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public T SelectNext<T>(IEnumerable<T> availableInstances, object? criteria = null)
    {
        var instances = availableInstances.ToList();
        if (!instances.Any())
            throw new InvalidOperationException("没有可用的实例");

        if (instances.Count == 1)
            return instances[0];

        lock (_lock)
        {
            // 初始化权重（如果需要）
            foreach (var instance in instances)
            {
                if (!_weights.ContainsKey(instance!))
                {
                    _weights[instance!] = 1; // 默认权重为1
                    _currentWeights[instance!] = 1;
                }
            }

            // 选择当前权重最高的实例
            var selected = instances.Aggregate((i1, i2) =>
                _currentWeights[i1!] > _currentWeights[i2!] ? i1 : i2);

            // 更新权重
            var totalWeight = instances.Sum(i => _weights[i!]);
            _currentWeights[selected!] -= totalWeight;

            foreach (var instance in instances)
            {
                _currentWeights[instance!] += _weights[instance!];
            }

            return selected;
        }
    }

    /// <summary>
    /// 设置实例权重
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="weight">权重</param>
    public void SetWeight(object instance, int weight)
    {
        lock (_lock)
        {
            _weights[instance] = Math.Max(1, weight);
            _currentWeights[instance] = Math.Max(1, weight);
        }
    }
}
