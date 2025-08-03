using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Infrastructure.Security;

/// <summary>
/// 安全日志服务实现，提供操作事件记录和查询功能
/// </summary>
public class SecurityLogService : ISecurityLogService
{
    private readonly IUserSecurityLogRepository _securityLogRepository;
    private readonly ILogger<SecurityLogService> _logger;

    /// <summary>
    /// 初始化安全日志服务
    /// </summary>
    /// <param name="securityLogRepository">安全日志仓储</param>
    /// <param name="logger">日志记录器</param>
    public SecurityLogService(
        IUserSecurityLogRepository securityLogRepository,
        ILogger<SecurityLogService> logger)
    {
        _securityLogRepository = securityLogRepository ?? throw new ArgumentNullException(nameof(securityLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 记录用户登录事件
    /// </summary>
    public async Task LogUserLoginAsync(
        Guid userId,
        string machineId,
        string? deviceInfo = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var loginLog = UserSecurityLog.CreateLoginLog(
                userId,
                machineId,
                deviceInfo,
                sessionId,
                isSuccessful,
                errorCode);

            await _securityLogRepository.AddAsync(loginLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("用户登录事件已记录: UserId={UserId}, IsSuccessful={IsSuccessful}, MachineId={MachineId}",
                userId, isSuccessful, machineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录用户登录事件失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 记录用户登出事件
    /// </summary>
    public async Task LogUserLogoutAsync(
        Guid userId,
        string machineId,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logoutLog = UserSecurityLog.CreateOperationLog(
                userId,
                SecurityEventType.UserLogout,
                "用户成功登出",
                eventDetails: null,
                machineId: machineId,
                sessionId: sessionId,
                isSuccessful: true);

            await _securityLogRepository.AddAsync(logoutLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("用户登出事件已记录: UserId={UserId}, MachineId={MachineId}",
                userId, machineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录用户登出事件失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 记录用户操作事件
    /// </summary>
    public async Task LogUserOperationAsync(
        Guid userId,
        SecurityEventType eventType,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operationLog = UserSecurityLog.CreateOperationLog(
                userId,
                eventType,
                description,
                eventDetails,
                machineId,
                sessionId,
                isSuccessful,
                errorCode);

            await _securityLogRepository.AddAsync(operationLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("用户操作事件已记录: UserId={UserId}, EventType={EventType}, IsSuccessful={IsSuccessful}",
                userId, eventType, isSuccessful);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录用户操作事件失败: UserId={UserId}, EventType={EventType}", userId, eventType);
            throw;
        }
    }

    /// <summary>
    /// 记录安全警告事件
    /// </summary>
    public async Task LogSecurityWarningAsync(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var warningLog = UserSecurityLog.CreateSecurityWarning(
                userId,
                description,
                eventDetails,
                machineId,
                sessionId);

            await _securityLogRepository.AddAsync(warningLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("安全警告事件已记录: UserId={UserId}, Description={Description}",
                userId, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录安全警告事件失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 记录系统错误事件
    /// </summary>
    public async Task LogSystemErrorAsync(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? errorCode = null,
        string? machineId = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var errorLog = UserSecurityLog.CreateSystemErrorLog(
                userId,
                description,
                eventDetails,
                errorCode,
                machineId,
                sessionId);

            await _securityLogRepository.AddAsync(errorLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogError("系统错误事件已记录: UserId={UserId}, Description={Description}, ErrorCode={ErrorCode}",
                userId, description, errorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录系统错误事件失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户的操作日志
    /// </summary>
    public async Task<IEnumerable<UserSecurityLog>> GetUserOperationLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        int pageIndex = 0,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证参数
            if (pageIndex < 0)
                pageIndex = 0;

            if (pageSize <= 0 || pageSize > 1000)
                pageSize = 50;

            var logs = await _securityLogRepository.GetUserLogsAsync(
                userId,
                fromDate,
                toDate,
                eventTypes,
                severities,
                pageIndex,
                pageSize,
                cancellationToken);

            _logger.LogDebug("获取用户操作日志: UserId={UserId}, Count={Count}",
                userId, logs.Count());

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户操作日志失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取安全事件统计信息
    /// </summary>
    public async Task<SecurityEventStatistics> GetSecurityEventStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventTypeStats = await _securityLogRepository.GetEventTypeStatisticsAsync(
                userId, fromDate, toDate, cancellationToken);

            var severityStats = await _securityLogRepository.GetSeverityStatisticsAsync(
                userId, fromDate, toDate, cancellationToken);

            var totalEvents = eventTypeStats.Values.Sum();
            var lastLoginTime = await _securityLogRepository.GetLastLoginTimeAsync(userId, cancellationToken);
            var lastActivityTime = await _securityLogRepository.GetLastActivityTimeAsync(userId, cancellationToken);

            var statistics = new SecurityEventStatistics
            {
                TotalEvents = totalEvents,
                SuccessfulEvents = await GetSuccessfulEventsCountAsync(userId, fromDate, toDate, cancellationToken),
                FailedEvents = await GetFailedEventsCountAsync(userId, fromDate, toDate, cancellationToken),
                WarningEvents = severityStats.GetValueOrDefault(SecurityEventSeverity.Warning, 0),
                ErrorEvents = severityStats.GetValueOrDefault(SecurityEventSeverity.Error, 0),
                CriticalEvents = severityStats.GetValueOrDefault(SecurityEventSeverity.Critical, 0),
                EventTypeStatistics = eventTypeStats,
                SeverityStatistics = severityStats,
                LastLoginTime = lastLoginTime,
                LastActivityTime = lastActivityTime
            };

            _logger.LogDebug("获取安全事件统计信息: UserId={UserId}, TotalEvents={TotalEvents}",
                userId, totalEvents);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取安全事件统计信息失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取最近的安全事件
    /// </summary>
    public async Task<IEnumerable<UserSecurityLog>> GetRecentSecurityEventsAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count <= 0 || count > 100)
                count = 10;

            var recentEvents = await _securityLogRepository.GetRecentUserLogsAsync(
                userId, count, cancellationToken);

            _logger.LogDebug("获取最近安全事件: UserId={UserId}, Count={Count}",
                userId, recentEvents.Count());

            return recentEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近安全事件失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 清理过期的日志记录
    /// </summary>
    public async Task<int> CleanupExpiredLogsAsync(
        int retentionDays = 90,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (retentionDays <= 0)
                retentionDays = 90;

            var retentionDate = DateTime.UtcNow.AddDays(-retentionDays);
            var deletedCount = await _securityLogRepository.DeleteExpiredLogsAsync(retentionDate, cancellationToken);

            if (deletedCount > 0)
            {
                await _securityLogRepository.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("清理过期日志记录完成: DeletedCount={DeletedCount}, RetentionDays={RetentionDays}",
                deletedCount, retentionDays);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期日志记录失败: RetentionDays={RetentionDays}", retentionDays);
            throw;
        }
    }

    /// <summary>
    /// 检查是否存在可疑活动
    /// </summary>
    public async Task<bool> HasSuspiciousActivityAsync(
        Guid userId,
        int timeWindow = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (timeWindow <= 0)
                timeWindow = 24;

            var hasSuspiciousActivity = await _securityLogRepository.HasSuspiciousActivityAsync(
                userId, timeWindow, cancellationToken);

            if (hasSuspiciousActivity)
            {
                _logger.LogWarning("检测到可疑活动: UserId={UserId}, TimeWindow={TimeWindow}小时",
                    userId, timeWindow);
            }

            return hasSuspiciousActivity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查可疑活动失败: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取成功事件数量
    /// </summary>
    private async Task<int> GetSuccessfulEventsCountAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        // 通过仓储查询成功的事件数量
        return await _securityLogRepository.GetUserLogCountAsync(
            userId,
            fromDate,
            toDate,
            eventTypes: null,
            severities: null,
            cancellationToken);
    }

    /// <summary>
    /// 获取失败事件数量
    /// </summary>
    private async Task<int> GetFailedEventsCountAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        // 通过仓储查询失败的事件数量 - 查询所有非成功的事件
        return await _securityLogRepository.GetUserLogCountAsync(
            userId,
            fromDate,
            toDate,
            eventTypes: null,
            severities: new[] { SecurityEventSeverity.Error, SecurityEventSeverity.Critical },
            cancellationToken);
    }
}