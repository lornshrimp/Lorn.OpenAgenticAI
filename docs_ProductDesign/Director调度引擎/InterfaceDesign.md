# Director调度引擎接口设计

## 概述

本文档详细描述了Director调度引擎对外提供的接口设计，包括与用户界面、大语言模型、Agent调度中心、工作流管理器和知识库的交互接口。作为系统的中枢神经，Director调度引擎需要通过这些接口协调各组件的工作，确保任务能够被正确理解、规划和执行。

## 接口分类

Director调度引擎的接口可分为以下几类：

1. **用户界面接口** - 接收用户请求，返回执行结果和状态
2. **LLM交互接口** - 与大语言模型进行意图解析和任务规划
3. **Agent调度接口** - 与AgentHub交互，调度和监控Agent执行
4. **工作流接口** - 与工作流管理器交互，加载和执行工作流
5. **知识库接口** - 与知识库交互，获取和存储上下文信息
6. **内部组件接口** - Director内部各模块之间的接口

## 接口详细设计

### 1. 用户界面接口

#### 1.1 接收用户请求

```csharp
/// <summary>
/// 接收用户的自然语言请求，开始处理流程
/// </summary>
/// <param name="userRequest">用户的自然语言请求内容</param>
/// <param name="userId">用户标识</param>
/// <param name="sessionId">会话标识，用于关联上下文</param>
/// <returns>任务标识，用于后续查询任务状态</returns>
Task<string> ProcessUserRequestAsync(string userRequest, string userId, string sessionId);
```

#### 1.2 获取任务状态

```csharp
/// <summary>
/// 获取指定任务的当前执行状态
/// </summary>
/// <param name="taskId">任务标识</param>
/// <returns>任务状态信息，包括进度、当前步骤等</returns>
Task<TaskStatusInfo> GetTaskStatusAsync(string taskId);
```

#### 1.3 获取任务结果

```csharp
/// <summary>
/// 获取已完成任务的执行结果
/// </summary>
/// <param name="taskId">任务标识</param>
/// <returns>任务执行结果，包括成功/失败状态、输出内容等</returns>
Task<TaskResult> GetTaskResultAsync(string taskId);
```

#### 1.4 控制任务执行

```csharp
/// <summary>
/// 控制任务的执行流程，如暂停、继续、取消等
/// </summary>
/// <param name="taskId">任务标识</param>
/// <param name="action">控制动作（暂停/继续/取消）</param>
/// <returns>操作是否成功</returns>
Task<bool> ControlTaskExecutionAsync(string taskId, TaskControlAction action);
```

### 2. LLM交互接口

#### 2.1 意图解析请求

```csharp
/// <summary>
/// 向LLM发送意图解析请求
/// </summary>
/// <param name="userRequest">用户原始请求</param>
/// <param name="contextInfo">相关上下文信息</param>
/// <returns>解析后的意图信息</returns>
Task<IntentInfo> ParseIntentAsync(string userRequest, ContextInfo contextInfo);
```

#### 2.2 任务规划请求

```csharp
/// <summary>
/// 向LLM请求任务规划
/// </summary>
/// <param name="intentInfo">解析后的意图信息</param>
/// <param name="availableCapabilities">系统当前可用的Agent能力列表</param>
/// <param name="contextInfo">相关上下文信息</param>
/// <returns>任务执行计划</returns>
Task<ExecutionPlan> PlanTaskExecutionAsync(IntentInfo intentInfo, List<AgentCapability> availableCapabilities, ContextInfo contextInfo);
```

#### 2.3 执行调整请求

```csharp
/// <summary>
/// 根据执行反馈，请求LLM调整执行计划
/// </summary>
/// <param name="currentPlan">当前执行计划</param>
/// <param name="executionFeedback">执行过程中的反馈信息</param>
/// <param name="contextInfo">相关上下文信息</param>
/// <returns>调整后的执行计划</returns>
Task<ExecutionPlan> AdjustExecutionPlanAsync(ExecutionPlan currentPlan, ExecutionFeedback executionFeedback, ContextInfo contextInfo);
```

