using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 用户管理服务实现类，提供用户信息CRUD操作
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSecurityLogRepository _securityLogRepository;
    private readonly ILogger<UserManagementService> _logger;

    /// <summary>
    /// 初始化用户管理服务
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="securityLogRepository">安全日志仓储</param>
    /// <param name="logger">日志记录器</param>
    public UserManagementService(
        IUserRepository userRepository,
        IUserSecurityLogRepository securityLogRepository,
        ILogger<UserManagementService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _securityLogRepository = securityLogRepository ?? throw new ArgumentNullException(nameof(securityLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取用户档案信息
    /// </summary>
    public async Task<UserProfileResult> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取用户档案信息: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserProfileResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            var userProfile = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (userProfile == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", userId);
                return UserProfileResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            // 记录操作日志
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "查看用户档案", cancellationToken: cancellationToken);

            _logger.LogDebug("成功获取用户档案信息: {Username} ({UserId})", userProfile.Username, userId);
            return UserProfileResult.Success(userProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户档案信息失败: {UserId}", userId);
            return UserProfileResult.Failure("获取用户档案信息失败", "GET_PROFILE_ERROR");
        }
    }

    /// <summary>
    /// 更新用户档案信息
    /// </summary>
    public async Task<UserProfileUpdateResult> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("更新用户档案信息: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserProfileUpdateResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            ArgumentNullException.ThrowIfNull(request);

            // 验证请求数据
            var validationResult = request.Validate();
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("用户档案更新数据验证失败: {UserId}, 错误: {Errors}", userId, string.Join(", ", validationResult.Errors));
                return UserProfileUpdateResult.ValidationFailure(validationResult.Errors);
            }

            // 获取现有用户
            var existingUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (existingUser == null)
            {
                _logger.LogWarning("要更新的用户不存在: {UserId}", userId);
                return UserProfileUpdateResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            // 检查用户名唯一性
            if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != existingUser.Username)
            {
                if (await _userRepository.IsUsernameExistsAsync(request.Username, userId, cancellationToken))
                {
                    _logger.LogWarning("用户名已存在: {Username}", request.Username);
                    return UserProfileUpdateResult.Failure("用户名已存在", "USERNAME_EXISTS");
                }
                existingUser.Username = request.Username;
            }

            // 检查邮箱唯一性
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != existingUser.Email)
            {
                if (await _userRepository.IsEmailExistsAsync(request.Email, userId, cancellationToken))
                {
                    _logger.LogWarning("邮箱地址已存在: {Email}", request.Email);
                    return UserProfileUpdateResult.Failure("邮箱地址已存在", "EMAIL_EXISTS");
                }
                existingUser.UpdateEmail(request.Email);
            }

            // 更新其他字段（这里需要根据实际的UserProfile模型来调整）
            // 注意：由于UserProfile模型中没有DisplayName、Description、AvatarUrl等字段，
            // 这些可能需要通过UserMetadataEntry来存储

            // 增加版本号
            existingUser.IncrementVersion();

            // 保存更改
            var updatedUser = await _userRepository.UpdateAsync(existingUser, cancellationToken);

            // 记录操作日志
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "更新用户档案", cancellationToken: cancellationToken);

            _logger.LogInformation("成功更新用户档案: {Username} ({UserId})", updatedUser.Username, userId);
            return UserProfileUpdateResult.Success(updatedUser);
        }
        catch (UsernameAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "用户名已存在: {UserId}", userId);
            return UserProfileUpdateResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "邮箱地址已存在: {UserId}", userId);
            return UserProfileUpdateResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户档案失败: {UserId}", userId);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "更新用户档案失败", isSuccessful: false, errorCode: "UPDATE_PROFILE_ERROR", cancellationToken: cancellationToken);
            return UserProfileUpdateResult.Failure("更新用户档案失败", "UPDATE_PROFILE_ERROR");
        }
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    public async Task<UserCreationResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("创建新用户: {Username}", request?.Username);

            ArgumentNullException.ThrowIfNull(request);

            // 验证请求数据
            var validationResult = request.Validate();
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("用户创建数据验证失败: {Username}, 错误: {Errors}", request.Username, string.Join(", ", validationResult.Errors));
                return UserCreationResult.ValidationFailure(validationResult.Errors);
            }

            // 检查用户名唯一性
            if (await _userRepository.IsUsernameExistsAsync(request.Username, null, cancellationToken))
            {
                _logger.LogWarning("用户名已存在: {Username}", request.Username);
                return UserCreationResult.Failure("用户名已存在", "USERNAME_EXISTS");
            }

            // 检查邮箱唯一性
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                await _userRepository.IsEmailExistsAsync(request.Email, null, cancellationToken))
            {
                _logger.LogWarning("邮箱地址已存在: {Email}", request.Email);
                return UserCreationResult.Failure("邮箱地址已存在", "EMAIL_EXISTS");
            }

            // 创建默认安全设置
            var securitySettings = new SecuritySettings(
                authenticationMethod: "Silent",
                sessionTimeoutMinutes: 1440, // 24小时
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow
            );

            // 创建新用户
            var newUser = new UserProfile(
                userId: Guid.NewGuid(),
                username: request.Username,
                email: request.Email ?? string.Empty,
                securitySettings: securitySettings
            );

            // 保存用户
            var createdUser = await _userRepository.AddAsync(newUser, cancellationToken);

            // 记录操作日志
            await LogUserOperationAsync(createdUser.UserId, SecurityEventType.UserCreated, "创建新用户", cancellationToken: cancellationToken);

            _logger.LogInformation("成功创建新用户: {Username} ({UserId})", createdUser.Username, createdUser.UserId);
            return UserCreationResult.Success(createdUser);
        }
        catch (UsernameAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "用户名已存在: {Username}", request?.Username);
            return UserCreationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "邮箱地址已存在: {Email}", request?.Email);
            return UserCreationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Username}", request?.Username);
            return UserCreationResult.Failure("创建用户失败", "CREATE_USER_ERROR");
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<UserDeletionResult> DeleteUserAsync(Guid userId, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("删除用户: {UserId}, 硬删除: {HardDelete}", userId, hardDelete);

            if (userId == Guid.Empty)
            {
                return UserDeletionResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            // 检查用户是否存在
            var existingUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (existingUser == null)
            {
                _logger.LogWarning("要删除的用户不存在: {UserId}", userId);
                return UserDeletionResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            bool deleteResult;
            if (hardDelete)
            {
                // 硬删除：物理删除用户及其所有相关数据
                deleteResult = await _userRepository.DeleteAsync(userId, cancellationToken);

                // 删除用户的所有安全日志
                await _securityLogRepository.DeleteUserLogsAsync(userId, cancellationToken);
                await _securityLogRepository.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // 软删除：设置用户为非活跃状态
                deleteResult = await _userRepository.SoftDeleteAsync(userId, cancellationToken);
            }

            if (!deleteResult)
            {
                _logger.LogWarning("删除用户失败: {UserId}", userId);
                return UserDeletionResult.Failure("删除用户失败", "DELETE_USER_ERROR");
            }

            // 记录操作日志（如果是软删除）
            if (!hardDelete)
            {
                await LogUserOperationAsync(userId, SecurityEventType.UserDeleted, "删除用户", cancellationToken: cancellationToken);
            }

            _logger.LogInformation("成功删除用户: {UserId}, 硬删除: {HardDelete}", userId, hardDelete);
            return UserDeletionResult.Success(userId, hardDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败: {UserId}", userId);
            await LogUserOperationAsync(userId, SecurityEventType.UserDeleted, "删除用户失败", isSuccessful: false, errorCode: "DELETE_USER_ERROR", cancellationToken: cancellationToken);
            return UserDeletionResult.Failure("删除用户失败", "DELETE_USER_ERROR");
        }
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    public async Task<UserOperationResult> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("激活用户: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserOperationResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("要激活的用户不存在: {UserId}", userId);
                return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            if (user.IsActive)
            {
                _logger.LogDebug("用户已经是激活状态: {UserId}", userId);
                return UserOperationResult.Success(userId, "激活用户");
            }

            user.Activate();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 记录操作日志
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "激活用户", cancellationToken: cancellationToken);

            _logger.LogInformation("成功激活用户: {UserId}", userId);
            return UserOperationResult.Success(userId, "激活用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活用户失败: {UserId}", userId);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "激活用户失败", isSuccessful: false, errorCode: "ACTIVATE_USER_ERROR", cancellationToken: cancellationToken);
            return UserOperationResult.Failure("激活用户失败", "ACTIVATE_USER_ERROR");
        }
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    public async Task<UserOperationResult> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("停用用户: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserOperationResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("要停用的用户不存在: {UserId}", userId);
                return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                _logger.LogDebug("用户已经是停用状态: {UserId}", userId);
                return UserOperationResult.Success(userId, "停用用户");
            }

            user.Deactivate();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 记录操作日志
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "停用用户", cancellationToken: cancellationToken);

            _logger.LogInformation("成功停用用户: {UserId}", userId);
            return UserOperationResult.Success(userId, "停用用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用用户失败: {UserId}", userId);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "停用用户失败", isSuccessful: false, errorCode: "DEACTIVATE_USER_ERROR", cancellationToken: cancellationToken);
            return UserOperationResult.Failure("停用用户失败", "DEACTIVATE_USER_ERROR");
        }
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<UserListResult> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取用户列表: 页索引 {PageIndex}, 页大小 {PageSize}, 仅活跃用户 {ActiveOnly}",
                request?.PageIndex, request?.PageSize, request?.ActiveOnly);

            ArgumentNullException.ThrowIfNull(request);

            if (request.PageIndex < 0)
            {
                return UserListResult.Failure("页索引不能为负数", "INVALID_PAGE_INDEX");
            }

            if (request.PageSize <= 0 || request.PageSize > 100)
            {
                return UserListResult.Failure("页大小必须在1-100之间", "INVALID_PAGE_SIZE");
            }

            var (users, totalCount) = await _userRepository.GetUsersPagedAsync(
                request.PageIndex,
                request.PageSize,
                request.ActiveOnly,
                cancellationToken);

            // 如果有搜索关键词，进行过滤
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.ToLowerInvariant();
                users = users.Where(u =>
                    u.Username.ToLowerInvariant().Contains(keyword) ||
                    u.Email.ToLowerInvariant().Contains(keyword));

                totalCount = users.Count();
            }

            // 应用排序
            users = ApplySorting(users, request.SortBy, request.SortDescending);

            _logger.LogDebug("成功获取用户列表: 找到 {Count} 个用户，总数 {TotalCount}", users.Count(), totalCount);
            return UserListResult.Success(users, totalCount, request.PageIndex, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return UserListResult.Failure("获取用户列表失败", "GET_USERS_ERROR");
        }
    }

    /// <summary>
    /// 验证用户名是否可用
    /// </summary>
    public async Task<UsernameValidationResult> ValidateUsernameAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("验证用户名可用性: {Username}, 排除用户: {ExcludeUserId}", username, excludeUserId);

            if (string.IsNullOrWhiteSpace(username))
            {
                return UsernameValidationResult.NotAvailable("用户名不能为空");
            }

            if (username.Length < 3 || username.Length > 50)
            {
                return UsernameValidationResult.NotAvailable("用户名长度必须在3-50个字符之间");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$"))
            {
                return UsernameValidationResult.NotAvailable("用户名只能包含字母、数字、下划线和连字符");
            }

            var exists = await _userRepository.IsUsernameExistsAsync(username, excludeUserId, cancellationToken);
            if (exists)
            {
                // 生成建议的用户名
                var suggestions = await GenerateUsernameSuggestions(username, cancellationToken);
                return UsernameValidationResult.NotAvailable("用户名已存在", suggestions);
            }

            _logger.LogDebug("用户名可用: {Username}", username);
            return UsernameValidationResult.Available();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证用户名失败: {Username}", username);
            return UsernameValidationResult.NotAvailable("验证用户名时发生错误");
        }
    }

    /// <summary>
    /// 验证邮箱是否可用
    /// </summary>
    public async Task<EmailValidationResult> ValidateEmailAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("验证邮箱可用性: {Email}, 排除用户: {ExcludeUserId}", email, excludeUserId);

            if (string.IsNullOrWhiteSpace(email))
            {
                return EmailValidationResult.NotAvailable("邮箱地址不能为空");
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    return EmailValidationResult.NotAvailable("邮箱地址格式无效");
                }
            }
            catch
            {
                return EmailValidationResult.NotAvailable("邮箱地址格式无效");
            }

            var exists = await _userRepository.IsEmailExistsAsync(email, excludeUserId, cancellationToken);
            if (exists)
            {
                return EmailValidationResult.NotAvailable("邮箱地址已存在");
            }

            _logger.LogDebug("邮箱地址可用: {Email}", email);
            return EmailValidationResult.Available();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证邮箱失败: {Email}", email);
            return EmailValidationResult.NotAvailable("验证邮箱时发生错误");
        }
    }

    /// <summary>
    /// 获取用户操作历史
    /// </summary>
    public async Task<UserOperationHistoryResult> GetUserOperationHistoryAsync(Guid userId, GetOperationHistoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取用户操作历史: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserOperationHistoryResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            ArgumentNullException.ThrowIfNull(request);

            // 检查用户是否存在
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return UserOperationHistoryResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            // 转换事件类型过滤器
            IEnumerable<SecurityEventType>? eventTypes = null;
            if (request.EventTypes.Count > 0)
            {
                eventTypes = request.EventTypes
                    .Select(et => Enum.TryParse<SecurityEventType>(et, out var eventType) ? eventType : (SecurityEventType?)null)
                    .Where(et => et.HasValue)
                    .Select(et => et!.Value);
            }

            // 获取操作历史
            var operationHistory = await _securityLogRepository.GetUserLogsAsync(
                userId,
                request.FromDate,
                request.ToDate,
                eventTypes,
                null, // 不过滤严重级别
                request.PageIndex,
                request.PageSize,
                cancellationToken);

            // 获取总数量
            var totalCount = await _securityLogRepository.GetUserLogCountAsync(
                userId,
                request.FromDate,
                request.ToDate,
                eventTypes,
                null,
                cancellationToken);

            _logger.LogDebug("成功获取用户操作历史: {UserId}, 找到 {Count} 条记录", userId, operationHistory.Count());
            return UserOperationHistoryResult.Success(operationHistory, totalCount, request.PageIndex, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户操作历史失败: {UserId}", userId);
            return UserOperationHistoryResult.Failure("获取用户操作历史失败", "GET_OPERATION_HISTORY_ERROR");
        }
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    public async Task<UserStatisticsResult> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取用户统计信息: {UserId}", userId);

            if (userId == Guid.Empty)
            {
                return UserStatisticsResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            // 检查用户是否存在
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return UserStatisticsResult.Failure("用户不存在", "USER_NOT_FOUND");
            }

            // 获取统计信息
            var lastLoginTime = await _securityLogRepository.GetLastLoginTimeAsync(userId, cancellationToken);
            var lastActivityTime = await _securityLogRepository.GetLastActivityTimeAsync(userId, cancellationToken);

            // 获取登录次数统计
            var loginEvents = new[] { SecurityEventType.UserLogin };
            var totalLogins = await _securityLogRepository.GetUserLogCountAsync(
                userId, eventTypes: loginEvents, cancellationToken: cancellationToken);

            // 获取总操作次数
            var totalOperations = await _securityLogRepository.GetUserLogCountAsync(userId, cancellationToken: cancellationToken);

            // 计算活跃天数（简化计算，基于日志记录）
            var activeDays = await CalculateActiveDays(userId, cancellationToken);

            // 获取偏好设置数量和工作流模板数量（这里简化处理）
            var preferencesCount = user.UserPreferences?.Count ?? 0;
            var workflowTemplatesCount = user.WorkflowTemplates?.Count ?? 0;

            var result = UserStatisticsResult.Success(
                user.CreatedTime,
                lastLoginTime,
                lastActivityTime,
                totalLogins,
                totalOperations,
                activeDays,
                preferencesCount,
                workflowTemplatesCount);

            _logger.LogDebug("成功获取用户统计信息: {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计信息失败: {UserId}", userId);
            return UserStatisticsResult.Failure("获取用户统计信息失败", "GET_USER_STATISTICS_ERROR");
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 记录用户操作日志
    /// </summary>
    private async Task LogUserOperationAsync(
        Guid userId,
        SecurityEventType eventType,
        string description,
        string? eventDetails = null,
        bool isSuccessful = true,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var securityLog = UserSecurityLog.CreateOperationLog(
                userId,
                eventType,
                description,
                eventDetails,
                machineId: Environment.MachineName,
                sessionId: null, // 可以从当前上下文获取
                isSuccessful,
                errorCode);

            await _securityLogRepository.AddAsync(securityLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录用户操作日志失败: {UserId}, {EventType}", userId, eventType);
            // 不抛出异常，避免影响主要业务流程
        }
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private static IEnumerable<UserProfile> ApplySorting(IEnumerable<UserProfile> users, string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return sortDescending ? users.OrderByDescending(u => u.CreatedTime) : users.OrderBy(u => u.CreatedTime);
        }

        return sortBy.ToLowerInvariant() switch
        {
            "username" => sortDescending ? users.OrderByDescending(u => u.Username) : users.OrderBy(u => u.Username),
            "email" => sortDescending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            "createdtime" => sortDescending ? users.OrderByDescending(u => u.CreatedTime) : users.OrderBy(u => u.CreatedTime),
            "lastlogintime" => sortDescending ? users.OrderByDescending(u => u.LastLoginTime) : users.OrderBy(u => u.LastLoginTime),
            _ => sortDescending ? users.OrderByDescending(u => u.CreatedTime) : users.OrderBy(u => u.CreatedTime)
        };
    }

    /// <summary>
    /// 生成用户名建议
    /// </summary>
    private async Task<List<string>> GenerateUsernameSuggestions(string baseUsername, CancellationToken cancellationToken)
    {
        var suggestions = new List<string>();

        try
        {
            // 生成数字后缀建议
            for (int i = 1; i <= 5; i++)
            {
                var suggestion = $"{baseUsername}{i}";
                if (!await _userRepository.IsUsernameExistsAsync(suggestion, null, cancellationToken))
                {
                    suggestions.Add(suggestion);
                }
            }

            // 生成下划线后缀建议
            for (int i = 1; i <= 3; i++)
            {
                var suggestion = $"{baseUsername}_{i}";
                if (!await _userRepository.IsUsernameExistsAsync(suggestion, null, cancellationToken))
                {
                    suggestions.Add(suggestion);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "生成用户名建议失败: {BaseUsername}", baseUsername);
        }

        return suggestions;
    }

    /// <summary>
    /// 计算用户活跃天数
    /// </summary>
    private async Task<int> CalculateActiveDays(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            // 获取用户的所有操作日志，按日期分组计算活跃天数
            var logs = await _securityLogRepository.GetUserLogsAsync(
                userId,
                pageIndex: 0,
                pageSize: int.MaxValue,
                cancellationToken: cancellationToken);

            var activeDates = logs
                .Select(log => log.Timestamp.Date)
                .Distinct()
                .Count();

            return activeDates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "计算用户活跃天数失败: {UserId}", userId);
            return 0;
        }
    }

    #endregion
}