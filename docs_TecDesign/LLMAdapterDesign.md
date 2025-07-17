# LLM适配器技术设计文档

## 文档信息

- **文档版本**: v1.0
- **创建日期**: 2025年7月4日
- **更新日期**: 2025年7月4日
- **作者**: 技术专家
- **文档类型**: 详细技术设计

## 1. 概述

### 1.1 设计目标

LLM适配器作为Lorn.OpenAgenticAI系统的核心组件，位于`3.Domain/Lorn.OpenAgenticAI.Domain.LLM/`项目中，基于Microsoft.SemanticKernel框架构建，负责业务层面的LLM服务管理和调度。其主要设计目标包括：

- **SemanticKernel集成**: 基于Microsoft.SemanticKernel构建，复用其成熟的LLM抽象层
- **业务逻辑封装**: 在SemanticKernel基础上添加业务特定的模型管理和调度逻辑
- **智能调度**: 实现模型选择、负载均衡和故障转移等高级功能
- **性能优化**: 请求缓存、指标监控和异步处理
- **配置管理**: 统一的模型配置、API密钥管理和权限控制
- **可扩展性**: 支持自定义模型提供商和业务规则扩展

### 1.2 技术架构位置

```mermaid
graph TB
    subgraph "应用服务层"
        DIR[Director调度引擎]
        KM[知识库管理器]
    end
    
    subgraph "领域服务层"
        LLM[LLM适配器]
        MCP[MCP协议引擎]
    end
    
    subgraph "基础设施层"
        CONFIG[配置管理]
        LOG[日志系统]
        DATA[数据存储]
    end
    
    DIR --> LLM
    KM --> LLM
    LLM --> CONFIG
    LLM --> LOG
    LLM --> DATA
    
    style LLM fill:#ff9999
```

### 1.3 核心职责

- **Kernel管理**: 管理SemanticKernel实例的生命周期和配置
- **服务协调**: 协调多个IChatCompletionService实例进行智能调度
- **业务逻辑**: 实现业务特定的模型选择、缓存和监控逻辑
- **配置管理**: 统一管理不同LLM提供商的配置和密钥
- **性能监控**: 监控模型调用性能、成本和可用性
- **扩展支持**: 支持自定义模型提供商和业务规则

## 2. 系统架构设计

### 2.1 整体架构图

```mermaid
graph TB
    subgraph "LLM适配器 (Lorn.OpenAgenticAI.Domain.LLM)"
        subgraph "业务服务层"
            LLMService[LLMService]
            ModelManager[ModelManager]
            RequestRouter[RequestRouter]
        end
        
        subgraph "SemanticKernel集成层"
            KernelManager[KernelManager]
            ServiceRegistry[ServiceRegistry]
            PromptManager[PromptManager]
        end
        
        subgraph "基础组件层"
            ConfigManager[配置管理器]
            ResponseCache[响应缓存]
            MetricsCollector[指标收集器]
        end
    end
    
    subgraph "Microsoft.SemanticKernel框架"
        Kernel[Kernel实例]
        ChatService[IChatCompletionService]
        OpenAIConnector[OpenAI Connector]
        AzureConnector[Azure OpenAI Connector]
        CustomConnector[Custom Connector]
    end
    
    subgraph "外部LLM服务"
        OpenAI_API[OpenAI API]
        Azure_API[Azure OpenAI API]
        Claude_API[Claude API]
        Local_LLM[本地LLM]
    end
    
    LLMService --> RequestRouter
    LLMService --> ResponseCache
    LLMService --> MetricsCollector
    
    RequestRouter --> ModelManager
    RequestRouter --> KernelManager
    
    ModelManager --> ConfigManager
    
    KernelManager --> ServiceRegistry
    KernelManager --> PromptManager
    KernelManager --> Kernel
    
    ServiceRegistry --> ChatService
    
    Kernel --> ChatService
    ChatService --> OpenAIConnector
    ChatService --> AzureConnector
    ChatService --> CustomConnector
    
    OpenAIConnector --> OpenAI_API
    AzureConnector --> Azure_API
    CustomConnector --> Claude_API
    CustomConnector --> Local_LLM
```

### 2.2 分层设计

#### 2.2.1 业务服务层

在SemanticKernel基础上实现业务特定的LLM服务逻辑，包括智能调度、缓存管理和监控。

#### 2.2.2 SemanticKernel集成层

封装SemanticKernel的核心组件，提供统一的Kernel管理、服务注册和Prompt管理功能。

#### 2.2.3 基础组件层

提供通用的基础设施服务，如配置管理、缓存、监控等，支撑上层业务逻辑。

#### 2.2.4 Microsoft.SemanticKernel框架层

直接使用SemanticKernel提供的成熟LLM抽象和连接器，无需重复实现。

### 2.3 数据流设计