### 3. Agent调度接口

#### 3.1 Agent能力查询

```csharp
/// <summary>
— 查询当前系统中可用的Agent能力
/// </summary>
/// <returns>可用的Agent能力列表</returns>
Task<List<AgentCapability>> GetAvailableCapabilitiesAsync();
```

#### 3.2 Agent调度请求

```csharp
/// <summary>
— 请求AgentHub调度Agent执行任务
/// </summary>
/// <param name="stepId">执行步骤标识</param>
/// <param name="requiredCapability">所需Agent能力</param>
/// <param name="parameters">执行参数</param>
/// <returns>执行结果</returns>
Task<StepExecutionResult> ExecuteAgentStepAsync(string stepId, AgentCapability requiredCapability, Dictionary<string, object> parameters);
```

#### 3.3 Agent执行状态查询

```csharp
/// <summary>
— 查询Agent执行状态
/// </summary>
/// <param name="stepId">执行步骤标识</param>
/// <returns>步骤执行状态</returns>
Task<StepStatus> GetStepStatusAsync(string stepId);
```

#### 3.4 Agent执行控制

```csharp
/// <summary>
— 控制Agent执行，如暂停、继续、取消等
/// </summary>
/// <param name="stepId">执行步骤标识</param>
/// <param name="action">控制动作</param>
/// <returns>操作是否成功</returns>
Task<bool> ControlAgentExecutionAsync(string stepId, AgentControlAction action);
```

### 4. 工作流接口

#### 4.1 加载工作流定义

```csharp
/// <summary>
— 从工作流管理器加载工作流定义
/// </summary>
/// <param name="workflowId">工作流标识</param>
/// <returns>工作流定义</returns>
Task<WorkflowDefinition> LoadWorkflowDefinitionAsync(string workflowId);
```

#### 4.2 执行工作流

```csharp
/// <summary>
— 执行指定的工作流
/// </summary>
/// <param name="workflowId">工作流标识</param>
/// <param name="parameters">工作流参数</param>
/// <param name="userId">用户标识</param>
/// <returns>工作流执行标识</returns>
Task<string> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters, string userId);
```

#### 4.3 更新工作流执行状态

```csharp
/// <summary>
— 更新工作流执行状态
/// </summary>
/// <param name="executionId">工作流执行标识</param>
/// <param name="status">最新状态</param>
/// <param name="executionResults">执行结果（如果有）</param>
/// <returns>操作是否成功</returns>
Task<bool> UpdateWorkflowExecutionStatusAsync(string executionId, ExecutionStatus status, Dictionary<string, object> executionResults);
```

### 5. 知识库接口

#### 5.1 获取上下文信息

```csharp
/// <summary>
— 从知识库获取相关上下文信息
/// </summary>
/// <param name="userId">用户标识</param>
/// <param name="taskId">任务标识</param>
/// <param name="contextType">上下文类型</param>
/// <returns>上下文信息</returns>
Task<ContextInfo> GetContextInfoAsync(string userId, string taskId, ContextType contextType);
```

#### 5.2 存储执行记录

```csharp
/// <summary>
— 将执行记录存入知识库
/// </summary>
/// <param name="taskId">任务标识</param>
/// <param name="executionRecord">执行记录</param>
/// <returns>操作是否成功</returns>
Task<bool> StoreExecutionRecordAsync(string taskId, ExecutionRecord executionRecord);
```

#### 5.3 更新用户偏好

```csharp
/// <summary>
— 更新用户偏好信息
/// </summary>
/// <param name="userId">用户标识</param>
/// <param name="preferences">偏好信息</param>
/// <returns>操作是否成功</returns>
Task<bool> UpdateUserPreferencesAsync(string userId, Dictionary<string, object> preferences);
```

### 6. 内部组件接口

#### 6.1 意图解析器接口

