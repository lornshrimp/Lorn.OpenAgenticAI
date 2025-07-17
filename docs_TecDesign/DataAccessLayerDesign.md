# 数据访问层技术设计

## 文档信息

- **文档版本**: v1.0
- **创建日期**: 2025年7月3日
- **作者**: 技术专家
- **文档类型**: 数据访问层技术设计
- **依赖文档**:
  - docs_TecDesign/TechnicalArchitecture.md
  - docs_TecDesign/PersistentDataStructureDesign.md

## 概述

本文档描述Lorn.OpenAgenticAI系统中数据访问层的技术实现设计，重点关注数据访问接口、仓储模式实现、查询优化策略和事务管理机制。基于领域驱动设计(DDD)原则，提供清晰的数据访问边界和高效的数据操作接口。

## 技术架构定位

### 在整体架构中的位置

```mermaid
graph TB
    subgraph "应用层"
        APP_SERVICE[应用服务]
        DOMAIN_SERVICE[领域服务]
    end
    
    subgraph "领域层"
        DOMAIN_MODEL[领域模型]
        DOMAIN_INTERFACE[领域接口]
    end
    
    subgraph "基础设施层"
        DATA_ACCESS[数据访问层]
        REPOSITORY_IMPL[仓储实现]
        DB_CONTEXT[数据库上下文]
    end
    
    subgraph "数据存储层"
        SQLITE[(SQLite数据库)]
        JSON_FILE[JSON配置文件]
        MEMORY_CACHE[内存缓存]
    end
    
    APP_SERVICE --> DOMAIN_INTERFACE
    DOMAIN_SERVICE --> DOMAIN_INTERFACE
    DOMAIN_INTERFACE -.-> REPOSITORY_IMPL
    REPOSITORY_IMPL --> DB_CONTEXT
    DB_CONTEXT --> SQLITE
    DATA_ACCESS --> JSON_FILE
    DATA_ACCESS --> MEMORY_CACHE
    
    style DATA_ACCESS fill:#f9f,stroke:#333,stroke-width:3px
    style REPOSITORY_IMPL fill:#bbf,stroke:#333,stroke-width:2px
```

### 项目结构定位

本设计文档主要指导以下项目中的实现：

- **Lorn.OpenAgenticAI.Domain.Contracts**: 数据访问接口定义
- **Lorn.OpenAgenticAI.Infrastructure.Data**: 数据访问层实现
- **Lorn.OpenAgenticAI.Infrastructure.Data.Repositories**: 仓储模式实现
- **Lorn.OpenAgenticAI.Infrastructure.Data.Specifications**: 查询规约实现
- **Lorn.OpenAgenticAI.Shared.Contracts**: 数据传输对象和查询模型

## 数据访问层架构设计

### 整体架构图

```mermaid
graph TB
    subgraph "接口层 (Domain.Contracts)"
        IREPO["IRepository<T>"]
        IUOW["IUnitOfWork"]
        ISPEC["ISpecification<T>"]
        IQUERYSERVICE["IQueryService"]
    end
    
    subgraph "实现层 (Infrastructure.Data)"
        REPO_BASE["RepositoryBase<T>"]
        UOW["UnitOfWork"]
        QUERY_SERVICE["QueryService"]
        DB_CONTEXT["OpenAgenticAIDbContext"]
    end
    
    subgraph "专门仓储"
        USER_REPO["UserProfileRepository"]
        TASK_REPO["TaskExecutionRepository"]
        AGENT_REPO["AgentCapabilityRepository"]
    end
    
    subgraph "查询规约"
        USER_SPECS["UserSpecifications"]
        TASK_SPECS["TaskSpecifications"]
        AGENT_SPECS["AgentSpecifications"]
    end
    
    %% 修正实现关系
    REPO_BASE -->|实现| IREPO
    UOW -->|实现| IUOW
    QUERY_SERVICE -->|实现| IQUERYSERVICE
    
    %% 数据库依赖关系
    REPO_BASE --> DB_CONTEXT
    UOW --> DB_CONTEXT
    
    %% 继承关系
    USER_REPO -->|继承| REPO_BASE
    TASK_REPO -->|继承| REPO_BASE
    AGENT_REPO -->|继承| REPO_BASE
    
    %% 规约使用关系
    USER_REPO --> USER_SPECS
    TASK_REPO --> TASK_SPECS
    AGENT_REPO --> AGENT_SPECS
```

## 核心接口设计

### 1. 基础仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/IRepository.cs`

```mermaid
classDiagram
    class IRepository~T~ {
        <<Interface>>
        +GetByIdAsync(TId id, CancellationToken cancellationToken) Task~T~
        +GetAllAsync(CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +FindAsync(ISpecification~T~ specification, CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +FindOneAsync(ISpecification~T~ specification, CancellationToken cancellationToken) Task~T~
        +ExistsAsync(TId id, CancellationToken cancellationToken) Task~bool~
        +ExistsAsync(ISpecification~T~ specification, CancellationToken cancellationToken) Task~bool~
        +CountAsync(ISpecification~T~ specification, CancellationToken cancellationToken) Task~int~
        +AddAsync(T entity, CancellationToken cancellationToken) Task~T~
        +AddRangeAsync(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
        +UpdateAsync(T entity, CancellationToken cancellationToken) Task~T~
        +UpdateRangeAsync(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
        +DeleteAsync(T entity, CancellationToken cancellationToken) Task~void~
        +DeleteRangeAsync(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
        +DeleteByIdAsync(TId id, CancellationToken cancellationToken) Task~bool~
    }
    
    class IPagedRepository~T~ {
        <<Interface>>
        +GetPagedAsync(ISpecification~T~ specification, int pageNumber, int pageSize, CancellationToken cancellationToken) Task~PagedResult~T~~
        +GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) Task~PagedResult~T~~
    }
    
    class IAsyncQueryable~T~ {
        <<Interface>>
        +AsQueryable() IQueryable~T~
        +ToListAsync(CancellationToken cancellationToken) Task~List~T~~
        +FirstOrDefaultAsync(Expression~Func~T, bool~~ predicate, CancellationToken cancellationToken) Task~T~
    }
    
    IPagedRepository --|> IRepository
    IAsyncQueryable --|> IRepository
```

