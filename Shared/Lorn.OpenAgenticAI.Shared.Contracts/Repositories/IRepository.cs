using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lorn.OpenAgenticAI.Shared.Contracts.Repositories;

/// <summary>
/// 通用仓储接口（同步版）
/// 遵循接口契约：不关心具体数据源，只定义行为。
/// </summary>
public interface IRepository<T> where T : class
{
    // Create
    T Add(T entity);
    void AddRange(IEnumerable<T> entities);

    // Read
    T? GetById(object id);
    IEnumerable<T> ListAll();
    IEnumerable<T> List(Expression<Func<T, bool>> predicate);

    // Update
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);

    // Delete
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);

    // Aggregates
    int Count(Expression<Func<T, bool>>? predicate = null);
    bool Any(Expression<Func<T, bool>>? predicate = null);

    // Paging
    IEnumerable<T> Page(
        int pageIndex,
        int pageSize,
        out int totalCount,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool orderByDescending = false);
}
