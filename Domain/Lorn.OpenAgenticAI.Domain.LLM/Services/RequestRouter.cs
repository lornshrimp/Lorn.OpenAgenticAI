using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Services;

/// <summary>
/// 请求路由器实现
/// 智能路由请求到最优的Kernel实例
/// </summary>
public class RequestRouter : IRequestRouter
{
    private readonly IModelManager _modelManager;
    private readonly ILoadBalancingStrategy _loadBalancingStrategy;
    private readonly IMetricsCollector _metricsCollector;
    private readonly LLMServiceOptions _options;
    private readonly ILogger<RequestRouter> _logger;

    public RequestRouter(
        IModelManager modelManager,
        ILoadBalancingStrategy loadBalancingStrategy,
        IMetricsCollector metricsCollector,
        IOptions<LLMServiceOptions> options,
        ILogger<RequestRouter> logger)
    {
        _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
        _loadBalancingStrategy = loadBalancingStrategy ?? throw new ArgumentNullException(nameof(loadBalancingStrategy));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RoutedRequest> RouteRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogDebug("开始路由请求: ModelId={ModelId}", request.ModelId);

            // 1. 确定目标模型
            var selectedModelId = await SelectModelAsync(request, cancellationToken);

            // 2. 创建路由结果
            var routedRequest = new RoutedRequest
            {
                OriginalRequest = request,
                SelectedModelId = selectedModelId,
                RoutingReason = "Based on model selection criteria",
                RoutingTime = DateTime.UtcNow,
                RoutingMetadata = new Dictionary<string, object>
                {
                    ["OriginalModelId"] = request.ModelId,
                    ["SelectionTime"] = DateTime.UtcNow - startTime,
                    ["Router"] = "DefaultRequestRouter"
                }
            };

            _logger.LogDebug("路由完成: OriginalModel={Original}, SelectedModel={Selected}, Duration={Duration}ms",
                request.ModelId, selectedModelId, (DateTime.UtcNow - startTime).TotalMilliseconds);

            return routedRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求路由失败: {ModelId}", request.ModelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Microsoft.SemanticKernel.Kernel> SelectKernelAsync(RoutingCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        try
        {
            // 这个方法在当前架构中不直接返回Kernel，而是返回模型ID
            // Kernel的获取由KernelManager负责
            await Task.CompletedTask; // 添加异步操作以消除警告
            throw new NotImplementedException("SelectKernelAsync should be replaced with model selection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "选择Kernel失败");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ILoadBalancingStrategy> GetLoadBalancingStrategyAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _loadBalancingStrategy;
    }

    /// <summary>
    /// 选择最适合的模型
    /// </summary>
    private async Task<string> SelectModelAsync(LLMRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 如果请求指定了具体的模型ID（非"default"），直接使用
            if (!string.IsNullOrWhiteSpace(request.ModelId) &&
                request.ModelId != "default" &&
                request.ModelId != "auto")
            {
                // 验证模型是否存在和可用
                try
                {
                    var modelInfo = await _modelManager.GetModelInfoAsync(request.ModelId, cancellationToken);
                    if (modelInfo.IsAvailable)
                    {
                        _logger.LogDebug("使用指定模型: {ModelId}", request.ModelId);
                        return request.ModelId;
                    }
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogWarning("指定的模型不存在，将自动选择: {ModelId}", request.ModelId);
                }
            }

            // 自动模型选择
            var selectionCriteria = BuildModelSelectionCriteria(request);
            var selectedModel = await _modelManager.SelectOptimalModelAsync(selectionCriteria, cancellationToken);

            _logger.LogDebug("自动选择模型: {ModelId}, 原始请求: {OriginalModelId}",
                selectedModel.ModelId, request.ModelId);

            return selectedModel.ModelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模型选择失败，使用默认模型");

            // 回退到默认模型
            var defaultModelId = _options.DefaultModelId ?? "gpt-3.5-turbo";
            _logger.LogWarning("使用默认模型: {DefaultModelId}", defaultModelId);

            return defaultModelId;
        }
    }

    /// <summary>
    /// 根据请求构建模型选择条件
    /// </summary>
    private static ModelSelectionCriteria BuildModelSelectionCriteria(LLMRequest request)
    {
        var criteria = new ModelSelectionCriteria
        {
            RequiredCapabilities = new List<ModelCapability> { ModelCapability.TextGeneration }
        };

        // 根据请求内容分析所需能力
        if (request.ConversationHistory != null && request.ConversationHistory.Count > 0)
        {
            // 如果有对话历史，确保支持对话
            criteria.RequiredCapabilities.Add(ModelCapability.TextGeneration);
        }

        // 检查是否需要函数调用能力
        if (request.ExecutionSettings != null)
        {
            // 这里可以根据ExecutionSettings的内容判断是否需要函数调用
            // 暂时简化处理
        }

        // 根据请求来源设置性能优先级
        var source = request.Metadata.Source?.ToLowerInvariant();
        criteria.PerformancePriority = source switch
        {
            "realtime" or "streaming" => PerformancePriority.Speed,
            "batch" or "analysis" => PerformancePriority.Quality,
            "cost-sensitive" => PerformancePriority.Cost,
            _ => PerformancePriority.Balanced
        };

        // 根据用户提示词长度估算上下文需求
        var contentLength = (request.SystemPrompt?.Length ?? 0) +
                           (request.UserPrompt?.Length ?? 0);

        if (request.ConversationHistory != null)
        {
            contentLength += request.ConversationHistory.Sum(m => m.Content?.Length ?? 0);
        }

        // 粗略估算所需的上下文长度（字符数 / 4 ≈ token数）
        var estimatedTokens = contentLength / 4;
        if (estimatedTokens > 1000)
        {
            criteria.MinContextLength = Math.Max(4096, estimatedTokens * 2); // 留出响应空间
        }

        return criteria;
    }

    /// <summary>
    /// 评估模型健康状态
    /// </summary>
    private async Task<bool> IsModelHealthyAsync(string modelId, CancellationToken cancellationToken)
    {
        try
        {
            var healthStatus = await _metricsCollector.GetHealthStatusAsync(modelId);
            return healthStatus.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查模型健康状态失败: {ModelId}", modelId);
            return true; // 默认认为健康
        }
    }

    /// <summary>
    /// 检查模型是否有故障转移需求
    /// </summary>
    private async Task<bool> RequiresFailoverAsync(string modelId, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.EnableFailover)
                return false;

            var metrics = await _metricsCollector.GetMetricsAsync(modelId, TimeSpan.FromMinutes(10));

            // 如果错误率超过阈值，需要故障转移
            return metrics.ErrorRate > _options.FailoverThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查故障转移需求失败: {ModelId}", modelId);
            return false;
        }
    }
}