**接口设计要点**：

1. **泛型约束**: `T`类型约束为实体基类，确保类型安全
2. **异步操作**: 所有数据库操作都支持异步，提升性能
3. **取消令牌**: 支持操作取消，增强用户体验
4. **规约模式**: 通过`ISpecification<T>`实现复杂查询逻辑
5. **分页支持**: 内置分页查询能力，处理大数据集

### 2. 工作单元接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/UnitOfWork/IUnitOfWork.cs`

```mermaid
classDiagram
    class IUnitOfWork {
        <<Interface>>
        +BeginTransactionAsync(CancellationToken cancellationToken) Task~IDbContextTransaction~
        +SaveChangesAsync(CancellationToken cancellationToken) Task~int~
        +ExecuteInTransactionAsync~T~(Func~Task~T~~ operation, CancellationToken cancellationToken) Task~T~
        +GetRepository~TEntity~() IRepository~TEntity~
        +GetSpecializedRepository~TRepository~() TRepository
        +BulkInsertAsync~T~(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
        +BulkUpdateAsync~T~(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
        +BulkDeleteAsync~T~(IEnumerable~T~ entities, CancellationToken cancellationToken) Task~void~
    }
    
    class ITransactionScope {
        <<Interface>>
        +CommitAsync(CancellationToken cancellationToken) Task~void~
        +RollbackAsync(CancellationToken cancellationToken) Task~void~
    }
    
    class IBulkOperations {
        <<Interface>>
        +BulkInsertAsync~T~(IEnumerable~T~ entities, BulkConfig config, CancellationToken cancellationToken) Task~void~
        +BulkUpdateAsync~T~(IEnumerable~T~ entities, BulkConfig config, CancellationToken cancellationToken) Task~void~
        +BulkDeleteAsync~T~(IEnumerable~T~ entities, BulkConfig config, CancellationToken cancellationToken) Task~void~
    }
    
    IUnitOfWork --> ITransactionScope : manages
    IUnitOfWork --> IBulkOperations : supports
```

### 3. 查询规约接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Specifications/ISpecification.cs`

```mermaid
classDiagram
    class ISpecification~T~ {
        <<Interface>>
        +Expression~Func~T, bool~~ Criteria
        +List~Expression~Func~T, object~~~ Includes
        +List~string~ IncludeStrings
        +Expression~Func~T, object~~ OrderBy
        +Expression~Func~T, object~~ OrderByDescending
        +List~Expression~Func~T, object~~~ ThenBy
        +List~Expression~Func~T, object~~~ ThenByDescending
        +int Take
        +int Skip
        +bool IsPagingEnabled
        +bool IsSatisfiedBy(T entity) bool
        +ISpecification~T~ And(ISpecification~T~ specification)
        +ISpecification~T~ Or(ISpecification~T~ specification)
        +ISpecification~T~ Not()
    }
    
    class ISpecificationBuilder~T~ {
        <<Interface>>
        +Where(Expression~Func~T, bool~~ predicate) ISpecificationBuilder~T~
        +Include(Expression~Func~T, object~~ includeExpression) ISpecificationBuilder~T~
        +Include(string includePath) ISpecificationBuilder~T~
        +OrderBy(Expression~Func~T, object~~ orderExpression) ISpecificationBuilder~T~
        +OrderByDescending(Expression~Func~T, object~~ orderExpression) ISpecificationBuilder~T~
        +Take(int count) ISpecificationBuilder~T~
        +Skip(int count) ISpecificationBuilder~T~
        +Build() ISpecification~T~
    }
    
    class BaseSpecification~T~ {
        <<Abstract>>
        #AddInclude(Expression~Func~T, object~~ includeExpression) void
        #AddInclude(string includePath) void
        #ApplyPaging(int skip, int take) void
        #ApplyOrderBy(Expression~Func~T, object~~ orderByExpression) void
    }
    
    ISpecification <-- ISpecificationBuilder : builds
    BaseSpecification ..|> ISpecification : implements
```

## 专门仓储接口设计

### 1. 用户管理仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/IUserProfileRepository.cs`

```mermaid
classDiagram
    class IUserProfileRepository {
        <<Interface>>
        +GetByUsernameAsync(string username, CancellationToken cancellationToken) Task~UserProfile~
        +GetByEmailAsync(string email, CancellationToken cancellationToken) Task~UserProfile~
        +GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~UserPreferences~~
        +UpdateUserPreferencesAsync(Guid userId, Dictionary~string, object~ preferences, CancellationToken cancellationToken) Task~void~
        +GetUsersByRoleAsync(string role, CancellationToken cancellationToken) Task~IReadOnlyList~UserProfile~~
        +UpdateLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken) Task~void~
        +IncrementLoginCountAsync(Guid userId, CancellationToken cancellationToken) Task~void~
        +SearchUsersAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken) Task~PagedResult~UserProfile~~
        +GetActiveUsersAsync(DateTime since, CancellationToken cancellationToken) Task~IReadOnlyList~UserProfile~~
        +BulkUpdateSecuritySettingsAsync(List~Guid~ userIds, SecuritySettings settings, CancellationToken cancellationToken) Task~void~
    }
    
    class UserSearchCriteria {
        <<DTO>>
        +string Username
        +string Email
        +string Role
        +bool? IsActive
        +DateTime? CreatedFrom
        +DateTime? CreatedTo
        +DateTime? LastLoginFrom
        +DateTime? LastLoginTo
    }
    
    class UserStatistics {
        <<DTO>>
        +int TotalUsers
        +int ActiveUsers
        +int NewUsersThisMonth
        +Dictionary~string, int~ UsersByRole
        +double AverageSessionTime
    }
    
    IUserProfileRepository --> UserSearchCriteria : uses
    IUserProfileRepository --> UserStatistics : returns
```

