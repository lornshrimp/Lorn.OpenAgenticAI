using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户元数据仓储实现类，提供用户元数据访问功能
/// </summary>
public class UserMetadataRepository : IUserMetadataRepository
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<UserMetadataRepository> _logger;

    public UserMetadataRepository(OpenAgenticAIDbContext context, ILogger<UserMetadataRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 根据元数据ID获取用户元数据条目
    /// </summary>
    public async Task<UserMetadataEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user metadata entry by ID: {Id}", id);

            var metadataEntry = await _context.UserMetadataEntries
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogDebug("User metadata entry not found with ID: {Id}", id);
            }

            return metadataEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user metadata entry by ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID获取所有元数据条目
    /// </summary>
    public async Task<IEnumerable<UserMetadataEntry>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all metadata entries for user: {UserId}", userId);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => m.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} metadata entries for user: {UserId}", metadataEntries.Count, userId);
            return metadataEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata entries for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID和分类获取元数据条目
    /// </summary>
    public async Task<IEnumerable<UserMetadataEntry>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _logger.LogDebug("Getting metadata entries for user: {UserId}, category: {Category}", userId, category);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => m.UserId == userId && m.Category == category)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} metadata entries for user: {UserId}, category: {Category}",
                metadataEntries.Count, userId, category);
            return metadataEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata entries for user: {UserId}, category: {Category}", userId, category);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID和键获取特定元数据条目
    /// </summary>
    public async Task<UserMetadataEntry?> GetByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Getting metadata entry for user: {UserId}, key: {Key}", userId, key);

            var metadataEntry = await _context.UserMetadataEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Key == key, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogDebug("Metadata entry not found for user: {UserId}, key: {Key}", userId, key);
            }

            return metadataEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata entry for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID、分类和键获取特定元数据条目
    /// </summary>
    public async Task<UserMetadataEntry?> GetByCategoryAndKeyAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default)
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

            _logger.LogDebug("Getting metadata entry for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);

            var metadataEntry = await _context.UserMetadataEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Category == category && m.Key == key, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogDebug("Metadata entry not found for user: {UserId}, category: {Category}, key: {Key}",
                    userId, category, key);
            }

            return metadataEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata entry for user: {UserId}, category: {Category}, key: {Key}",
                userId, category, key);
            throw;
        }
    }

    /// <summary>
    /// 检查元数据条目是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Guid userId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Checking if metadata entry exists for user: {UserId}, key: {Key}", userId, key);

            var exists = await _context.UserMetadataEntries
                .AnyAsync(m => m.UserId == userId && m.Key == key, cancellationToken);

            _logger.LogDebug("Metadata entry exists: {Exists} for user: {UserId}, key: {Key}", exists, userId, key);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if metadata entry exists for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 添加新的元数据条目
    /// </summary>
    public async Task<UserMetadataEntry> AddAsync(UserMetadataEntry metadataEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (metadataEntry == null)
            {
                throw new ArgumentNullException(nameof(metadataEntry));
            }

            _logger.LogDebug("Adding metadata entry for user: {UserId}, key: {Key}",
                metadataEntry.UserId, metadataEntry.Key);

            // 检查是否已存在相同的元数据条目
            if (await ExistsAsync(metadataEntry.UserId, metadataEntry.Key, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Metadata entry already exists for user {metadataEntry.UserId}, key '{metadataEntry.Key}'");
            }

            _context.UserMetadataEntries.Add(metadataEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added metadata entry for user: {UserId}, key: {Key}",
                metadataEntry.UserId, metadataEntry.Key);
            return metadataEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding metadata entry for user: {UserId}, key: {Key}",
                metadataEntry?.UserId, metadataEntry?.Key);
            throw;
        }
    }

    /// <summary>
    /// 批量添加元数据条目
    /// </summary>
    public async Task<IEnumerable<UserMetadataEntry>> AddRangeAsync(IEnumerable<UserMetadataEntry> metadataEntries, CancellationToken cancellationToken = default)
    {
        try
        {
            if (metadataEntries == null)
            {
                throw new ArgumentNullException(nameof(metadataEntries));
            }

            var metadataEntriesList = metadataEntries.ToList();
            if (!metadataEntriesList.Any())
            {
                return metadataEntriesList;
            }

            _logger.LogDebug("Adding {Count} metadata entries in batch", metadataEntriesList.Count);

            // 检查重复项
            foreach (var metadataEntry in metadataEntriesList)
            {
                if (await ExistsAsync(metadataEntry.UserId, metadataEntry.Key, cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Metadata entry already exists for user {metadataEntry.UserId}, key '{metadataEntry.Key}'");
                }
            }

            _context.UserMetadataEntries.AddRange(metadataEntriesList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added {Count} metadata entries in batch", metadataEntriesList.Count);
            return metadataEntriesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding metadata entries in batch");
            throw;
        }
    }

    /// <summary>
    /// 更新元数据条目
    /// </summary>
    public async Task<UserMetadataEntry> UpdateAsync(UserMetadataEntry metadataEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (metadataEntry == null)
            {
                throw new ArgumentNullException(nameof(metadataEntry));
            }

            _logger.LogDebug("Updating metadata entry: {Id} for user: {UserId}",
                metadataEntry.Id, metadataEntry.UserId);

            _context.UserMetadataEntries.Update(metadataEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated metadata entry: {Id} for user: {UserId}",
                metadataEntry.Id, metadataEntry.UserId);
            return metadataEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata entry: {Id} for user: {UserId}",
                metadataEntry?.Id, metadataEntry?.UserId);
            throw;
        }
    }

    /// <summary>
    /// 设置或更新元数据条目（如果不存在则创建，存在则更新）
    /// </summary>
    public async Task<UserMetadataEntry> SetMetadataAsync(
        Guid userId,
        string key,
        object value,
        string category = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Setting metadata for user: {UserId}, key: {Key}", userId, key);

            var existingMetadata = await GetByKeyAsync(userId, key, cancellationToken);

            if (existingMetadata != null)
            {
                // 更新现有元数据条目
                existingMetadata.SetValue(value);
                return await UpdateAsync(existingMetadata, cancellationToken);
            }
            else
            {
                // 创建新的元数据条目
                var newMetadata = new UserMetadataEntry(userId, key, value, category);
                return await AddAsync(newMetadata, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting metadata for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 获取元数据值（泛型版本）
    /// </summary>
    public async Task<T?> GetValueAsync<T>(Guid userId, string key, T? defaultValue = default, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Getting metadata value for user: {UserId}, key: {Key}", userId, key);

            var metadataEntry = await GetByKeyAsync(userId, key, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogDebug("Metadata entry not found, returning default value for user: {UserId}, key: {Key}", userId, key);
                return defaultValue;
            }

            try
            {
                var value = metadataEntry.GetValue<T>();
                _logger.LogDebug("Successfully retrieved metadata value for user: {UserId}, key: {Key}", userId, key);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize metadata value, returning default for user: {UserId}, key: {Key}", userId, key);
                return defaultValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata value for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 设置元数据值（泛型版本）
    /// </summary>
    public async Task<UserMetadataEntry> SetValueAsync<T>(
        Guid userId,
        string key,
        T value,
        string category = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Setting metadata value for user: {UserId}, key: {Key}", userId, key);

            var existingMetadata = await GetByKeyAsync(userId, key, cancellationToken);

            if (existingMetadata != null)
            {
                // 更新现有元数据条目
                existingMetadata.SetValue(value!);
                return await UpdateAsync(existingMetadata, cancellationToken);
            }
            else
            {
                // 创建新的元数据条目
                var newMetadata = new UserMetadataEntry(userId, key, value!, category);
                return await AddAsync(newMetadata, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting metadata value for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 删除元数据条目
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting metadata entry: {Id}", id);

            var metadataEntry = await _context.UserMetadataEntries
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogWarning("Metadata entry not found for deletion: {Id}", id);
                return false;
            }

            _context.UserMetadataEntries.Remove(metadataEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted metadata entry: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata entry: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 删除用户的所有元数据条目
    /// </summary>
    public async Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting all metadata entries for user: {UserId}", userId);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => m.UserId == userId)
                .ToListAsync(cancellationToken);

            if (!metadataEntries.Any())
            {
                _logger.LogDebug("No metadata entries found for user: {UserId}", userId);
                return 0;
            }

            _context.UserMetadataEntries.RemoveRange(metadataEntries);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} metadata entries for user: {UserId}",
                metadataEntries.Count, userId);
            return metadataEntries.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata entries for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 删除用户指定分类的所有元数据条目
    /// </summary>
    public async Task<int> DeleteByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _logger.LogDebug("Deleting metadata entries for user: {UserId}, category: {Category}", userId, category);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => m.UserId == userId && m.Category == category)
                .ToListAsync(cancellationToken);

            if (!metadataEntries.Any())
            {
                _logger.LogDebug("No metadata entries found for user: {UserId}, category: {Category}", userId, category);
                return 0;
            }

            _context.UserMetadataEntries.RemoveRange(metadataEntries);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} metadata entries for user: {UserId}, category: {Category}",
                metadataEntries.Count, userId, category);
            return metadataEntries.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata entries for user: {UserId}, category: {Category}", userId, category);
            throw;
        }
    }

    /// <summary>
    /// 删除用户指定键的元数据条目
    /// </summary>
    public async Task<bool> DeleteByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Deleting metadata entry for user: {UserId}, key: {Key}", userId, key);

            var metadataEntry = await _context.UserMetadataEntries
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Key == key, cancellationToken);

            if (metadataEntry == null)
            {
                _logger.LogWarning("Metadata entry not found for deletion: user {UserId}, key {Key}", userId, key);
                return false;
            }

            _context.UserMetadataEntries.Remove(metadataEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted metadata entry for user: {UserId}, key: {Key}", userId, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata entry for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    /// <summary>
    /// 获取元数据条目的统计信息
    /// </summary>
    public async Task<(int CategoryCount, int TotalEntries, DateTime? LastUpdated)> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting metadata statistics for user: {UserId}", userId);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => m.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var categoryCount = metadataEntries.Select(m => m.Category).Distinct().Count();
            var totalEntries = metadataEntries.Count;
            var lastUpdated = metadataEntries.Any() ? metadataEntries.Max(m => m.UpdatedTime) : (DateTime?)null;

            _logger.LogDebug("Metadata statistics for user {UserId}: {CategoryCount} categories, {TotalEntries} total entries, last updated: {LastUpdated}",
                userId, categoryCount, totalEntries, lastUpdated);

            return (categoryCount, totalEntries, lastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata statistics for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 批量删除元数据条目
    /// </summary>
    public async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        try
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var idsList = ids.ToList();
            if (!idsList.Any())
            {
                return 0;
            }

            _logger.LogDebug("Deleting {Count} metadata entries in batch", idsList.Count);

            var metadataEntries = await _context.UserMetadataEntries
                .Where(m => idsList.Contains(m.Id))
                .ToListAsync(cancellationToken);

            if (!metadataEntries.Any())
            {
                _logger.LogWarning("No metadata entries found for batch deletion");
                return 0;
            }

            _context.UserMetadataEntries.RemoveRange(metadataEntries);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} metadata entries in batch", metadataEntries.Count);
            return metadataEntries.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata entries in batch");
            throw;
        }
    }

    /// <summary>
    /// 搜索元数据条目
    /// </summary>
    public async Task<IEnumerable<UserMetadataEntry>> SearchAsync(
        Guid userId,
        string searchTerm,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            _logger.LogDebug("Searching metadata entries for user: {UserId}, searchTerm: {SearchTerm}, category: {Category}",
                userId, searchTerm, category);

            var query = _context.UserMetadataEntries.Where(m => m.UserId == userId);

            // 在键和值中搜索
            query = query.Where(m => m.Key.Contains(searchTerm) || m.ValueJson.Contains(searchTerm));

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(m => m.Category == category);
            }

            var metadataEntries = await query
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} metadata entries matching search criteria for user: {UserId}",
                metadataEntries.Count, userId);

            return metadataEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching metadata entries for user: {UserId}", userId);
            throw;
        }
    }
}
