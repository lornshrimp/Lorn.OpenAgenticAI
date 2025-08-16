using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 用户上下文会话服务（Session Scope）。
/// 仅维护当前运行期（进程 / 线程关联）的活跃用户信息与轻量缓存：
/// 1. 不直接执行业务规则验证（交由 <see cref="IUserManagementService"/>）。
/// 2. 不直接进行持久化 CRUD（交由 <see cref="IUserDataService"/>）。
/// 3. 提供获取 / 切换 / 清除 / 事件通知，用于 UI 与应用层其它组件感知用户切换。
/// 4. 线程安全：实现内部并发访问保护，但不保证跨进程同步。
/// 职责边界：变更用户档案或偏好前，应先通过管理服务完成业务校验，再刷新上下文。
/// </summary>

/// <summary>
/// 用户上下文服务接口（会话与缓存层）—— 不直接做持久化 CRUD / 偏好设置写入
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// 获取当前用户上下文
    /// </summary>
    /// <returns>当前用户上下文，如果没有则返回null</returns>
    Task<UserContext?> GetCurrentUserContextAsync();

    /// <summary>
    /// 设置当前用户上下文
    /// </summary>
    /// <param name="userContext">用户上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetCurrentUserContextAsync(UserContext userContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换用户上下文
    /// </summary>
    /// <param name="userId">目标用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换结果</returns>
    Task<UserContextSwitchResult> SwitchUserContextAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理当前用户上下文
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task ClearCurrentUserContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID，如果没有则返回null</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// 获取当前用户档案
    /// </summary>
    /// <returns>当前用户档案，如果没有则返回null</returns>
    Task<UserProfile?> GetCurrentUserProfileAsync();

    /// <summary>
    /// 检查当前是否有活跃的用户上下文
    /// </summary>
    /// <returns>是否有活跃的用户上下文</returns>
    bool HasActiveUserContext();

    /// <summary>
    /// 刷新当前用户上下文缓存
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task RefreshCurrentUserContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户上下文变更事件
    /// </summary>
    event EventHandler<UserContextChangedEventArgs>? UserContextChanged;

}

/// <summary>
/// 用户上下文信息
/// </summary>
public class UserContext
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户档案
    /// </summary>
    public UserProfile UserProfile { get; set; } = null!;

    /// <summary>
    /// 会话令牌
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 会话过期时间
    /// </summary>
    public DateTime? SessionExpirationTime { get; set; }

    /// <summary>
    /// 机器ID
    /// </summary>
    public string? MachineId { get; set; }

    /// <summary>
    /// 上下文创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 上下文最后更新时间
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 缓存的用户偏好设置
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> CachedPreferences { get; set; } = new();

    /// <summary>
    /// 是否为默认用户
    /// </summary>
    public bool IsDefaultUser { get; set; }

    /// <summary>
    /// 验证用户上下文是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return UserId != Guid.Empty &&
               UserProfile != null &&
               UserProfile.IsActive &&
               (SessionExpirationTime == null || SessionExpirationTime > DateTime.UtcNow);
    }

    /// <summary>
    /// 检查会话是否即将过期
    /// </summary>
    /// <param name="thresholdMinutes">过期阈值（分钟）</param>
    /// <returns>是否即将过期</returns>
    public bool IsSessionNearExpiry(int thresholdMinutes = 30)
    {
        return SessionExpirationTime.HasValue &&
               SessionExpirationTime.Value.Subtract(DateTime.UtcNow).TotalMinutes <= thresholdMinutes;
    }

    /// <summary>
    /// 更新最后更新时间
    /// </summary>
    public void UpdateLastModified()
    {
        LastUpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 用户上下文切换结果
/// </summary>
public class UserContextSwitchResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 新的用户上下文
    /// </summary>
    public UserContext? NewUserContext { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="newUserContext">新的用户上下文</param>
    /// <returns>成功结果</returns>
    public static UserContextSwitchResult Success(UserContext newUserContext)
    {
        return new UserContextSwitchResult
        {
            IsSuccessful = true,
            NewUserContext = newUserContext
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <returns>失败结果</returns>
    public static UserContextSwitchResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserContextSwitchResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 用户上下文变更事件参数
/// </summary>
public class UserContextChangedEventArgs : EventArgs
{
    /// <summary>
    /// 旧的用户上下文
    /// </summary>
    public UserContext? OldUserContext { get; set; }

    /// <summary>
    /// 新的用户上下文
    /// </summary>
    public UserContext? NewUserContext { get; set; }

    /// <summary>
    /// 变更类型
    /// </summary>
    public UserContextChangeType ChangeType { get; set; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// 用户上下文变更类型
/// </summary>
public enum UserContextChangeType
{
    /// <summary>
    /// 用户登录
    /// </summary>
    UserLogin,

    /// <summary>
    /// 用户切换
    /// </summary>
    UserSwitch,

    /// <summary>
    /// 用户登出
    /// </summary>
    UserLogout,

    /// <summary>
    /// 上下文刷新
    /// </summary>
    ContextRefresh,

    /// <summary>
    /// 会话过期
    /// </summary>
    SessionExpired
}