**输入输出定义**：

- **输入参数**：
  - `username/email`: 用户标识字符串，支持精确匹配
  - `userId`: 用户唯一标识符(Guid)
  - `preferences`: 用户偏好设置字典
  - `searchTerm`: 模糊搜索关键词
  - `pageNumber/pageSize`: 分页参数
  
- **输出类型**：
  - `UserProfile`: 完整用户档案实体
  - `PagedResult<UserProfile>`: 分页用户列表
  - `UserStatistics`: 用户统计信息
  - `IReadOnlyList<T>`: 只读集合，防止意外修改

### 2. 任务执行仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/ITaskExecutionHistoryRepository.cs`

```mermaid
classDiagram
    class ITaskExecutionHistoryRepository {
        <<Interface>>
        +GetExecutionsByUserAsync(Guid userId, DateTime? from, DateTime? to, CancellationToken cancellationToken) Task~IReadOnlyList~TaskExecutionHistory~~
        +GetExecutionStatisticsAsync(Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ExecutionStatistics~
        +GetFailedExecutionsAsync(DateTime since, CancellationToken cancellationToken) Task~IReadOnlyList~TaskExecutionHistory~~
        +GetExecutionsByStatusAsync(ExecutionStatus status, int limit, CancellationToken cancellationToken) Task~IReadOnlyList~TaskExecutionHistory~~
        +GetLongRunningExecutionsAsync(long thresholdMs, CancellationToken cancellationToken) Task~IReadOnlyList~TaskExecutionHistory~~
        +GetExecutionStepsAsync(Guid executionId, CancellationToken cancellationToken) Task~IReadOnlyList~ExecutionStepRecord~~
        +AddExecutionStepAsync(Guid executionId, ExecutionStepRecord step, CancellationToken cancellationToken) Task~void~
        +UpdateExecutionStatusAsync(Guid executionId, ExecutionStatus status, CancellationToken cancellationToken) Task~void~
        +GetResourceUsageStatsAsync(DateTime from, DateTime to, CancellationToken cancellationToken) Task~ResourceUsageStatistics~
        +CleanupOldExecutionsAsync(DateTime cutoffDate, CancellationToken cancellationToken) Task~int~
        +GetTopErrorPatternsAsync(int topCount, DateTime since, CancellationToken cancellationToken) Task~IReadOnlyList~ErrorPattern~~
    }
    
    class ExecutionStatistics {
        <<DTO>>
        +int TotalExecutions
        +int SuccessfulExecutions
        +int FailedExecutions
        +double SuccessRate
        +long AverageExecutionTime
        +long MedianExecutionTime
        +decimal TotalCost
        +int TotalTokenUsage
        +Dictionary~string, int~ ExecutionsByType
        +Dictionary~string, double~ AgentPerformance
    }
    
    class ResourceUsageStatistics {
        <<DTO>>
        +double AverageCpuUsage
        +long AverageMemoryUsage
        +long TotalDiskIO
        +long TotalNetworkIO
        +Dictionary~string, double~ PeakUsage
        +DateTime[] HighUsagePeriods
    }
    
    class ErrorPattern {
        <<DTO>>
        +string ErrorCode
        +string ErrorMessage
        +int Frequency
        +DateTime FirstOccurrence
        +DateTime LastOccurrence
        +List~string~ AffectedComponents
    }
    
    ITaskExecutionHistoryRepository --> ExecutionStatistics : returns
    ITaskExecutionHistoryRepository --> ResourceUsageStatistics : returns
    ITaskExecutionHistoryRepository --> ErrorPattern : returns
```

### 3. Agent能力仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/IAgentCapabilityRepository.cs`

```mermaid
classDiagram
    class IAgentCapabilityRepository {
        <<Interface>>
        +GetByAgentIdAsync(string agentId, CancellationToken cancellationToken) Task~AgentCapabilityRegistry~
        +GetByCapabilityAsync(string capability, CancellationToken cancellationToken) Task~IReadOnlyList~AgentCapabilityRegistry~~
        +GetActiveAgentsAsync(CancellationToken cancellationToken) Task~IReadOnlyList~AgentCapabilityRegistry~~
        +GetAgentsByTypeAsync(AgentType agentType, CancellationToken cancellationToken) Task~IReadOnlyList~AgentCapabilityRegistry~~
        +UpdateHealthStatusAsync(string agentId, HealthStatus status, CancellationToken cancellationToken) Task~void~
        +UpdatePerformanceMetricsAsync(string agentId, PerformanceMetrics metrics, CancellationToken cancellationToken) Task~void~
        +GetAgentActionsAsync(string agentId, CancellationToken cancellationToken) Task~IReadOnlyList~AgentActionDefinition~~
        +RegisterActionAsync(string agentId, AgentActionDefinition action, CancellationToken cancellationToken) Task~void~
        +UnregisterActionAsync(string agentId, Guid actionId, CancellationToken cancellationToken) Task~bool~
        +SearchAgentsAsync(AgentSearchCriteria criteria, CancellationToken cancellationToken) Task~PagedResult~AgentCapabilityRegistry~~
        +GetAgentHealthReportAsync(DateTime since, CancellationToken cancellationToken) Task~AgentHealthReport~
        +IncrementActionUsageAsync(Guid actionId, CancellationToken cancellationToken) Task~void~
    }
    
    class AgentSearchCriteria {
        <<DTO>>
        +string AgentName
        +AgentType? AgentType
        +bool? IsActive
        +List~string~ RequiredCapabilities
        +HealthStatus? MinHealthStatus
        +double? MinSuccessRate
        +int PageNumber
        +int PageSize
    }
    
    class AgentHealthReport {
        <<DTO>>
        +int TotalAgents
        +int HealthyAgents
        +int WarningAgents
        +int CriticalAgents
        +int OfflineAgents
        +Dictionary~string, HealthStatus~ AgentStatuses
        +List~string~ RecentlyFailedAgents
        +double OverallHealthScore
    }
    
    IAgentCapabilityRepository --> AgentSearchCriteria : uses
    IAgentCapabilityRepository --> AgentHealthReport : returns
```

