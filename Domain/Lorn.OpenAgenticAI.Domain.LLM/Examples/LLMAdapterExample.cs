using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;
using Lorn.OpenAgenticAI.Domain.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Examples;

/// <summary>
/// LLM适配器使用示例
/// 演示如何配置和使用LLM服务
/// </summary>
public class LLMAdapterExample
{
    /// <summary>
    /// 配置服务示例
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // 1. 添加基础服务
        services.AddLogging(builder => builder.AddConsole());

        // 2. 添加缓存服务
        services.AddMemoryCache();
        // 可选：添加分布式缓存
        // services.AddStackExchangeRedisCache(options => {
        //     options.Configuration = "localhost:6379";
        // });

        // 3. 添加LLM域服务
        services.AddLLMDomainServices(options =>
        {
            // 配置缓存选项
            options.CacheOptions.DefaultExpirationMinutes = 60;
            options.CacheOptions.MaxCacheSize = 2000;
            options.CacheOptions.EnableDistributedCache = false;

            // 配置指标选项
            options.MetricsOptions.EnableMetrics = true;
            options.MetricsOptions.MetricsRetentionHours = 48;
            options.MetricsOptions.PerformanceThresholdMs = 3000;

            // 配置负载均衡选项
            options.LoadBalancingOptions.DefaultStrategy = LoadBalancingStrategyType.PerformanceBased;
            options.LoadBalancingOptions.HealthCheckIntervalSeconds = 60;
            options.LoadBalancingOptions.MaxRetryAttempts = 3;
        });
    }

    /// <summary>
    /// 使用LLM服务示例
    /// </summary>
    public static async Task<string> UseLLMServiceExample(IServiceProvider serviceProvider)
    {
        var llmService = serviceProvider.GetRequiredService<ILLMService>();
        var logger = serviceProvider.GetRequiredService<ILogger<LLMAdapterExample>>();

        try
        {
            // 创建聊天请求
            var request = new LLMRequest
            {
                UserPrompt = "请介绍一下人工智能的发展历史",
                ModelId = "gpt-3.5-turbo",
                ExecutionSettings = new PromptExecutionSettings()
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["max_tokens"] = 1000,
                        ["temperature"] = 0.7
                    }
                }
            };

            logger.LogInformation("发送LLM请求: {ModelId}", request.ModelId);

            // 发送请求并获取响应
            var response = await llmService.GenerateTextAsync(request);

            if (response.IsSuccess && !string.IsNullOrEmpty(response.Content))
            {
                logger.LogInformation("收到LLM响应: 长度={Length}, 模型={ModelId}",
                    response.Content.Length, response.ModelId);

                return response.Content;
            }
            else
            {
                logger.LogWarning("LLM请求失败: {Error}", response.ErrorMessage);
                return $"请求失败: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM服务调用异常");
            return $"服务调用异常: {ex.Message}";
        }
    }

    /// <summary>
    /// 模型管理示例
    /// </summary>
    public static async Task ModelManagementExample(IServiceProvider serviceProvider)
    {
        var modelManager = serviceProvider.GetRequiredService<IModelManager>();
        var logger = serviceProvider.GetRequiredService<ILogger<LLMAdapterExample>>();

        try
        {
            // 创建模型注册信息
            var registration = new ModelRegistration
            {
                ModelInfo = new ModelInfo
                {
                    ModelId = "gpt-3.5-turbo",
                    DisplayName = "GPT-3.5 Turbo",
                    ProviderName = "openai",
                    Description = "OpenAI GPT-3.5 Turbo模型",
                    Capabilities = new ModelCapabilities
                    {
                        SupportsTextGeneration = true,
                        SupportsStreaming = true,
                        SupportedLanguages = ["zh-CN", "en-US"]
                    },
                    Limitations = new ModelLimitations
                    {
                        MaxContextLength = 4096,
                        MaxOutputTokens = 4096
                    }
                },
                Configuration = new ModelConfiguration
                {
                    ModelId = "gpt-3.5-turbo",
                    ProviderId = "openai",
                    DisplayName = "GPT-3.5 Turbo",
                    ApiKey = "your-openai-api-key" // 实际使用时应从配置或环境变量获取
                }
            };

            // 注册模型
            await modelManager.RegisterModelAsync(registration);
            logger.LogInformation("已注册模型: {ModelId}", registration.ModelInfo.ModelId);

            // 获取模型信息
            var modelInfo = await modelManager.GetModelInfoAsync("gpt-3.5-turbo");
            logger.LogInformation("模型信息: {ModelId}, 提供商: {ProviderName}, 显示名: {DisplayName}",
                modelInfo.ModelId, modelInfo.ProviderName, modelInfo.DisplayName);

            // 根据能力获取模型
            var textGenerationModels = await modelManager.GetModelsByCapabilityAsync(
                ModelCapability.TextGeneration);
            logger.LogInformation("支持文本生成的模型数量: {Count}", textGenerationModels.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "模型管理操作异常");
        }
    }

    /// <summary>
    /// 指标监控示例
    /// </summary>
    public static async Task MetricsMonitoringExample(IServiceProvider serviceProvider)
    {
        var metricsCollector = serviceProvider.GetRequiredService<IMetricsCollector>();
        var logger = serviceProvider.GetRequiredService<ILogger<LLMAdapterExample>>();

        try
        {
            // 获取性能指标
            var performanceMetrics = await metricsCollector.GetMetricsAsync("gpt-3.5-turbo", TimeSpan.FromHours(24));

            if (performanceMetrics != null)
            {
                logger.LogInformation("模型性能指标 - 平均响应时间: {AvgResponseTime}ms, " +
                    "成功率: {SuccessRate:P2}, 总请求数: {TotalRequests}",
                    performanceMetrics.AverageResponseTime,
                    performanceMetrics.SuccessRate,
                    performanceMetrics.TotalRequests);
            }

            // 获取健康状态
            var healthStatus = await metricsCollector.GetHealthStatusAsync("gpt-3.5-turbo");
            logger.LogInformation("模型健康状态: {Status}, 是否健康: {IsHealthy}",
                healthStatus.Status, healthStatus.IsHealthy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "指标监控操作异常");
        }
    }
}

/// <summary>
/// 完整的控制台应用程序示例
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // 创建主机构建器
        var builder = Host.CreateDefaultBuilder(args);

        // 配置服务
        builder.ConfigureServices((context, services) =>
        {
            LLMAdapterExample.ConfigureServices(services);
        });

        // 构建主机
        var host = builder.Build();

        // 获取服务提供者
        var serviceProvider = host.Services;

        // 运行示例
        try
        {
            Console.WriteLine("=== LLM适配器使用示例 ===");

            // 1. 模型管理示例
            Console.WriteLine("\n1. 模型管理示例:");
            await LLMAdapterExample.ModelManagementExample(serviceProvider);

            // 2. LLM服务使用示例
            Console.WriteLine("\n2. LLM服务使用示例:");
            var response = await LLMAdapterExample.UseLLMServiceExample(serviceProvider);
            Console.WriteLine($"AI响应: {response}");

            // 3. 指标监控示例
            Console.WriteLine("\n3. 指标监控示例:");
            await LLMAdapterExample.MetricsMonitoringExample(serviceProvider);

            Console.WriteLine("\n=== 示例执行完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例执行失败: {ex.Message}");
        }

        // 等待用户输入后退出
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
