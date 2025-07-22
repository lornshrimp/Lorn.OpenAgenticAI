using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json.Serialization;

namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// LLM请求模型
/// 基于SemanticKernel的ChatHistory扩展业务元数据
/// </summary>
public class LLMRequest
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 用户提示词
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 对话历史（SemanticKernel原生对象）
    /// </summary>
    public ChatHistory? ConversationHistory { get; set; }

    /// <summary>
    /// 执行设置（SemanticKernel原生对象）
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings { get; set; }

    /// <summary>
    /// 请求元数据
    /// </summary>
    public RequestMetadata Metadata { get; set; } = new();

    /// <summary>
    /// 取消令牌
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// LLM响应模型
/// 封装SemanticKernel的ChatMessageContent并添加业务统计信息
/// </summary>
public class LLMResponse
{
    /// <summary>
    /// 响应ID
    /// </summary>
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 响应内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 原始ChatMessageContent（SemanticKernel原生对象）
    /// </summary>
    [JsonIgnore]
    public ChatMessageContent? ChatMessageContent { get; set; }

    /// <summary>
    /// 响应元数据
    /// </summary>
    public ResponseMetadata Metadata { get; set; } = new();

    /// <summary>
    /// 使用统计
    /// </summary>
    public UsageStatistics Usage { get; set; } = new();

    /// <summary>
    /// 响应时长
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// LLM流式响应模型
/// </summary>
public class LLMStreamResponse
{
    /// <summary>
    /// 响应ID
    /// </summary>
    public string ResponseId { get; set; } = string.Empty;

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 增量内容
    /// </summary>
    public string DeltaContent { get; set; } = string.Empty;

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 函数调用请求
/// </summary>
public class FunctionCallRequest
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 函数参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 取消令牌
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// 函数调用响应
/// </summary>
public class FunctionCallResponse
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 执行结果
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行时长
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// 嵌入请求
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 取消令牌
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// 嵌入响应
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// 嵌入向量
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 请求元数据
/// </summary>
public class RequestMetadata
{
    /// <summary>
    /// 请求时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 请求来源
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// 响应元数据
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// 响应时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 处理节点
    /// </summary>
    public string? ProcessingNode { get; set; }

    /// <summary>
    /// 缓存命中
    /// </summary>
    public bool CacheHit { get; set; }

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// 使用统计
/// </summary>
public class UsageStatistics
{
    /// <summary>
    /// 输入Token数量
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// 输出Token数量
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// 总Token数量
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// 成本（如果有）
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// 货币单位
    /// </summary>
    public string? Currency { get; set; }
}