### 4. LLM管理仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/IModelProviderRepository.cs`

```mermaid
classDiagram
    class IModelProviderRepository {
        <<Interface>>
        +GetByTypeAsync(ProviderType providerType, CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +GetActiveProvidersAsync(CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +GetPrebuiltProvidersAsync(CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +SearchProvidersAsync(string searchTerm, CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +GetProviderWithModelsAsync(Guid providerId, CancellationToken cancellationToken) Task~ModelProvider~
        +UpdateProviderStatusAsync(Guid providerId, ServiceStatus status, CancellationToken cancellationToken) Task~void~
        +GetProviderUsageStatisticsAsync(Guid providerId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ProviderUsageStatistics~
        +ValidateProviderConfigurationAsync(Guid providerId, CancellationToken cancellationToken) Task~ValidationResult~
        +GetProviderByNameAsync(string providerName, CancellationToken cancellationToken) Task~ModelProvider~
        +GetProvidersRequiringHealthCheckAsync(CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
    }
    
    class IModelRepository {
        <<Interface>>
        +GetByProviderAsync(Guid providerId, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +GetByCapabilityAsync(ModelCapability capability, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +GetPrebuiltModelsAsync(CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +SearchModelsAsync(ModelSearchCriteria criteria, CancellationToken cancellationToken) Task~PagedResult~Model~~
        +GetModelsByGroupAsync(string modelGroup, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +GetLatestVersionModelsAsync(CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +UpdatePerformanceMetricsAsync(Guid modelId, PerformanceMetrics metrics, CancellationToken cancellationToken) Task~void~
        +GetModelUsageStatisticsAsync(Guid modelId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ModelUsageStatistics~
        +GetRecommendedModelsAsync(RecommendationCriteria criteria, CancellationToken cancellationToken) Task~IReadOnlyList~ModelRecommendation~~
        +GetModelsByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +BulkUpdateModelMetricsAsync(List~ModelMetricsUpdate~ updates, CancellationToken cancellationToken) Task~void~
    }
    
    class ProviderUsageStatistics {
        <<DTO>>
        +Guid ProviderId
        +string ProviderName
        +int TotalRequests
        +int SuccessfulRequests
        +int FailedRequests
        +double SuccessRate
        +long TotalTokensUsed
        +decimal TotalCost
        +double AverageResponseTime
        +Dictionary~string, int~ RequestsByModel
        +Dictionary~string, decimal~ CostByModel
        +DateTime StatisticsPeriodStart
        +DateTime StatisticsPeriodEnd
    }
    
    class ModelSearchCriteria {
        <<DTO>>
        +string SearchTerm
        +Guid? ProviderId
        +List~ModelCapability~ RequiredCapabilities
        +string ModelGroup
        +decimal? MaxInputPrice
        +decimal? MaxOutputPrice
        +int? MinContextLength
        +bool LatestVersionOnly
        +bool PrebuiltOnly
        +int PageNumber
        +int PageSize
    }
    
    class ModelUsageStatistics {
        <<DTO>>
        +Guid ModelId
        +string ModelName
        +int TotalUsage
        +long TotalTokensProcessed
        +decimal TotalCost
        +double AverageResponseTime
        +double AverageQualityScore
        +int ErrorCount
        +Dictionary~string, object~ UsagePatterns
        +List~UsageTrend~ TrendData
    }
    
    class ModelRecommendation {
        <<DTO>>
        +Guid ModelId
        +string ModelName
        +double RecommendationScore
        +string RecommendationReason
        +decimal EstimatedCost
        +double EstimatedPerformance
        +List~string~ Strengths
        +List~string~ Considerations
    }
    
    class RecommendationCriteria {
        <<DTO>>
        +string TaskType
        +int ExpectedTokenUsage
        +decimal MaxBudget
        +List~ModelCapability~ RequiredCapabilities
        +PerformancePriority Priority
        +bool ConsiderCost
        +bool ConsiderLatency
        +bool ConsiderQuality
    }
    
    IModelProviderRepository --> ProviderUsageStatistics : returns
    IModelRepository --> ModelSearchCriteria : uses
    IModelRepository --> ModelUsageStatistics : returns
    IModelRepository --> ModelRecommendation : returns
    IModelRepository --> RecommendationCriteria : uses
```

### 5. LLM用户配置仓储接口

**项目位置**: `Lorn.OpenAgenticAI.OpenAgenticAI.Domain.Contracts/Repositories/IProviderUserConfigurationRepository.cs`

