using Microsoft.EntityFrameworkCore;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户安全日志仓储实现，提供安全日志的数据访问功能
/// </summary>
public class UserSecurityLogRepository : IUserSecurityLogRepository
{
    private readonly OpenAgenticAIDbContext _context;

    /// <summary>
    /// 初始化用户安全日志仓储
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public UserSecurityLogRepository(OpenAgenticAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 添加安全日志记录
    /// </summary>
    public async Task AddAsync(UserSecurityLog securityLog, CancellationToken cancellationToken = default)
    {
        if (securityLog == null)
            throw new ArgumentNullException(nameof(securityLog));

        await _context.UserSecurityLogs.AddAsync(securityLog, cancellationToken);
    }

    /// <summary>
    /// 批量添加安全日志记录
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<UserSecurityLog> securityLogs, CancellationToken cancellationToken = default)
    {
        if (securityLogs == null)
            throw new ArgumentNullException(nameof(securityLogs));

        await _context.UserSecurityLogs.AddRangeAsync(securityLogs, cancellationToken);
    }

    /// <summary>
    /// 根据ID获取安全日志记录
    /// </summary>
    public async Task<UserSecurityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserSecurityLogs
            .Include(log => log.User)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取用户的安全日志记录
    /// </summary>
    public async Task<IEnumerable<UserSecurityLog>> GetUserLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        int pageIndex = 0,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserSecurityLogs
            .Where(log => log.UserId == userId);

        // 应用日期过滤
        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);

        // 应用事件类型过滤
        if (eventTypes != null && eventTypes.Any())
        {
            var eventTypeList = eventTypes.ToList();
            query = query.Where(log => eventTypeList.Contains(log.EventType));
        }

        // 应用严重级别过滤
        if (severities != null && severities.Any())
        {
            var severityList = severities.ToList();
            query = query.Where(log => severityList.Contains(log.Severity));
        }

        // 应用分页和排序
        return await query
            .OrderByDescending(log => log.Timestamp)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Include(log => log.User)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户最近的安全日志记录
    /// </summary>
    public async Task<IEnumerable<UserSecurityLog>> GetRecentUserLogsAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserSecurityLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(count)
            .Include(log => log.User)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 统计用户的安全事件
    /// </summary>
    public async Task<Dictionary<SecurityEventType, int>> GetEventTypeStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserSecurityLogs
            .Where(log => log.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);

        return await query
            .GroupBy(log => log.EventType)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Count(),
                cancellationToken);
    }

    /// <summary>
    /// 统计用户的安全事件严重级别
    /// </summary>
    public async Task<Dictionary<SecurityEventSeverity, int>> GetSeverityStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserSecurityLogs
            .Where(log => log.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);

        return await query
            .GroupBy(log => log.Severity)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Count(),
                cancellationToken);
    }

    /// <summary>
    /// 获取用户的总日志数量
    /// </summary>
    public async Task<int> GetUserLogCountAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserSecurityLogs
            .Where(log => log.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);

        if (eventTypes != null && eventTypes.Any())
        {
            var eventTypeList = eventTypes.ToList();
            query = query.Where(log => eventTypeList.Contains(log.EventType));
        }

        if (severities != null && severities.Any())
        {
            var severityList = severities.ToList();
            query = query.Where(log => severityList.Contains(log.Severity));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户最后一次登录时间
    /// </summary>
    public async Task<DateTime?> GetLastLoginTimeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSecurityLogs
            .Where(log => log.UserId == userId &&
                         log.EventType == SecurityEventType.UserLogin &&
                         log.IsSuccessful)
            .OrderByDescending(log => log.Timestamp)
            .Select(log => log.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户最后一次活动时间
    /// </summary>
    public async Task<DateTime?> GetLastActivityTimeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSecurityLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Select(log => log.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 检查用户在指定时间窗口内是否有可疑活动
    /// </summary>
    public async Task<bool> HasSuspiciousActivityAsync(
        Guid userId,
        int timeWindow = 24,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);

        // 检查是否有可疑活动事件
        var hasSuspiciousEvents = await _context.UserSecurityLogs
            .AnyAsync(log => log.UserId == userId &&
                           log.Timestamp >= cutoffTime &&
                           (log.EventType == SecurityEventType.SuspiciousActivity ||
                            log.EventType == SecurityEventType.AuthenticationFailed ||
                            log.EventType == SecurityEventType.AccessDenied ||
                            log.Severity == SecurityEventSeverity.Critical),
                     cancellationToken);

        if (hasSuspiciousEvents)
            return true;

        // 检查是否有异常频繁的失败操作
        var failedOperationsCount = await _context.UserSecurityLogs
            .CountAsync(log => log.UserId == userId &&
                             log.Timestamp >= cutoffTime &&
                             !log.IsSuccessful,
                       cancellationToken);

        // 如果在时间窗口内有超过10次失败操作，认为是可疑活动
        return failedOperationsCount > 10;
    }

    /// <summary>
    /// 删除过期的日志记录
    /// </summary>
    public async Task<int> DeleteExpiredLogsAsync(DateTime retentionDate, CancellationToken cancellationToken = default)
    {
        var expiredLogs = await _context.UserSecurityLogs
            .Where(log => log.Timestamp < retentionDate)
            .ToListAsync(cancellationToken);

        if (expiredLogs.Any())
        {
            _context.UserSecurityLogs.RemoveRange(expiredLogs);
            return expiredLogs.Count;
        }

        return 0;
    }

    /// <summary>
    /// 删除用户的所有日志记录
    /// </summary>
    public async Task<int> DeleteUserLogsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userLogs = await _context.UserSecurityLogs
            .Where(log => log.UserId == userId)
            .ToListAsync(cancellationToken);

        if (userLogs.Any())
        {
            _context.UserSecurityLogs.RemoveRange(userLogs);
            return userLogs.Count;
        }

        return 0;
    }

    /// <summary>
    /// 保存更改
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}