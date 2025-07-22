using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;
using Lorn.OpenAgenticAI.Domain.LLM.Infrastructure;
using Lorn.OpenAgenticAI.Domain.LLM.Services;

namespace Lorn.OpenAgenticAI.Domain.LLM;

/// <summary>
/// LLM域服务注册扩展
/// 提供统一的依赖注入配置，确保所有组件按正确的生命周期注册
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// 注册LLM域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLLMDomainServices(this IServiceCollection services)
    {
        // 注册基础设施组件
        RegisterInfrastructureServices(services);

        // 注册核心域服务
        RegisterDomainServices(services);

        // 注册服务注册器
        RegisterServiceRegistry(services);

        return services;
    }

    /// <summary>
    /// 注册LLM域服务（带配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLLMDomainServices(
        this IServiceCollection services,
        Action<LLMDomainOptions> configureOptions)
    {
        var options = new LLMDomainOptions();
        configureOptions(options);

        services.Configure<LLMDomainOptions>(opt =>
        {
            opt.CacheOptions = options.CacheOptions;
            opt.MetricsOptions = options.MetricsOptions;
            opt.LoadBalancingOptions = options.LoadBalancingOptions;
        });

        return services.AddLLMDomainServices();
    }

    /// <summary>
    /// 注册基础设施服务
    /// </summary>
    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        // 缓存服务 - 单例，因为缓存状态需要在应用程序生命周期内保持
        services.TryAddSingleton<IResponseCache, ResponseCache>();
        services.TryAddSingleton<ICacheSerializer, JsonCacheSerializer>();

        // 指标收集器 - 单例，因为指标状态需要在应用程序生命周期内保持
        services.TryAddSingleton<IMetricsCollector, MetricsCollector>();

        // 负载均衡策略 - 单例，因为它们是无状态的
        services.TryAddSingleton<ILoadBalancingStrategy, RoundRobinLoadBalancingStrategy>();
        services.TryAddSingleton<RandomLoadBalancingStrategy>();
        services.TryAddSingleton<PerformanceBasedLoadBalancingStrategy>();

        // 负载均衡策略工厂
        services.TryAddSingleton<ILoadBalancingStrategyFactory, LoadBalancingStrategyFactory>();
    }

    /// <summary>
    /// 注册核心域服务
    /// </summary>
    private static void RegisterDomainServices(IServiceCollection services)
    {
        // 模型管理器 - 单例，因为模型配置在应用程序生命周期内相对稳定
        services.TryAddSingleton<IModelManager, ModelManager>();

        // Kernel管理器 - 单例，因为Kernel创建开销较大，需要复用
        services.TryAddSingleton<IKernelManager, KernelManager>();

        // 请求路由器 - 单例，因为路由逻辑是无状态的
        services.TryAddSingleton<IRequestRouter, RequestRouter>();

        // LLM服务 - 作用域，因为每个请求可能有不同的上下文
        services.TryAddScoped<ILLMService, LLMService>();
    }

    /// <summary>
    /// 注册服务注册器
    /// </summary>
    private static void RegisterServiceRegistry(IServiceCollection services)
    {
        // 服务注册器 - 单例，因为它只负责服务注册逻辑
        services.TryAddSingleton<ILLMServiceRegistry, LLMServiceRegistry>();
    }
}

/// <summary>
/// 负载均衡策略工厂
/// </summary>
public interface ILoadBalancingStrategyFactory
{
    /// <summary>
    /// 创建负载均衡策略
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>负载均衡策略</returns>
    ILoadBalancingStrategy CreateStrategy(LoadBalancingStrategyType strategyType);
}

/// <summary>
/// 负载均衡策略工厂实现
/// </summary>
public class LoadBalancingStrategyFactory : ILoadBalancingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoadBalancingStrategyFactory> _logger;

    public LoadBalancingStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<LoadBalancingStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ILoadBalancingStrategy CreateStrategy(LoadBalancingStrategyType strategyType)
    {
        try
        {
            return strategyType switch
            {
                LoadBalancingStrategyType.RoundRobin =>
                    _serviceProvider.GetRequiredService<RoundRobinLoadBalancingStrategy>(),

                LoadBalancingStrategyType.Random =>
                    _serviceProvider.GetRequiredService<RandomLoadBalancingStrategy>(),

                LoadBalancingStrategyType.PerformanceBased =>
                    _serviceProvider.GetRequiredService<PerformanceBasedLoadBalancingStrategy>(),

                _ => throw new NotSupportedException($"不支持的负载均衡策略类型: {strategyType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建负载均衡策略失败: {StrategyType}", strategyType);
            throw;
        }
    }
}

/// <summary>
/// LLM域配置选项
/// </summary>
public class LLMDomainOptions
{
    /// <summary>
    /// 缓存选项
    /// </summary>
    public CacheOptions CacheOptions { get; set; } = new();

    /// <summary>
    /// 指标选项
    /// </summary>
    public MetricsOptions MetricsOptions { get; set; } = new();

    /// <summary>
    /// 负载均衡选项
    /// </summary>
    public LoadBalancingOptions LoadBalancingOptions { get; set; } = new();
}

/// <summary>
/// 缓存选项
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 最大缓存大小（项目数）
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// 是否启用分布式缓存
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;
}

/// <summary>
/// 指标选项
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// 是否启用指标收集
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 指标保留时间（小时）
    /// </summary>
    public int MetricsRetentionHours { get; set; } = 24;

    /// <summary>
    /// 性能阈值（毫秒）
    /// </summary>
    public int PerformanceThresholdMs { get; set; } = 5000;
}

/// <summary>
/// 负载均衡选项
/// </summary>
public class LoadBalancingOptions
{
    /// <summary>
    /// 默认策略类型
    /// </summary>
    public LoadBalancingStrategyType DefaultStrategy { get; set; } = LoadBalancingStrategyType.RoundRobin;

    /// <summary>
    /// 健康检查间隔（秒）
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
