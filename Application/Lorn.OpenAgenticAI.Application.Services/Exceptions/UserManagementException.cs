namespace Lorn.OpenAgenticAI.Application.Services.Exceptions;

/// <summary>
/// 用户管理异常基类
/// </summary>
public abstract class UserManagementException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; }

    protected UserManagementException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected UserManagementException(string message, Exception innerException, string? errorCode = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// 用户不存在异常
/// </summary>
public class UserNotFoundException : UserManagementException
{
    public UserNotFoundException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserNotFoundException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public UserNotFoundException(Guid userId) : base($"用户不存在: {userId}", "USER_NOT_FOUND")
    {
    }
}

/// <summary>
/// 用户名已存在异常
/// </summary>
public class UsernameAlreadyExistsException : UserManagementException
{
    public UsernameAlreadyExistsException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UsernameAlreadyExistsException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public UsernameAlreadyExistsException(string username) : base($"用户名已存在: {username}", "USERNAME_EXISTS")
    {
    }
}

/// <summary>
/// 邮箱已存在异常
/// </summary>
public class EmailAlreadyExistsException : UserManagementException
{
    public EmailAlreadyExistsException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public EmailAlreadyExistsException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public EmailAlreadyExistsException(string email) : base($"邮箱地址已存在: {email}", "EMAIL_EXISTS")
    {
    }
}

/// <summary>
/// 用户验证异常
/// </summary>
public class UserValidationException : UserManagementException
{
    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> ValidationErrors { get; }

    public UserValidationException(List<string> validationErrors, string? errorCode = null)
        : base($"用户数据验证失败: {string.Join(", ", validationErrors)}", errorCode)
    {
        ValidationErrors = validationErrors;
    }

    public UserValidationException(string validationError, string? errorCode = null)
        : base($"用户数据验证失败: {validationError}", errorCode)
    {
        ValidationErrors = [validationError];
    }
}

/// <summary>
/// 用户操作异常
/// </summary>
public class UserOperationException : UserManagementException
{
    public UserOperationException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserOperationException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }
}

/// <summary>
/// 用户删除异常
/// </summary>
public class UserDeletionException : UserManagementException
{
    public UserDeletionException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserDeletionException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public UserDeletionException(Guid userId, string reason) : base($"无法删除用户 {userId}: {reason}", "USER_DELETION_FAILED")
    {
    }
}

/// <summary>
/// 用户状态异常
/// </summary>
public class UserStateException : UserManagementException
{
    public UserStateException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserStateException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public UserStateException(Guid userId, string currentState, string requestedOperation)
        : base($"用户 {userId} 当前状态为 {currentState}，无法执行 {requestedOperation} 操作", "INVALID_USER_STATE")
    {
    }
}

/// <summary>
/// 用户权限异常
/// </summary>
public class UserPermissionException : UserManagementException
{
    public UserPermissionException(string message, string? errorCode = null) : base(message, errorCode)
    {
    }

    public UserPermissionException(string message, Exception innerException, string? errorCode = null) : base(message, innerException, errorCode)
    {
    }

    public UserPermissionException(Guid userId, string operation)
        : base($"用户 {userId} 没有权限执行 {operation} 操作", "INSUFFICIENT_PERMISSIONS")
    {
    }
}