# 业务流程图及交互序列图

## 业务流程图

### 提示词驱动模式（智能助手模式）

```mermaid
graph TD
    A[开始] --> B[用户通过界面输入自然语言需求]
    B --> C[Director接收需求]
    C --> D1[知识库提供上下文]
    C --> D2[LLM解析需求和规划任务]
    D1 --> D2
    D2 --> E[Director制定执行计划]
    E --> F[AgentHub调度相关Agent]
    F --> G[各Agent执行任务]
    G --> H[AgentHub汇总执行结果]
    H --> I[Director整合反馈]
    I --> J[界面展示执行结果]
    I --> K[知识库存储交互记录]
    J --> L[结束]
```

### 流程编排模式（专业工作流模式）

```mermaid
graph TD
    A[开始] --> B[用户使用工作流设计器创建流程]
    B --> C[工作流管理器存储工作流]
    C --> D[用户触发工作流执行]
    D --> E[工作流管理器加载工作流]
    E --> F[Director解析工作流步骤]
    F --> G[AgentHub按流程调度Agent]
    G --> H[各Agent执行任务]
    H --> I[AgentHub汇总执行结果]
    I --> J[Director整合反馈]
    J --> K[界面展示执行结果]
    J --> L[知识库存储执行记录]
    K --> M[结束]
```

## 交互序列图

### 提示词驱动模式

```mermaid
sequenceDiagram
    participant User as 用户
    participant UI as 用户界面
    participant Director as Director调度引擎
    participant KB as 知识库
    participant LLM as 大语言模型
    participant AgentHub as Agent调度中心
    participant Agent as 各类Agent

    User->>UI: 输入自然语言需求
    UI->>Director: 传递用户需求
    Director->>KB: 获取上下文信息
    KB->>Director: 返回相关上下文
    Director->>LLM: 提供需求和上下文
    LLM->>Director: 返回任务规划
    Director->>AgentHub: 调度Agent
    AgentHub->>Agent: 分配任务
    Agent->>AgentHub: 返回执行状态
    AgentHub->>Director: 汇总状态
    Director->>KB: 存储交互记录
    Director->>UI: 返回结果
    UI->>User: 展示执行结果
```

### 流程编排模式

```mermaid
sequenceDiagram
    participant User as 用户
    participant UI as 工作流设计器
    participant WFM as 工作流管理器
    participant Director as Director调度引擎
    participant KB as 知识库
    participant AgentHub as Agent调度中心
    participant Agent as 各类Agent

    User->>UI: 创建/编辑工作流
    UI->>WFM: 保存工作流
    WFM->>WFM: 存储工作流模板
    User->>UI: 触发工作流执行
    UI->>WFM: 请求执行工作流
    WFM->>Director: 加载工作流步骤
    Director->>KB: 获取相关上下文
    KB->>Director: 返回上下文信息
    Director->>AgentHub: 根据工作流调度Agent
    AgentHub->>Agent: 分配任务
    Agent->>AgentHub: 返回执行状态
    AgentHub->>Director: 汇总状态
    Director->>KB: 存储执行记录
    Director->>WFM: 更新工作流执行状态
    Director->>UI: 返回执行结果
    UI->>User: 展示执行结果
```

## 核心组件交互步骤

### 界面层与核心程序的交互
1. 用户通过界面层输入需求或触发工作流
2. 界面层将请求传递给Director调度引擎
3. Director完成任务处理后，将结果返回界面层
4. 界面层向用户展示执行结果和状态

### Director与知识库的交互
1. Director接收到请求后，从知识库获取相关上下文
2. 任务执行完成后，Director将交互记录存入知识库
3. 知识库根据新增记录更新个性化配置和优化建议

### Director与工作流管理器的交互
1. 用户通过工作流设计器创建/编辑工作流，由工作流管理器存储
2. 工作流触发执行时，工作流管理器将工作流步骤传递给Director
3. Director根据工作流步骤调度Agent执行任务
4. 执行完成后，Director将状态反馈给工作流管理器更新工作流执行历史

### 知识库与大语言模型的协作
1. Director从知识库获取上下文后，将其与用户需求一并传递给大语言模型
2. 大语言模型结合上下文信息，生成更准确的任务规划
3. 任务规划返回给Director进行进一步处理
