using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

// --------------------------------------------------------------------------------------------------
// Layer Responsibility Notes
// IUserManagementService 处于应用服务/业务编排层：
// * 负责：输入校验、唯一性检查、业务规则、合并/批量偏好、调用安全/操作日志记录、默认用户互斥逻辑。
// * 不做：直接持久化细节（委托给 IUserDataService），运行期会话缓存（由 IUserContextService 维护）。
// * 方向：UI / 工作流 -> IUserManagementService -> IUserDataService -> Repositories
// --------------------------------------------------------------------------------------------------

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 用户管理服务接口，定义用户管理契约 (业务层) —— 依赖 IUserDataService 实现数据持久化。
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 获取用户档案信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案信息</returns>
    Task<UserProfileResult> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新用户（包含输入验证、用户名/邮箱唯一性检查、初始化默认偏好等）
    /// </summary>
    /// <param name="request">创建用户请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    Task<UserCreationResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户档案（支持局部字段更新 + 业务验证）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<UserProfileUpdateResult> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 激活用户（可能触发安全日志、事件发布）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<UserOperationResult> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停用用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<UserOperationResult> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将指定用户设为默认用户（调度到数据层 SetDefaultUser 并处理原默认用户状态）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<UserOperationResult> SetDefaultUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改用户名（包含合法性与唯一性检查）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="newUsername">新用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<UsernameValidationResult> ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改邮箱地址（包含格式与唯一性检查）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="newEmail">新邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<EmailValidationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户（区分软/硬删除，进行关联资源校验）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="hardDelete">是否硬删除（true：物理删除，false：软删除）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    Task<UserDeletionResult> DeleteUserAsync(Guid userId, bool hardDelete = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分页用户列表（含过滤/排序封装）
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<UserListResult> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户名是否可用（包装数据层 Exists 查询 + 建议生成）
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<UsernameValidationResult> ValidateUsernameAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证邮箱是否可用
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<EmailValidationResult> ValidateEmailAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新（合并式）用户偏好设置（高层批量 -> 拆分调用数据层 SaveUserPreferencesBatchAsync）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<UserOperationResult> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户操作历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作历史</returns>
    Task<UserOperationHistoryResult> GetUserOperationHistoryAsync(Guid userId, GetOperationHistoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<UserStatisticsResult> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户档案结果
/// </summary>
public class UserProfileResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 用户档案
    /// </summary>
    public UserProfile? UserProfile { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserProfileResult Success(UserProfile userProfile)
    {
        return new UserProfileResult
        {
            IsSuccessful = true,
            UserProfile = userProfile
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserProfileResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserProfileResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 更新用户档案请求
/// </summary>
public class UpdateUserProfileRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 个人描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 验证请求数据
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(Username))
        {
            if (Username.Length < 3 || Username.Length > 50)
                errors.Add("用户名长度必须在3-50个字符之间");

            if (!IsValidUsername(Username))
                errors.Add("用户名只能包含字母、数字、下划线和连字符");
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            if (!IsValidEmail(Email))
                errors.Add("邮箱地址格式无效");
        }

        if (!string.IsNullOrWhiteSpace(DisplayName) && DisplayName.Length > 100)
            errors.Add("显示名称不能超过100个字符");

        if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 500)
            errors.Add("个人描述不能超过500个字符");

        return new ValidationResult(errors);
    }

    private static bool IsValidUsername(string username)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 用户档案更新结果
/// </summary>
public class UserProfileUpdateResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 更新后的用户档案
    /// </summary>
    public UserProfile? UpdatedProfile { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserProfileUpdateResult Success(UserProfile updatedProfile)
    {
        return new UserProfileUpdateResult
        {
            IsSuccessful = true,
            UpdatedProfile = updatedProfile
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserProfileUpdateResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserProfileUpdateResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 创建验证失败结果
    /// </summary>
    public static UserProfileUpdateResult ValidationFailure(List<string> validationErrors)
    {
        return new UserProfileUpdateResult
        {
            IsSuccessful = false,
            ValidationErrors = validationErrors,
            ErrorMessage = "数据验证失败"
        };
    }
}

/// <summary>
/// 创建用户请求
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 个人描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 机器ID
    /// </summary>
    public string? MachineId { get; set; }

    /// <summary>
    /// 是否设为默认用户
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 验证请求数据
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("用户名不能为空");
        else
        {
            if (Username.Length < 3 || Username.Length > 50)
                errors.Add("用户名长度必须在3-50个字符之间");

            if (!System.Text.RegularExpressions.Regex.IsMatch(Username, @"^[a-zA-Z0-9_-]+$"))
                errors.Add("用户名只能包含字母、数字、下划线和连字符");
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(Email);
                if (addr.Address != Email)
                    errors.Add("邮箱地址格式无效");
            }
            catch
            {
                errors.Add("邮箱地址格式无效");
            }
        }

        if (!string.IsNullOrWhiteSpace(DisplayName) && DisplayName.Length > 100)
            errors.Add("显示名称不能超过100个字符");

        if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 500)
            errors.Add("个人描述不能超过500个字符");

        return new ValidationResult(errors);
    }
}

