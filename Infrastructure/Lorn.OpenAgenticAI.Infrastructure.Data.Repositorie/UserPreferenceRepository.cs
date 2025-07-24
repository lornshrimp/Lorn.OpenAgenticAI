using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户偏好设置仓储实现类，提供用户偏好数据访问功能
/// </summary>
public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<UserPreferenceRepository> _logger;

    public UserPreferenceRepository(OpenAgenticAIDbContext context, ILogger<UserPreferenceRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 根据偏好ID获取用户偏好设置
    /// </summary>
    public async Task<UserPreferences?> GetByIdAsync(Guid preferenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user preference by ID: {PreferenceId}", preferenceId);

            var preference = await _context.UserPreferences
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PreferenceId == preferenceId, cancellationToken);

            if (preference == null)
            {
                _logger.LogDebug("User preference not found with ID: {PreferenceId}", preferenceId);
            }

            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user preference by ID: {PreferenceId}", preferenceId);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID获取所有偏好设置
    /// </summary>
    public async Task<IEnumerable<UserPreferences>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all preferences for user: {UserId}", userId);

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} preferences for user: {UserId}", preferences.Count, userId);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID和分类获取偏好设置
    /// </summary>
    public async Task<IEnumerable<UserPreferences>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _logger.LogDebug("Getting preferences for user: {UserId}, category: {Category}", userId, category);

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId && p.PreferenceCategory == category)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} preferences for user: {UserId}, category: {Category}",
                preferences.Count, userId, category);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user: {UserId}, category: {Category}", userId, category);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID、分类和键获取特定偏好设置
    /// </summary>
    public async Task<UserPreferences?> GetByKeyAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Getting preference for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);

            var preference = await _context.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId &&
                                        p.PreferenceCategory == category &&
                                        p.PreferenceKey == key, cancellationToken);

            if (preference == null)
            {
                _logger.LogDebug("Preference not found for user: {UserId}, category: {Category}, key: {Key}",
                    userId, category, key);
            }

            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preference for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);
            throw;
        }
    }

    /// <summary>
    /// 获取系统默认偏好设置
    /// </summary>
    public async Task<IEnumerable<UserPreferences>> GetSystemDefaultsAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting system default preferences, category: {Category}", category);

            var query = _context.UserPreferences.Where(p => p.IsSystemDefault);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.PreferenceCategory == category);
            }

            var preferences = await query
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} system default preferences", preferences.Count);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system default preferences");
            throw;
        }
    }

    /// <summary>
    /// 检查偏好设置是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Checking if preference exists for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);

            var exists = await _context.UserPreferences
                .AnyAsync(p => p.UserId == userId &&
                              p.PreferenceCategory == category &&
                              p.PreferenceKey == key, cancellationToken);

            _logger.LogDebug("Preference exists: {Exists} for user: {UserId}, category: {Category}, key: {Key}",
                exists, userId, category, key);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if preference exists for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);
            throw;
        }
    }

    /// <summary>
    /// 添加新的偏好设置
    /// </summary>
    public async Task<UserPreferences> AddAsync(UserPreferences preference, CancellationToken cancellationToken = default)
    {
        try
        {
            if (preference == null)
            {
                throw new ArgumentNullException(nameof(preference));
            }

            _logger.LogDebug("Adding preference for user: {UserId}, category: {Category}, key: {Key}",
                preference.UserId, preference.PreferenceCategory, preference.PreferenceKey);

            // 检查是否已存在相同的偏好设置
            if (await ExistsAsync(preference.UserId, preference.PreferenceCategory, preference.PreferenceKey, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Preference already exists for user {preference.UserId}, category '{preference.PreferenceCategory}', key '{preference.PreferenceKey}'");
            }

            _context.UserPreferences.Add(preference);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added preference for user: {UserId}, category: {Category}, key: {Key}",
                preference.UserId, preference.PreferenceCategory, preference.PreferenceKey);
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding preference for user: {UserId}, category: {Category}, key: {Key}",
                preference?.UserId, preference?.PreferenceCategory, preference?.PreferenceKey);
            throw;
        }
    }

    /// <summary>
    /// 批量添加偏好设置
    /// </summary>
    public async Task<IEnumerable<UserPreferences>> AddRangeAsync(IEnumerable<UserPreferences> preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            var preferencesList = preferences.ToList();
            if (!preferencesList.Any())
            {
                return preferencesList;
            }

            _logger.LogDebug("Adding {Count} preferences in batch", preferencesList.Count);

            // 检查重复项
            foreach (var preference in preferencesList)
            {
                if (await ExistsAsync(preference.UserId, preference.PreferenceCategory, preference.PreferenceKey, cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Preference already exists for user {preference.UserId}, category '{preference.PreferenceCategory}', key '{preference.PreferenceKey}'");
                }
            }

            _context.UserPreferences.AddRange(preferencesList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added {Count} preferences in batch", preferencesList.Count);
            return preferencesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding preferences in batch");
            throw;
        }
    }

    /// <summary>
    /// 更新偏好设置
    /// </summary>
    public async Task<UserPreferences> UpdateAsync(UserPreferences preference, CancellationToken cancellationToken = default)
    {
        try
        {
            if (preference == null)
            {
                throw new ArgumentNullException(nameof(preference));
            }

            _logger.LogDebug("Updating preference: {PreferenceId} for user: {UserId}",
                preference.PreferenceId, preference.UserId);

            _context.UserPreferences.Update(preference);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated preference: {PreferenceId} for user: {UserId}",
                preference.PreferenceId, preference.UserId);
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preference: {PreferenceId} for user: {UserId}",
                preference?.PreferenceId, preference?.UserId);
            throw;
        }
    }

    /// <summary>
    /// 设置或更新偏好设置（如果不存在则创建，存在则更新）
    /// </summary>
    public async Task<UserPreferences> SetPreferenceAsync(
        Guid userId,
        string category,
        string key,
        string value,
        string valueType = "String",
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Setting preference for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);

            var existingPreference = await GetByKeyAsync(userId, category, key, cancellationToken);

            if (existingPreference != null)
            {
                // 更新现有偏好设置
                existingPreference.UpdateValue(value, valueType);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    existingPreference.UpdateDescription(description);
                }
                return await UpdateAsync(existingPreference, cancellationToken);
            }
            else
            {
                // 创建新的偏好设置
                var newPreference = new UserPreferences(userId, category, key, value, valueType, false, description);
                return await AddAsync(newPreference, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting preference for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);
            throw;
        }
    }

    /// <summary>
    /// 删除偏好设置
    /// </summary>
    public async Task<bool> DeleteAsync(Guid preferenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting preference: {PreferenceId}", preferenceId);

            var preference = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.PreferenceId == preferenceId, cancellationToken);

            if (preference == null)
            {
                _logger.LogWarning("Preference not found for deletion: {PreferenceId}", preferenceId);
                return false;
            }

            _context.UserPreferences.Remove(preference);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted preference: {PreferenceId}", preferenceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preference: {PreferenceId}", preferenceId);
            throw;
        }
    }

    /// <summary>
    /// 删除用户的所有偏好设置
    /// </summary>
    public async Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting all preferences for user: {UserId}", userId);

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync(cancellationToken);

            if (!preferences.Any())
            {
                _logger.LogDebug("No preferences found for user: {UserId}", userId);
                return 0;
            }

            _context.UserPreferences.RemoveRange(preferences);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} preferences for user: {UserId}",
                preferences.Count, userId);
            return preferences.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preferences for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 删除用户指定分类的所有偏好设置
    /// </summary>
    public async Task<int> DeleteByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _logger.LogDebug("Deleting preferences for user: {UserId}, category: {Category}", userId, category);

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId && p.PreferenceCategory == category)
                .ToListAsync(cancellationToken);

            if (!preferences.Any())
            {
                _logger.LogDebug("No preferences found for user: {UserId}, category: {Category}", userId, category);
                return 0;
            }

            _context.UserPreferences.RemoveRange(preferences);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} preferences for user: {UserId}, category: {Category}",
                preferences.Count, userId, category);
            return preferences.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preferences for user: {UserId}, category: {Category}", userId, category);
            throw;
        }
    }

    /// <summary>
    /// 重置用户偏好设置为系统默认值
    /// </summary>
    public async Task<int> ResetToDefaultsAsync(Guid userId, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resetting preferences to defaults for user: {UserId}, category: {Category}",
                userId, category);

            // 获取系统默认偏好设置
            var systemDefaults = await GetSystemDefaultsAsync(category, cancellationToken);
            var systemDefaultsList = systemDefaults.ToList();

            if (!systemDefaultsList.Any())
            {
                _logger.LogWarning("No system default preferences found for category: {Category}", category);
                return 0;
            }

            // 删除现有的用户偏好设置
            var deletedCount = string.IsNullOrWhiteSpace(category)
                ? await DeleteByUserIdAsync(userId, cancellationToken)
                : await DeleteByCategoryAsync(userId, category, cancellationToken);

            // 创建基于系统默认值的新偏好设置
            var newPreferences = systemDefaultsList.Select(defaultPref =>
                new UserPreferences(
                    userId,
                    defaultPref.PreferenceCategory,
                    defaultPref.PreferenceKey,
                    defaultPref.PreferenceValue,
                    defaultPref.ValueType,
                    false,
                    defaultPref.Description
                )).ToList();

            if (newPreferences.Any())
            {
                _context.UserPreferences.AddRange(newPreferences);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully reset {Count} preferences to defaults for user: {UserId}, category: {Category}",
                newPreferences.Count, userId, category);
            return newPreferences.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting preferences to defaults for user: {UserId}, category: {Category}",
                userId, category);
            throw;
        }
    }

    /// <summary>
    /// 获取偏好设置的统计信息
    /// </summary>
    public async Task<(int CategoryCount, int TotalPreferences, DateTime? LastUpdated)> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting preference statistics for user: {UserId}", userId);

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var categoryCount = preferences.Select(p => p.PreferenceCategory).Distinct().Count();
            var totalPreferences = preferences.Count;
            var lastUpdated = preferences.Any() ? preferences.Max(p => p.LastUpdatedTime) : (DateTime?)null;

            _logger.LogDebug("Preference statistics for user {UserId}: {CategoryCount} categories, {TotalPreferences} total preferences, last updated: {LastUpdated}",
                userId, categoryCount, totalPreferences, lastUpdated);

            return (categoryCount, totalPreferences, lastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preference statistics for user: {UserId}", userId);
            throw;
        }
    }
}