```csharp
/// <summary>
— 解析用户意图
/// </summary>
/// <param name="request">用户请求</param>
/// <param name="context">上下文信息</param>
/// <returns>解析后的意图</returns>
Task<IntentInfo> ParseIntent(string request, ContextInfo context);
```

#### 6.2 任务规划器接口

```csharp
/// <summary>
— 创建任务执行计划
/// </summary>
/// <param name="intent">解析后的意图</param>
/// <param name="availableCapabilities">可用能力</param>
/// <param name="context">上下文信息</param>
/// <returns>执行计划</returns>
Task<ExecutionPlan> CreateExecutionPlan(IntentInfo intent, List<AgentCapability> availableCapabilities, ContextInfo context);
```

#### 6.3 执行协调器接口

```csharp
/// <summary>
— 执行计划
/// </summary>
/// <param name="plan">执行计划</param>
/// <param name="taskId">任务标识</param>
/// <returns>执行结果</returns>
Task<ExecutionResult> ExecutePlan(ExecutionPlan plan, string taskId);

/// <summary>
— 监控执行状态
/// </summary>
/// <param name="taskId">任务标识</param>
/// <returns>当前执行状态</returns>
Task<ExecutionStatus> MonitorExecution(string taskId);
```

#### 6.4 上下文管理器接口

```csharp
/// <summary>
— 创建执行上下文
/// </summary>
/// <param name="taskId">任务标识</param>
/// <param name="initialContext">初始上下文</param>
/// <returns>上下文标识</returns>
Task<string> CreateExecutionContext(string taskId, ContextInfo initialContext);

/// <summary>
— 更新执行上下文
/// </summary>
/// <param name="contextId">上下文标识</param>
/// <param name="updates">更新内容</param>
/// <returns>操作是否成功</returns>
Task<bool> UpdateExecutionContext(string contextId, Dictionary<string, object> updates);
```

#### 6.5 错误处理器接口

```csharp
/// <summary>
— 处理执行错误
/// </summary>
/// <param name="error">错误信息</param>
/// <param name="taskId">任务标识</param>
/// <param name="stepId">步骤标识</param>
/// <returns>错误处理策略</returns>
Task<ErrorHandlingStrategy> HandleError(ErrorInfo error, string taskId, string stepId);
```

## 数据模型

### 主要数据对象

