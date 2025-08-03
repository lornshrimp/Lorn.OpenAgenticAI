using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 安全日志服务接口，提供操作事件记录和查询功能
/// </summary>
public interface ISecurityLogService
{
    /// <summary>
    /// 记录用户登录事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="isSuccessful">是否成功</param>
    /// <param name="errorCode">错误代码（如果失败）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志记录任务</returns>
    Task LogUserLoginAsync(
        Guid userId,
        string machineId,
        string? deviceInfo = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录用户登出事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志记录任务</returns>
    Task LogUserLogoutAsync(
        Guid userId,
        string machineId,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录用户操作事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="description">事件描述</param>
    /// <param name="eventDetails">事件详细信息</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="isSuccessful">是否成功</param>
    /// <param name="errorCode">错误代码（如果失败）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志记录任务</returns>
    Task LogUserOperationAsync(
        Guid userId,
        SecurityEventType eventType,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录安全警告事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="description">警告描述</param>
    /// <param name="eventDetails">事件详细信息</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志记录任务</returns>
    Task LogSecurityWarningAsync(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录系统错误事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="description">错误描述</param>
    /// <param name="eventDetails">事件详细信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志记录任务</returns>
    Task LogSystemErrorAsync(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? errorCode = null,
        string? machineId = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="eventTypes">事件类型过滤</param>
    /// <param name="severities">严重级别过滤</param>
    /// <param name="pageIndex">页码（从0开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<IEnumerable<UserSecurityLog>> GetUserOperationLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        int pageIndex = 0,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取安全事件统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安全事件统计</returns>
    Task<SecurityEventStatistics> GetSecurityEventStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近的安全事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">获取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近的安全事件列表</returns>
    Task<IEnumerable<UserSecurityLog>> GetRecentSecurityEventsAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的日志记录
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数量</returns>
    Task<int> CleanupExpiredLogsAsync(
        int retentionDays = 90,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否存在可疑活动
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="timeWindow">时间窗口（小时）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在可疑活动</returns>
    Task<bool> HasSuspiciousActivityAsync(
        Guid userId,
        int timeWindow = 24,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 安全事件统计信息
/// </summary>
public class SecurityEventStatistics
{
    /// <summary>
    /// 总事件数量
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// 成功事件数量
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// 失败事件数量
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// 警告事件数量
    /// </summary>
    public int WarningEvents { get; set; }

    /// <summary>
    /// 错误事件数量
    /// </summary>
    public int ErrorEvents { get; set; }

    /// <summary>
    /// 严重事件数量
    /// </summary>
    public int CriticalEvents { get; set; }

    /// <summary>
    /// 按事件类型分组的统计
    /// </summary>
    public Dictionary<SecurityEventType, int> EventTypeStatistics { get; set; } = new();

    /// <summary>
    /// 按严重级别分组的统计
    /// </summary>
    public Dictionary<SecurityEventSeverity, int> SeverityStatistics { get; set; } = new();

    /// <summary>
    /// 最后一次登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最后一次活动时间
    /// </summary>
    public DateTime? LastActivityTime { get; set; }
}