```mermaid
classDiagram
    class IProviderUserConfigurationRepository {
        <<Interface>>
        +GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +GetByUserAndProviderAsync(Guid userId, Guid providerId, CancellationToken cancellationToken) Task~ProviderUserConfiguration~
        +GetActiveConfigurationsAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +GetConfigurationsByPriorityAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +UpdateUsageQuotaAsync(Guid configurationId, UsageQuota quota, CancellationToken cancellationToken) Task~void~
        +RecordUsageAsync(Guid configurationId, int tokenUsage, decimal cost, CancellationToken cancellationToken) Task~void~
        +GetUsageStatisticsAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ConfigurationUsageStatistics~
        +ValidateApiConfigurationAsync(Guid configurationId, CancellationToken cancellationToken) Task~ValidationResult~
        +GetConfigurationsNearQuotaLimitAsync(double thresholdPercentage, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +BulkUpdateQuotasAsync(List~QuotaUpdate~ updates, CancellationToken cancellationToken) Task~void~
    }
    
    class IModelUserConfigurationRepository {
        <<Interface>>
        +GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ModelUserConfiguration~~
        +GetByUserAndModelAsync(Guid userId, Guid modelId, CancellationToken cancellationToken) Task~ModelUserConfiguration~
        +GetActiveConfigurationsAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ModelUserConfiguration~~
        +GetConfigurationsByPriorityAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ModelUserConfiguration~~
        +GetFallbackModelsAsync(Guid userId, Guid primaryModelId, CancellationToken cancellationToken) Task~IReadOnlyList~ModelUserConfiguration~~
        +UpdateModelParametersAsync(Guid configurationId, ModelParameters parameters, CancellationToken cancellationToken) Task~void~
        +RecordModelUsageAsync(Guid configurationId, ModelUsageRecord usage, CancellationToken cancellationToken) Task~void~
        +GetOptimalModelForTaskAsync(Guid userId, TaskOptimizationCriteria criteria, CancellationToken cancellationToken) Task~ModelUserConfiguration~
        +GetModelPerformanceComparisonAsync(Guid userId, List~Guid~ modelIds, CancellationToken cancellationToken) Task~ModelPerformanceComparison~
        +UpdateQualitySettingsAsync(Guid configurationId, QualitySettings settings, CancellationToken cancellationToken) Task~void~
    }
    
    class ConfigurationUsageStatistics {
        <<DTO>>
        +Guid ConfigurationId
        +int TotalRequests
        +long TotalTokensUsed
        +decimal TotalCost
        +double AverageResponseTime
        +int QuotaUtilizationPercentage
        +List~DailyUsage~ DailyBreakdown
        +Dictionary~string, object~ UsagePatterns
    }
    
    class ModelUsageRecord {
        <<DTO>>
        +int InputTokens
        +int OutputTokens
        +double ResponseTimeMs
        +double QualityScore
        +bool IsSuccessful
        +string ErrorType
        +Dictionary~string, object~ Metadata
    }
    
    class TaskOptimizationCriteria {
        <<DTO>>
        +string TaskType
        +int EstimatedTokens
        +PerformancePriority Priority
        +decimal MaxCost
        +int MaxResponseTimeMs
        +List~ModelCapability~ RequiredCapabilities
    }
    
    class ModelPerformanceComparison {
        <<DTO>>
        +List~ModelPerformanceMetrics~ ModelMetrics
        +ModelPerformanceMetrics BestOverall
        +ModelPerformanceMetrics BestForCost
        +ModelPerformanceMetrics BestForSpeed
        +ModelPerformanceMetrics BestForQuality
        +string RecommendedModelId
        +string RecommendationReason
    }
    
    IProviderUserConfigurationRepository --> ConfigurationUsageStatistics : returns
    IModelUserConfigurationRepository --> ModelUsageRecord : uses
    IModelUserConfigurationRepository --> TaskOptimizationCriteria : uses
    IModelUserConfigurationRepository --> ModelPerformanceComparison : returns
```

### 6. MCP配置仓储接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Repositories/IMCPConfigurationRepository.cs`