/// <summary>
/// 用户创建结果
/// </summary>
public class UserCreationResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 创建的用户档案
    /// </summary>
    public UserProfile? CreatedUser { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserCreationResult Success(UserProfile createdUser)
    {
        return new UserCreationResult
        {
            IsSuccessful = true,
            CreatedUser = createdUser,
            UserId = createdUser.UserId
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserCreationResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserCreationResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 创建验证失败结果
    /// </summary>
    public static UserCreationResult ValidationFailure(List<string> validationErrors)
    {
        return new UserCreationResult
        {
            IsSuccessful = false,
            ValidationErrors = validationErrors,
            ErrorMessage = "数据验证失败"
        };
    }
}

/// <summary>
/// 用户删除结果
/// </summary>
public class UserDeletionResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 删除的用户ID
    /// </summary>
    public Guid? DeletedUserId { get; set; }

    /// <summary>
    /// 是否为硬删除
    /// </summary>
    public bool IsHardDelete { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserDeletionResult Success(Guid deletedUserId, bool isHardDelete)
    {
        return new UserDeletionResult
        {
            IsSuccessful = true,
            DeletedUserId = deletedUserId,
            IsHardDelete = isHardDelete
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserDeletionResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserDeletionResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 用户操作结果
/// </summary>
public class UserOperationResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 操作的用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserOperationResult Success(Guid userId, string operationType)
    {
        return new UserOperationResult
        {
            IsSuccessful = true,
            UserId = userId,
            OperationType = operationType
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserOperationResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserOperationResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 获取用户列表请求
/// </summary>
public class GetUsersRequest
{
    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 是否只获取活跃用户
    /// </summary>
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// 搜索关键词（用户名或邮箱）
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; } = "CreatedTime";

    /// <summary>
    /// 是否降序排列
    /// </summary>
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// 用户列表结果
/// </summary>
public class UserListResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    public IEnumerable<UserProfile> Users { get; set; } = [];

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 页索引
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserListResult Success(IEnumerable<UserProfile> users, int totalCount, int pageIndex, int pageSize)
    {
        return new UserListResult
        {
            IsSuccessful = true,
            Users = users,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserListResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserListResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 用户名验证结果
/// </summary>
public class UsernameValidationResult
{
    /// <summary>
    /// 用户名是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 建议的用户名（如果当前用户名不可用）
    /// </summary>
    public List<string> SuggestedUsernames { get; set; } = [];

    /// <summary>
    /// 创建可用结果
    /// </summary>
    public static UsernameValidationResult Available()
    {
        return new UsernameValidationResult
        {
            IsAvailable = true,
            Message = "用户名可用"
        };
    }

    /// <summary>
    /// 创建不可用结果
    /// </summary>
    public static UsernameValidationResult NotAvailable(string message, List<string>? suggestions = null)
    {
        return new UsernameValidationResult
        {
            IsAvailable = false,
            Message = message,
            SuggestedUsernames = suggestions ?? []
        };
    }
}

/// <summary>
/// 邮箱验证结果
/// </summary>
public class EmailValidationResult
{
    /// <summary>
    /// 邮箱是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 创建可用结果
    /// </summary>
    public static EmailValidationResult Available()
    {
        return new EmailValidationResult
        {
            IsAvailable = true,
            Message = "邮箱地址可用"
        };
    }

    /// <summary>
    /// 创建不可用结果
    /// </summary>
    public static EmailValidationResult NotAvailable(string message)
    {
        return new EmailValidationResult
        {
            IsAvailable = false,
            Message = message
        };
    }
}

/// <summary>
/// 获取操作历史请求
/// </summary>
public class GetOperationHistoryRequest
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// 事件类型过滤
    /// </summary>
    public List<string> EventTypes { get; set; } = [];

    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// 用户操作历史结果
/// </summary>
public class UserOperationHistoryResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 操作历史列表
    /// </summary>
    public IEnumerable<UserSecurityLog> OperationHistory { get; set; } = [];

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 页索引
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserOperationHistoryResult Success(IEnumerable<UserSecurityLog> history, int totalCount, int pageIndex, int pageSize)
    {
        return new UserOperationHistoryResult
        {
            IsSuccessful = true,
            OperationHistory = history,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserOperationHistoryResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserOperationHistoryResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 用户统计信息结果
/// </summary>
public class UserStatisticsResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 用户创建时间
    /// </summary>
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime? LastActivityTime { get; set; }

    /// <summary>
    /// 总登录次数
    /// </summary>
    public int TotalLogins { get; set; }

    /// <summary>
    /// 总操作次数
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// 活跃天数
    /// </summary>
    public int ActiveDays { get; set; }

    /// <summary>
    /// 偏好设置数量
    /// </summary>
    public int PreferencesCount { get; set; }

    /// <summary>
    /// 工作流模板数量
    /// </summary>
    public int WorkflowTemplatesCount { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static UserStatisticsResult Success(
        DateTime createdTime,
        DateTime? lastLoginTime,
        DateTime? lastActivityTime,
        int totalLogins,
        int totalOperations,
        int activeDays,
        int preferencesCount,
        int workflowTemplatesCount)
    {
        return new UserStatisticsResult
        {
            IsSuccessful = true,
            CreatedTime = createdTime,
            LastLoginTime = lastLoginTime,
            LastActivityTime = lastActivityTime,
            TotalLogins = totalLogins,
            TotalOperations = totalOperations,
            ActiveDays = activeDays,
            PreferencesCount = preferencesCount,
            WorkflowTemplatesCount = workflowTemplatesCount
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static UserStatisticsResult Failure(string errorMessage, string? errorCode = null)
    {
        return new UserStatisticsResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> Errors { get; }

    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? [];
    }
}

/// <summary>
/// 更新偏好设置请求（键值合并语义）
/// </summary>
public class UpdatePreferencesRequest
{
    /// <summary>
    /// 偏好集合: category -> (key -> value)
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> Preferences { get; set; } = new();

    /// <summary>
    /// 可选需要删除的项目: category -> keys (为空表示整个类别删除)
    /// </summary>
    public Dictionary<string, List<string>>? RemoveItems { get; set; }

    /// <summary>
    /// 验证
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        foreach (var cat in Preferences.Keys)
        {
            if (string.IsNullOrWhiteSpace(cat)) errors.Add("Category 不能为空");
        }
        return new ValidationResult(errors);
    }
}