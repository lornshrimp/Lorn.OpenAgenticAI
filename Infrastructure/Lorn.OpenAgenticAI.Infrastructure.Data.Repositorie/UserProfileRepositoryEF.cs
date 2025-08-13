using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// EF Core 实现的用户档案仓储
/// </summary>
public class UserProfileRepositoryEF : IUserProfileRepository
{
    private readonly OpenAgenticAIDbContext _ctx;
    private readonly ILogger<UserProfileRepositoryEF>? _logger;

    public UserProfileRepositoryEF(OpenAgenticAIDbContext ctx, ILogger<UserProfileRepositoryEF>? logger = null)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _logger = logger;
    }

    // Sync
    public UserProfile Add(UserProfile entity)
    {
        _ctx.UserProfiles.Add(entity);
        _ctx.SaveChanges();
        return entity;
    }

    public void AddRange(IEnumerable<UserProfile> entities)
    {
        _ctx.UserProfiles.AddRange(entities);
        _ctx.SaveChanges();
    }

    public UserProfile? GetById(object id)
    {
        if (id is Guid gid)
            return _ctx.UserProfiles.Find(gid);
        return null;
    }

    public IEnumerable<UserProfile> ListAll() => _ctx.UserProfiles.AsNoTracking().ToList();

    public IEnumerable<UserProfile> List(Expression<Func<UserProfile, bool>> predicate)
        => _ctx.UserProfiles.AsNoTracking().Where(predicate).ToList();

    public void Update(UserProfile entity)
    {
        _ctx.UserProfiles.Update(entity);
        _ctx.SaveChanges();
    }

    public void UpdateRange(IEnumerable<UserProfile> entities)
    {
        _ctx.UserProfiles.UpdateRange(entities);
        _ctx.SaveChanges();
    }

    public void Delete(UserProfile entity)
    {
        _ctx.UserProfiles.Remove(entity);
        _ctx.SaveChanges();
    }

    public void DeleteRange(IEnumerable<UserProfile> entities)
    {
        _ctx.UserProfiles.RemoveRange(entities);
        _ctx.SaveChanges();
    }

    public int Count(Expression<Func<UserProfile, bool>>? predicate = null)
        => predicate == null ? _ctx.UserProfiles.Count() : _ctx.UserProfiles.Count(predicate);

    public bool Any(Expression<Func<UserProfile, bool>>? predicate = null)
        => predicate == null ? _ctx.UserProfiles.Any() : _ctx.UserProfiles.Any(predicate);

    public IEnumerable<UserProfile> Page(int pageIndex, int pageSize, out int totalCount,
        Expression<Func<UserProfile, bool>>? predicate = null,
        Expression<Func<UserProfile, object>>? orderBy = null,
        bool orderByDescending = false)
    {
        var query = _ctx.UserProfiles.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        totalCount = query.Count();
        if (orderBy != null)
            query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
    }

    // Async
    public async Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        await _ctx.UserProfiles.AddAsync(entity, cancellationToken);
        await _ctx.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        await _ctx.UserProfiles.AddRangeAsync(entities, cancellationToken);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfile?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        if (id is Guid gid)
            return await _ctx.UserProfiles.FindAsync([gid], cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<UserProfile>> ListAllAsync(CancellationToken cancellationToken = default)
        => await _ctx.UserProfiles.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserProfile>> ListAsync(Expression<Func<UserProfile, bool>> predicate, CancellationToken cancellationToken = default)
        => await _ctx.UserProfiles.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        _ctx.UserProfiles.Update(entity);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        _ctx.UserProfiles.UpdateRange(entities);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        _ctx.UserProfiles.Remove(entity);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        _ctx.UserProfiles.RemoveRange(entities);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<UserProfile, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
            ? await _ctx.UserProfiles.CountAsync(cancellationToken)
            : await _ctx.UserProfiles.CountAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<UserProfile, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
            ? await _ctx.UserProfiles.AnyAsync(cancellationToken)
            : await _ctx.UserProfiles.AnyAsync(predicate, cancellationToken);

    public async Task<(IReadOnlyList<UserProfile> Items, int TotalCount)> PageAsync(int pageIndex, int pageSize, Expression<Func<UserProfile, bool>>? predicate = null, Expression<Func<UserProfile, object>>? orderBy = null, bool orderByDescending = false, CancellationToken cancellationToken = default)
    {
        var query = _ctx.UserProfiles.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        var total = await query.CountAsync(cancellationToken);
        if (orderBy != null)
            query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    // Domain-specific
    public UserProfile? GetByUserName(string userName)
        => _ctx.UserProfiles.AsNoTracking().FirstOrDefault(u => u.Username == userName);

    public Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        => _ctx.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Username == userName, cancellationToken);
}
