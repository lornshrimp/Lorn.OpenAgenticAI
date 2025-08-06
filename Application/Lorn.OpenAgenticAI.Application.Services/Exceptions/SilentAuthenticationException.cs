namespace Lorn.OpenAgenticAI.Application.Services.Exceptions;

/// <summary>
/// 静默认证异常基类
/// </summary>
public abstract class SilentAuthenticationException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; }

    protected SilentAuthenticationException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected SilentAuthenticationException(string message, Exception innerException, string? errorCode = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// 用户创建异常
/// </summary>
public class UserCreationException : SilentAuthenticationException
{
    public UserCreationException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserCreationException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 会话过期异常
/// </summary>
public class SessionExpiredException : SilentAuthenticationException
{
    public SessionExpiredException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public SessionExpiredException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 重复用户异常
/// </summary>
public class DuplicateUserException : SilentAuthenticationException
{
    public DuplicateUserException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public DuplicateUserException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 机器ID不匹配异常
/// </summary>
public class MachineIdMismatchException : SilentAuthenticationException
{
    public MachineIdMismatchException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public MachineIdMismatchException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 用户切换异常
/// </summary>
public class UserSwitchException : SilentAuthenticationException
{
    public UserSwitchException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserSwitchException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 会话令牌无效异常
/// </summary>
public class InvalidSessionTokenException : SilentAuthenticationException
{
    public InvalidSessionTokenException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public InvalidSessionTokenException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}