```mermaid
classDiagram
    class IMCPConfigurationRepository {
        <<Interface>>
        +GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +GetByProtocolTypeAsync(MCPProtocolType protocolType, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +GetActiveConfigurationsAsync(CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +GetByTagsAsync(List~string~ tags, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +SearchConfigurationsAsync(MCPSearchCriteria criteria, CancellationToken cancellationToken) Task~PagedResult~MCPConfiguration~~
        +TestConfigurationAsync(Guid configurationId, CancellationToken cancellationToken) Task~ConnectionTestResult~
        +UpdateLastUsedTimeAsync(Guid configurationId, CancellationToken cancellationToken) Task~void~
        +GetConfigurationUsageStatisticsAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~MCPUsageStatistics~
        +ValidateConfigurationAsync(Guid configurationId, CancellationToken cancellationToken) Task~ValidationResult~
        +GetConfigurationsByProviderAsync(string providerName, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +BulkTestConfigurationsAsync(List~Guid~ configurationIds, CancellationToken cancellationToken) Task~List~ConnectionTestResult~~
        +GetFailedConfigurationsAsync(DateTime since, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
    }
    
    class IConfigurationTemplateRepository {
        <<Interface>>
        +GetByProtocolTypeAsync(MCPProtocolType protocolType, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +GetByCategoryAsync(string category, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +GetBuiltInTemplatesAsync(CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +GetPopularTemplatesAsync(int count, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +IncrementPopularityAsync(Guid templateId, CancellationToken cancellationToken) Task~void~
        +GetTemplateUsageStatisticsAsync(Guid templateId, CancellationToken cancellationToken) Task~TemplateUsageStatistics~
        +CreateConfigurationFromTemplateAsync(Guid templateId, Dictionary~string, object~ parameters, Guid userId, CancellationToken cancellationToken) Task~MCPConfiguration~
        +ValidateTemplateParametersAsync(Guid templateId, Dictionary~string, object~ parameters, CancellationToken cancellationToken) Task~ValidationResult~
        +GetTemplatesByCompatibilityAsync(string targetSystem, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
    }
    
    class MCPSearchCriteria {
        <<DTO>>
        +string SearchTerm
        +Guid? UserId
        +MCPProtocolType? ProtocolType
        +List~string~ Tags
        +bool? IsEnabled
        +string ProviderName
        +DateTime? CreatedAfter
        +DateTime? CreatedBefore
        +int PageNumber
        +int PageSize
    }
    
    class ConnectionTestResult {
        <<DTO>>
        +Guid ConfigurationId
        +bool IsSuccessful
        +string ErrorMessage
        +double ResponseTimeMs
        +DateTime TestTimestamp
        +Dictionary~string, object~ TestDetails
        +List~string~ Warnings
        +string ConnectionStatus
        +object HealthData
    }
    
    class MCPUsageStatistics {
        <<DTO>>
        +Guid ConfigurationId
        +string ConfigurationName
        +int TotalCalls
        +int SuccessfulCalls
        +int FailedCalls
        +double SuccessRate
        +double AverageResponseTime
        +long TotalDataTransferred
        +List~UsageByDay~ DailyUsage
        +Dictionary~string, int~ CallsByOperation
        +List~ErrorSummary~ CommonErrors
    }
    
    class TemplateUsageStatistics {
        <<DTO>>
        +Guid TemplateId
        +string TemplateName
        +int TimesUsed
        +int SuccessfulConfigurations
        +double SuccessRate
        +double AverageRating
        +List~string~ PopularParameters
        +Dictionary~string, int~ UsageByCategory
        +DateTime LastUsed
    }
    
    IMCPConfigurationRepository --> MCPSearchCriteria : uses
    IMCPConfigurationRepository --> ConnectionTestResult : returns
    IMCPConfigurationRepository --> MCPUsageStatistics : returns
    IConfigurationTemplateRepository --> TemplateUsageStatistics : returns
```

## 查询服务接口设计

### 复杂查询服务接口

**项目位置**: `Lorn.OpenAgenticAI.Domain.Contracts/Services/IQueryService.cs`

```mermaid
classDiagram
    class IQueryService {
        <<Interface>>
        +ExecuteQueryAsync~T~(string queryName, object parameters, CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +ExecuteScalarQueryAsync~T~(string queryName, object parameters, CancellationToken cancellationToken) Task~T~
        +ExecutePagedQueryAsync~T~(string queryName, object parameters, int pageNumber, int pageSize, CancellationToken cancellationToken) Task~PagedResult~T~~
        +ExecuteRawSqlAsync~T~(string sql, object[] parameters, CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +GetQueryDefinitionAsync(string queryName, CancellationToken cancellationToken) Task~QueryDefinition~
        +RegisterQueryAsync(QueryDefinition queryDefinition, CancellationToken cancellationToken) Task~void~
    }
    
    class IAnalyticsQueryService {
        <<Interface>>
        +GetDashboardDataAsync(DashboardRequest request, CancellationToken cancellationToken) Task~DashboardData~
        +GetTrendAnalysisAsync(TrendRequest request, CancellationToken cancellationToken) Task~TrendAnalysis~
        +GetPerformanceReportAsync(PerformanceReportRequest request, CancellationToken cancellationToken) Task~PerformanceReport~
        +ExportDataAsync(DataExportRequest request, CancellationToken cancellationToken) Task~ExportResult~
    }
    
    class QueryDefinition {
        <<DTO>>
        +string QueryName
        +string Description
        +string SqlTemplate
        +Dictionary~string, QueryParameter~ Parameters
        +bool IsReadOnly
        +int TimeoutSeconds
        +string[] RequiredPermissions
    }
    
    class QueryParameter {
        <<DTO>>
        +string Name
        +Type ParameterType
        +bool IsRequired
        +object DefaultValue
        +string ValidationRule
    }
    
    IQueryService --> QueryDefinition : uses
    IQueryService --> QueryParameter : configures
    IAnalyticsQueryService --|> IQueryService : extends
```

## 仓储实现设计

### 1. 基础仓储实现

**项目位置**: `Lorn.OpenAgenticAI.Infrastructure.Data/Repositories/RepositoryBase.cs`

```mermaid
classDiagram
    class RepositoryBase~T~ {
        <<Abstract>>
        #OpenAgenticAIDbContext DbContext
        #DbSet~T~ DbSet
        #ILogger~RepositoryBase~T~~ Logger
        +GetByIdAsync(TId id, CancellationToken cancellationToken) Task~T~
        +GetAllAsync(CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +FindAsync(ISpecification~T~ specification, CancellationToken cancellationToken) Task~IReadOnlyList~T~~
        +AddAsync(T entity, CancellationToken cancellationToken) Task~T~
        +UpdateAsync(T entity, CancellationToken cancellationToken) Task~T~
        +DeleteAsync(T entity, CancellationToken cancellationToken) Task~void~
        #ApplySpecification(ISpecification~T~ spec) IQueryable~T~
        #LogPerformance(string operation, TimeSpan duration) void
        #ValidateEntity(T entity) ValidationResult
    }
    
    class IRepositoryCache {
        <<Interface>>
        +GetAsync~T~(string key, CancellationToken cancellationToken) Task~T~
        +SetAsync~T~(string key, T value, TimeSpan expiration, CancellationToken cancellationToken) Task~void~
        +RemoveAsync(string key, CancellationToken cancellationToken) Task~void~
        +RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) Task~void~
    }
    
    class CachedRepositoryBase~T~ {
        <<Abstract>>
        #IRepositoryCache Cache
        #string GetCacheKey(object id) string
        #string GetCachePattern() string
        +GetByIdAsync(TId id, CancellationToken cancellationToken) Task~T~
        +InvalidateCacheAsync(T entity, CancellationToken cancellationToken) Task~void~
    }
    
    RepositoryBase --> IRepositoryCache : optionally_uses
    CachedRepositoryBase --|> RepositoryBase : extends
    CachedRepositoryBase --> IRepositoryCache : uses
```

