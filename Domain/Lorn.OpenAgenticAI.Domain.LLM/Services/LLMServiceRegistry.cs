#pragma warning disable SKEXP0010 // Ollama connectors are experimental
#pragma warning disable SKEXP0001 // Text embedding generation APIs are experimental
#pragma warning disable SKEXP0070 // Ollama chat completion is experimental
#pragma warning disable SKEXP0071 // Ollama text embedding is experimental

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Services;

/// <summary>
/// LLM服务注册器接口
/// 负责向SemanticKernel注册不同LLM提供商的服务
/// </summary>
public interface ILLMServiceRegistry
{
    /// <summary>
    /// 注册服务到KernelBuilder
    /// </summary>
    /// <param name="builder">KernelBuilder实例</param>
    /// <param name="config">模型配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RegisterServicesAsync(IKernelBuilder builder, ModelConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <param name="config">模型配置</param>
    /// <returns>验证结果</returns>
    ValidationResult ValidateConfiguration(ModelConfiguration config);
}

/// <summary>
/// LLM服务注册器实现
/// 封装SemanticKernel的服务注册复杂性，提供业务友好的注册接口
/// </summary>
public class LLMServiceRegistry : ILLMServiceRegistry
{
    private readonly ILogger<LLMServiceRegistry> _logger;

    public LLMServiceRegistry(ILogger<LLMServiceRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task RegisterServicesAsync(IKernelBuilder builder, ModelConfiguration config, CancellationToken cancellationToken = default)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            _logger.LogDebug("开始注册服务: ModelId={ModelId}, Provider={ProviderId}",
                config.ModelId, config.ProviderId);

            // 验证配置
            var validationResult = ValidateConfiguration(config);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"配置验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 根据提供商类型注册相应的服务
            switch (config.ProviderId.ToLowerInvariant())
            {
                case "openai":
                    RegisterOpenAIServices(builder, config);
                    break;

                case "azure":
                case "azureopenai":
                    RegisterAzureOpenAIServices(builder, config);
                    break;

                case "ollama":
                    RegisterOllamaServices(builder, config);
                    break;

                default:
                    _logger.LogWarning("未知的提供商类型: {ProviderId}", config.ProviderId);
                    throw new NotSupportedException($"不支持的提供商: {config.ProviderId}");
            }

            _logger.LogInformation("成功注册服务: ModelId={ModelId}, Provider={ProviderId}",
                config.ModelId, config.ProviderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册服务失败: ModelId={ModelId}, Provider={ProviderId}",
                config.ModelId, config.ProviderId);
            throw;
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfiguration(ModelConfiguration config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.ModelId))
            errors.Add("ModelId不能为空");

        if (string.IsNullOrWhiteSpace(config.ProviderId))
            errors.Add("ProviderId不能为空");

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            errors.Add("ApiKey不能为空");

        // 根据提供商验证特定配置
        switch (config.ProviderId.ToLowerInvariant())
        {
            case "azure":
            case "azureopenai":
                if (string.IsNullOrWhiteSpace(config.Endpoint))
                    errors.Add("Azure提供商需要指定Endpoint");
                break;

            case "ollama":
                if (string.IsNullOrWhiteSpace(config.Endpoint))
                    errors.Add("Ollama提供商需要指定Endpoint");
                break;
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// 注册OpenAI服务
    /// </summary>
    private void RegisterOpenAIServices(IKernelBuilder builder, ModelConfiguration config)
    {
        try
        {
            _logger.LogDebug("注册OpenAI服务: {ModelId}", config.ModelId);

            // 注册ChatCompletion服务
            if (config.Capabilities.SupportsTextGeneration)
            {
                builder.Services.AddOpenAIChatCompletion(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey,
                    serviceId: config.ModelId);

                _logger.LogDebug("已注册OpenAI ChatCompletion服务: {ModelId}", config.ModelId);
            }

            // 注册TextEmbedding服务（如果支持）
            if (config.Capabilities.SupportsEmbedding)
            {
                builder.Services.AddOpenAITextEmbeddingGeneration(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey,
                    serviceId: $"{config.ModelId}-embedding");

                _logger.LogDebug("已注册OpenAI TextEmbedding服务: {ModelId}", config.ModelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册OpenAI服务失败: {ModelId}", config.ModelId);
            throw;
        }
    }

    /// <summary>
    /// 注册Azure OpenAI服务
    /// </summary>
    private void RegisterAzureOpenAIServices(IKernelBuilder builder, ModelConfiguration config)
    {
        try
        {
            _logger.LogDebug("注册Azure OpenAI服务: {ModelId}", config.ModelId);

            // 注册ChatCompletion服务
            if (config.Capabilities.SupportsTextGeneration)
            {
                builder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: config.ModelId,
                    endpoint: config.Endpoint!,
                    apiKey: config.ApiKey,
                    serviceId: config.ModelId);

                _logger.LogDebug("已注册Azure OpenAI ChatCompletion服务: {ModelId}", config.ModelId);
            }

            // 注册TextEmbedding服务（如果支持）
            if (config.Capabilities.SupportsEmbedding)
            {
                builder.Services.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: config.ModelId,
                    endpoint: config.Endpoint!,
                    apiKey: config.ApiKey,
                    serviceId: $"{config.ModelId}-embedding");

                _logger.LogDebug("已注册Azure OpenAI TextEmbedding服务: {ModelId}", config.ModelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册Azure OpenAI服务失败: {ModelId}", config.ModelId);
            throw;
        }
    }

    /// <summary>
    /// 注册Ollama服务
    /// </summary>
    private void RegisterOllamaServices(IKernelBuilder builder, ModelConfiguration config)
    {
        try
        {
            _logger.LogDebug("注册Ollama服务: {ModelId}", config.ModelId);

            // 注册ChatCompletion服务
            if (config.Capabilities.SupportsTextGeneration)
            {
                builder.Services.AddOllamaChatCompletion(
                    modelId: config.ModelId,
                    endpoint: new Uri(config.Endpoint!),
                    serviceId: config.ModelId);

                _logger.LogDebug("已注册Ollama ChatCompletion服务: {ModelId}", config.ModelId);
            }

            // 注册TextEmbedding服务（如果支持）
            if (config.Capabilities.SupportsEmbedding)
            {
                builder.Services.AddOllamaTextEmbeddingGeneration(
                    modelId: config.ModelId,
                    endpoint: new Uri(config.Endpoint!),
                    serviceId: $"{config.ModelId}-embedding");

                _logger.LogDebug("已注册Ollama TextEmbedding服务: {ModelId}", config.ModelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册Ollama服务失败: {ModelId}", config.ModelId);
            throw;
        }
    }
}
