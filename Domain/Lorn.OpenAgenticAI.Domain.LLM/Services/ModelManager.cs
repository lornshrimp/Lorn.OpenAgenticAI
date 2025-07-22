using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;
using System.Collections.Concurrent;

namespace Lorn.OpenAgenticAI.Domain.LLM.Services;

/// <summary>
/// 模型管理器实现
/// 管理模型元数据和配置
/// </summary>
public class ModelManager : IModelManager
{
    private readonly LLMServiceOptions _options;
    private readonly ILogger<ModelManager> _logger;

    // 模型注册表
    private readonly ConcurrentDictionary<string, ModelRegistration> _modelRegistry = new();

    // 按能力索引的模型
    private readonly ConcurrentDictionary<ModelCapability, List<string>> _capabilityIndex = new();

    public ModelManager(
        IOptions<LLMServiceOptions> options,
        ILogger<ModelManager> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化默认模型
        InitializeDefaultModels();
    }

    /// <inheritdoc />
    public async Task RegisterModelAsync(ModelRegistration registration, CancellationToken cancellationToken = default)
    {
        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

        try
        {
            var modelId = registration.ModelInfo.ModelId;

            // 验证模型配置
            var validationResult = ValidateModelRegistration(registration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"模型注册验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 注册模型
            _modelRegistry[modelId] = registration;

            // 更新能力索引
            UpdateCapabilityIndex(modelId, registration.ModelInfo.Capabilities);

            _logger.LogInformation("成功注册模型: {ModelId}, 提供商: {Provider}",
                modelId, registration.ModelInfo.ProviderName);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册模型失败: {ModelId}", registration.ModelInfo.ModelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ModelInfo> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("ModelId不能为空", nameof(modelId));

        try
        {
            if (_modelRegistry.TryGetValue(modelId, out var registration))
            {
                await Task.CompletedTask;
                return registration.ModelInfo;
            }

            _logger.LogWarning("未找到模型: {ModelId}", modelId);
            throw new KeyNotFoundException($"未找到模型: {modelId}");
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "获取模型信息失败: {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ModelInfo>> GetModelsByCapabilityAsync(ModelCapability capability, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_capabilityIndex.TryGetValue(capability, out var modelIds))
            {
                await Task.CompletedTask;
                return Enumerable.Empty<ModelInfo>();
            }

            var models = modelIds
                .Where(id => _modelRegistry.ContainsKey(id))
                .Select(id => _modelRegistry[id].ModelInfo)
                .Where(model => model.IsAvailable)
                .ToList();

            _logger.LogDebug("根据能力 {Capability} 找到 {Count} 个模型", capability, models.Count);

            await Task.CompletedTask;
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据能力获取模型失败: {Capability}", capability);
            return Enumerable.Empty<ModelInfo>();
        }
    }

    /// <inheritdoc />
    public async Task<ModelInfo> SelectOptimalModelAsync(ModelSelectionCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        try
        {
            var candidateModels = new List<ModelInfo>();

            // 根据能力筛选模型
            if (criteria.RequiredCapabilities.Any())
            {
                foreach (var capability in criteria.RequiredCapabilities)
                {
                    var models = await GetModelsByCapabilityAsync(capability, cancellationToken);
                    candidateModels.AddRange(models);
                }

                // 只保留支持所有必需能力的模型
                candidateModels = candidateModels
                    .GroupBy(m => m.ModelId)
                    .Where(g => g.Count() == criteria.RequiredCapabilities.Count)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                // 如果没有特定能力要求，获取所有可用模型
                candidateModels = _modelRegistry.Values
                    .Select(r => r.ModelInfo)
                    .Where(m => m.IsAvailable)
                    .ToList();
            }

            // 根据提供商偏好筛选
            if (criteria.PreferredProviders.Any())
            {
                var preferredModels = candidateModels
                    .Where(m => criteria.PreferredProviders.Contains(m.ProviderName))
                    .ToList();

                if (preferredModels.Any())
                {
                    candidateModels = preferredModels;
                }
            }

            // 根据成本限制筛选
            if (criteria.MaxCostPerThousandTokens.HasValue)
            {
                candidateModels = candidateModels
                    .Where(m => _modelRegistry.TryGetValue(m.ModelId, out var reg) &&
                               reg.Configuration.Limitations.CostPerThousandTokens <= criteria.MaxCostPerThousandTokens)
                    .ToList();
            }

            // 根据上下文长度要求筛选
            if (criteria.MinContextLength.HasValue)
            {
                candidateModels = candidateModels
                    .Where(m => _modelRegistry.TryGetValue(m.ModelId, out var reg) &&
                               reg.Configuration.Limitations.MaxContextLength >= criteria.MinContextLength)
                    .ToList();
            }

            if (!candidateModels.Any())
            {
                _logger.LogWarning("没有找到符合条件的模型");
                throw new InvalidOperationException("没有找到符合条件的模型");
            }

            // 根据性能优先级排序选择
            var selectedModel = criteria.PerformancePriority switch
            {
                PerformancePriority.Speed => candidateModels.OrderByDescending(m => m.PerformanceScore).First(),
                PerformancePriority.Quality => candidateModels.OrderByDescending(m => m.PerformanceScore).First(),
                PerformancePriority.Cost => candidateModels
                    .OrderBy(m => _modelRegistry.TryGetValue(m.ModelId, out var reg) ?
                        reg.Configuration.Limitations.CostPerThousandTokens : decimal.MaxValue)
                    .First(),
                _ => candidateModels.OrderByDescending(m => m.PerformanceScore).First()
            };

            _logger.LogDebug("选择最优模型: {ModelId}, 候选数量: {Count}", selectedModel.ModelId, candidateModels.Count);
            return selectedModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "选择最优模型失败");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ModelConfiguration> GetModelConfigurationAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("ModelId不能为空", nameof(modelId));

        try
        {
            if (_modelRegistry.TryGetValue(modelId, out var registration))
            {
                await Task.CompletedTask;
                return registration.Configuration;
            }

            // 如果找不到具体模型，尝试返回默认配置
            if (modelId == "default" && !string.IsNullOrEmpty(_options.DefaultModelId))
            {
                return await GetModelConfigurationAsync(_options.DefaultModelId, cancellationToken);
            }

            _logger.LogWarning("未找到模型配置: {ModelId}", modelId);
            throw new KeyNotFoundException($"未找到模型配置: {modelId}");
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "获取模型配置失败: {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateModelConfigurationAsync(string modelId, ModelConfiguration config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("ModelId不能为空", nameof(modelId));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            if (_modelRegistry.TryGetValue(modelId, out var registration))
            {
                registration.Configuration = config;
                _logger.LogInformation("更新模型配置: {ModelId}", modelId);
            }
            else
            {
                _logger.LogWarning("尝试更新不存在的模型配置: {ModelId}", modelId);
                throw new KeyNotFoundException($"模型不存在: {modelId}");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "更新模型配置失败: {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// 初始化默认模型
    /// </summary>
    private void InitializeDefaultModels()
    {
        try
        {
            // 添加一些默认模型配置
            var defaultModels = new[]
            {
                new ModelRegistration
                {
                    ModelInfo = new ModelInfo
                    {
                        ModelId = "gpt-3.5-turbo",
                        DisplayName = "GPT-3.5 Turbo",
                        ProviderName = "OpenAI",
                        Description = "OpenAI的GPT-3.5 Turbo模型",
                        Capabilities = new ModelCapabilities
                        {
                            SupportsTextGeneration = true,
                            SupportsFunctionCalling = true,
                            SupportsStreaming = true
                        },
                        IsAvailable = true,
                        PerformanceScore = 85.0
                    },
                    Configuration = new ModelConfiguration
                    {
                        ModelId = "gpt-3.5-turbo",
                        ProviderId = "openai",
                        DisplayName = "GPT-3.5 Turbo",
                        IsEnabled = true,
                        Limitations = new ModelLimitations
                        {
                            MaxContextLength = 4096,
                            CostPerThousandTokens = 0.002m
                        }
                    }
                }
            };

            foreach (var model in defaultModels)
            {
                _ = RegisterModelAsync(model);
            }

            _logger.LogInformation("初始化默认模型完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化默认模型失败");
        }
    }

    /// <summary>
    /// 验证模型注册
    /// </summary>
    private static ValidationResult ValidateModelRegistration(ModelRegistration registration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(registration.ModelInfo.ModelId))
            errors.Add("ModelId不能为空");

        if (string.IsNullOrWhiteSpace(registration.ModelInfo.ProviderName))
            errors.Add("ProviderName不能为空");

        if (string.IsNullOrWhiteSpace(registration.Configuration.ModelId))
            errors.Add("Configuration.ModelId不能为空");

        if (registration.ModelInfo.ModelId != registration.Configuration.ModelId)
            errors.Add("ModelInfo和Configuration的ModelId必须一致");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// 更新能力索引
    /// </summary>
    private void UpdateCapabilityIndex(string modelId, ModelCapabilities capabilities)
    {
        var supportedCapabilities = new List<ModelCapability>();

        if (capabilities.SupportsTextGeneration)
            supportedCapabilities.Add(ModelCapability.TextGeneration);

        if (capabilities.SupportsFunctionCalling)
            supportedCapabilities.Add(ModelCapability.FunctionCalling);

        if (capabilities.SupportsStreaming)
            supportedCapabilities.Add(ModelCapability.Streaming);

        if (capabilities.SupportsEmbedding)
            supportedCapabilities.Add(ModelCapability.Embedding);

        if (capabilities.SupportsVision)
            supportedCapabilities.Add(ModelCapability.Vision);

        foreach (var capability in supportedCapabilities)
        {
            _capabilityIndex.AddOrUpdate(
                capability,
                new List<string> { modelId },
                (key, existingList) =>
                {
                    if (!existingList.Contains(modelId))
                    {
                        existingList.Add(modelId);
                    }
                    return existingList;
                });
        }
    }
}
