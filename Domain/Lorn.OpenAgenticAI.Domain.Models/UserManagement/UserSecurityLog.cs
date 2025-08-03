using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户安全日志实体，记录用户相关的安全事件和操作历史
/// </summary>
public class UserSecurityLog : IEntity
{
    /// <summary>
    /// 日志记录唯一标识符
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// 用户ID，关联到UserProfile
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 安全事件类型
    /// </summary>
    public SecurityEventType EventType { get; private set; }

    /// <summary>
    /// 事件严重级别
    /// </summary>
    public SecurityEventSeverity Severity { get; private set; }

    /// <summary>
    /// 事件描述信息
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// 事件详细信息（JSON格式）
    /// </summary>
    public string? EventDetails { get; private set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; private set; }

    /// <summary>
    /// 机器ID
    /// </summary>
    public string? MachineId { get; private set; }

    /// <summary>
    /// 事件发生时间戳
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// 事件来源（应用程序、服务等）
    /// </summary>
    public string? Source { get; private set; }

    /// <summary>
    /// 关联的会话ID
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// 操作结果（成功/失败）
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// 错误代码（如果操作失败）
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// 导航属性 - 关联的用户
    /// </summary>
    public virtual UserProfile? User { get; private set; }

    // 私有构造函数用于EF Core
    private UserSecurityLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建新的安全日志记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="severity">严重级别</param>
    /// <param name="description">事件描述</param>
    /// <param name="eventDetails">事件详细信息</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <param name="machineId">机器ID</param>
    /// <param name="source">事件来源</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="isSuccessful">操作是否成功</param>
    /// <param name="errorCode">错误代码</param>
    public UserSecurityLog(
        Guid userId,
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string description,
        string? eventDetails = null,
        string? ipAddress = null,
        string? deviceInfo = null,
        string? machineId = null,
        string? source = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null)
    {
        Id = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("用户ID不能为空", nameof(userId));
        EventType = eventType;
        Severity = severity;
        Description = !string.IsNullOrWhiteSpace(description) ? description : throw new ArgumentException("事件描述不能为空", nameof(description));
        EventDetails = eventDetails;
        IpAddress = ipAddress;
        DeviceInfo = deviceInfo;
        MachineId = machineId;
        Source = source;
        SessionId = sessionId;
        IsSuccessful = isSuccessful;
        ErrorCode = errorCode;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建用户登录日志
    /// </summary>
    public static UserSecurityLog CreateLoginLog(
        Guid userId,
        string machineId,
        string? deviceInfo = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null)
    {
        var severity = isSuccessful ? SecurityEventSeverity.Information : SecurityEventSeverity.Warning;
        var description = isSuccessful ? "用户成功登录" : "用户登录失败";

        return new UserSecurityLog(
            userId,
            SecurityEventType.UserLogin,
            severity,
            description,
            eventDetails: null,
            ipAddress: "127.0.0.1", // 本地应用
            deviceInfo: deviceInfo,
            machineId: machineId,
            source: "UserAuthentication",
            sessionId: sessionId,
            isSuccessful: isSuccessful,
            errorCode: errorCode);
    }

    /// <summary>
    /// 创建用户操作日志
    /// </summary>
    public static UserSecurityLog CreateOperationLog(
        Guid userId,
        SecurityEventType eventType,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorCode = null)
    {
        var severity = isSuccessful ? SecurityEventSeverity.Information : SecurityEventSeverity.Error;

        return new UserSecurityLog(
            userId,
            eventType,
            severity,
            description,
            eventDetails: eventDetails,
            ipAddress: "127.0.0.1", // 本地应用
            deviceInfo: Environment.MachineName,
            machineId: machineId,
            source: "UserOperation",
            sessionId: sessionId,
            isSuccessful: isSuccessful,
            errorCode: errorCode);
    }

    /// <summary>
    /// 创建安全警告日志
    /// </summary>
    public static UserSecurityLog CreateSecurityWarning(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? machineId = null,
        string? sessionId = null)
    {
        return new UserSecurityLog(
            userId,
            SecurityEventType.SuspiciousActivity,
            SecurityEventSeverity.Warning,
            description,
            eventDetails: eventDetails,
            ipAddress: "127.0.0.1",
            deviceInfo: Environment.MachineName,
            machineId: machineId,
            source: "SecurityMonitor",
            sessionId: sessionId,
            isSuccessful: false);
    }

    /// <summary>
    /// 创建系统错误日志
    /// </summary>
    public static UserSecurityLog CreateSystemErrorLog(
        Guid userId,
        string description,
        string? eventDetails = null,
        string? errorCode = null,
        string? machineId = null,
        string? sessionId = null)
    {
        return new UserSecurityLog(
            userId,
            SecurityEventType.SystemError,
            SecurityEventSeverity.Critical,
            description,
            eventDetails: eventDetails,
            ipAddress: "127.0.0.1",
            deviceInfo: Environment.MachineName,
            machineId: machineId,
            source: "System",
            sessionId: sessionId,
            isSuccessful: false,
            errorCode: errorCode);
    }

    /// <summary>
    /// 验证日志记录的有效性
    /// </summary>
    public bool IsValid()
    {
        return UserId != Guid.Empty &&
               !string.IsNullOrWhiteSpace(Description) &&
               Timestamp != default;
    }

    /// <summary>
    /// 获取事件的显示名称
    /// </summary>
    public string GetEventDisplayName()
    {
        return EventType switch
        {
            SecurityEventType.UserLogin => "用户登录",
            SecurityEventType.UserLogout => "用户登出",
            SecurityEventType.UserCreated => "用户创建",
            SecurityEventType.UserDeleted => "用户删除",
            SecurityEventType.UserProfileUpdated => "用户信息修改",
            SecurityEventType.PreferencesUpdated => "偏好设置修改",
            SecurityEventType.UserSwitched => "用户切换",
            SecurityEventType.DataExported => "数据导出",
            SecurityEventType.DataDeleted => "数据删除",
            SecurityEventType.SessionCreated => "会话创建",
            SecurityEventType.SessionExpired => "会话过期",
            SecurityEventType.AuthenticationFailed => "认证失败",
            SecurityEventType.AccessDenied => "权限拒绝",
            SecurityEventType.SuspiciousActivity => "异常操作",
            SecurityEventType.SystemError => "系统错误",
            SecurityEventType.ConfigurationChanged => "配置修改",
            SecurityEventType.DataBackup => "数据备份",
            SecurityEventType.DataRestore => "数据恢复",
            _ => "未知事件"
        };
    }

    /// <summary>
    /// 获取严重级别的显示名称
    /// </summary>
    public string GetSeverityDisplayName()
    {
        return Severity switch
        {
            SecurityEventSeverity.Information => "信息",
            SecurityEventSeverity.Warning => "警告",
            SecurityEventSeverity.Error => "错误",
            SecurityEventSeverity.Critical => "严重",
            _ => "未知"
        };
    }
}