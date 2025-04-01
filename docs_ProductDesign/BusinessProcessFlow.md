# 业务流程图及交互序列图

## 业务流程图

```mermaid
graph TD
    A[开始] --> B[用户输入需求]
    B --> C[Director解析需求]
    C --> D[LLM任务规划]
    D --> E[AgentHub调度]
    E --> F[各Agent执行任务]
    F --> G[返回执行结果]
    G --> H[结束]
```

## 交互序列图

```mermaid
sequenceDiagram
    participant User as 用户
    participant Director as Director调度引擎
    participant LLM as 大语言模型
    participant AgentHub as Agent调度中心
    participant Agent as 各类Agent

    User->>Director: 输入需求
    Director->>LLM: 解析需求
    LLM->>Director: 返回任务规划
    Director->>AgentHub: 调度Agent
    AgentHub->>Agent: 分配任务
    Agent->>AgentHub: 返回执行状态
    AgentHub->>Director: 汇总状态
    Director->>User: 返回结果
```

## 各模块交互步骤

### 用户输入需求
1. 用户通过自然语言或可视化界面输入需求。
2. 输入的需求被传递给Director调度引擎。

### Director解析需求
1. Director接收到用户需求后，调用大语言模型（LLM）进行解析。
2. LLM根据需求生成任务规划，并返回给Director。

### LLM任务规划
1. LLM根据用户需求，生成详细的任务规划。
2. 任务规划包括各个步骤的执行顺序和所需的Agent。

### AgentHub调度
1. Director根据任务规划，调用AgentHub进行任务调度。
2. AgentHub根据任务类型，选择合适的Agent执行任务。

### 各Agent执行任务
1. 各Agent接收到任务后，开始执行具体操作。
2. 执行过程中，Agent会实时反馈执行状态给AgentHub。

### 返回执行结果
1. AgentHub汇总各Agent的执行状态，并返回给Director。
2. Director将最终结果返回给用户。
