using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;
using System.Collections.Concurrent;

namespace Lorn.OpenAgenticAI.Domain.LLM.Services;

/// <summary>
/// Kernel管理器实现
/// 管理SemanticKernel实例的完整生命周期
/// </summary>
public class KernelManager : IKernelManager, IDisposable
{
    private readonly IModelManager _modelManager;
    private readonly ILLMServiceRegistry _serviceRegistry;
    private readonly LLMServiceOptions _options;
    private readonly ILogger<KernelManager> _logger;

    // 用于存储Kernel实例的线程安全字典
    private readonly ConcurrentDictionary<string, Kernel> _kernelCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _kernelLastAccess = new();
    private readonly Timer _cleanupTimer;

    private volatile bool _disposed = false;

    public KernelManager(
        IModelManager modelManager,
        ILLMServiceRegistry serviceRegistry,
        IOptions<LLMServiceOptions> options,
        ILogger<KernelManager> logger)
    {
        _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 启动定时清理任务
        _cleanupTimer = new Timer(CleanupExpiredKernels, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public async Task<Kernel> GetKernelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("ModelId不能为空", nameof(modelId));

        try
        {
            // 检查缓存
            if (_kernelCache.TryGetValue(modelId, out var cachedKernel))
            {
                _kernelLastAccess[modelId] = DateTime.UtcNow;
                _logger.LogDebug("从缓存获取Kernel: {ModelId}", modelId);
                return cachedKernel;
            }

            // 创建新的Kernel
            var modelConfig = await _modelManager.GetModelConfigurationAsync(modelId, cancellationToken);
            var kernel = await CreateKernelAsync(modelConfig, cancellationToken);

            // 添加到缓存
            _kernelCache[modelId] = kernel;
            _kernelLastAccess[modelId] = DateTime.UtcNow;

            _logger.LogInformation("创建并缓存新Kernel: {ModelId}", modelId);
            return kernel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Kernel失败: {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Kernel> CreateKernelAsync(ModelConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            _logger.LogDebug("开始创建Kernel: {ModelId}, 提供商: {ProviderId}", config.ModelId, config.ProviderId);

            // 使用KernelBuilder创建Kernel
            var builder = Kernel.CreateBuilder();

            // 注册LLM服务
            await _serviceRegistry.RegisterServicesAsync(builder, config, cancellationToken);

            // 构建Kernel
            var kernel = builder.Build();

            _logger.LogInformation("成功创建Kernel: {ModelId}", config.ModelId);
            return kernel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Kernel失败: {ModelId}", config.ModelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RegisterServiceAsync(string modelId, Type serviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_kernelCache.TryGetValue(modelId, out var kernel))
            {
                // 这里需要根据serviceType来注册不同的服务
                // SemanticKernel的KernelBuilder.Services在构建后不能修改
                // 所以这个方法可能需要重新设计或者重新创建Kernel
                _logger.LogWarning("Kernel已创建，无法动态注册服务: {ModelId}, {ServiceType}", modelId, serviceType.Name);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册服务失败: {ModelId}, {ServiceType}", modelId, serviceType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetServiceAsync<T>(string modelId, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var kernel = await GetKernelAsync(modelId, cancellationToken);
            return kernel.GetRequiredService<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务失败: {ModelId}, {ServiceType}", modelId, typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Kernel> GetOptimalKernelAsync(KernelSelectionCriteria criteria, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取符合条件的模型
            var availableModels = new List<string>();
            foreach (var capability in criteria.RequiredCapabilities)
            {
                var models = await _modelManager.GetModelsByCapabilityAsync(capability, cancellationToken);
                availableModels.AddRange(models.Select(m => m.ModelId));
            }

            if (!availableModels.Any())
            {
                _logger.LogWarning("未找到符合条件的模型");
                // 返回默认模型的Kernel
                var defaultModelId = _options.DefaultModelId ?? "default";
                return await GetKernelAsync(defaultModelId, cancellationToken);
            }

            // 选择最优模型（简化实现，选择第一个）
            var selectedModelId = availableModels.First();

            _logger.LogDebug("选择最优Kernel: {ModelId}", selectedModelId);
            return await GetKernelAsync(selectedModelId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最优Kernel失败");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DisposeKernelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_kernelCache.TryRemove(modelId, out var kernel))
            {
                _kernelLastAccess.TryRemove(modelId, out _);

                // SemanticKernel 1.30.0+ 中 Kernel 不再实现 IDisposable
                // 直接从缓存中移除即可，GC会处理资源清理

                _logger.LogDebug("销毁Kernel: {ModelId}", modelId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "销毁Kernel失败: {ModelId}", modelId);
        }
    }

    /// <summary>
    /// 定时清理过期的Kernel实例
    /// </summary>
    private void CleanupExpiredKernels(object? state)
    {
        try
        {
            var expiredThreshold = DateTime.UtcNow.AddMinutes(-30); // 30分钟未使用视为过期
            var expiredModels = _kernelLastAccess
                .Where(kvp => kvp.Value < expiredThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var modelId in expiredModels)
            {
                _ = Task.Run(async () => await DisposeKernelAsync(modelId));
            }

            if (expiredModels.Any())
            {
                _logger.LogDebug("清理过期Kernel: {Count}个", expiredModels.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期Kernel时发生错误");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _cleanupTimer?.Dispose();

            // 清理所有Kernel
            foreach (var kvp in _kernelCache)
            {
                // SemanticKernel 1.30.0+ 中 Kernel 不再实现 IDisposable
                // 直接清除缓存即可，GC会处理资源清理
            }

            _kernelCache.Clear();
            _kernelLastAccess.Clear();

            _disposed = true;
            _logger.LogDebug("KernelManager已释放");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放KernelManager时发生错误");
        }
    }
}
