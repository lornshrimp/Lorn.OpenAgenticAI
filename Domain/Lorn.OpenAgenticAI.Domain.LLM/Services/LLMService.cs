using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;
using System.Runtime.CompilerServices;

namespace Lorn.OpenAgenticAI.Domain.LLM.Services;

/// <summary>
/// LLM服务实现
/// 基于SemanticKernel提供统一的LLM服务接口
/// </summary>
public class LLMService : ILLMService
{
    private readonly IRequestRouter _requestRouter;
    private readonly IKernelManager _kernelManager;
    private readonly IResponseCache _responseCache;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ILogger<LLMService> _logger;

    public LLMService(
        IRequestRouter requestRouter,
        IKernelManager kernelManager,
        IResponseCache responseCache,
        IMetricsCollector metricsCollector,
        ILogger<LLMService> logger)
    {
        _requestRouter = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
        _kernelManager = kernelManager ?? throw new ArgumentNullException(nameof(kernelManager));
        _responseCache = responseCache ?? throw new ArgumentNullException(nameof(responseCache));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var startTime = DateTime.UtcNow;
        var responseId = Guid.NewGuid().ToString();
        var trackingId = _metricsCollector.StartRequest(request.ModelId, "TextGeneration");

        try
        {
            // 验证请求
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("请求验证失败: {Errors}", string.Join(", ", validationResult.Errors));
                var errorResponse = CreateErrorResponse(responseId, request.ModelId, "请求验证失败: " + string.Join(", ", validationResult.Errors), startTime);
                _metricsCollector.EndRequest(trackingId, false, DateTime.UtcNow - startTime);
                return errorResponse;
            }

            // 检查缓存
            var cacheKey = _responseCache.GenerateCacheKey(request);
            var cachedResponse = await _responseCache.GetAsync<LLMResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogDebug("缓存命中，返回缓存结果: {ResponseId}", cachedResponse.ResponseId);
                _metricsCollector.RecordCacheHit(request.ModelId, "memory");
                _metricsCollector.EndRequest(trackingId, true, DateTime.UtcNow - startTime);

                // 更新响应ID和时间
                cachedResponse.ResponseId = responseId;
                cachedResponse.Metadata.Timestamp = DateTime.UtcNow;
                cachedResponse.Metadata.CacheHit = true;

                return cachedResponse;
            }

            _metricsCollector.RecordCacheMiss(request.ModelId, "memory");

            // 路由请求
            var routedRequest = await _requestRouter.RouteRequestAsync(request, cancellationToken);

            // 获取Kernel实例
            var kernel = await _kernelManager.GetKernelAsync(routedRequest.SelectedModelId, cancellationToken);

            // 获取IChatCompletionService
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 构建ChatHistory
            var chatHistory = BuildChatHistory(request);

            // 调用SemanticKernel
            var chatResults = await chatService.GetChatMessageContentsAsync(
                chatHistory,
                request.ExecutionSettings,
                kernel,
                cancellationToken);

            // 转换响应
            var response = ConvertToLLMResponse(chatResults, responseId, routedRequest.SelectedModelId, startTime);
            response.Metadata.ProcessingNode = Environment.MachineName;

            // 更新缓存
            var cacheExpiry = TimeSpan.FromMinutes(30); // 可配置
            await _responseCache.SetAsync(cacheKey, response, cacheExpiry, cancellationToken);

            // 记录指标
            _metricsCollector.EndRequest(trackingId, true, response.Duration, response.Usage);

            _logger.LogDebug("请求处理成功: {ResponseId}, 模型: {ModelId}", responseId, routedRequest.SelectedModelId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理LLM请求时发生错误: {RequestId}", responseId);
            _metricsCollector.EndRequest(trackingId, false, DateTime.UtcNow - startTime);
            _metricsCollector.RecordError(request.ModelId, ex.GetType().Name, ex);

            return CreateErrorResponse(responseId, request.ModelId, ex.Message, startTime);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LLMStreamResponse> GenerateTextStreamAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var responseId = Guid.NewGuid().ToString();
        var trackingId = _metricsCollector.StartRequest(request.ModelId, "StreamingTextGeneration");
        var startTime = DateTime.UtcNow;
        var sequenceNumber = 0;

        // 验证请求
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("流式请求验证失败: {Errors}", string.Join(", ", validationResult.Errors));
            yield break;
        }

        // 路由请求 - 这里可能抛出异常，但不在yield方法中处理
        var routedRequest = await _requestRouter.RouteRequestAsync(request, cancellationToken);

        // 获取Kernel实例 - 这里可能抛出异常，但不在yield方法中处理
        var kernel = await _kernelManager.GetKernelAsync(routedRequest.SelectedModelId, cancellationToken);

        // 获取IChatCompletionService
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // 构建ChatHistory
        var chatHistory = BuildChatHistory(request);

