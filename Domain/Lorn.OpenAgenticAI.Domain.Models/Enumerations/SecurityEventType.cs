namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// 安全事件类型枚举
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// 用户登录
    /// </summary>
    UserLogin = 1,

    /// <summary>
    /// 用户登出
    /// </summary>
    UserLogout = 2,

    /// <summary>
    /// 用户创建
    /// </summary>
    UserCreated = 3,

    /// <summary>
    /// 用户删除
    /// </summary>
    UserDeleted = 4,

    /// <summary>
    /// 用户信息修改
    /// </summary>
    UserProfileUpdated = 5,

    /// <summary>
    /// 偏好设置修改
    /// </summary>
    PreferencesUpdated = 6,

    /// <summary>
    /// 用户切换
    /// </summary>
    UserSwitched = 7,

    /// <summary>
    /// 数据导出
    /// </summary>
    DataExported = 8,

    /// <summary>
    /// 数据删除
    /// </summary>
    DataDeleted = 9,

    /// <summary>
    /// 会话创建
    /// </summary>
    SessionCreated = 10,

    /// <summary>
    /// 会话过期
    /// </summary>
    SessionExpired = 11,

    /// <summary>
    /// 认证失败
    /// </summary>
    AuthenticationFailed = 12,

    /// <summary>
    /// 权限拒绝
    /// </summary>
    AccessDenied = 13,

    /// <summary>
    /// 异常操作
    /// </summary>
    SuspiciousActivity = 14,

    /// <summary>
    /// 系统错误
    /// </summary>
    SystemError = 15,

    /// <summary>
    /// 配置修改
    /// </summary>
    ConfigurationChanged = 16,

    /// <summary>
    /// 数据备份
    /// </summary>
    DataBackup = 17,

    /// <summary>
    /// 数据恢复
    /// </summary>
    DataRestore = 18
}

/// <summary>
/// 安全事件严重级别枚举
/// </summary>
public enum SecurityEventSeverity
{
    /// <summary>
    /// 信息级别 - 正常操作记录
    /// </summary>
    Information = 1,

    /// <summary>
    /// 警告级别 - 需要关注的事件
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 错误级别 - 操作失败或异常
    /// </summary>
    Error = 3,

    /// <summary>
    /// 严重级别 - 安全威胁或系统故障
    /// </summary>
    Critical = 4
}