```mermaid
sequenceDiagram
    participant App as 应用层
    participant LLMService as LLMService
    participant Router as RequestRouter
    participant KernelMgr as KernelManager
    participant Kernel as SemanticKernel
    participant ChatService as IChatCompletionService
    participant Provider as LLM提供商
    participant Cache as ResponseCache
    participant Metrics as MetricsCollector
    
    App->>LLMService: 发送LLM请求
    LLMService->>Cache: 检查缓存
    
    alt 缓存命中
        Cache-->>LLMService: 返回缓存结果
        LLMService-->>App: 返回响应
    else 缓存未命中
        LLMService->>Router: 路由请求
        Router->>KernelMgr: 获取最佳Kernel实例
        KernelMgr-->>Router: 返回Kernel实例
        Router->>Kernel: 调用GetRequiredService<IChatCompletionService>
        Kernel-->>Router: 返回ChatCompletionService
        Router->>ChatService: 调用GetChatMessageContentsAsync
        ChatService->>Provider: 调用外部API
        Provider-->>ChatService: 返回API响应
        ChatService-->>Router: 返回ChatMessageContent
        Router-->>LLMService: 返回标准化响应
        LLMService->>Cache: 更新缓存
        LLMService->>Metrics: 记录指标
        LLMService-->>App: 返回响应
    end
```

## 3. 核心组件详细设计

### 3.1 基于SemanticKernel的核心接口类图

```mermaid
classDiagram
    class ILLMService {
        <<interface>>
        +GenerateTextAsync(request: LLMRequest) Task~LLMResponse~
        +GenerateTextStreamAsync(request: LLMRequest) IAsyncEnumerable~LLMStreamResponse~
        +ProcessConversationAsync(history: ConversationHistory) Task~LLMResponse~
        +CallFunctionAsync(request: FunctionCallRequest) Task~FunctionCallResponse~
        +GenerateEmbeddingAsync(request: EmbeddingRequest) Task~EmbeddingResponse~
        +GetAvailableModelsAsync() Task~IEnumerable~ModelInfo~~
    }
    
    class IKernelManager {
        <<interface>>
        +GetKernelAsync(modelId: string) Task~Kernel~
        +CreateKernelAsync(config: ModelConfiguration) Task~Kernel~
        +RegisterServiceAsync(modelId: string, serviceType: Type) Task
        +GetServiceAsync(modelId: string, serviceType: Type) Task~T~
        +GetOptimalKernelAsync(criteria: KernelSelectionCriteria) Task~Kernel~
        +DisposeKernelAsync(modelId: string) Task
    }
    
    class IModelManager {
        <<interface>>
        +RegisterModelAsync(registration: ModelRegistration) Task
        +GetModelInfoAsync(modelId: string) Task~ModelInfo~
        +GetModelsByCapabilityAsync(capability: ModelCapability) Task~IEnumerable~ModelInfo~~
        +SelectOptimalModelAsync(criteria: ModelSelectionCriteria) Task~ModelInfo~
        +GetModelConfigurationAsync(modelId: string) Task~ModelConfiguration~
        +UpdateModelConfigurationAsync(modelId: string, config: ModelConfiguration) Task
    }
    
    class IRequestRouter {
        <<interface>>
        +RouteRequestAsync(request: LLMRequest) Task~RoutedRequest~
        +SelectKernelAsync(criteria: RoutingCriteria) Task~Kernel~
        +GetLoadBalancingStrategyAsync() Task~ILoadBalancingStrategy~
    }
    
    class Kernel {
        <<SemanticKernel>>
        +Services: IServiceProvider
        +GetRequiredService~T~() T
        +InvokeAsync(function: KernelFunction) Task~FunctionResult~
        +InvokeStreamingAsync(function: KernelFunction) IAsyncEnumerable~StreamingKernelContent~
    }
    
    class IChatCompletionService {
        <<SemanticKernel>>
        +GetChatMessageContentsAsync(chatHistory: ChatHistory) Task~IReadOnlyList~ChatMessageContent~~
        +GetStreamingChatMessageContentsAsync(chatHistory: ChatHistory) IAsyncEnumerable~StreamingChatMessageContent~
    }
    
    ILLMService --> IRequestRouter : uses
    ILLMService --> IKernelManager : uses
    IRequestRouter --> IModelManager : uses
    IKernelManager --> Kernel : manages
    Kernel --> IChatCompletionService : contains
```

**接口职责说明**：

#### 3.1.1 ILLMService (位置: Lorn.OpenAgenticAI.Shared.Contracts/)

- **核心职责**: 提供统一的LLM服务接口，封装SemanticKernel复杂性
- **主要方法**: 基于SemanticKernel的文本生成、流式响应、对话处理、函数调用
- **设计要点**: 复用SemanticKernel的ChatHistory和ChatMessageContent等成熟抽象

#### 3.1.2 IKernelManager (位置: Lorn.OpenAgenticAI.Shared.Contracts/)

- **核心职责**: 管理SemanticKernel实例的生命周期和服务注册
- **主要功能**: Kernel创建、服务注册、智能选择、资源管理
- **设计要点**: 封装SemanticKernel的复杂初始化逻辑，提供简单易用的管理接口

#### 3.1.3 IModelManager (位置: Lorn.OpenAgenticAI.Shared.Contracts/)

