using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 静默认证服务接口，提供自动用户创建和认证功能
/// </summary>
public interface ISilentAuthenticationService
{
    /// <summary>
    /// 获取或创建默认用户，基于机器ID自动识别用户
    /// </summary>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> GetOrCreateDefaultUserAsync(string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换到指定用户
    /// </summary>
    /// <param name="userId">目标用户ID</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> SwitchUserAsync(Guid userId, string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证会话令牌的有效性
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<SessionValidationResult> ValidateSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新会话令牌
    /// </summary>
    /// <param name="sessionToken">当前会话令牌</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新结果</returns>
    Task<SessionRefreshResult> RefreshSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前机器上的可用用户列表
    /// </summary>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<IEnumerable<UserProfile>> GetAvailableUsersAsync(string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新用户会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话创建结果</returns>
    Task<SessionCreationResult> CreateUserSessionAsync(Guid userId, string machineId, string? deviceInfo = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 结束用户会话
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话结束结果</returns>
    Task<bool> EndUserSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前活跃会话信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="machineId">机器标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃会话列表</returns>
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, string machineId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 认证结果
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// 认证是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 用户档案
    /// </summary>
    public UserProfile? User { get; set; }

    /// <summary>
    /// 会话令牌
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 是否为新创建的用户
    /// </summary>
    public bool IsNewUser { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功的认证结果
    /// </summary>
    public static AuthenticationResult Success(UserProfile user, string sessionToken, string sessionId, DateTime expirationTime, bool isNewUser = false)
    {
        return new AuthenticationResult
        {
            IsSuccessful = true,
            User = user,
            SessionToken = sessionToken,
            SessionId = sessionId,
            ExpirationTime = expirationTime,
            IsNewUser = isNewUser
        };
    }

    /// <summary>
    /// 创建失败的认证结果
    /// </summary>
    public static AuthenticationResult Failure(string errorMessage, string? errorCode = null)
    {
        return new AuthenticationResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 会话验证结果
/// </summary>
public class SessionValidationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 令牌是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 验证失败原因
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    public static SessionValidationResult Success(Guid userId, string sessionId, DateTime expirationTime)
    {
        return new SessionValidationResult
        {
            IsValid = true,
            UserId = userId,
            SessionId = sessionId,
            ExpirationTime = expirationTime
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    public static SessionValidationResult Failure(string failureReason, bool isExpired = false)
    {
        return new SessionValidationResult
        {
            IsValid = false,
            IsExpired = isExpired,
            FailureReason = failureReason
        };
    }
}

/// <summary>
/// 会话刷新结果
/// </summary>
public class SessionRefreshResult
{
    /// <summary>
    /// 刷新是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 新的会话令牌
    /// </summary>
    public string? NewSessionToken { get; set; }

    /// <summary>
    /// 新的过期时间
    /// </summary>
    public DateTime? NewExpirationTime { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功的刷新结果
    /// </summary>
    public static SessionRefreshResult Success(string newSessionToken, DateTime newExpirationTime, Guid userId, string sessionId)
    {
        return new SessionRefreshResult
        {
            IsSuccessful = true,
            NewSessionToken = newSessionToken,
            NewExpirationTime = newExpirationTime,
            UserId = userId,
            SessionId = sessionId
        };
    }

    /// <summary>
    /// 创建失败的刷新结果
    /// </summary>
    public static SessionRefreshResult Failure(string errorMessage)
    {
        return new SessionRefreshResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// 会话创建结果
/// </summary>
public class SessionCreationResult
{
    /// <summary>
    /// 创建是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 会话令牌
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功的会话创建结果
    /// </summary>
    public static SessionCreationResult Success(string sessionToken, string sessionId, DateTime expirationTime, Guid userId)
    {
        return new SessionCreationResult
        {
            IsSuccessful = true,
            SessionToken = sessionToken,
            SessionId = sessionId,
            ExpirationTime = expirationTime,
            UserId = userId
        };
    }

    /// <summary>
    /// 创建失败的会话创建结果
    /// </summary>
    public static SessionCreationResult Failure(string errorMessage)
    {
        return new SessionCreationResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// 用户会话信息
/// </summary>
public class UserSession
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 机器ID
    /// </summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpirationTime { get; set; }

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 是否自动创建
    /// </summary>
    public bool IsAutoCreated { get; set; }
}