        // 流式调用SemanticKernel
        await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            request.ExecutionSettings,
            kernel,
            cancellationToken))
        {
            var streamResponse = new LLMStreamResponse
            {
                ResponseId = responseId,
                ModelId = routedRequest.SelectedModelId,
                DeltaContent = content.Content ?? string.Empty,
                IsComplete = false,
                SequenceNumber = sequenceNumber++,
                Timestamp = DateTime.UtcNow
            };

            yield return streamResponse;
        }

        // 发送完成标记
        yield return new LLMStreamResponse
        {
            ResponseId = responseId,
            ModelId = routedRequest.SelectedModelId,
            DeltaContent = string.Empty,
            IsComplete = true,
            SequenceNumber = sequenceNumber,
            Timestamp = DateTime.UtcNow
        };

        // 记录成功指标
        var duration = DateTime.UtcNow - startTime;
        _metricsCollector.EndRequest(trackingId, true, duration);

        _logger.LogDebug("流式请求处理成功: {ResponseId}, 模型: {ModelId}", responseId, routedRequest.SelectedModelId);
    }

    /// <inheritdoc />
    public async Task<LLMResponse> ProcessConversationAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
        if (history == null || history.Count == 0)
            throw new ArgumentNullException(nameof(history));

        // 转换为LLMRequest
        var request = new LLMRequest
        {
            ConversationHistory = history,
            ModelId = "default", // 可以从配置中获取
            Metadata = new RequestMetadata
            {
                Source = "ConversationProcessor"
            }
        };

        return await GenerateTextAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FunctionCallResponse> CallFunctionAsync(FunctionCallRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var trackingId = _metricsCollector.StartRequest("function", "FunctionCall");
        var startTime = DateTime.UtcNow;

        try
        {
            // 获取默认Kernel
            var kernel = await _kernelManager.GetKernelAsync("default", cancellationToken);

            // 这里需要根据实际的Function调用逻辑来实现
            // 暂时返回一个基本的响应
            var response = new FunctionCallResponse
            {
                FunctionName = request.FunctionName,
                Result = "Function call not implemented yet",
                IsSuccess = false,
                ErrorMessage = "Function calling feature is not yet implemented"
            };

            var duration = DateTime.UtcNow - startTime;
            _metricsCollector.EndRequest(trackingId, false, duration);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "函数调用失败: {FunctionName}", request.FunctionName);
            var duration = DateTime.UtcNow - startTime;
            _metricsCollector.EndRequest(trackingId, false, duration);
            _metricsCollector.RecordError("function", ex.GetType().Name, ex);

            return new FunctionCallResponse
            {
                FunctionName = request.FunctionName,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<EmbeddingResponse> GenerateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var trackingId = _metricsCollector.StartRequest(request.ModelId, "Embedding");
        var startTime = DateTime.UtcNow;

        try
        {
            // 获取Kernel实例
            var kernel = await _kernelManager.GetKernelAsync(request.ModelId, cancellationToken);

            // 这里需要根据实际的Embedding生成逻辑来实现
            // 暂时返回一个基本的响应
            var response = new EmbeddingResponse
            {
                ModelId = request.ModelId,
                IsSuccess = false,
                ErrorMessage = "Embedding generation is not yet implemented"
            };

            var duration = DateTime.UtcNow - startTime;
            _metricsCollector.EndRequest(trackingId, false, duration);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成嵌入向量失败: {ModelId}", request.ModelId);
            var duration = DateTime.UtcNow - startTime;
            _metricsCollector.EndRequest(trackingId, false, duration);
            _metricsCollector.RecordError(request.ModelId, ex.GetType().Name, ex);

            return new EmbeddingResponse
            {
                ModelId = request.ModelId,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该从模型管理器获取可用模型列表
            // 暂时返回空列表
            await Task.CompletedTask;
            return new List<ModelInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用模型列表失败");
            return new List<ModelInfo>();
        }
    }

    /// <summary>
    /// 验证请求
    /// </summary>
    private static ValidationResult ValidateRequest(LLMRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ModelId))
            errors.Add("ModelId不能为空");

        if (string.IsNullOrWhiteSpace(request.UserPrompt) &&
            (request.ConversationHistory == null || request.ConversationHistory.Count == 0))
            errors.Add("UserPrompt和ConversationHistory不能同时为空");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// 构建ChatHistory
    /// </summary>
    private static ChatHistory BuildChatHistory(LLMRequest request)
    {
        var chatHistory = request.ConversationHistory ?? new ChatHistory();

        // 如果有系统提示词，添加到开头
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt) &&
            (chatHistory.Count == 0 || chatHistory[0].Role != AuthorRole.System))
        {
            chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, request.SystemPrompt));
        }

        // 如果有用户提示词，添加到末尾
        if (!string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            chatHistory.AddUserMessage(request.UserPrompt);
        }

        return chatHistory;
    }

    /// <summary>
    /// 转换SemanticKernel响应为LLMResponse
    /// </summary>
    private static LLMResponse ConvertToLLMResponse(
        IReadOnlyList<ChatMessageContent> chatResults,
        string responseId,
        string modelId,
        DateTime startTime)
    {
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        var content = string.Join("", chatResults.Select(r => r.Content));
        var response = new LLMResponse
        {
            ResponseId = responseId,
            ModelId = modelId,
            Content = content,
            Duration = duration,
            IsSuccess = true,
            Metadata = new ResponseMetadata
            {
                Timestamp = endTime
            }
        };

        // 提取使用统计（如果有）
        var firstResult = chatResults.FirstOrDefault();
        if (firstResult?.Metadata != null)
        {
            response.ChatMessageContent = firstResult;

            // 尝试从元数据中提取Token使用情况
            if (firstResult.Metadata.TryGetValue("Usage", out var usageObj))
            {
                // 这里需要根据实际的元数据结构来解析
                // 不同的LLM提供商可能有不同的格式
            }
        }

        return response;
    }

    /// <summary>
    /// 创建错误响应
    /// </summary>
    private static LLMResponse CreateErrorResponse(string responseId, string modelId, string errorMessage, DateTime startTime)
    {
        var endTime = DateTime.UtcNow;
        return new LLMResponse
        {
            ResponseId = responseId,
            ModelId = modelId,
            Content = string.Empty,
            Duration = endTime - startTime,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Metadata = new ResponseMetadata
            {
                Timestamp = endTime
            }
        };
    }
}
