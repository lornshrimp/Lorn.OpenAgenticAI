using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// EF Core 实现的任务执行历史仓储
/// </summary>
public class TaskExecutionRepository : ITaskExecutionRepository
{
    private readonly OpenAgenticAIDbContext _ctx;
    private readonly ILogger<TaskExecutionRepository>? _logger;

    public TaskExecutionRepository(OpenAgenticAIDbContext ctx, ILogger<TaskExecutionRepository>? logger = null)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _logger = logger;
    }

    // Sync generic operations
    public TaskExecutionHistory Add(TaskExecutionHistory entity)
    {
        _ctx.TaskExecutionHistories.Add(entity);
        _ctx.SaveChanges();
        return entity;
    }

    public void AddRange(IEnumerable<TaskExecutionHistory> entities)
    {
        _ctx.TaskExecutionHistories.AddRange(entities);
        _ctx.SaveChanges();
    }

    public TaskExecutionHistory? GetById(object id)
    {
        if (id is Guid gid)
            return _ctx.TaskExecutionHistories.Find(gid);
        return null;
    }

    public IEnumerable<TaskExecutionHistory> ListAll()
        => _ctx.TaskExecutionHistories.AsNoTracking().ToList();

    public IEnumerable<TaskExecutionHistory> List(Expression<Func<TaskExecutionHistory, bool>> predicate)
        => _ctx.TaskExecutionHistories.AsNoTracking().Where(predicate).ToList();

    public void Update(TaskExecutionHistory entity)
    {
        _ctx.TaskExecutionHistories.Update(entity);
        _ctx.SaveChanges();
    }

    public void UpdateRange(IEnumerable<TaskExecutionHistory> entities)
    {
        _ctx.TaskExecutionHistories.UpdateRange(entities);
        _ctx.SaveChanges();
    }

    public void Delete(TaskExecutionHistory entity)
    {
        _ctx.TaskExecutionHistories.Remove(entity);
        _ctx.SaveChanges();
    }

    public void DeleteRange(IEnumerable<TaskExecutionHistory> entities)
    {
        _ctx.TaskExecutionHistories.RemoveRange(entities);
        _ctx.SaveChanges();
    }

    public int Count(Expression<Func<TaskExecutionHistory, bool>>? predicate = null)
        => predicate == null ? _ctx.TaskExecutionHistories.Count() : _ctx.TaskExecutionHistories.Count(predicate);

    public bool Any(Expression<Func<TaskExecutionHistory, bool>>? predicate = null)
        => predicate == null ? _ctx.TaskExecutionHistories.Any() : _ctx.TaskExecutionHistories.Any(predicate);

    public IEnumerable<TaskExecutionHistory> Page(int pageIndex, int pageSize, out int totalCount, Expression<Func<TaskExecutionHistory, bool>>? predicate = null, Expression<Func<TaskExecutionHistory, object>>? orderBy = null, bool orderByDescending = false)
    {
        var query = _ctx.TaskExecutionHistories.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        totalCount = query.Count();
        if (orderBy != null)
            query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
    }

    // Async generic operations
    public async Task<TaskExecutionHistory> AddAsync(TaskExecutionHistory entity, CancellationToken cancellationToken = default)
    {
        await _ctx.TaskExecutionHistories.AddAsync(entity, cancellationToken);
        await _ctx.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<TaskExecutionHistory> entities, CancellationToken cancellationToken = default)
    {
        await _ctx.TaskExecutionHistories.AddRangeAsync(entities, cancellationToken);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskExecutionHistory?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        if (id is Guid gid)
            return await _ctx.TaskExecutionHistories.FindAsync([gid], cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<TaskExecutionHistory>> ListAllAsync(CancellationToken cancellationToken = default)
        => await _ctx.TaskExecutionHistories.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskExecutionHistory>> ListAsync(Expression<Func<TaskExecutionHistory, bool>> predicate, CancellationToken cancellationToken = default)
        => await _ctx.TaskExecutionHistories.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task UpdateAsync(TaskExecutionHistory entity, CancellationToken cancellationToken = default)
    {
        _ctx.TaskExecutionHistories.Update(entity);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<TaskExecutionHistory> entities, CancellationToken cancellationToken = default)
    {
        _ctx.TaskExecutionHistories.UpdateRange(entities);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskExecutionHistory entity, CancellationToken cancellationToken = default)
    {
        _ctx.TaskExecutionHistories.Remove(entity);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<TaskExecutionHistory> entities, CancellationToken cancellationToken = default)
    {
        _ctx.TaskExecutionHistories.RemoveRange(entities);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<TaskExecutionHistory, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
            ? await _ctx.TaskExecutionHistories.CountAsync(cancellationToken)
            : await _ctx.TaskExecutionHistories.CountAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<TaskExecutionHistory, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
            ? await _ctx.TaskExecutionHistories.AnyAsync(cancellationToken)
            : await _ctx.TaskExecutionHistories.AnyAsync(predicate, cancellationToken);

    public async Task<(IReadOnlyList<TaskExecutionHistory> Items, int TotalCount)> PageAsync(int pageIndex, int pageSize, Expression<Func<TaskExecutionHistory, bool>>? predicate = null, Expression<Func<TaskExecutionHistory, object>>? orderBy = null, bool orderByDescending = false, CancellationToken cancellationToken = default)
    {
        var query = _ctx.TaskExecutionHistories.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        var total = await query.CountAsync(cancellationToken);
        if (orderBy != null)
            query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    // Domain-specific
    public TaskExecutionHistory? GetByRequestId(string requestId)
        => _ctx.TaskExecutionHistories.AsNoTracking().FirstOrDefault(h => h.RequestId == requestId);

    public Task<TaskExecutionHistory?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
        => _ctx.TaskExecutionHistories.AsNoTracking().FirstOrDefaultAsync(h => h.RequestId == requestId, cancellationToken);

    public async Task<IReadOnlyList<ExecutionStepRecord>> ListStepsAsync(Guid executionId, CancellationToken cancellationToken = default)
        => await _ctx.ExecutionStepRecords.AsNoTracking().Where(s => s.ExecutionId == executionId)
            .OrderBy(s => s.StepOrder).ToListAsync(cancellationToken);
}
