# LLM适配器实现

本项目实现了基于Microsoft.SemanticKernel的LLM适配器，提供了统一的LLM服务抽象层，支持多种LLM提供商（OpenAI、Azure OpenAI、Ollama等）。

## 架构概览

```
┌─────────────────────────────────────────┐
│             应用层                        │
├─────────────────────────────────────────┤
│           LLM域服务层                     │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │LLMService│ │ModelMgr │ │RequestR │    │
│  └─────────┘ └─────────┘ └─────────┘    │
├─────────────────────────────────────────┤
│          基础设施层                       │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │  Cache  │ │ Metrics │ │LoadBal  │    │
│  └─────────┘ └─────────┘ └─────────┘    │
├─────────────────────────────────────────┤
│        SemanticKernel                   │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │ OpenAI  │ │ Azure   │ │ Ollama  │    │
│  └─────────┘ └─────────┘ └─────────┘    │
└─────────────────────────────────────────┘
```

## 核心组件

### 1. 服务层 (Services/)

#### ILLMService / LLMService
- **职责**: 提供统一的LLM服务接口
- **功能**: 
  - 异步请求处理 (`SendRequestAsync`)
  - 流式响应支持 (`SendStreamRequestAsync`) 
  - 批量处理 (`SendBatchRequestAsync`)
  - 集成缓存、指标收集和错误处理

#### IModelManager / ModelManager  
- **职责**: 管理LLM模型配置和生命周期
- **功能**:
  - 模型配置CRUD操作
  - 模型能力查询 (`GetModelCapabilitiesAsync`)
  - 模型可用性检查 (`CheckModelAvailabilityAsync`)
  - 支持模型的动态加载和卸载

#### IKernelManager / KernelManager
- **职责**: 管理SemanticKernel实例
- **功能**:
  - Kernel创建和缓存 (`GetKernelAsync`)
  - 多模型Kernel管理 (`GetKernelsAsync`)
  - 自动服务注册和配置
  - Kernel生命周期管理

#### IRequestRouter / RequestRouter
- **职责**: 智能请求路由
- **功能**:
  - 基于请求特征的模型选择
  - 负载均衡策略应用
  - 故障转移和重试机制
  - 性能优化的路由决策

### 2. 基础设施层 (Infrastructure/)

#### IResponseCache / ResponseCache
- **职责**: 响应缓存管理
- **功能**:
  - 内存缓存 (MemoryCache)
  - 分布式缓存支持 (IDistributedCache)
  - SHA256哈希键生成
  - 智能缓存失效策略

#### IMetricsCollector / MetricsCollector
- **职责**: 性能指标收集
- **功能**:
  - 响应时间统计
  - 成功率监控
  - 使用量统计
  - 健康状态检查
  - 并发安全的指标聚合

#### ILoadBalancingStrategy / *LoadBalancingStrategy
- **职责**: 负载均衡策略
- **实现**:
  - `RoundRobinLoadBalancingStrategy`: 轮询策略
  - `RandomLoadBalancingStrategy`: 随机策略  
  - `PerformanceBasedLoadBalancingStrategy`: 性能优先策略

### 3. 共享契约层 (Shared.Contracts/)

#### 数据模型
- `LLMRequest/LLMResponse`: 统一的请求/响应模型
- `ModelConfiguration`: 模型配置定义
- `ModelCapabilities`: 模型能力描述
- `PerformanceMetrics`: 性能指标数据

#### 枚举和常量
- `LoadBalancingStrategyType`: 负载均衡策略类型
- `HealthStatus`: 健康状态枚举
- `ValidationResult`: 验证结果模型

## 使用方式

### 1. 服务注册

```csharp
// 在Startup.cs或Program.cs中
services.AddLLMDomainServices(options =>
{
    // 配置缓存
    options.CacheOptions.DefaultExpirationMinutes = 60;
    options.CacheOptions.EnableDistributedCache = true;
    
    // 配置指标
    options.MetricsOptions.EnableMetrics = true;
    options.MetricsOptions.PerformanceThresholdMs = 3000;
    
    // 配置负载均衡
    options.LoadBalancingOptions.DefaultStrategy = LoadBalancingStrategyType.PerformanceBased;
});
```

### 2. 基本使用