- **核心职责**: 管理模型元数据和配置，配合SemanticKernel进行模型管理
- **主要功能**: 模型注册、发现、选择、配置管理
- **设计要点**: 专注于业务层面的模型管理，技术层面交给SemanticKernel

### 3.2 基于SemanticKernel的核心数据模型类图

```mermaid
classDiagram
    class LLMRequest {
        +ModelId: string
        +SystemPrompt: string
        +UserPrompt: string
        +ConversationHistory: ChatHistory
        +ExecutionSettings: PromptExecutionSettings
        +Metadata: RequestMetadata
        +CancellationToken: CancellationToken
    }
    
    class LLMResponse {
        +ResponseId: string
        +ModelId: string
        +Content: string
        +ChatMessageContent: ChatMessageContent
        +Metadata: ResponseMetadata
        +Usage: UsageStatistics
        +Duration: TimeSpan
        +IsSuccess: bool
        +ErrorMessage: string
    }
    
    class ChatHistory {
        <<SemanticKernel>>
        +Messages: IList~ChatMessageContent~
        +AddSystemMessage(content: string) void
        +AddUserMessage(content: string) void
        +AddAssistantMessage(content: string) void
    }
    
    class ChatMessageContent {
        <<SemanticKernel>>
        +Role: AuthorRole
        +Content: string
        +ModelId: string
        +Metadata: IDictionary~string,object~
    }
    
    class PromptExecutionSettings {
        <<SemanticKernel>>
        +ModelId: string
        +Temperature: double
        +TopP: double
        +MaxTokens: int
        +StopSequences: IList~string~
        +ExtensionData: IDictionary~string,object~
    }
    
    class ModelConfiguration {
        +ModelId: string
        +ProviderId: string
        +DisplayName: string
        +ApiKey: string
        +Endpoint: string
        +ExecutionSettings: PromptExecutionSettings
        +Capabilities: ModelCapabilities
        +Limitations: ModelLimitations
        +IsEnabled: bool
    }
    
    class KernelConfiguration {
        +KernelId: string
        +ModelConfigurations: List~ModelConfiguration~
        +Services: List~ServiceRegistration~
        +Plugins: List~PluginRegistration~
        +DefaultModel: string
        +CreatedAt: DateTime
    }
    
    LLMRequest --> ChatHistory : contains
    LLMRequest --> PromptExecutionSettings : contains
    LLMResponse --> ChatMessageContent : contains
    ChatHistory --> ChatMessageContent : contains
    ModelConfiguration --> PromptExecutionSettings : contains
    KernelConfiguration --> ModelConfiguration : contains
```

**数据模型设计说明**：

#### 3.2.1 请求响应模型 (位置: Lorn.OpenAgenticAI.Shared.Models/LLM/)

**LLMRequest**:

- **设计目标**: 在SemanticKernel ChatHistory基础上添加业务元数据
- **关键属性**: 复用ChatHistory对象、集成PromptExecutionSettings
- **扩展性**: 通过Metadata提供业务特定信息，无需修改SemanticKernel核心结构

**LLMResponse**:

- **设计目标**: 封装SemanticKernel的ChatMessageContent，添加业务统计信息
- **核心信息**: 保留原始ChatMessageContent、添加使用统计和性能指标
- **错误处理**: 统一的错误处理机制，兼容SemanticKernel异常体系

#### 3.2.2 配置管理模型 (位置: Lorn.OpenAgenticAI.Shared.Models/LLM/)

**ModelConfiguration**:

- **设计目标**: 管理SemanticKernel服务注册所需的配置信息
- **核心配置**: 集成PromptExecutionSettings、API密钥、终端地址
- **业务扩展**: ModelCapabilities和ModelLimitations提供业务层面的模型描述

**KernelConfiguration**:

- **设计目标**: 管理完整的Kernel实例配置，支持多模型和多服务
- **配置内容**: 模型配置集合、服务注册列表、插件配置
- **生命周期**: 支持配置版本管理和动态更新

### 3.3 基于SemanticKernel的服务管理层组件图

```mermaid
graph TD
    subgraph "业务服务层 (Lorn.OpenAgenticAI.Domain.LLM/Services/)"
        LLMService[LLMService]
        RequestRouter[RequestRouter]
        ModelManager[ModelManager]
        KernelManager[KernelManager]
    end
    
    subgraph "SemanticKernel集成"
        Kernel[Kernel实例]
        ChatService[IChatCompletionService]
        KernelBuilder[KernelBuilder]
        ServiceCollection[IServiceCollection]
    end
    
    subgraph "基础设施依赖"
        ResponseCache[IResponseCache]
        MetricsCollector[IMetricsCollector]
        ConfigManager[IConfigManager]
        Logger[ILogger]
    end
    
    subgraph "LLM提供商连接器"
        OpenAIConnector[OpenAI Connector]
        AzureConnector[Azure OpenAI Connector]
        OllamaConnector[Ollama Connector]
        CustomConnector[Custom Connector]
    end
    
    LLMService --> RequestRouter
    LLMService --> ResponseCache
    LLMService --> MetricsCollector
    
    RequestRouter --> ModelManager
    RequestRouter --> KernelManager
    
    ModelManager --> ConfigManager
    
    KernelManager --> KernelBuilder
    KernelManager --> Kernel
    
    KernelBuilder --> ServiceCollection
    ServiceCollection --> OpenAIConnector
    ServiceCollection --> AzureConnector
    ServiceCollection --> OllamaConnector
    ServiceCollection --> CustomConnector
    
    Kernel --> ChatService
    ChatService --> OpenAIConnector
    ChatService --> AzureConnector
    ChatService --> OllamaConnector
    ChatService --> CustomConnector
```

