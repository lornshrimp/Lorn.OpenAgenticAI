using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户仓储实现类，提供用户数据访问功能
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(OpenAgenticAIDbContext context, ILogger<UserRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 根据用户ID获取用户档案
    /// </summary>
    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user profile by ID: {UserId}", userId);

            var user = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogDebug("User not found with ID: {UserId}", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 根据用户名获取用户档案
    /// </summary>
    public async Task<UserProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be null or empty", nameof(username));
            }

            _logger.LogDebug("Getting user profile by username: {Username}", username);

            var user = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (user == null)
            {
                _logger.LogDebug("User not found with username: {Username}", username);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by username: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// 根据邮箱获取用户档案
    /// </summary>
    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            _logger.LogDebug("Getting user profile by email: {Email}", email);

            var user = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                _logger.LogDebug("User not found with email: {Email}", email);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile by email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// 获取所有活跃用户
    /// </summary>
    public async Task<IEnumerable<UserProfile>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all active users");

            var users = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .Where(u => u.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} active users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            throw;
        }
    }

    /// <summary>
    /// 获取所有用户（包括非活跃用户）
    /// </summary>
    public async Task<IEnumerable<UserProfile>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all users");

            var users = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    public async Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            _logger.LogDebug("Checking if username exists: {Username}, excluding user: {ExcludeUserId}", username, excludeUserId);

            var query = _context.UserProfiles.Where(u => u.Username == username);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);

            _logger.LogDebug("Username {Username} exists: {Exists}", username, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if username exists: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// 检查邮箱是否已存在
    /// </summary>
    public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            _logger.LogDebug("Checking if email exists: {Email}, excluding user: {ExcludeUserId}", email, excludeUserId);

            var query = _context.UserProfiles.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);

            _logger.LogDebug("Email {Email} exists: {Exists}", email, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// 添加新用户
    /// </summary>
    public async Task<UserProfile> AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userProfile == null)
            {
                throw new ArgumentNullException(nameof(userProfile));
            }

            _logger.LogDebug("Adding new user: {Username} ({UserId})", userProfile.Username, userProfile.UserId);

            // 检查用户名和邮箱是否已存在
            if (await IsUsernameExistsAsync(userProfile.Username, null, cancellationToken))
            {
                throw new InvalidOperationException($"Username '{userProfile.Username}' already exists");
            }

            if (!string.IsNullOrWhiteSpace(userProfile.Email) &&
                await IsEmailExistsAsync(userProfile.Email, null, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{userProfile.Email}' already exists");
            }

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added user: {Username} ({UserId})", userProfile.Username, userProfile.UserId);
            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user: {Username} ({UserId})", userProfile?.Username, userProfile?.UserId);
            throw;
        }
    }

    /// <summary>
    /// 更新用户档案
    /// </summary>
    public async Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userProfile == null)
            {
                throw new ArgumentNullException(nameof(userProfile));
            }

            _logger.LogDebug("Updating user: {Username} ({UserId})", userProfile.Username, userProfile.UserId);

            // 检查用户名和邮箱是否与其他用户冲突
            if (await IsUsernameExistsAsync(userProfile.Username, userProfile.UserId, cancellationToken))
            {
                throw new InvalidOperationException($"Username '{userProfile.Username}' already exists");
            }

            if (!string.IsNullOrWhiteSpace(userProfile.Email) &&
                await IsEmailExistsAsync(userProfile.Email, userProfile.UserId, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{userProfile.Email}' already exists");
            }

            _context.UserProfiles.Update(userProfile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated user: {Username} ({UserId})", userProfile.Username, userProfile.UserId);
            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {Username} ({UserId})", userProfile?.Username, userProfile?.UserId);
            throw;
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting user: {UserId}", userId);

            var user = await _context.UserProfiles
                .Include(u => u.UserPreferences)
                .Include(u => u.MetadataEntries)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for deletion: {UserId}", userId);
                return false;
            }

            // 删除关联的偏好设置和元数据
            _context.UserPreferences.RemoveRange(user.UserPreferences);
            _context.UserMetadataEntries.RemoveRange(user.MetadataEntries);
            _context.UserProfiles.Remove(user);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 软删除用户（设置为非活跃状态）
    /// </summary>
    public async Task<bool> SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Soft deleting user: {UserId}", userId);

            var user = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for soft deletion: {UserId}", userId);
                return false;
            }

            user.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully soft deleted user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户数量
    /// </summary>
    public async Task<int> GetUserCountAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user count, active only: {ActiveOnly}", activeOnly);

            var query = _context.UserProfiles.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(u => u.IsActive);
            }

            var count = await query.CountAsync(cancellationToken);

            _logger.LogDebug("User count: {Count} (active only: {ActiveOnly})", count, activeOnly);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user count");
            throw;
        }
    }

    /// <summary>
    /// 分页获取用户列表
    /// </summary>
    public async Task<(IEnumerable<UserProfile> Users, int TotalCount)> GetUsersPagedAsync(
        int pageIndex,
        int pageSize,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("Page index cannot be negative", nameof(pageIndex));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("Page size must be positive", nameof(pageSize));
            }

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
                .OrderBy(u => u.Username)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} users on page {PageIndex} of {TotalCount} total",
                users.Count, pageIndex, totalCount);

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged users");
            throw;
        }
    }
}
