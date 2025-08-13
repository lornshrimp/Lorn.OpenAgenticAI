using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Shared.Contracts.Repositories;

namespace Lorn.OpenAgenticAI.Domain.Contracts.Repositories;

/// <summary>
/// 任务执行历史仓储接口（领域层契约）
/// </summary>
public interface ITaskExecutionRepository : IRepository<TaskExecutionHistory>, IAsyncRepository<TaskExecutionHistory>
{
    TaskExecutionHistory? GetByRequestId(string requestId);
    Task<TaskExecutionHistory?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionStepRecord>> ListStepsAsync(Guid executionId, CancellationToken cancellationToken = default);
}