#### 3.3.1 LLMService 基于SemanticKernel的业务流程图

```mermaid
flowchart TD
    Start([接收LLM请求]) --> Validate{验证请求}
    Validate -->|无效| Error1[返回验证错误]
    Validate -->|有效| Cache{检查缓存}
    
    Cache -->|命中| CacheHit[记录缓存命中]
    CacheHit --> Return1[返回缓存结果]
    
    Cache -->|未命中| Route[请求路由]
    Route --> GetKernel[获取Kernel实例]
    GetKernel --> GetService[获取IChatCompletionService]
    GetService --> ConvertRequest[转换为ChatHistory]
    ConvertRequest --> CallKernel[调用Kernel.InvokeAsync]
    CallKernel --> Success{调用成功?}
    
    Success -->|成功| Metrics1[记录成功指标]
    Metrics1 --> ConvertResponse[转换响应格式]
    ConvertResponse --> UpdateCache[更新缓存]
    UpdateCache --> Return2[返回响应结果]
    
    Success -->|失败| Metrics2[记录失败指标]
    Metrics2 --> LogError[记录错误日志]
    LogError --> Error2[返回错误响应]
    
    Error1 --> End([结束])
    Return1 --> End
    Return2 --> End
    Error2 --> End
```

**LLMService设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Services/LLMService.cs):

- **核心职责**: 基于SemanticKernel提供统一的LLM服务接口
- **处理流程**: 请求验证 → 缓存检查 → 路由到Kernel → 调用IChatCompletionService → 响应处理
- **关键特性**:
  - 利用SemanticKernel的ChatHistory和ChatMessageContent进行标准化处理
  - 集成SemanticKernel的异常处理机制
  - 保持业务层面的缓存和监控功能

#### 3.3.2 KernelManager 生命周期管理图

```mermaid
stateDiagram-v2
    [*] --> 未初始化
    未初始化 --> 配置加载中 : LoadConfiguration()
    配置加载中 --> 已配置 : 配置加载成功
    配置加载中 --> 配置失败 : 配置加载失败
    配置失败 --> 未初始化 : 重试
    
    已配置 --> Kernel创建中 : CreateKernel()
    Kernel创建中 --> Kernel已创建 : 使用KernelBuilder
    Kernel创建中 --> 创建失败 : 创建失败
    创建失败 --> 已配置 : 重试
    
    Kernel已创建 --> 服务注册中 : RegisterServices()
    服务注册中 --> 服务已注册 : 注册IChatCompletionService
    服务注册中 --> 注册失败 : 注册失败
    注册失败 --> Kernel已创建 : 重试
    
    服务已注册 --> 运行中 : Kernel可用
    运行中 --> 服务注册中 : 添加新服务
    运行中 --> 销毁中 : DisposeKernel()
    销毁中 --> 未初始化 : 销毁完成
```

**KernelManager设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Services/KernelManager.cs):

- **核心职责**: 管理SemanticKernel实例的完整生命周期
- **状态管理**: 配置加载 → Kernel创建 → 服务注册 → 运行 → 销毁的完整状态机
- **关键特性**:
  - 基于SemanticKernel的KernelBuilder进行标准化构建
  - 动态服务注册，支持IChatCompletionService、ITextEmbeddingGenerationService等
  - 智能Kernel选择和负载均衡
  - 完整的资源管理和清理机制

#### 3.3.3 RequestRouter 基于SemanticKernel的路由策略图

```mermaid
stateDiagram-v2
    [*] --> 接收请求
    接收请求 --> 获取模型信息
    获取模型信息 --> 检查Kernel状态
    
    检查Kernel状态 --> Kernel健康 : Kernel可用
    检查Kernel状态 --> 故障转移 : Kernel不可用
    
    故障转移 --> 选择备用Kernel
    选择备用Kernel --> Kernel健康
    
    Kernel健康 --> 获取ChatCompletionService
    获取ChatCompletionService --> 转换为ChatHistory
    转换为ChatHistory --> 应用执行设置
    应用执行设置 --> 返回路由结果
    返回路由结果 --> [*]
```

**RequestRouter设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Services/RequestRouter.cs):

- **核心职责**: 智能路由请求到最优的Kernel实例和ChatCompletionService
- **路由策略**: Kernel健康检查 → 服务获取 → 请求转换 → 执行设置应用
- **关键特性**:
  - 基于SemanticKernel的服务发现机制
  - 利用Kernel的内置服务管理
  - 智能的ChatHistory构建和PromptExecutionSettings应用
  - 故障转移到备用Kernel实例

### 3.4 基于SemanticKernel连接器的设计

