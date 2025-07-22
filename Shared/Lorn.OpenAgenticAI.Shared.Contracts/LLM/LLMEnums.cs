namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 模型能力枚举
/// </summary>
public enum ModelCapability
{
    /// <summary>
    /// 文本生成
    /// </summary>
    TextGeneration,

    /// <summary>
    /// 函数调用
    /// </summary>
    FunctionCalling,

    /// <summary>
    /// 流式处理
    /// </summary>
    Streaming,

    /// <summary>
    /// 向量嵌入
    /// </summary>
    Embedding,

    /// <summary>
    /// 视觉处理
    /// </summary>
    Vision,

    /// <summary>
    /// 代码生成
    /// </summary>
    CodeGeneration,

    /// <summary>
    /// 翻译
    /// </summary>
    Translation,

    /// <summary>
    /// 摘要
    /// </summary>
    Summarization
}

/// <summary>
/// 性能优先级
/// </summary>
public enum PerformancePriority
{
    /// <summary>
    /// 速度优先
    /// </summary>
    Speed,

    /// <summary>
    /// 质量优先
    /// </summary>
    Quality,

    /// <summary>
    /// 成本优先
    /// </summary>
    Cost,

    /// <summary>
    /// 平衡
    /// </summary>
    Balanced
}

/// <summary>
/// 请求类型
/// </summary>
public enum RequestType
{
    /// <summary>
    /// 文本生成
    /// </summary>
    TextGeneration,

    /// <summary>
    /// 函数调用
    /// </summary>
    FunctionCall,

    /// <summary>
    /// 向量嵌入
    /// </summary>
    Embedding,

    /// <summary>
    /// 流式处理
    /// </summary>
    Streaming
}

/// <summary>
/// 优先级
/// </summary>
public enum Priority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low,

    /// <summary>
    /// 普通优先级
    /// </summary>
    Normal,

    /// <summary>
    /// 高优先级
    /// </summary>
    High,

    /// <summary>
    /// 关键优先级
    /// </summary>
    Critical
}
