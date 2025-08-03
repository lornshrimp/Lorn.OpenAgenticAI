using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户安全日志仓储接口，提供安全日志的数据访问功能
/// </summary>
public interface IUserSecurityLogRepository
{
    /// <summary>
    /// 添加安全日志记录
    /// </summary>
    /// <param name="securityLog">安全日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加任务</returns>
    Task AddAsync(UserSecurityLog securityLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加安全日志记录
    /// </summary>
    /// <param name="securityLogs">安全日志实体列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加任务</returns>
    Task AddRangeAsync(IEnumerable<UserSecurityLog> securityLogs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取安全日志记录
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安全日志实体</returns>
    Task<UserSecurityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的安全日志记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="eventTypes">事件类型过滤</param>
    /// <param name="severities">严重级别过滤</param>
    /// <param name="pageIndex">页码（从0开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安全日志列表</returns>
    Task<IEnumerable<UserSecurityLog>> GetUserLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        int pageIndex = 0,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最近的安全日志记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">获取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近的安全日志列表</returns>
    Task<IEnumerable<UserSecurityLog>> GetRecentUserLogsAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计用户的安全事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事件统计信息</returns>
    Task<Dictionary<SecurityEventType, int>> GetEventTypeStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计用户的安全事件严重级别
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>严重级别统计信息</returns>
    Task<Dictionary<SecurityEventSeverity, int>> GetSeverityStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的总日志数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="eventTypes">事件类型过滤</param>
    /// <param name="severities">严重级别过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志总数</returns>
    Task<int> GetUserLogCountAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        IEnumerable<SecurityEventType>? eventTypes = null,
        IEnumerable<SecurityEventSeverity>? severities = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最后一次登录时间
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最后登录时间</returns>
    Task<DateTime?> GetLastLoginTimeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最后一次活动时间
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最后活动时间</returns>
    Task<DateTime?> GetLastActivityTimeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户在指定时间窗口内是否有可疑活动
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="timeWindow">时间窗口（小时）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在可疑活动</returns>
    Task<bool> HasSuspiciousActivityAsync(
        Guid userId,
        int timeWindow = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除过期的日志记录
    /// </summary>
    /// <param name="retentionDate">保留截止日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的记录数量</returns>
    Task<int> DeleteExpiredLogsAsync(DateTime retentionDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有日志记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的记录数量</returns>
    Task<int> DeleteUserLogsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存任务</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}