#### 3.4.1 连接器集成关系图

```mermaid
classDiagram
    class IKernelBuilder {
        <<SemanticKernel>>
        +AddOpenAIChatCompletion(modelId, apiKey) IKernelBuilder
        +AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey) IKernelBuilder
        +AddOllamaChatCompletion(modelId, endpoint) IKernelBuilder
        +Services: IServiceCollection
        +Build() Kernel
    }
    
    class IChatCompletionService {
        <<SemanticKernel>>
        +GetChatMessageContentsAsync(chatHistory, settings) Task~IReadOnlyList~ChatMessageContent~~
        +GetStreamingChatMessageContentsAsync(chatHistory, settings) IAsyncEnumerable~StreamingChatMessageContent~
    }
    
    class LLMServiceRegistry {
        +RegisterOpenAIService(config: OpenAIConfiguration) Task
        +RegisterAzureOpenAIService(config: AzureConfiguration) Task
        +RegisterOllamaService(config: OllamaConfiguration) Task
        +RegisterCustomService(config: CustomConfiguration) Task
        +GetRegisteredServices() IEnumerable~ServiceInfo~
        +CreateKernelWithServices(services: ServiceSelection) Kernel
    }
    
    class ServiceConfiguration {
        <<abstract>>
        +ServiceId: string
        +ModelId: string
        +IsEnabled: bool
        +Priority: int
        +CreatedAt: DateTime
    }
    
    class OpenAIConfiguration {
        +ApiKey: string
        +OrganizationId: string
        +BaseUrl: string
        +ModelName: string
    }
    
    class AzureOpenAIConfiguration {
        +DeploymentName: string
        +Endpoint: string
        +ApiKey: string
        +ApiVersion: string
    }
    
    class OllamaConfiguration {
        +Endpoint: string
        +ModelName: string
        +TimeoutSeconds: int
    }
    
    LLMServiceRegistry --> IKernelBuilder : uses
    IKernelBuilder --> IChatCompletionService : registers
    ServiceConfiguration <|-- OpenAIConfiguration
    ServiceConfiguration <|-- AzureOpenAIConfiguration
    ServiceConfiguration <|-- OllamaConfiguration
```

#### 3.4.2 连接器配置和使用时序图

```mermaid
sequenceDiagram
    participant App as 应用层
    participant Registry as LLMServiceRegistry
    participant Builder as IKernelBuilder
    participant Kernel as Kernel
    participant Service as IChatCompletionService
    participant Provider as LLM提供商
    
    App->>Registry: RegisterOpenAIService(config)
    Registry->>Builder: AddOpenAIChatCompletion(modelId, apiKey)
    Builder-->>Registry: 返回IKernelBuilder
    
    App->>Registry: CreateKernelWithServices(selection)
    Registry->>Builder: Build()
    Builder-->>Registry: 返回Kernel实例
    Registry-->>App: 返回配置好的Kernel
    
    App->>Kernel: GetRequiredService<IChatCompletionService>()
    Kernel-->>App: 返回Service实例
    
    App->>Service: GetChatMessageContentsAsync(chatHistory, settings)
    Service->>Provider: 发送HTTP请求
    Provider-->>Service: 返回响应
    Service-->>App: 返回ChatMessageContent列表
```

**基于SemanticKernel连接器的设计说明**：

#### 3.4.3 LLMServiceRegistry (位置: Lorn.OpenAgenticAI.Domain.LLM/Services/LLMServiceRegistry.cs)

- **职责**: 封装SemanticKernel的服务注册复杂性，提供业务友好的注册接口
- **特殊处理**:
  - 统一的配置验证和错误处理
  - 支持多个同类型服务的注册（如多个OpenAI配置）
  - 服务优先级和负载均衡策略

#### 3.4.4 配置管理 (位置: Lorn.OpenAgenticAI.Domain.LLM/Configuration/)

- **OpenAIConfiguration**: 封装OpenAI API的配置参数
- **AzureOpenAIConfiguration**: 封装Azure OpenAI服务的配置参数  
- **OllamaConfiguration**: 封装本地Ollama服务的配置参数
- **CustomConfiguration**: 支持自定义LLM提供商的配置扩展

**简化设计的优势**:

1. **减少重复工作**: 无需重复实现HTTP客户端、连接管理等基础功能
2. **提高稳定性**: 基于Microsoft维护的成熟框架，减少Bug和安全风险
3. **更好的兼容性**: 自动跟进LLM提供商的API变化和新功能
4. **统一的抽象**: 使用SemanticKernel标准的ChatHistory、ChatMessageContent等抽象
5. **专注业务逻辑**: 将精力集中在业务特定的功能如路由、缓存、监控等

### 3.5 基础组件层设计

#### 3.5.1 基础组件架构图