### 2. 专门仓储实现

**项目位置**: `Lorn.OpenAgenticAI.OpenAgenticAI.Infrastructure.Data/Repositories/TaskExecutionHistoryRepository.cs`

```mermaid
classDiagram
    class TaskExecutionHistoryRepository {
        <<Repository>>
        +GetExecutionsByUserAsync(Guid userId, DateTime? from, DateTime? to, CancellationToken cancellationToken) Task~IReadOnlyList~TaskExecutionHistory~~
        +GetExecutionStatisticsAsync(Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ExecutionStatistics~
        -BuildUserExecutionsQuery(Guid userId, DateTime? from, DateTime? to) IQueryable~TaskExecutionHistory~
        -BuildStatisticsQuery(Guid userId, DateTime from, DateTime to) IQueryable~TaskExecutionHistory~
        -CalculateStatistics(IQueryable~TaskExecutionHistory~ query) Task~ExecutionStatistics~
        -LogQueryPerformance(string queryName, TimeSpan duration, int resultCount) void
    }
    
    class UserProfileRepository {
        <<Repository>>
        +GetByUsernameAsync(string username, CancellationToken cancellationToken) Task~UserProfile~
        +GetByEmailAsync(string email, CancellationToken cancellationToken) Task~UserProfile~
        +UpdateUserPreferencesAsync(Guid userId, Dictionary~string, object~ preferences, CancellationToken cancellationToken) Task~void~
        -BuildUsernameQuery(string username) IQueryable~UserProfile~
        -BuildEmailQuery(string email) IQueryable~UserProfile~
        -ValidatePreferences(Dictionary~string, object~ preferences) ValidationResult
    }
    
    TaskExecutionHistoryRepository --|> RepositoryBase : extends
    UserProfileRepository --|> CachedRepositoryBase : extends
    TaskExecutionHistoryRepository ..|> ITaskExecutionHistoryRepository : implements
    UserProfileRepository ..|> IUserProfileRepository : implements
```

### 3. LLM管理仓储实现

**项目位置**: `Lorn.OpenAgenticAI.OpenAgenticAI.Infrastructure.Data/Repositories/LLM/`

```mermaid
classDiagram
    class ModelProviderRepository {
        <<Repository>>
        +GetByTypeAsync(ProviderType providerType, CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +GetActiveProvidersAsync(CancellationToken cancellationToken) Task~IReadOnlyList~ModelProvider~~
        +GetProviderWithModelsAsync(Guid providerId, CancellationToken cancellationToken) Task~ModelProvider~
        +UpdateProviderStatusAsync(Guid providerId, ServiceStatus status, CancellationToken cancellationToken) Task~void~
        +GetProviderUsageStatisticsAsync(Guid providerId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ProviderUsageStatistics~
        -BuildProviderQuery(string searchTerm) IQueryable~ModelProvider~
        -CalculateProviderStatistics(Guid providerId, DateTime from, DateTime to) Task~ProviderUsageStatistics~
        -ValidateProviderConfiguration(ModelProvider provider) Task~ValidationResult~
        -LoadProviderModels(ModelProvider provider) Task~void~
    }
    
    class ModelRepository {
        <<Repository>>
        +GetByProviderAsync(Guid providerId, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +GetByCapabilityAsync(ModelCapability capability, CancellationToken cancellationToken) Task~IReadOnlyList~Model~~
        +SearchModelsAsync(ModelSearchCriteria criteria, CancellationToken cancellationToken) Task~PagedResult~Model~~
        +GetRecommendedModelsAsync(RecommendationCriteria criteria, CancellationToken cancellationToken) Task~IReadOnlyList~ModelRecommendation~~
        +UpdatePerformanceMetricsAsync(Guid modelId, PerformanceMetrics metrics, CancellationToken cancellationToken) Task~void~
        +GetModelUsageStatisticsAsync(Guid modelId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ModelUsageStatistics~
        -BuildModelSearchQuery(ModelSearchCriteria criteria) IQueryable~Model~
        -CalculateRecommendationScore(Model model, RecommendationCriteria criteria) double
        -AnalyzeModelPerformance(Guid modelId, DateTime from, DateTime to) Task~ModelUsageStatistics~
        -ApplyCapabilityFilters(IQueryable~Model~ query, List~ModelCapability~ capabilities) IQueryable~Model~
    }
    
    class ProviderUserConfigurationRepository {
        <<Repository>>
        +GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +GetByUserAndProviderAsync(Guid userId, Guid providerId, CancellationToken cancellationToken) Task~ProviderUserConfiguration~
        +GetActiveConfigurationsAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~ProviderUserConfiguration~~
        +RecordUsageAsync(Guid configurationId, int tokenUsage, decimal cost, CancellationToken cancellationToken) Task~void~
        +GetUsageStatisticsAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~ConfigurationUsageStatistics~
        -BuildUserConfigurationQuery(Guid userId, bool activeOnly) IQueryable~ProviderUserConfiguration~
        -UpdateUsageQuota(ProviderUserConfiguration configuration, int tokenUsage, decimal cost) Task~void~
        -CalculateUsageStatistics(Guid configurationId, DateTime from, DateTime to) Task~ConfigurationUsageStatistics~
        -ValidateApiConfiguration(ApiConfiguration apiConfig) Task~ValidationResult~
    }
    
    ModelProviderRepository --|> CachedRepositoryBase : extends
    ModelRepository --|> CachedRepositoryBase : extends
    ProviderUserConfigurationRepository --|> RepositoryBase : extends
    
    ModelProviderRepository ..|> IModelProviderRepository : implements
    ModelRepository ..|> IModelRepository : implements
    ProviderUserConfigurationRepository ..|> IProviderUserConfigurationRepository : implements
```

