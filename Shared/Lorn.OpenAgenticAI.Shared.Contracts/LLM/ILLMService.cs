using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// LLM服务的核心接口
/// 基于SemanticKernel，提供统一的LLM服务抽象
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// 生成文本响应（非流式）
    /// </summary>
    /// <param name="request">LLM请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>LLM响应</returns>
    Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成流式文本响应
    /// </summary>
    /// <param name="request">LLM请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式响应</returns>
    IAsyncEnumerable<LLMStreamResponse> GenerateTextStreamAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理对话历史
    /// </summary>
    /// <param name="history">对话历史</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>LLM响应</returns>
    Task<LLMResponse> ProcessConversationAsync(ChatHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// 调用函数
    /// </summary>
    /// <param name="request">函数调用请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>函数调用响应</returns>
    Task<FunctionCallResponse> CallFunctionAsync(FunctionCallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成嵌入向量
    /// </summary>
    /// <param name="request">嵌入请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>嵌入响应</returns>
    Task<EmbeddingResponse> GenerateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型信息列表</returns>
    Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Kernel管理器接口
/// 管理SemanticKernel实例的生命周期
/// </summary>
public interface IKernelManager
{
    /// <summary>
    /// 获取指定模型的Kernel实例
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Kernel实例</returns>
    Task<Kernel> GetKernelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新的Kernel实例
    /// </summary>
    /// <param name="config">模型配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Kernel实例</returns>
    Task<Kernel> CreateKernelAsync(ModelConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册服务到Kernel
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="serviceType">服务类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RegisterServiceAsync(string modelId, Type serviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="modelId">模型ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务实例</returns>
    Task<T> GetServiceAsync<T>(string modelId, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 获取最优Kernel实例
    /// </summary>
    /// <param name="criteria">选择条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Kernel实例</returns>
    Task<Kernel> GetOptimalKernelAsync(KernelSelectionCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// 销毁指定模型的Kernel实例
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task DisposeKernelAsync(string modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 模型管理器接口
/// 管理模型元数据和配置
/// </summary>
public interface IModelManager
{
    /// <summary>
    /// 注册模型
    /// </summary>
    /// <param name="registration">模型注册信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RegisterModelAsync(ModelRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取模型信息
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型信息</returns>
    Task<ModelInfo> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据能力获取模型列表
    /// </summary>
    /// <param name="capability">模型能力</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型信息列表</returns>
    Task<IEnumerable<ModelInfo>> GetModelsByCapabilityAsync(ModelCapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// 选择最优模型
    /// </summary>
    /// <param name="criteria">选择条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型信息</returns>
    Task<ModelInfo> SelectOptimalModelAsync(ModelSelectionCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取模型配置
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型配置</returns>
    Task<ModelConfiguration> GetModelConfigurationAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模型配置
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <param name="config">模型配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task UpdateModelConfigurationAsync(string modelId, ModelConfiguration config, CancellationToken cancellationToken = default);
}

/// <summary>
/// 请求路由器接口
/// 智能路由请求到最优的Kernel实例
/// </summary>
public interface IRequestRouter
{
    /// <summary>
    /// 路由请求
    /// </summary>
    /// <param name="request">LLM请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>路由结果</returns>
    Task<RoutedRequest> RouteRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 选择Kernel实例
    /// </summary>
    /// <param name="criteria">路由条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Kernel实例</returns>
    Task<Kernel> SelectKernelAsync(RoutingCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取负载均衡策略
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>负载均衡策略</returns>
    Task<ILoadBalancingStrategy> GetLoadBalancingStrategyAsync(CancellationToken cancellationToken = default);
}