```mermaid
graph TB
    subgraph "基础组件层 (Lorn.OpenAgenticAI.Domain.LLM/Infrastructure/)"
        subgraph "连接管理"
            ConnectionPoolManager[ConnectionPoolManager]
            HttpConnectionPool[HttpConnectionPool]
            PooledHttpClient[PooledHttpClient]
        end
        
        subgraph "缓存管理"
            ResponseCache[ResponseCache]
            MemoryCache[IMemoryCache]
            DistributedCache[IDistributedCache]
            CacheSerializer[ICacheSerializer]
        end
        
        subgraph "监控指标"
            MetricsCollector[MetricsCollector]
            PerformanceMonitor[PerformanceMonitor]
            HealthChecker[HealthChecker]
        end
        
        subgraph "弹性处理"
            RetryPolicy[RetryPolicy]
            CircuitBreaker[CircuitBreaker]
            LoadBalancer[LoadBalancer]
        end
    end
    
    ConnectionPoolManager --> HttpConnectionPool
    HttpConnectionPool --> PooledHttpClient
    
    ResponseCache --> MemoryCache
    ResponseCache --> DistributedCache
    ResponseCache --> CacheSerializer
    
    MetricsCollector --> PerformanceMonitor
    PerformanceMonitor --> HealthChecker
    
    RetryPolicy --> CircuitBreaker
    CircuitBreaker --> LoadBalancer
```

#### 3.5.2 连接池状态图

```mermaid
stateDiagram-v2
    [*] --> 连接池初始化
    连接池初始化 --> 空闲状态
    
    空闲状态 --> 获取连接请求 : getConnection()
    获取连接请求 --> 检查可用连接
    
    检查可用连接 --> 返回现有连接 : 有可用连接
    检查可用连接 --> 创建新连接 : 无可用连接且未达上限
    检查可用连接 --> 等待连接 : 无可用连接且已达上限
    
    返回现有连接 --> 连接使用中
    创建新连接 --> 连接使用中
    等待连接 --> 连接使用中 : 有连接释放
    
    连接使用中 --> 连接验证 : returnConnection()
    连接验证 --> 回归连接池 : 连接有效
    连接验证 --> 销毁连接 : 连接无效
    
    回归连接池 --> 空闲状态
    销毁连接 --> 空闲状态
    
    空闲状态 --> 清理过期连接 : 定时清理
    清理过期连接 --> 空闲状态
```

**连接池管理设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Infrastructure/ConnectionPoolManager.cs):

- **设计目标**: 高效管理HTTP连接，避免连接创建开销
- **核心特性**:
  - 支持多提供商的连接池隔离
  - 智能连接复用和生命周期管理
  - 连接健康检查和自动清理
  - 并发安全的连接分配机制

#### 3.5.3 缓存策略流程图

```mermaid
flowchart TD
    Start([缓存请求]) --> GenerateKey[生成缓存键]
    GenerateKey --> CheckMemory{检查内存缓存}
    
    CheckMemory -->|命中| MemoryHit[记录内存命中]
    MemoryHit --> Return1[返回缓存结果]
    
    CheckMemory -->|未命中| CheckDistributed{检查分布式缓存}
    CheckDistributed -->|命中| DistributedHit[记录分布式命中]
    DistributedHit --> SyncToMemory[同步到内存缓存]
    SyncToMemory --> Return2[返回缓存结果]
    
    CheckDistributed -->|未命中| CacheMiss[记录缓存未命中]
    CacheMiss --> ExecuteOriginal[执行原始操作]
    ExecuteOriginal --> CacheResult[缓存结果]
    CacheResult --> Return3[返回操作结果]
    
    Return1 --> End([结束])
    Return2 --> End
    Return3 --> End
```

**响应缓存设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Infrastructure/ResponseCache.cs):

- **设计目标**: 提供多级缓存策略，最大化响应性能
- **缓存策略**:
  - L1缓存: 内存缓存，响应最快但容量有限
  - L2缓存: 分布式缓存，支持集群共享
  - 智能失效: 基于内容和时间的智能失效策略

#### 3.5.4 指标收集器组件图

```mermaid
graph LR
    subgraph "指标收集 (MetricsCollector)"
        RequestCounter[请求计数器]
        DurationHistogram[延迟直方图]
        ErrorCounter[错误计数器]
        CacheHitRate[缓存命中率]
    end
    
    subgraph "指标存储"
        MetricsLogger[指标日志]
        TimeSeriesDB[时序数据库]
        Dashboard[监控面板]
    end
    
    subgraph "告警系统"
        AlertEngine[告警引擎]
        NotificationService[通知服务]
    end
    
    RequestCounter --> MetricsLogger
    DurationHistogram --> TimeSeriesDB
    ErrorCounter --> AlertEngine
    CacheHitRate --> Dashboard
    
    AlertEngine --> NotificationService
```

**指标收集器设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Infrastructure/MetricsCollector.cs):

- **设计目标**: 全面收集系统运行指标，支持性能监控和问题诊断
- **关键指标**:
  - 请求量指标: QPS、成功率、错误率
  - 性能指标: 响应时间、吞吐量、资源使用率
  - 业务指标: 成本统计、模型使用分布、用户行为

#### 3.5.5 熔断器状态转换图

```mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Open : 失败次数达到阈值
    Open --> HalfOpen : 超时时间到达
    HalfOpen --> Closed : 请求成功
    HalfOpen --> Open : 请求失败
    
    state Closed {
        [*] --> 正常处理请求
        正常处理请求 --> 记录成功 : 请求成功
        正常处理请求 --> 记录失败 : 请求失败
        记录失败 --> 检查失败阈值
        检查失败阈值 --> 正常处理请求 : 未达阈值
    }
    
    state Open {
        [*] --> 拒绝请求
        拒绝请求 --> 等待超时
        等待超时 --> 拒绝请求
    }
    
    state HalfOpen {
        [*] --> 允许部分请求
        允许部分请求 --> 评估结果
    }
```

**熔断器设计说明** (位置: Lorn.OpenAgenticAI.Domain.LLM/Resilience/CircuitBreaker.cs):

- **设计目标**: 防止级联故障，提供快速失败机制
- **状态管理**: 关闭 → 开启 → 半开启的标准熔断器状态机
- **配置参数**: 失败阈值、超时时间、半开启测试频率

## 4. 配置管理设计

### 4.1 配置文件结构设计

```mermaid
graph TD
    subgraph "配置层次结构"
        AppSettings[appsettings.json]
        UserSettings[user-settings.json]
        EnvironmentVars[环境变量]
        KeyVault[密钥保管库]
    end
    
    subgraph "配置对象模型"
        LLMConfig[LLMConfiguration]
        ProviderConfig[ProviderConfiguration]
        ModelConfig[ModelConfiguration]
        SecurityConfig[SecurityConfiguration]
    end
    
    AppSettings --> LLMConfig
    UserSettings --> ProviderConfig
    EnvironmentVars --> SecurityConfig
    KeyVault --> SecurityConfig
    
    LLMConfig --> ProviderConfig
    ProviderConfig --> ModelConfig
```

### 4.2 配置优先级策略

**配置加载优先级**（从高到低）：

1. **环境变量**: 用于敏感信息如API密钥
2. **用户配置文件**: 用户个性化设置
3. **应用配置文件**: 系统默认配置
4. **代码默认值**: 最终兜底配置

### 4.3 配置类层次结构

```mermaid
classDiagram
    class LLMConfiguration {
        +DefaultProvider: string
        +RequestTimeout: TimeSpan
        +MaxRetries: int
        +EnableCaching: bool
        +CacheDuration: TimeSpan
        +Providers: Dictionary~string,ProviderConfiguration~
        +LoadBalancing: LoadBalancingConfiguration
        +Fallback: FallbackConfiguration
    }
    
    class ProviderConfiguration {
        +BaseUrl: string
        +ApiKey: string
        +MaxConnections: int
        +RequestsPerMinute: int
        +Models: Dictionary~string,ModelConfiguration~
        +CustomHeaders: Dictionary~string,string~
        +Proxy: ProxyConfiguration
    }
    
    class ModelConfiguration {
        +MaxTokens: int
        +DefaultTemperature: double
        +PricingPerMillionTokens: PricingConfiguration
        +SupportedCapabilities: List~string~
        +CustomParameters: Dictionary~string,object~
    }
    
    class LoadBalancingConfiguration {
        +Strategy: LoadBalancingStrategy
        +HealthCheckInterval: TimeSpan
        +CircuitBreakerThreshold: int
        +CircuitBreakerTimeout: TimeSpan
    }
    
    class FallbackConfiguration {
        +EnableAutoFallback: bool
        +FallbackChain: List~string~
        +FallbackTimeout: TimeSpan
    }
    
    LLMConfiguration --> ProviderConfiguration
    LLMConfiguration --> LoadBalancingConfiguration
    LLMConfiguration --> FallbackConfiguration
    ProviderConfiguration --> ModelConfiguration
```

### 4.4 配置验证流程

```mermaid
flowchart TD
    Start([配置加载开始]) --> LoadBase[加载基础配置]
    LoadBase --> LoadUser[加载用户配置]
    LoadUser --> LoadEnv[加载环境变量]
    LoadEnv --> MergeConfig[合并配置]
    
    MergeConfig --> ValidateStructure{结构验证}
    ValidateStructure -->|失败| StructureError[配置结构错误]
    ValidateStructure -->|成功| ValidateValues{值验证}
    
    ValidateValues -->|失败| ValueError[配置值错误]
    ValidateValues -->|成功| ValidateConnections{连接验证}
    
    ValidateConnections -->|失败| ConnectionError[连接测试失败]
    ValidateConnections -->|成功| ConfigReady[配置就绪]
    
    StructureError --> LogError[记录错误日志]
    ValueError --> LogError
    ConnectionError --> LogError
    LogError --> LoadDefaults[加载默认配置]
    LoadDefaults --> ConfigReady
    
    ConfigReady --> End([配置加载完成])
```

**配置验证规则**：

- **结构验证**: 必填字段、数据类型、枚举值检查
- **值验证**: 数值范围、URL格式、超时时间合理性
- **连接验证**: API密钥有效性、网络连通性测试
- **依赖验证**: 模型与提供商对应关系、能力匹配检查

## 4. 简化设计总结

### 4.1 基于SemanticKernel的设计优势