### 4. MCP配置仓储实现

**项目位置**: `Lorn.OpenAgenticAI.Infrastructure.Data/Repositories/MCP/`

```mermaid
classDiagram
    class MCPConfigurationRepository {
        <<Repository>>
        +GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +GetByProtocolTypeAsync(MCPProtocolType protocolType, CancellationToken cancellationToken) Task~IReadOnlyList~MCPConfiguration~~
        +SearchConfigurationsAsync(MCPSearchCriteria criteria, CancellationToken cancellationToken) Task~PagedResult~MCPConfiguration~~
        +TestConfigurationAsync(Guid configurationId, CancellationToken cancellationToken) Task~ConnectionTestResult~
        +GetConfigurationUsageStatisticsAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken cancellationToken) Task~MCPUsageStatistics~
        +BulkTestConfigurationsAsync(List~Guid~ configurationIds, CancellationToken cancellationToken) Task~List~ConnectionTestResult~~
        -BuildConfigurationSearchQuery(MCPSearchCriteria criteria) IQueryable~MCPConfiguration~
        -ExecuteConnectionTest(MCPConfiguration configuration) Task~ConnectionTestResult~
        -AnalyzeConfigurationUsage(Guid configurationId, DateTime from, DateTime to) Task~MCPUsageStatistics~
        -ValidateConfiguration(MCPConfiguration configuration) Task~ValidationResult~
        -LoadAdapterConfiguration(MCPConfiguration configuration) Task~void~
    }
    
    class ConfigurationTemplateRepository {
        <<Repository>>
        +GetByProtocolTypeAsync(MCPProtocolType protocolType, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +GetPopularTemplatesAsync(int count, CancellationToken cancellationToken) Task~IReadOnlyList~ConfigurationTemplate~~
        +CreateConfigurationFromTemplateAsync(Guid templateId, Dictionary~string, object~ parameters, Guid userId, CancellationToken cancellationToken) Task~MCPConfiguration~
        +ValidateTemplateParametersAsync(Guid templateId, Dictionary~string, object~ parameters, CancellationToken cancellationToken) Task~ValidationResult~
        +GetTemplateUsageStatisticsAsync(Guid templateId, CancellationToken cancellationToken) Task~TemplateUsageStatistics~
        -BuildTemplateQuery(string searchTerm, string category) IQueryable~ConfigurationTemplate~
        -ApplyTemplateParameters(ConfigurationTemplate template, Dictionary~string, object~ parameters) MCPConfiguration
        -ValidateParameterConstraints(ConfigurationTemplate template, Dictionary~string, object~ parameters) ValidationResult
        -CalculateTemplateStatistics(Guid templateId) Task~TemplateUsageStatistics~
    }
    
    MCPConfigurationRepository --|> RepositoryBase : extends
    ConfigurationTemplateRepository --|> CachedRepositoryBase : extends
    
    MCPConfigurationRepository ..|> IMCPConfigurationRepository : implements
    ConfigurationTemplateRepository ..|> IConfigurationTemplateRepository : implements
```

## 技术实现要点

### 1. LLM仓储实现指导

**输入输出参数详细定义**：

1. **ModelProviderRepository**：
   - `GetProviderUsageStatisticsAsync`: 输入时间范围和提供商ID，输出详细的使用统计信息，包括请求数、成功率、成本分析等
   - `UpdateProviderStatusAsync`: 实现提供商状态的原子性更新，确保状态变更的一致性
   - `ValidateProviderConfiguration`: 验证API配置的有效性，包括连接测试和权限验证

2. **ModelRepository**：
   - `GetRecommendedModelsAsync`: 根据任务类型、预算、性能要求等条件，计算推荐分数并返回排序后的模型列表
   - `SearchModelsAsync`: 支持多维度搜索，包括能力、价格范围、上下文长度等复合条件
   - `AnalyzeModelPerformance`: 生成模型使用报告，包括平均响应时间、质量评分、成本效益分析

### 2. MCP仓储实现指导

**功能职责和实现要求**：

1. **MCPConfigurationRepository**：
   - `TestConfigurationAsync`: 实现各种协议类型的连接测试，返回详细的测试结果和诊断信息
   - `BulkTestConfigurationsAsync`: 支持批量测试配置，提高运维效率
   - `SearchConfigurationsAsync`: 实现复合条件搜索，支持标签、协议类型、提供商等维度

2. **ConfigurationTemplateRepository**：
   - `CreateConfigurationFromTemplateAsync`: 基于模板生成新配置，自动验证参数并应用默认值
   - `ValidateTemplateParametersAsync`: 验证用户提供的参数是否符合模板约束
   - `CalculateTemplateStatistics`: 统计模板使用情况，支持热门模板推荐

### 3. 性能优化策略

**缓存和查询优化**：

- LLM相关仓储使用`CachedRepositoryBase`，缓存常用的提供商和模型信息
- MCP配置仓储针对经常查询的活跃配置进行缓存
- 使用适当的索引策略优化搜索和统计查询性能
- 实现懒加载和预加载策略，平衡内存使用和查询效率