```csharp
public class ChatController : ControllerBase
{
    private readonly ILLMService _llmService;
    
    public ChatController(ILLMService llmService)
    {
        _llmService = llmService;
    }
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var llmRequest = new LLMRequest
        {
            Messages = new ChatHistory(),
            ModelId = "gpt-3.5-turbo",
            ExecutionSettings = new PromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.7
            }
        };
        
        llmRequest.Messages.AddUserMessage(request.Message);
        
        var response = await _llmService.SendRequestAsync(llmRequest);
        
        return Ok(new { response = response.Content?.Content });
    }
}
```

### 3. 流式响应

```csharp
[HttpPost("chat/stream")]
public async IAsyncEnumerable<string> ChatStream([FromBody] ChatRequest request)
{
    var llmRequest = new LLMRequest
    {
        Messages = new ChatHistory(),
        ModelId = "gpt-3.5-turbo"
    };
    
    llmRequest.Messages.AddUserMessage(request.Message);
    
    await foreach (var chunk in _llmService.SendStreamRequestAsync(llmRequest))
    {
        if (chunk.IsSuccess && !string.IsNullOrEmpty(chunk.Content?.Content))
        {
            yield return chunk.Content.Content;
        }
    }
}
```

### 4. 模型管理

```csharp
public class ModelController : ControllerBase
{
    private readonly IModelManager _modelManager;
    
    [HttpPost("models")]
    public async Task<IActionResult> AddModel([FromBody] ModelConfiguration config)
    {
        await _modelManager.AddModelAsync(config);
        return Ok();
    }
    
    [HttpGet("models")]
    public async Task<IActionResult> GetModels()
    {
        var models = await _modelManager.GetAvailableModelsAsync();
        return Ok(models);
    }
    
    [HttpGet("models/{modelId}/capabilities")]
    public async Task<IActionResult> GetCapabilities(string modelId)
    {
        var capabilities = await _modelManager.GetModelCapabilitiesAsync(modelId);
        return Ok(capabilities);
    }
}
```

## 配置选项

### 缓存配置
```csharp
public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int MaxCacheSize { get; set; } = 1000;
    public bool EnableDistributedCache { get; set; } = false;
}
```

### 指标配置
```csharp
public class MetricsOptions
{
    public bool EnableMetrics { get; set; } = true;
    public int MetricsRetentionHours { get; set; } = 24;
    public int PerformanceThresholdMs { get; set; } = 5000;
}
```

### 负载均衡配置
```csharp
public class LoadBalancingOptions
{
    public LoadBalancingStrategyType DefaultStrategy { get; set; } = LoadBalancingStrategyType.RoundRobin;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
}
```

## 扩展性

### 1. 添加新的LLM提供商

1. 在 `LLMServiceRegistry` 中添加新的注册方法
2. 更新 `ValidateConfiguration` 方法以支持新提供商的验证
3. 添加对应的SemanticKernel连接器包引用

### 2. 自定义负载均衡策略

1. 实现 `ILoadBalancingStrategy` 接口
2. 在 `LoadBalancingStrategyFactory` 中注册新策略
3. 更新 `LoadBalancingStrategyType` 枚举

### 3. 自定义缓存策略

1. 实现 `IResponseCache` 接口
2. 可选择实现 `ICacheSerializer` 以支持自定义序列化
3. 在依赖注入中替换默认实现

## 性能特性

- **缓存**: 支持内存和分布式缓存，避免重复请求
- **连接池**: 基于SemanticKernel的连接复用
- **异步处理**: 全异步API设计，支持高并发
- **负载均衡**: 智能路由，提高可用性和性能
- **指标监控**: 实时性能监控和健康检查
- **故障恢复**: 自动重试和故障转移机制

## 依赖项

- Microsoft.SemanticKernel (1.29.0)
- Microsoft.Extensions.* (9.0.0)
- System.Text.Json (9.0.0)
- System.Diagnostics.DiagnosticSource (9.0.0)

## 项目结构

```
Domain/Lorn.OpenAgenticAI.Domain.LLM/
├── Services/                    # 域服务实现
│   ├── LLMServiceNew.cs
│   ├── ModelManagerNew.cs
│   ├── KernelManagerNew.cs
│   ├── RequestRouterNew.cs
│   └── LLMServiceRegistryNew.cs
├── Infrastructure/              # 基础设施实现
│   ├── ResponseCache.cs
│   ├── MetricsCollector.cs
│   ├── JsonCacheSerializer.cs
│   └── *LoadBalancingStrategy.cs
├── Examples/                    # 使用示例
│   └── LLMAdapterExample.cs
├── ServiceRegistrationExtensions.cs
└── README.md
```

这个实现提供了完整的LLM适配器功能，支持企业级的扩展性、性能和可维护性需求。
