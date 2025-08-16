using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts; // corrected namespace
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 用户管理服务（业务层）
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserDataService _userDataService;
    private readonly IUserSecurityLogRepository _securityLogRepository;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IUserDataService userDataService, IUserSecurityLogRepository securityLogRepository, ILogger<UserManagementService> logger)
    {
        _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
        _securityLogRepository = securityLogRepository ?? throw new ArgumentNullException(nameof(securityLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取用户档案信息
    /// </summary>
    public async Task<UserProfileResult> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return UserProfileResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserProfileResult.Failure("用户不存在", "USER_NOT_FOUND");
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "查看用户档案", cancellationToken: cancellationToken);
            return UserProfileResult.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户档案失败: {UserId}", userId);
            return UserProfileResult.Failure("获取用户档案信息失败", "GET_PROFILE_ERROR");
        }
    }

    /// <summary>
    /// 更新用户档案信息
    /// </summary>
    public async Task<UserProfileUpdateResult> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return UserProfileUpdateResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        ArgumentNullException.ThrowIfNull(request);
        var validation = request.Validate();
        if (!validation.IsValid) return UserProfileUpdateResult.ValidationFailure(validation.Errors);
        try
        {
            var existing = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (existing == null) return UserProfileUpdateResult.Failure("用户不存在", "USER_NOT_FOUND");
            // 唯一性检查
            if (!string.IsNullOrWhiteSpace(request.Username) && !string.Equals(request.Username, existing.Username, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userDataService.UsernameExistsAsync(request.Username, userId, cancellationToken))
                    throw new UsernameAlreadyExistsException(request.Username);
                existing.Username = request.Username;
            }
            if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email, existing.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userDataService.EmailExistsAsync(request.Email, userId, cancellationToken))
                    throw new EmailAlreadyExistsException(request.Email);
                existing.UpdateEmail(request.Email);
            }
            existing.IncrementVersion();
            await _userDataService.UpdateUserProfileAsync(existing, cancellationToken);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "更新用户档案", cancellationToken: cancellationToken);
            return UserProfileUpdateResult.Success(existing);
        }
        catch (UsernameAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "用户名已存在");
            return UserProfileUpdateResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "邮箱已存在");
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
        ArgumentNullException.ThrowIfNull(request);
        var validation = request.Validate();
        if (!validation.IsValid) return UserCreationResult.ValidationFailure(validation.Errors);
        try
        {
            if (await _userDataService.UsernameExistsAsync(request.Username, null, cancellationToken))
                throw new UsernameAlreadyExistsException(request.Username);
            if (await _userDataService.EmailExistsAsync(request.Email, null, cancellationToken))
                throw new EmailAlreadyExistsException(request.Email);
            // 使用显式构造函数确保用户名按请求保留
            var security = new Lorn.OpenAgenticAI.Domain.Models.ValueObjects.SecuritySettings(
                authenticationMethod: "Created",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow,
                additionalSettings: new Dictionary<string, string>());
            var profile = new UserProfile(Guid.NewGuid(), request.Username, request.Email, security);
            var created = await _userDataService.CreateUserProfileAsync(profile, cancellationToken);
            await LogUserOperationAsync(created.UserId, SecurityEventType.UserCreated, "创建新用户", cancellationToken: cancellationToken);
            return UserCreationResult.Success(created);
        }
        catch (UsernameAlreadyExistsException ex)
        {
            return UserCreationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return UserCreationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Username}", request.Username);
            return UserCreationResult.Failure("创建用户失败", "CREATE_USER_ERROR");
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<UserDeletionResult> DeleteUserAsync(Guid userId, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return UserDeletionResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var existing = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (existing == null) return UserDeletionResult.Failure("用户不存在", "USER_NOT_FOUND");
            var ok = await _userDataService.DeleteUserProfileAsync(userId, cancellationToken);
            if (!ok) return UserDeletionResult.Failure("删除用户失败", "DELETE_USER_ERROR");
            if (hardDelete)
            {
                await _securityLogRepository.DeleteUserLogsAsync(userId, cancellationToken);
                await _securityLogRepository.SaveChangesAsync(cancellationToken);
            }
            await LogUserOperationAsync(userId, SecurityEventType.UserDeleted, "删除用户", cancellationToken: cancellationToken);
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
        if (userId == Guid.Empty) return UserOperationResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
            if (user.IsActive) return UserOperationResult.Success(userId, "激活用户");
            user.Activate();
            await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "激活用户", cancellationToken: cancellationToken);
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
        if (userId == Guid.Empty) return UserOperationResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
            if (!user.IsActive) return UserOperationResult.Success(userId, "停用用户");
            user.Deactivate();
            await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "停用用户", cancellationToken: cancellationToken);
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
    /// 设置默认用户
    /// </summary>
    public async Task<UserOperationResult> SetDefaultUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return UserOperationResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
            // 清除其他默认用户（业务层可额外扩展; 目前简单实现）
            var all = await _userDataService.GetAllUserProfilesAsync(true, cancellationToken);
            foreach (var u in all.Where(u => u.IsDefault && u.UserId != userId))
            {
                u.UnsetAsDefault();
                await _userDataService.UpdateUserProfileAsync(u, cancellationToken);
            }
            user.SetAsDefault();
            await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
            await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "设置默认用户", cancellationToken: cancellationToken);
            return UserOperationResult.Success(userId, "设置默认用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置默认用户失败: {UserId}", userId);
            return UserOperationResult.Failure("设置默认用户失败", "SET_DEFAULT_USER_ERROR");
        }
    }

    /// <summary>
    /// 验证用户名是否可用
    /// </summary>
    public Task<UsernameValidationResult> ValidateUsernameAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => InternalValidateUsernameAsync(username, excludeUserId, cancellationToken);

    private async Task<UsernameValidationResult> InternalValidateUsernameAsync(string username, Guid? excludeUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username)) return UsernameValidationResult.NotAvailable("用户名不能为空");
        if (username.Length < 3 || username.Length > 50) return UsernameValidationResult.NotAvailable("用户名长度必须在3-50个字符之间");
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$")) return UsernameValidationResult.NotAvailable("用户名只能包含字母、数字、下划线和连字符");
        var exists = await _userDataService.UsernameExistsAsync(username, excludeUserId, ct);
        if (exists)
        {
            var suggestions = await GenerateUsernameSuggestions(username, ct);
            return UsernameValidationResult.NotAvailable("用户名已存在", suggestions);
        }
        return UsernameValidationResult.Available();
    }

    /// <summary>
    /// 验证邮箱是否可用
    /// </summary>
    public Task<EmailValidationResult> ValidateEmailAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => InternalValidateEmailAsync(email, excludeUserId, cancellationToken);

    private async Task<EmailValidationResult> InternalValidateEmailAsync(string email, Guid? excludeUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return EmailValidationResult.NotAvailable("邮箱地址不能为空");
        try { var addr = new System.Net.Mail.MailAddress(email); if (addr.Address != email) return EmailValidationResult.NotAvailable("邮箱地址格式无效"); }
        catch { return EmailValidationResult.NotAvailable("邮箱地址格式无效"); }
        var exists = await _userDataService.EmailExistsAsync(email, excludeUserId, ct);
        return exists ? EmailValidationResult.NotAvailable("邮箱地址已存在") : EmailValidationResult.Available();
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<UserListResult> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PageIndex < 0) return UserListResult.Failure("页索引不能为负数", "INVALID_PAGE_INDEX");
        if (request.PageSize <= 0 || request.PageSize > 100) return UserListResult.Failure("页大小必须在1-100之间", "INVALID_PAGE_SIZE");
        try
        {
            var all = await _userDataService.GetAllUserProfilesAsync(!request.ActiveOnly, cancellationToken);
            IEnumerable<UserProfile> filtered = all;
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var kw = request.SearchKeyword.ToLowerInvariant();
                filtered = filtered.Where(u => u.Username.ToLowerInvariant().Contains(kw) || u.Email.ToLowerInvariant().Contains(kw));
            }
            filtered = ApplySorting(filtered, request.SortBy, request.SortDescending);
            var total = filtered.Count();
            var page = filtered.Skip(request.PageIndex * request.PageSize).Take(request.PageSize);
            return UserListResult.Success(page, total, request.PageIndex, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return UserListResult.Failure("获取用户列表失败", "GET_USERS_ERROR");
        }
    }

    /// <summary>
    /// 获取用户操作历史
    /// </summary>
    public async Task<UserOperationHistoryResult> GetUserOperationHistoryAsync(Guid userId, GetOperationHistoryRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return UserOperationHistoryResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserOperationHistoryResult.Failure("用户不存在", "USER_NOT_FOUND");
            IEnumerable<SecurityEventType>? eventTypes = null;
            if (request.EventTypes.Count > 0)
            {
                eventTypes = request.EventTypes.Select(et => Enum.TryParse<SecurityEventType>(et, out var ev) ? ev : (SecurityEventType?)null).Where(e => e.HasValue).Select(e => e!.Value);
            }
            var logs = await _securityLogRepository.GetUserLogsAsync(userId, request.FromDate, request.ToDate, eventTypes, null, request.PageIndex, request.PageSize, cancellationToken);
            var total = await _securityLogRepository.GetUserLogCountAsync(userId, request.FromDate, request.ToDate, eventTypes, null, cancellationToken);
            return UserOperationHistoryResult.Success(logs, total, request.PageIndex, request.PageSize);
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
        if (userId == Guid.Empty) return UserStatisticsResult.Failure("用户ID不能为空", "INVALID_USER_ID");
        try
        {
            var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (user == null) return UserStatisticsResult.Failure("用户不存在", "USER_NOT_FOUND");
            var lastLogin = await _securityLogRepository.GetLastLoginTimeAsync(userId, cancellationToken);
            var lastActivity = await _securityLogRepository.GetLastActivityTimeAsync(userId, cancellationToken);
            var totalLogins = await _securityLogRepository.GetUserLogCountAsync(userId, eventTypes: new[] { SecurityEventType.UserLogin }, cancellationToken: cancellationToken);
            var totalOps = await _securityLogRepository.GetUserLogCountAsync(userId, cancellationToken: cancellationToken);
            var activeDays = await CalculateActiveDays(userId, cancellationToken);
            var preferencesCount = user.UserPreferences?.Count ?? 0;
            var workflowTemplatesCount = user.WorkflowTemplates?.Count ?? 0;
            return UserStatisticsResult.Success(user.CreatedTime, lastLogin, lastActivity, totalLogins, totalOps, activeDays, preferencesCount, workflowTemplatesCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计信息失败: {UserId}", userId);
            return UserStatisticsResult.Failure("获取用户统计信息失败", "GET_USER_STATISTICS_ERROR");
        }
    }

    /// <summary>
    /// 修改用户名
    /// </summary>
    public async Task<UsernameValidationResult> ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default)
    {
        var validation = await InternalValidateUsernameAsync(newUsername, userId, cancellationToken);
        if (!validation.IsAvailable) return validation;
        var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
        if (user == null) return UsernameValidationResult.NotAvailable("用户不存在");
        user.Username = newUsername;
        await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
        await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "修改用户名", cancellationToken: cancellationToken);
        return UsernameValidationResult.Available();
    }

    /// <summary>
    /// 修改邮箱
    /// </summary>
    public async Task<EmailValidationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        var validation = await InternalValidateEmailAsync(newEmail, userId, cancellationToken);
        if (!validation.IsAvailable) return validation;
        var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
        if (user == null) return EmailValidationResult.NotAvailable("用户不存在");
        user.UpdateEmail(newEmail);
        await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
        await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "修改邮箱", cancellationToken: cancellationToken);
        return EmailValidationResult.Available();
    }

    /// <summary>
    /// 更新用户偏好设置
    /// </summary>
    public async Task<UserOperationResult> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
        if (user == null) return UserOperationResult.Failure("用户不存在", "USER_NOT_FOUND");
        // 删除项
        if (request.RemoveItems != null)
        {
            foreach (var cat in request.RemoveItems)
            {
                foreach (var key in cat.Value)
                {
                    var toRemove = user.UserPreferences.FirstOrDefault(p => p.PreferenceCategory == cat.Key && p.PreferenceKey == key);
                    if (toRemove != null) user.UserPreferences.Remove(toRemove);
                }
                if (cat.Value.Count == 0)
                {
                    // 删除整个类别
                    var allCat = user.UserPreferences.Where(p => p.PreferenceCategory == cat.Key).ToList();
                    foreach (var p in allCat) user.UserPreferences.Remove(p);
                }
            }
        }
        // 添加/更新
        foreach (var cat in request.Preferences)
        {
            foreach (var kv in cat.Value)
            {
                var existing = user.UserPreferences.FirstOrDefault(p => p.PreferenceCategory == cat.Key && p.PreferenceKey == kv.Key);
                if (existing != null)
                {
                    existing.UpdateValue(kv.Value?.ToString() ?? string.Empty);
                }
                else
                {
                    user.UserPreferences.Add(new UserPreferences(user.UserId, cat.Key, kv.Key, kv.Value?.ToString() ?? string.Empty, "String", false, null));
                }
            }
        }
        await _userDataService.UpdateUserProfileAsync(user, cancellationToken);
        await LogUserOperationAsync(userId, SecurityEventType.UserProfileUpdated, "更新偏好设置", cancellationToken: cancellationToken);
        return UserOperationResult.Success(userId, "更新偏好设置");
    }

    #region 私有辅助方法

    /// <summary>
    /// 记录用户操作日志
    /// </summary>
    private async Task LogUserOperationAsync(Guid userId, SecurityEventType eventType, string description, string? eventDetails = null, bool isSuccessful = true, string? errorCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var securityLog = UserSecurityLog.CreateOperationLog(userId, eventType, description, eventDetails, Environment.MachineName, null, isSuccessful, errorCode);
            await _securityLogRepository.AddAsync(securityLog, cancellationToken);
            await _securityLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录用户操作日志失败: {UserId}, {EventType}", userId, eventType);
        }
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private static IEnumerable<UserProfile> ApplySorting(IEnumerable<UserProfile> users, string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return sortDescending ? users.OrderByDescending(u => u.CreatedTime) : users.OrderBy(u => u.CreatedTime);
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
            var all = await _userDataService.GetAllUserProfilesAsync(true, cancellationToken);
            var set = new HashSet<string>(all.Select(u => u.Username), StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i <= 5; i++) { var s = baseUsername + i; if (!set.Contains(s)) suggestions.Add(s); }
            for (int i = 1; i <= 3; i++) { var s = baseUsername + "_" + i; if (!set.Contains(s)) suggestions.Add(s); }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "生成用户名建议失败: {Base}", baseUsername); }
        return suggestions;
    }

    /// <summary>
    /// 计算用户活跃天数
    /// </summary>
    private async Task<int> CalculateActiveDays(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var logs = await _securityLogRepository.GetUserLogsAsync(userId, pageIndex: 0, pageSize: int.MaxValue, cancellationToken: cancellationToken);
            return logs.Select(l => l.Timestamp.Date).Distinct().Count();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "计算活跃天数失败: {UserId}", userId); return 0; }
    }

    #endregion
}