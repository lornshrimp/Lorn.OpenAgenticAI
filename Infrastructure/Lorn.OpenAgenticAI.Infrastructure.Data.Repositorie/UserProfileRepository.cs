using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户档案仓储实现类，提供用户数据访问功能
/// 整合了所有UserProfile相关的数据访问操作，包括基础CRUD、业务查询、验证方法等
/// </summary>
public class UserProfileRepository : IUserProfileRepository
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<UserProfileRepository> _logger;

    public UserProfileRepository(OpenAgenticAIDbContext context, ILogger<UserProfileRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IRepository<UserProfile> Implementation

    public UserProfile Add(UserProfile entity)
    {
        var entry = _context.UserProfiles.Add(entity);
        return entry.Entity;
    }

    public void AddRange(IEnumerable<UserProfile> entities)
    {
        _context.UserProfiles.AddRange(entities);
    }

    public UserProfile? GetById(object id)
    {
        if (id is Guid guidId)
        {
            return _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .FirstOrDefault(u => u.UserId == guidId);
        }
        return null;
    }

    public IEnumerable<UserProfile> ListAll()
    {
        return _context.UserProfiles
            .Include(u => u.UserPreferences)
            .Include(u => u.MetadataEntries)
            .AsNoTracking()
            .ToList();
    }

    public IEnumerable<UserProfile> List(Expression<Func<UserProfile, bool>> predicate)
    {
        return _context.UserProfiles
            .Include(u => u.UserPreferences)
            .Include(u => u.MetadataEntries)
            .Where(predicate)
            .AsNoTracking()
            .ToList();
    }

    public void Update(UserProfile entity)
    {
        _context.UserProfiles.Update(entity);
    }

    public void UpdateRange(IEnumerable<UserProfile> entities)
    {
        _context.UserProfiles.UpdateRange(entities);
    }

    public void Delete(UserProfile entity)
    {
        _context.UserProfiles.Remove(entity);
    }

    public void DeleteRange(IEnumerable<UserProfile> entities)
    {
        _context.UserProfiles.RemoveRange(entities);
    }

    public int Count(Expression<Func<UserProfile, bool>>? predicate = null)
    {
        return predicate == null
            ? _context.UserProfiles.Count()
            : _context.UserProfiles.Count(predicate);
    }

    public bool Any(Expression<Func<UserProfile, bool>>? predicate = null)
    {
        return predicate == null
            ? _context.UserProfiles.Any()
            : _context.UserProfiles.Any(predicate);
    }

    public IEnumerable<UserProfile> Page(int pageIndex, int pageSize, out int totalCount,
        Expression<Func<UserProfile, bool>>? predicate = null,
        Expression<Func<UserProfile, object>>? orderBy = null,
        bool orderByDescending = false)
    {
        var query = _context.UserProfiles
            .Include(u => u.UserPreferences)
            .Include(u => u.MetadataEntries)
            .AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        totalCount = query.Count();

        if (orderBy != null)
        {
            query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        return query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToList();
    }

    #endregion

    #region IAsyncRepository<UserProfile> Implementation

    public async Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding new user profile: {UserId}", entity.UserId);

            var entry = await _context.UserProfiles.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully added user profile: {UserId}", entity.UserId);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user profile: {UserId}", entity.UserId);
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding multiple user profiles");

            await _context.UserProfiles.AddRangeAsync(entities, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully added multiple user profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple user profiles");
            throw;
        }
    }

    public async Task<UserProfile?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id is not Guid guidId)
            {
                _logger.LogWarning("Invalid ID type for GetByIdAsync: {IdType}", id?.GetType().Name ?? "null");
                return null;
            }

            _logger.LogDebug("Getting user profile by ID: {UserId}", guidId);

            var user = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == guidId, cancellationToken);

            if (user == null)
            {
                _logger.LogDebug("User not found with ID: {UserId}", guidId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by ID: {UserId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserProfile>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all user profiles");

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all user profiles");
            throw;
        }
    }

    public async Task<IReadOnlyList<UserProfile>> ListAsync(Expression<Func<UserProfile, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user profiles with predicate");

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .Where(predicate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profiles with predicate");
            throw;
        }
    }

    public async Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating user profile: {UserId}", entity.UserId);

            _context.UserProfiles.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully updated user profile: {UserId}", entity.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile: {UserId}", entity.UserId);
            throw;
        }
    }

    public async Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting user profile: {UserId}", entity.UserId);

            _context.UserProfiles.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully deleted user profile: {UserId}", entity.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user profile: {UserId}", entity.UserId);
            throw;
        }
    }

    public async Task<int> CountAsync(Expression<Func<UserProfile, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return predicate == null
                ? await _context.UserProfiles.CountAsync(cancellationToken)
                : await _context.UserProfiles.CountAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting user profiles");
            throw;
        }
    }

    public async Task<bool> AnyAsync(Expression<Func<UserProfile, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return predicate == null
                ? await _context.UserProfiles.AnyAsync(cancellationToken)
                : await _context.UserProfiles.AnyAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if any user profiles exist");
            throw;
        }
    }

    #endregion

    #region IUserProfileRepository Specific Methods

    public UserProfile? GetByUserName(string userName)
    {
        try
        {
            _logger.LogDebug("Getting user profile by username: {UserName}", userName);

            return _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .FirstOrDefault(u => u.Username == userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by username: {UserName}", userName);
            throw;
        }
    }

    public async Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user profile by username: {UserName}", userName);

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == userName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by username: {UserName}", userName);
            throw;
        }
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user profile by email: {Email}", email);

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by email: {Email}", email);
            throw;
        }
    }

    public async Task<UserProfile?> GetByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user profile by machine ID: {MachineId}", machineId);

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MachineId == machineId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by machine ID: {MachineId}", machineId);
            throw;
        }
    }

    public async Task<IEnumerable<UserProfile>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting active users");

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .Where(u => u.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfile>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all users");

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfile>> GetDefaultUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting default users");

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .Where(u => u.IsDefault)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfile>> GetRecentlyActiveUsersAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting recently active users since: {Since}", since);

            return await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .Where(u => u.IsActive && u.LastLoginTime >= since)
                .OrderByDescending(u => u.LastLoginTime)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently active users");
            throw;
        }
    }

    public async Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if username exists: {Username}", username);

            var query = _context.UserProfiles.Where(u => u.Username == username);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if username exists: {Username}", username);
            throw;
        }
    }

    public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if email exists: {Email}", email);

            var query = _context.UserProfiles.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists: {Email}", email);
            throw;
        }
    }

    public async Task<bool> IsMachineIdExistsAsync(string machineId, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if machine ID exists: {MachineId}", machineId);

            var query = _context.UserProfiles.Where(u => u.MachineId == machineId);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if machine ID exists: {MachineId}", machineId);
            throw;
        }
    }

    public async Task<int> GetUserCountAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user count, active only: {ActiveOnly}", activeOnly);

            return activeOnly
                ? await _context.UserProfiles.CountAsync(u => u.IsActive, cancellationToken)
                : await _context.UserProfiles.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user count");
            throw;
        }
    }

    public async Task<(IEnumerable<UserProfile> Users, int TotalCount)> GetUsersPagedAsync(
        int pageIndex,
        int pageSize,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting paged users: page {PageIndex}, size {PageSize}, active only: {ActiveOnly}",
                pageIndex, pageSize, activeOnly);

            var query = _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(u => u.IsActive);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderBy(u => u.DisplayName)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged users");
            throw;
        }
    }

    public async Task<bool> SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Soft deleting user: {UserId}", userId);

            var user = await _context.UserProfiles.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for soft delete: {UserId}", userId);
                return false;
            }

            user.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully soft deleted user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting user: {UserId}", userId);
            throw;
        }
    }

    public async Task<int> BulkUpdateSecuritySettingsAsync(List<Guid> userIds, SecuritySettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk updating security settings for {Count} users", userIds.Count);

            var users = await _context.UserProfiles
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.UpdateSecuritySettings(settings);
            }

            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully bulk updated security settings for {Count} users", users.Count);
            return users.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating security settings");
            throw;
        }
    }

    /// <summary>
    /// 批量更新用户档案（异步）
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk updating user profiles");

            _context.UserProfiles.UpdateRange(entities);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully bulk updated user profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating user profiles");
            throw;
        }
    }

    /// <summary>
    /// 批量删除用户档案（异步）
    /// </summary>
    public async Task DeleteRangeAsync(IEnumerable<UserProfile> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk deleting user profiles");

            _context.UserProfiles.RemoveRange(entities);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully bulk deleted user profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting user profiles");
            throw;
        }
    }

    /// <summary>
    /// 分页查询用户档案（异步）
    /// </summary>
    public async Task<(IReadOnlyList<UserProfile> Items, int TotalCount)> PageAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<UserProfile, bool>>? predicate = null,
        Expression<Func<UserProfile, object>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting paged user profiles - Page: {PageIndex}, Size: {PageSize}", pageIndex, pageSize);

            var query = _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
            {
                query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }

            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Successfully retrieved {Count} user profiles out of {Total}", items.Count, totalCount);
            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged user profiles");
            throw;
        }
    }

    #endregion
}
