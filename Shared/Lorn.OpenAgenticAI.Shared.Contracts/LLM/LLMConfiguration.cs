using Microsoft.SemanticKernel;

namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 模型配置
/// 管理SemanticKernel服务注册所需的配置信息
/// </summary>
public class ModelConfiguration
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 提供商ID
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 终端地址
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 执行设置（SemanticKernel原生对象）
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings { get; set; }

    /// <summary>
    /// 模型能力
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// 模型限制
    /// </summary>
    public ModelLimitations Limitations { get; set; } = new();

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Kernel配置
/// 管理完整的Kernel实例配置
/// </summary>
public class KernelConfiguration
{
    /// <summary>
    /// Kernel ID
    /// </summary>
    public string KernelId { get; set; } = string.Empty;

    /// <summary>
    /// 模型配置集合
    /// </summary>
    public List<ModelConfiguration> ModelConfigurations { get; set; } = new();

    /// <summary>
    /// 服务注册列表
    /// </summary>
    public List<ServiceRegistration> Services { get; set; } = new();

    /// <summary>
    /// 插件配置
    /// </summary>
    public List<PluginRegistration> Plugins { get; set; } = new();

    /// <summary>
    /// 默认模型
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 模型能力
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// 支持文本生成
    /// </summary>
    public bool SupportsTextGeneration { get; set; } = true;

    /// <summary>
    /// 支持函数调用
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// 支持流式响应
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// 支持嵌入生成
    /// </summary>
    public bool SupportsEmbedding { get; set; }

    /// <summary>
    /// 支持图像理解
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new();
}

/// <summary>
/// 模型限制
/// </summary>
public class ModelLimitations
{
    /// <summary>
    /// 最大上下文长度
    /// </summary>
    public int MaxContextLength { get; set; }

    /// <summary>
    /// 最大输出Token数
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 请求频率限制（每分钟）
    /// </summary>
    public int? RateLimitPerMinute { get; set; }

    /// <summary>
    /// 每日使用限制
    /// </summary>
    public int? DailyUsageLimit { get; set; }

    /// <summary>
    /// 成本限制（每千Token）
    /// </summary>
    public decimal? CostPerThousandTokens { get; set; }
}

/// <summary>
/// 服务注册信息
/// </summary>
public class ServiceRegistration
{
    /// <summary>
    /// 服务类型
    /// </summary>
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>
    /// 实现类型
    /// </summary>
    public string ImplementationType { get; set; } = string.Empty;

    /// <summary>
    /// 服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// 配置参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 插件注册信息
/// </summary>
public class PluginRegistration
{
    /// <summary>
    /// 插件名称
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 插件类型
    /// </summary>
    public string PluginType { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 配置参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 服务生命周期枚举
/// </summary>
public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}

/// <summary>
/// 模型信息
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 提供商名称
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// 模型描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模型能力
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// 模型限制
    /// </summary>
    public ModelLimitations Limitations { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 性能评分
    /// </summary>
    public double PerformanceScore { get; set; }
}

/// <summary>
/// 模型注册信息
/// </summary>
public class ModelRegistration
{
    /// <summary>
    /// 模型信息
    /// </summary>
    public ModelInfo ModelInfo { get; set; } = new();

    /// <summary>
    /// 模型配置
    /// </summary>
    public ModelConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
}