```csharp
// 任务状态信息
public class TaskStatusInfo
{
    public string TaskId { get; set; }
    public TaskStatus Status { get; set; }
    public double Progress { get; set; }
    public string CurrentStep { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string StatusMessage { get; set; }
}

// 任务结果
public class TaskResult
{
    public string TaskId { get; set; }
    public bool IsSuccessful { get; set; }
    public Dictionary<string, object> OutputData { get; set; }
    public string ErrorMessage { get; set; }
    public List<string> Warnings { get; set; }
    public DateTime CompletionTime { get; set; }
}

// 任务优先级
public enum TaskPriority
{
    Low,
    Normal,
    High,
    Critical
}

// 任务状态
public enum TaskStatus
{
    Pending,
    Planning,
    Executing,
    WaitingUserInput,
    Paused,
    Completed,
    Failed,
    Cancelled
}

// 意图信息
public class IntentInfo
{
    public string PrimaryIntent { get; set; }
    public List<string> SecondaryIntents { get; set; }
    public Dictionary<string, object> ExtractedParameters { get; set; }
    public float ConfidenceScore { get; set; }
    
    // 方法
    public void AnalyzeIntent() { /* 实现细节 */ }
    public void ExtractParameters() { /* 实现细节 */ }
}

// 执行计划
public class ExecutionPlan
{
    public string PlanId { get; set; }
    public string TaskId { get; set; }
    public List<ExecutionStep> Steps { get; set; }
    public Dictionary<string, object> ExpectedOutputs { get; set; }
    public Dictionary<string, List<string>> StepDependencies { get; set; }
    public DateTime CreatedTime { get; set; }
    
    // 方法
    public void GeneratePlan() { /* 实现细节 */ }
    public void OptimizePlan() { /* 实现细节 */ }
    public void ValidatePlan() { /* 实现细节 */ }
}

// 执行步骤
public class ExecutionStep
{
    public string StepId { get; set; }
    public string Description { get; set; }
    public AgentCapability RequiredCapability { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public List<string> DependsOnSteps { get; set; }
    public bool IsCompleted { get; set; }
    public StepExecutionResult Result { get; set; }
    
    // 方法
    public void ExecuteStep() { /* 实现细节 */ }
    public void MarkComplete() { /* 实现细节 */ }
}

// Agent能力
public class AgentCapability
{
    public string CapabilityId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public List<CapabilityAction> Actions { get; set; }
    public List<ParameterDefinition> Parameters { get; set; }
    public List<string> RequiredPermissions { get; set; }
    
    // 方法
    public void ValidateParameters() { /* 实现细节 */ }
    public string DescribeCapability() { /* 实现细节 */ return ""; }
}

// 能力操作
public class CapabilityAction
{
    public string ActionId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ParameterDefinition> Parameters { get; set; }
    public string ReturnType { get; set; }
    
    // 方法
    public void InvokeAction() { /* 实现细节 */ }
    public void ValidateActionParameters() { /* 实现细节 */ }
}

// 参数定义
public class ParameterDefinition
{
    public string ParameterId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public bool IsRequired { get; set; }
    public object DefaultValue { get; set; }
    public string ValidationRules { get; set; }
    
    // 方法
    public bool ValidateValue() { /* 实现细节 */ return true; }
}

// 步骤执行结果
public class StepExecutionResult
{
    public string StepId { get; set; }
    public bool IsSuccessful { get; set; }
    public Dictionary<string, object> OutputData { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CompletionTime { get; set; }
}

// 上下文信息
public class ContextInfo
{
    public string ContextId { get; set; }
    public Dictionary<string, object> ContextItems { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

// 上下文项
public class ContextItem
{
    public string ItemId { get; set; }
    public string Key { get; set; }
    public object Value { get; set; }
    public string Source { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    
    // 方法
    public void SerializeValue() { /* 实现细节 */ }
    public void DeserializeValue() { /* 实现细节 */ }
}

// 工作流定义
public class WorkflowDefinition
{
    public string WorkflowId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public List<WorkflowNode> Nodes { get; set; }
    public List<WorkflowConnection> Connections { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    
    // 方法
    public bool ValidateWorkflow() { /* 实现细节 */ return true; }
    public void ExportWorkflow() { /* 实现细节 */ }
    public void ImportWorkflow() { /* 实现细节 */ }
}

// 工作流节点
public class WorkflowNode
{
    public string NodeId { get; set; }
    public string Type { get; set; }
    public string Label { get; set; }
    public Dictionary<string, object> Configuration { get; set; }
    public Position Position { get; set; }
    
    // 方法
    public bool ValidateConfiguration() { /* 实现细节 */ return true; }
}

// 节点位置
public class Position
{
    public double X { get; set; }
    public double Y { get; set; }
}

// 工作流连接
public class WorkflowConnection
{
    public string ConnectionId { get; set; }
    public string SourceNodeId { get; set; }
    public string TargetNodeId { get; set; }
    public string Condition { get; set; }
    
    // 方法
    public bool EvaluateCondition() { /* 实现细节 */ return true; }
}

// 执行状态
public enum ExecutionStatus
{
    NotStarted,
    Running,
    Waiting,
    Paused,
    Completed,
    Failed,
    Cancelled
}

// Agent状态
public enum AgentStatus
{
    Offline,
    Online,
    Busy,
    Error,
    Maintenance
}
```

## 错误处理机制

### 错误分类

