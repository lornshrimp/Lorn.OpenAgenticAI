namespace Lorn.OpenAgenticAI.Application.Services.Exceptions;

/// <summary>
/// 用户上下文异常
/// </summary>
public class UserContextException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// 初始化用户上下文异常
    /// </summary>
    /// <param name="message">错误消息</param>
    public UserContextException(string message) : base(message)
    {
    }

    /// <summary>
    /// 初始化用户上下文异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public UserContextException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 初始化用户上下文异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    public UserContextException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 初始化用户上下文异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="errorCode">错误代码</param>
    public UserContextException(string message, Exception innerException, string errorCode) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}