通过采用Microsoft.SemanticKernel作为底层LLM框架，我们的LLM适配器设计得到了显著简化：

#### 4.1.1 技术优势

- **成熟稳定**: SemanticKernel是Microsoft官方维护的成熟AI框架
- **持续更新**: 自动跟进最新的LLM API变化和新功能支持
- **深度集成**: 与.NET生态系统完美集成，遵循.NET最佳实践
- **标准化抽象**: 提供统一的ChatHistory、ChatMessageContent等标准抽象

#### 4.1.2 开发效率提升

- **减少重复工作**: 无需重复实现HTTP客户端、连接管理、协议适配等基础功能
- **专注业务逻辑**: 将开发精力集中在业务特定功能（路由、缓存、监控等）
- **降低维护成本**: 减少自研代码量，降低长期维护负担
- **提高代码质量**: 基于经过大规模验证的开源框架

#### 4.1.3 功能覆盖

SemanticKernel已支持的LLM提供商：
- **OpenAI**: GPT-3.5、GPT-4、GPT-4o等全系列模型
- **Azure OpenAI**: 企业级OpenAI服务
- **Ollama**: 本地开源LLM部署方案
- **其他**: HuggingFace、Google AI等多种提供商

### 4.2 简化后的架构对比

#### 4.2.1 原设计复杂度

原始设计包含大量自研组件：
- 自研的提供商适配器层（OpenAIAdapter、ClaudeAdapter等）
- 自研的HTTP客户端管理和连接池
- 自研的请求响应格式转换
- 自研的流式响应处理
- 自研的错误处理和重试机制

#### 4.2.2 简化后的设计

基于SemanticKernel的简化设计：
- **利用现有**: 直接使用SemanticKernel的连接器
- **业务封装**: 在业务层面封装模型管理、路由、缓存等逻辑
- **配置管理**: 统一管理不同LLM提供商的配置
- **监控增强**: 专注于业务监控和性能优化

### 4.3 保留的核心价值

虽然底层实现简化了，但我们仍然保留了系统的核心业务价值：

#### 4.3.1 智能调度

- **模型选择**: 根据任务特性选择最适合的模型
- **负载均衡**: 在多个模型实例间分发请求
- **故障转移**: 自动切换到备用模型
- **成本优化**: 基于成本和性能的智能路由

#### 4.3.2 性能优化

- **多级缓存**: 内存缓存 + 分布式缓存
- **异步处理**: 全异步调用链，提升并发能力
- **连接复用**: 基于SemanticKernel的连接管理
- **流式响应**: 支持大模型的流式输出

#### 4.3.3 企业级特性

- **配置管理**: 集中化的模型配置和密钥管理
- **监控告警**: 完整的性能监控和健康检查
- **安全控制**: API密钥安全存储和权限控制
- **审计日志**: 完整的操作审计和追踪

### 4.4 后续实施计划

#### 4.4.1 第一阶段：核心框架

1. **搭建基础框架**: 基于SemanticKernel搭建核心LLM服务框架
2. **实现基础服务**: LLMService、KernelManager、ModelManager等核心服务
3. **配置管理**: 实现统一的模型配置和服务注册机制
4. **基础测试**: 单元测试和集成测试，验证核心功能

#### 4.4.2 第二阶段：业务增强

1. **智能路由**: 实现RequestRouter的智能调度逻辑
2. **缓存系统**: 实现多级缓存和智能失效策略
3. **监控系统**: 实现指标收集、性能监控和健康检查
4. **错误处理**: 完善异常处理和故障恢复机制

#### 4.4.3 第三阶段：企业级特性

1. **高级路由**: 基于成本、延迟、质量的智能路由算法
2. **扩展支持**: 支持自定义模型提供商和业务规则
3. **管理界面**: Web管理界面，支持可视化配置和监控
4. **部署优化**: 容器化部署和集群管理支持

### 4.5 技术风险评估

#### 4.5.1 依赖风险

- **框架依赖**: 对SemanticKernel的依赖，需关注版本兼容性
- **缓解措施**: 定期更新、版本锁定、渐进升级策略

#### 4.5.2 兼容性风险

- **API变化**: LLM提供商API可能发生变化
- **缓解措施**: SemanticKernel团队维护适配器，降低直接影响

#### 4.5.3 性能风险

- **抽象开销**: 多层抽象可能带来性能开销
- **缓解措施**: 性能测试验证、必要时进行针对性优化

## 5. 结论

基于Microsoft.SemanticKernel的LLM适配器设计方案显著简化了系统复杂度，提高了开发效率，同时保持了企业级应用所需的核心功能。这种设计充分体现了"站在巨人肩膀上"的工程智慧，让我们能够专注于业务价值的实现，而不是重复造轮子。

通过这种简化设计，我们可以：
1. **快速启动**: 基于成熟框架快速搭建原型
2. **稳定可靠**: 基于经过验证的开源项目
3. **持续演进**: 跟随SemanticKernel的发展持续改进
4. **专注创新**: 将精力集中在业务特色功能的开发上

---

*本设计文档将根据SemanticKernel框架的演进和项目实际需求持续更新。*