1. **意图解析错误** - 无法理解用户意图或提取必要参数
2. **规划错误** - 无法创建有效的执行计划
3. **执行错误** - Agent执行过程中发生错误
4. **资源错误** - 所需资源不可用
5. **权限错误** - 无足够权限执行操作
6. **超时错误** - 操作执行超时

### 错误处理策略

```csharp
// 错误处理策略
public enum ErrorHandlingStrategy
{
    Retry,              // 重试操作
    UseAlternative,     // 使用替代方案
    AskForClarification, // 向用户请求澄清
    SkipStep,           // 跳过当前步骤
    AbortTask           // 中止整个任务
}

// 错误类型
public enum ErrorType
{
    AgentUnavailable,
    ParameterInvalid,
    PermissionDenied,
    Timeout,
    ExecutionException,
    ResourceUnavailable,
    InternalError
}

// 错误信息
public class ErrorInfo
{
    public string ErrorId { get; set; }
    public ErrorType ErrorType { get; set; }
    public string Message { get; set; }
    public string Source { get; set; }
    public DateTime OccurredTime { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; }
}

// 错误记录
public class ErrorRecord
{
    public string ErrorId { get; set; }
    public string TaskId { get; set; }
    public string StepId { get; set; }
    public ErrorType Type { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public DateTime OccurredTime { get; set; }
    public ErrorHandlingStrategy RecoveryStrategy { get; set; }
    
    // 方法
    public void LogError() { /* 实现细节 */ }
    public void ApplyRecoveryStrategy() { /* 实现细节 */ }
}
```

## 安全性设计

### 接口安全控制

1. **认证验证** - 确保调用方有权限访问接口
2. **参数验证** - 验证所有输入参数的合法性
3. **数据脱敏** - 敏感信息在传输和记录时进行脱敏
4. **访问限流** - 防止过度频繁的接口调用
5. **操作审计** - 记录所有关键操作，便于追溯

### 权限控制

```csharp
// 权限检查接口
Task<bool> CheckPermission(string userId, string requiredPermission);

// 操作审计接口
Task LogAuditRecord(string userId, string operation, string targetResource, bool isSuccessful, string details);
```

## 接口版本控制

为确保系统可以平滑升级，所有接口都应包含版本信息，并保持向后兼容：

```csharp
// API版本标记
[ApiVersion("1.0")]
Task<string> ProcessUserRequestAsync(string userRequest, string userId, string sessionId);

[ApiVersion("1.1")]
Task<string> ProcessUserRequestAsync(string userRequest, string userId, string sessionId, UserPreferences preferences);
```

## 健康监控与度量

### 监控指标

1. **请求处理时间** - 各类请求的处理耗时
2. **错误率** - 按错误类型统计的错误发生率
3. **并发任务数** - 系统当前处理的任务数量
4. **资源利用率** - CPU、内存等资源使用情况
5. **调用频率** - 各接口被调用的频率

### 健康检查接口

```csharp
/// <summary>
— 系统健康状态检查
/// </summary>
/// <returns>健康状态信息</returns>
Task<HealthStatus> CheckHealthAsync();

public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public Dictionary<string, ComponentHealth> ComponentStatus { get; set; }
    public List<string> Warnings { get; set; }
    public DateTime CheckTime { get; set; }
}
```

## 接口测试建议

1. **单元测试** - 测试各接口的基本功能和边界条件
2. **集成测试** - 测试与其他组件的交互
3. **负载测试** - 测试在高负载下的性能和稳定性
4. **故障注入测试** - 模拟各种错误情况，测试错误处理能力
5. **安全性测试** - 测试接口的安全防护能力

## 接口演进路线图

### 短期计划

1. 完善基础接口功能实现
2. 添加全面的错误处理机制
3. 实现接口监控和日志记录

### 中期计划

1. 支持批量任务处理
2. 增强上下文管理能力
3. 添加任务优先级和资源调度功能

### 长期计划

1. 支持分布式部署和负载均衡
2. 实现跨设备任务协同
3. 添加自适应优化机制，根据历史执行数据优化计划生成