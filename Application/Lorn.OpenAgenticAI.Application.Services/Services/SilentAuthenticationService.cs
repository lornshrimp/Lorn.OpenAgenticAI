using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 静默认证服务实现，处理自动用户创建和认证
/// </summary>
public class SilentAuthenticationService : ISilentAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMetadataRepository _userMetadataRepository;
    private readonly ICryptoService _cryptoService;
    private readonly ISecurityLogService _securityLogService;
    private readonly ILogger<SilentAuthenticationService> _logger;

    // 会话令牌默认有效期（24小时）
    private static readonly TimeSpan DefaultSessionDuration = TimeSpan.FromHours(24);

    // 会话刷新阈值（剩余时间少于2小时时自动刷新）
    private static readonly TimeSpan SessionRefreshThreshold = TimeSpan.FromHours(2);

    public SilentAuthenticationService(
        IUserRepository userRepository,
        IUserMetadataRepository userMetadataRepository,
        ICryptoService cryptoService,
        ISecurityLogService securityLogService,
        ILogger<SilentAuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userMetadataRepository = userMetadataRepository ?? throw new ArgumentNullException(nameof(userMetadataRepository));
        _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        _securityLogService = securityLogService ?? throw new ArgumentNullException(nameof(securityLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取或创建默认用户，基于机器ID自动识别用户
    /// </summary>
    public async Task<AuthenticationResult> GetOrCreateDefaultUserAsync(string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(machineId))
            {
                throw new ArgumentException("机器ID不能为空", nameof(machineId));
            }

            _logger.LogDebug("开始获取或创建默认用户，机器ID: {MachineId}", machineId);

            // 首先尝试查找现有用户
            var existingUsers = await _userRepository.GetActiveUsersAsync(cancellationToken);
            var machineUser = existingUsers.FirstOrDefault(u =>
                u.MetadataEntries.Any(m => m.Key == "MachineId" && m.GetValue<string>() == machineId));

            UserProfile user;
            bool isNewUser = false;

            if (machineUser != null)
            {
                _logger.LogDebug("找到现有用户: {UserId} ({Username})", machineUser.UserId, machineUser.Username);
                user = machineUser;

                // 更新最后登录时间
                user.UpdateLastLogin();
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
            else
            {
                _logger.LogInformation("未找到现有用户，创建新的默认用户，机器ID: {MachineId}", machineId);
                user = await CreateDefaultUserAsync(machineId, cancellationToken);
                isNewUser = true;
            }

            // 创建会话
            var sessionResult = await CreateUserSessionAsync(user.UserId, machineId, Environment.MachineName, cancellationToken);
            if (!sessionResult.IsSuccessful)
            {
                throw new UserCreationException($"创建用户会话失败: {sessionResult.ErrorMessage}", "SESSION_CREATION_FAILED");
            }

            // 记录登录日志
            await _securityLogService.LogUserLoginAsync(
                user.UserId,
                machineId,
                Environment.MachineName,
                sessionResult.SessionId,
                true,
                null,
                cancellationToken);

            _logger.LogInformation("用户认证成功: {UserId} ({Username}), 新用户: {IsNewUser}",
                user.UserId, user.Username, isNewUser);

            return AuthenticationResult.Success(
                user,
                sessionResult.SessionToken!,
                sessionResult.SessionId!,
                sessionResult.ExpirationTime!.Value,
                isNewUser);
        }
        catch (SilentAuthenticationException ex)
        {
            _logger.LogWarning(ex, "获取或创建默认用户失败，机器ID: {MachineId}, 原因: {Reason}", machineId, ex.Message);
            return AuthenticationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或创建默认用户时发生错误，机器ID: {MachineId}", machineId);
            return AuthenticationResult.Failure("系统内部错误，请稍后重试", "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// 切换到指定用户
    /// </summary>
    public async Task<AuthenticationResult> SwitchUserAsync(Guid userId, string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                throw new ArgumentException("机器ID不能为空", nameof(machineId));
            }

            _logger.LogDebug("开始切换用户: {UserId}, 机器ID: {MachineId}", userId, machineId);

            // 获取目标用户
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new UserSwitchException("目标用户不存在", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                throw new UserSwitchException("目标用户已被停用", "USER_INACTIVE");
            }

            // 验证用户是否属于当前机器
            var userMachineId = user.MetadataEntries.FirstOrDefault(m => m.Key == "MachineId")?.GetValue<string>();
            if (userMachineId != machineId)
            {
                throw new MachineIdMismatchException("用户不属于当前机器", "MACHINE_ID_MISMATCH");
            }

            // 更新最后登录时间
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 创建新会话
            var sessionResult = await CreateUserSessionAsync(userId, machineId, Environment.MachineName, cancellationToken);
            if (!sessionResult.IsSuccessful)
            {
                throw new UserSwitchException($"创建用户会话失败: {sessionResult.ErrorMessage}", "SESSION_CREATION_FAILED");
            }

            // 记录用户切换日志
            await _securityLogService.LogUserOperationAsync(
                userId,
                SecurityEventType.UserSwitched,
                "用户切换成功",
                $"从机器 {machineId} 切换到用户 {user.Username}",
                machineId,
                sessionResult.SessionId,
                true,
                null,
                cancellationToken);

            _logger.LogInformation("用户切换成功: {UserId} ({Username})", userId, user.Username);

            return AuthenticationResult.Success(
                user,
                sessionResult.SessionToken!,
                sessionResult.SessionId!,
                sessionResult.ExpirationTime!.Value);
        }
        catch (SilentAuthenticationException ex)
        {
            _logger.LogWarning(ex, "用户切换失败: {UserId}, 机器ID: {MachineId}, 原因: {Reason}", userId, machineId, ex.Message);
            return AuthenticationResult.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户时发生错误: {UserId}, 机器ID: {MachineId}", userId, machineId);
            return AuthenticationResult.Failure("系统内部错误，请稍后重试", "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// 验证会话令牌的有效性
    /// </summary>
    public async Task<SessionValidationResult> ValidateSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return SessionValidationResult.Failure("会话令牌不能为空", false);
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                return SessionValidationResult.Failure("机器ID不能为空", false);
            }

            _logger.LogDebug("验证会话令牌，机器ID: {MachineId}", machineId);

            // 使用加密服务验证令牌
            var validationResult = await _cryptoService.ValidateSessionTokenAsync(sessionToken, "", machineId);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("会话令牌验证失败: {Reason}", validationResult.FailureReason);
                return SessionValidationResult.Failure(validationResult.FailureReason ?? "令牌无效", validationResult.IsExpired);
            }

            if (validationResult.IsExpired)
            {
                _logger.LogInformation("会话令牌已过期");
                return SessionValidationResult.Failure("会话已过期", true);
            }

            // 验证用户是否存在且活跃
            if (Guid.TryParse(validationResult.UserId, out var userId))
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("令牌对应的用户不存在或已停用: {UserId}", userId);
                    return SessionValidationResult.Failure("用户不存在或已停用", false);
                }
            }
            else
            {
                _logger.LogWarning("令牌中的用户ID格式无效: {UserId}", validationResult.UserId);
                return SessionValidationResult.Failure("令牌格式无效", false);
            }

            _logger.LogDebug("会话令牌验证成功: {UserId}", validationResult.UserId);

            return SessionValidationResult.Success(
                userId,
                Guid.NewGuid().ToString(), // 临时会话ID，实际应该从令牌中解析
                validationResult.ExpirationTime ?? DateTime.UtcNow.Add(DefaultSessionDuration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证会话令牌时发生错误");
            return SessionValidationResult.Failure("系统内部错误", false);
        }
    }

    /// <summary>
    /// 刷新会话令牌
    /// </summary>
    public async Task<SessionRefreshResult> RefreshSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return SessionRefreshResult.Failure("会话令牌不能为空");
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                return SessionRefreshResult.Failure("机器ID不能为空");
            }

            _logger.LogDebug("刷新会话令牌，机器ID: {MachineId}", machineId);

            // 首先验证当前令牌
            var validationResult = await ValidateSessionAsync(sessionToken, machineId, cancellationToken);
            if (!validationResult.IsValid)
            {
                return SessionRefreshResult.Failure($"当前令牌无效: {validationResult.FailureReason}");
            }

            // 检查是否需要刷新（剩余时间少于阈值）
            var remainingTime = validationResult.ExpirationTime!.Value - DateTime.UtcNow;
            if (remainingTime > SessionRefreshThreshold)
            {
                _logger.LogDebug("会话令牌尚未到达刷新阈值，剩余时间: {RemainingTime}", remainingTime);
                return SessionRefreshResult.Success(sessionToken, validationResult.ExpirationTime.Value, validationResult.UserId!.Value, validationResult.SessionId!);
            }

            // 生成新的令牌
            var newExpirationTime = DateTime.UtcNow.Add(DefaultSessionDuration);
            var newSessionToken = _cryptoService.GenerateSessionToken(
                validationResult.UserId!.Value.ToString(),
                machineId,
                newExpirationTime);

            // 记录会话刷新日志
            await _securityLogService.LogUserOperationAsync(
                validationResult.UserId.Value,
                SecurityEventType.SessionCreated,
                "会话令牌刷新",
                "自动刷新会话令牌",
                machineId,
                validationResult.SessionId,
                true,
                null,
                cancellationToken);

            _logger.LogInformation("会话令牌刷新成功: {UserId}", validationResult.UserId);

            return SessionRefreshResult.Success(
                newSessionToken,
                newExpirationTime,
                validationResult.UserId.Value,
                validationResult.SessionId!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新会话令牌时发生错误");
            return SessionRefreshResult.Failure("系统内部错误");
        }
    }

    /// <summary>
    /// 获取当前机器上的可用用户列表
    /// </summary>
    public async Task<IEnumerable<UserProfile>> GetAvailableUsersAsync(string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(machineId))
            {
                throw new ArgumentException("机器ID不能为空", nameof(machineId));
            }

            _logger.LogDebug("获取可用用户列表，机器ID: {MachineId}", machineId);

            var allUsers = await _userRepository.GetActiveUsersAsync(cancellationToken);
            var machineUsers = allUsers.Where(u =>
                u.MetadataEntries.Any(m => m.Key == "MachineId" && m.GetValue<string>() == machineId))
                .ToList();

            _logger.LogDebug("找到 {Count} 个可用用户", machineUsers.Count);

            return machineUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用用户列表时发生错误，机器ID: {MachineId}", machineId);
            return Enumerable.Empty<UserProfile>();
        }
    }

    /// <summary>
    /// 创建新用户会话
    /// </summary>
    public async Task<SessionCreationResult> CreateUserSessionAsync(Guid userId, string machineId, string? deviceInfo = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return SessionCreationResult.Failure("用户ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                return SessionCreationResult.Failure("机器ID不能为空");
            }

            _logger.LogDebug("创建用户会话: {UserId}, 机器ID: {MachineId}", userId, machineId);

            // 验证用户存在
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return SessionCreationResult.Failure("用户不存在");
            }

            if (!user.IsActive)
            {
                return SessionCreationResult.Failure("用户已被停用");
            }

            // 生成会话信息
            var sessionId = Guid.NewGuid().ToString();
            var expirationTime = DateTime.UtcNow.Add(DefaultSessionDuration);
            var sessionToken = _cryptoService.GenerateSessionToken(userId.ToString(), machineId, expirationTime);

            // 记录会话创建日志
            await _securityLogService.LogUserOperationAsync(
                userId,
                SecurityEventType.SessionCreated,
                "创建用户会话",
                $"为用户 {user.Username} 创建新会话",
                machineId,
                sessionId,
                true,
                null,
                cancellationToken);

            _logger.LogInformation("用户会话创建成功: {UserId}, 会话ID: {SessionId}", userId, sessionId);

            return SessionCreationResult.Success(sessionToken, sessionId, expirationTime, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户会话时发生错误: {UserId}", userId);
            return SessionCreationResult.Failure("系统内部错误");
        }
    }

    /// <summary>
    /// 结束用户会话
    /// </summary>
    public async Task<bool> EndUserSessionAsync(string sessionToken, string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                return false;
            }

            _logger.LogDebug("结束用户会话，机器ID: {MachineId}", machineId);

            // 验证会话
            var validationResult = await ValidateSessionAsync(sessionToken, machineId, cancellationToken);
            if (validationResult.IsValid && validationResult.UserId.HasValue)
            {
                // 记录登出日志
                await _securityLogService.LogUserLogoutAsync(
                    validationResult.UserId.Value,
                    machineId,
                    validationResult.SessionId,
                    cancellationToken);

                _logger.LogInformation("用户会话结束成功: {UserId}", validationResult.UserId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束用户会话时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 获取当前活跃会话信息
    /// </summary>
    public Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, string machineId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return Task.FromResult(Enumerable.Empty<UserSession>());
            }

            if (string.IsNullOrWhiteSpace(machineId))
            {
                return Task.FromResult(Enumerable.Empty<UserSession>());
            }

            _logger.LogDebug("获取活跃会话信息: {UserId}, 机器ID: {MachineId}", userId, machineId);

            // 这里应该从会话存储中获取实际的会话信息
            // 由于当前没有专门的会话存储，返回空列表
            // 在实际实现中，应该有一个会话仓储来管理会话信息

            return Task.FromResult(Enumerable.Empty<UserSession>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃会话信息时发生错误: {UserId}", userId);
            return Task.FromResult(Enumerable.Empty<UserSession>());
        }
    }

    /// <summary>
    /// 创建默认用户
    /// </summary>
    private async Task<UserProfile> CreateDefaultUserAsync(string machineId, CancellationToken cancellationToken)
    {
        try
        {
            // 生成默认用户名
            var defaultUsername = $"User_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}";
            var counter = 1;
            var username = defaultUsername;

            // 确保用户名唯一
            while (await _userRepository.IsUsernameExistsAsync(username, null, cancellationToken))
            {
                username = $"{defaultUsername}_{counter}";
                counter++;
            }

            // 创建安全设置
            var securitySettings = new SecuritySettings(
                "SilentAuthentication", // 认证方法
                (int)DefaultSessionDuration.TotalMinutes, // 会话超时分钟数
                false, // 不需要双因子认证
                DateTime.UtcNow, // 密码最后修改时间
                new Dictionary<string, string>
                {
                    ["EncryptionEnabled"] = "true",
                    ["RequireSecureConnection"] = "false",
                    ["AllowMultipleSessions"] = "true",
                    ["LogSecurityEvents"] = "true"
                });

            // 创建用户档案
            var user = new UserProfile(
                Guid.NewGuid(),
                username,
                string.Empty, // 默认不设置邮箱
                securitySettings);

            // 添加用户到数据库
            user = await _userRepository.AddAsync(user, cancellationToken);

            // 添加机器ID元数据
            await _userMetadataRepository.SetValueAsync(
                user.UserId,
                "MachineId",
                machineId,
                "System",
                cancellationToken);

            // 记录用户创建日志
            await _securityLogService.LogUserOperationAsync(
                user.UserId,
                SecurityEventType.UserCreated,
                "创建默认用户",
                $"为机器 {machineId} 创建默认用户 {username}",
                machineId,
                null,
                true,
                null,
                cancellationToken);

            _logger.LogInformation("默认用户创建成功: {UserId} ({Username})", user.UserId, user.Username);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建默认用户时发生错误，机器ID: {MachineId}", machineId);
            throw new UserCreationException("创建默认用户失败", ex, "USER_CREATION_FAILED");
        }
    }
}