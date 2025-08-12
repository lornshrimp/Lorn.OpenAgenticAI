using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 用户上下文服务实现，提供线程安全的用户上下文管理
/// </summary>
public class UserContextService : IUserContextService, IDisposable
{
    private readonly IUserRepository _userRepository;
    private readonly IPreferenceService _preferenceService;
    private readonly ISilentAuthenticationService _authenticationService;
    private readonly ISecurityLogService _securityLogService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<UserContextService> _logger;

    // 线程安全的用户上下文存储
    private readonly ThreadLocal<UserContext?> _currentUserContext;

    // 用户上下文缓存，按线程ID存储
    private readonly ConcurrentDictionary<int, UserContext> _contextCache;

    // 全局用户上下文（用于单用户场景）
    private volatile UserContext? _globalUserContext;

    // 读写锁，用于保护全局上下文的并发访问
    private readonly ReaderWriterLockSlim _contextLock;

    // 缓存键前缀
    private const string UserContextCacheKeyPrefix = "UserContext_";
    private const string UserPreferenceCacheKeyPrefix = "UserPreference_";

    // 缓存过期时间
    private static readonly TimeSpan ContextCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan PreferenceCacheExpiration = TimeSpan.FromMinutes(15);

    // 事件
    public event EventHandler<UserContextChangedEventArgs>? UserContextChanged;
    public event EventHandler<UserPreferenceChangedEventArgs>? UserPreferenceChanged;

    public UserContextService(
        IUserRepository userRepository,
        IPreferenceService preferenceService,
        ISilentAuthenticationService authenticationService,
        ISecurityLogService securityLogService,
        IMemoryCache memoryCache,
        ILogger<UserContextService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _securityLogService = securityLogService ?? throw new ArgumentNullException(nameof(securityLogService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentUserContext = new ThreadLocal<UserContext?>();
        _contextCache = new ConcurrentDictionary<int, UserContext>();
        _contextLock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// 获取当前用户上下文
    /// </summary>
    public async Task<UserContext?> GetCurrentUserContextAsync()
    {
        try
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            // 首先检查线程本地存储
            var threadContext = _currentUserContext.Value;
            if (threadContext != null && threadContext.IsValid())
            {
                _logger.LogDebug("从线程本地存储获取用户上下文: {UserId}", threadContext.UserId);
                return threadContext;
            }

            // 检查线程缓存
            if (_contextCache.TryGetValue(threadId, out var cachedContext) && cachedContext.IsValid())
            {
                _currentUserContext.Value = cachedContext;
                _logger.LogDebug("从线程缓存获取用户上下文: {UserId}", cachedContext.UserId);
                return cachedContext;
            }

            // 检查全局上下文
            _contextLock.EnterReadLock();
            try
            {
                if (_globalUserContext != null && _globalUserContext.IsValid())
                {
                    var globalContext = _globalUserContext;
                    _currentUserContext.Value = globalContext;
                    _contextCache.TryAdd(threadId, globalContext);
                    _logger.LogDebug("从全局上下文获取用户上下文: {UserId}", globalContext.UserId);
                    return globalContext;
                }
            }
            finally
            {
                _contextLock.ExitReadLock();
            }

            // 检查内存缓存
            var cacheKey = $"{UserContextCacheKeyPrefix}Global";
            if (_memoryCache.TryGetValue(cacheKey, out UserContext? memoryContext) &&
                memoryContext != null && memoryContext.IsValid())
            {
                await SetCurrentUserContextAsync(memoryContext);
                _logger.LogDebug("从内存缓存获取用户上下文: {UserId}", memoryContext.UserId);
                return memoryContext;
            }

            _logger.LogDebug("未找到有效的用户上下文");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户上下文时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 设置当前用户上下文
    /// </summary>
    public async Task SetCurrentUserContextAsync(UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext == null)
        {
            throw new ArgumentNullException(nameof(userContext));
        }

        if (!userContext.IsValid())
        {
            throw new ArgumentException("用户上下文无效", nameof(userContext));
        }

        try
        {
            var oldContext = await GetCurrentUserContextAsync();
            var threadId = Thread.CurrentThread.ManagedThreadId;

            // 更新线程本地存储
            _currentUserContext.Value = userContext;

            // 更新线程缓存
            _contextCache.AddOrUpdate(threadId, userContext, (key, oldValue) => userContext);

            // 更新全局上下文
            _contextLock.EnterWriteLock();
            try
            {
                _globalUserContext = userContext;
            }
            finally
            {
                _contextLock.ExitWriteLock();
            }

            // 更新内存缓存
            var cacheKey = $"{UserContextCacheKeyPrefix}Global";
            _memoryCache.Set(cacheKey, userContext, ContextCacheExpiration);

            // 预加载用户偏好设置到缓存
            await PreloadUserPreferencesAsync(userContext.UserId, cancellationToken);

            // 记录安全日志
            await _securityLogService.LogUserOperationAsync(
                userContext.UserId,
                SecurityEventType.UserLogin,
                "设置用户上下文",
                $"为用户 {userContext.UserProfile.Username} 设置上下文",
                userContext.MachineId,
                userContext.SessionId,
                true,
                null,
                cancellationToken);

            // 触发上下文变更事件
            var changeType = oldContext == null ? UserContextChangeType.UserLogin : UserContextChangeType.ContextRefresh;
            OnUserContextChanged(new UserContextChangedEventArgs
            {
                OldUserContext = oldContext,
                NewUserContext = userContext,
                ChangeType = changeType
            });

            _logger.LogInformation("用户上下文设置成功: {UserId} ({Username})",
                userContext.UserId, userContext.UserProfile.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置用户上下文时发生错误: {UserId}", userContext.UserId);
            throw new UserContextException("设置用户上下文失败", ex);
        }
    }

    /// <summary>
    /// 切换用户上下文
    /// </summary>
    public async Task<UserContextSwitchResult> SwitchUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return UserContextSwitchResult.Failure("用户ID不能为空", "INVALID_USER_ID");
            }

            _logger.LogDebug("开始切换用户上下文: {UserId}", userId);

            var oldContext = await GetCurrentUserContextAsync();

            // 检查是否切换到相同用户
            if (oldContext?.UserId == userId)
            {
                _logger.LogDebug("用户上下文已经是目标用户: {UserId}", userId);
                return UserContextSwitchResult.Success(oldContext);
            }

            // 获取机器ID
            var machineId = Environment.MachineName; // 简化实现，实际应该从配置或服务获取

            // 使用认证服务切换用户
            var authResult = await _authenticationService.SwitchUserAsync(userId, machineId, cancellationToken);
            if (!authResult.IsSuccessful)
            {
                _logger.LogWarning("用户认证切换失败: {UserId}, 原因: {Reason}", userId, authResult.ErrorMessage);
                return UserContextSwitchResult.Failure(authResult.ErrorMessage ?? "用户认证失败", authResult.ErrorCode);
            }

            // 创建新的用户上下文
            var newUserContext = new UserContext
            {
                UserId = authResult.User!.UserId,
                UserProfile = authResult.User,
                SessionToken = authResult.SessionToken,
                SessionId = authResult.SessionId,
                SessionExpirationTime = authResult.ExpirationTime,
                MachineId = machineId,
                IsDefaultUser = authResult.User.IsDefault
            };

            // 清理旧的上下文
            if (oldContext != null)
            {
                await ClearUserContextCacheAsync(oldContext.UserId);
            }

            // 设置新的上下文
            await SetCurrentUserContextAsync(newUserContext, cancellationToken);

            // 触发用户切换事件
            OnUserContextChanged(new UserContextChangedEventArgs
            {
                OldUserContext = oldContext,
                NewUserContext = newUserContext,
                ChangeType = UserContextChangeType.UserSwitch
            });

            _logger.LogInformation("用户上下文切换成功: {OldUserId} -> {NewUserId}",
                oldContext?.UserId, newUserContext.UserId);

            return UserContextSwitchResult.Success(newUserContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户上下文时发生错误: {UserId}", userId);
            return UserContextSwitchResult.Failure("系统内部错误", "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// 清理当前用户上下文
    /// </summary>
    public async Task ClearCurrentUserContextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var oldContext = await GetCurrentUserContextAsync();
            if (oldContext == null)
            {
                _logger.LogDebug("没有需要清理的用户上下文");
                return;
            }

            var threadId = Thread.CurrentThread.ManagedThreadId;

            // 清理线程本地存储
            _currentUserContext.Value = null;

            // 清理线程缓存
            _contextCache.TryRemove(threadId, out _);

            // 清理全局上下文
            _contextLock.EnterWriteLock();
            try
            {
                _globalUserContext = null;
            }
            finally
            {
                _contextLock.ExitWriteLock();
            }

            // 清理内存缓存
            var cacheKey = $"{UserContextCacheKeyPrefix}Global";
            _memoryCache.Remove(cacheKey);

            // 清理用户相关的偏好设置缓存
            await ClearUserContextCacheAsync(oldContext.UserId);

            // 记录安全日志
            await _securityLogService.LogUserOperationAsync(
                oldContext.UserId,
                SecurityEventType.UserLogout,
                "清理用户上下文",
                $"清理用户 {oldContext.UserProfile.Username} 的上下文",
                oldContext.MachineId,
                oldContext.SessionId,
                true,
                null,
                cancellationToken);

            // 触发上下文变更事件
            OnUserContextChanged(new UserContextChangedEventArgs
            {
                OldUserContext = oldContext,
                NewUserContext = null,
                ChangeType = UserContextChangeType.UserLogout
            });

            _logger.LogInformation("用户上下文清理成功: {UserId} ({Username})",
                oldContext.UserId, oldContext.UserProfile.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理用户上下文时发生错误");
            throw new UserContextException("清理用户上下文失败", ex);
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    public Guid? GetCurrentUserId()
    {
        try
        {
            var context = _currentUserContext.Value;
            if (context != null && context.IsValid())
            {
                return context.UserId;
            }

            // 尝试从全局上下文获取
            _contextLock.EnterReadLock();
            try
            {
                return _globalUserContext?.UserId;
            }
            finally
            {
                _contextLock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户ID时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 获取当前用户档案
    /// </summary>
    public async Task<UserProfile?> GetCurrentUserProfileAsync()
    {
        try
        {
            var context = await GetCurrentUserContextAsync();
            return context?.UserProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户档案时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 获取当前用户偏好设置
    /// </summary>
    public async Task<T?> GetCurrentUserPreferenceAsync<T>(string category, string key, T? defaultValue = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("获取用户偏好设置失败：没有当前用户上下文");
                return defaultValue;
            }

            // 首先尝试从上下文缓存获取
            var context = await GetCurrentUserContextAsync();
            if (context?.CachedPreferences.TryGetValue(category, out var categoryPrefs) == true &&
                categoryPrefs.TryGetValue(key, out var cachedValue))
            {
                try
                {
                    return (T?)cachedValue;
                }
                catch (InvalidCastException)
                {
                    _logger.LogWarning("缓存的偏好设置类型转换失败: {Category}.{Key}", category, key);
                }
            }

            // 从偏好设置服务获取
            var value = await _preferenceService.GetPreferenceAsync(userId.Value, category, key, defaultValue, cancellationToken);

            // 更新上下文缓存
            if (context != null)
            {
                if (!context.CachedPreferences.ContainsKey(category))
                {
                    context.CachedPreferences[category] = new Dictionary<string, object>();
                }
                context.CachedPreferences[category][key] = value!;
                context.UpdateLastModified();
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户偏好设置时发生错误: {Category}.{Key}", category, key);
            return defaultValue;
        }
    }

    /// <summary>
    /// 设置当前用户偏好设置
    /// </summary>
    public async Task<bool> SetCurrentUserPreferenceAsync<T>(string category, string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("设置用户偏好设置失败：没有当前用户上下文");
                return false;
            }

            // 获取旧值用于事件通知
            var oldValue = await GetCurrentUserPreferenceAsync<T>(category, key, default, cancellationToken);

            // 设置偏好设置
            var success = await _preferenceService.SetPreferenceAsync(userId.Value, category, key, value, null, cancellationToken);
            if (!success)
            {
                return false;
            }

            // 更新上下文缓存
            var context = await GetCurrentUserContextAsync();
            if (context != null)
            {
                if (!context.CachedPreferences.ContainsKey(category))
                {
                    context.CachedPreferences[category] = new Dictionary<string, object>();
                }
                context.CachedPreferences[category][key] = value!;
                context.UpdateLastModified();
            }

            // 触发偏好设置变更事件
            OnUserPreferenceChanged(new UserPreferenceChangedEventArgs
            {
                UserId = userId.Value,
                Category = category,
                Key = key,
                OldValue = oldValue,
                NewValue = value
            });

            _logger.LogDebug("用户偏好设置更新成功: {UserId}, {Category}.{Key} = {Value}",
                userId.Value, category, key, value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置当前用户偏好设置时发生错误: {Category}.{Key}", category, key);
            return false;
        }
    }

    /// <summary>
    /// 检查当前是否有活跃的用户上下文
    /// </summary>
    public bool HasActiveUserContext()
    {
        try
        {
            var context = _currentUserContext.Value;
            if (context != null && context.IsValid())
            {
                return true;
            }

            _contextLock.EnterReadLock();
            try
            {
                return _globalUserContext != null && _globalUserContext.IsValid();
            }
            finally
            {
                _contextLock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查活跃用户上下文时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 刷新当前用户上下文缓存
    /// </summary>
    public async Task RefreshCurrentUserContextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentContext = await GetCurrentUserContextAsync();
            if (currentContext == null)
            {
                _logger.LogDebug("没有需要刷新的用户上下文");
                return;
            }

            _logger.LogDebug("开始刷新用户上下文: {UserId}", currentContext.UserId);

            // 重新加载用户档案
            var userProfile = await _userRepository.GetByIdAsync(currentContext.UserId, cancellationToken);
            if (userProfile == null)
            {
                _logger.LogWarning("刷新用户上下文失败：用户不存在: {UserId}", currentContext.UserId);
                await ClearCurrentUserContextAsync(cancellationToken);
                return;
            }

            // 检查会话是否需要刷新
            if (currentContext.IsSessionNearExpiry())
            {
                _logger.LogInformation("会话即将过期，尝试刷新会话: {UserId}", currentContext.UserId);

                if (!string.IsNullOrEmpty(currentContext.SessionToken) && !string.IsNullOrEmpty(currentContext.MachineId))
                {
                    var refreshResult = await _authenticationService.RefreshSessionAsync(
                        currentContext.SessionToken, currentContext.MachineId, cancellationToken);

                    if (refreshResult.IsSuccessful)
                    {
                        currentContext.SessionToken = refreshResult.NewSessionToken;
                        currentContext.SessionExpirationTime = refreshResult.NewExpirationTime;
                        _logger.LogInformation("会话刷新成功: {UserId}", currentContext.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("会话刷新失败: {UserId}, 原因: {Reason}",
                            currentContext.UserId, refreshResult.ErrorMessage);
                    }
                }
            }

            // 更新用户档案
            currentContext.UserProfile = userProfile;
            currentContext.UpdateLastModified();

            // 清理并重新加载偏好设置缓存
            currentContext.CachedPreferences.Clear();
            await PreloadUserPreferencesAsync(currentContext.UserId, cancellationToken);

            // 重新设置上下文以更新所有缓存
            await SetCurrentUserContextAsync(currentContext, cancellationToken);

            _logger.LogInformation("用户上下文刷新成功: {UserId}", currentContext.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新用户上下文时发生错误");
            throw new UserContextException("刷新用户上下文失败", ex);
        }
    }

    /// <summary>
    /// 预加载用户偏好设置到缓存
    /// </summary>
    private async Task PreloadUserPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var categories = new[] { "Interface", "Operation", "Security" };
            var context = await GetCurrentUserContextAsync();

            if (context == null) return;

            foreach (var category in categories)
            {
                try
                {
                    var categoryPrefs = await _preferenceService.GetCategoryPreferencesAsync(
                        userId, category, cancellationToken);

                    if (categoryPrefs.Any())
                    {
                        context.CachedPreferences[category] = categoryPrefs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? new object());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "预加载用户偏好设置失败: {UserId}, 分类: {Category}", userId, category);
                }
            }

            _logger.LogDebug("用户偏好设置预加载完成: {UserId}, 分类数: {Count}",
                userId, context.CachedPreferences.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载用户偏好设置时发生错误: {UserId}", userId);
        }
    }

    /// <summary>
    /// 清理用户上下文相关缓存
    /// </summary>
    private async Task ClearUserContextCacheAsync(Guid userId)
    {
        try
        {
            // 清理偏好设置缓存
            var prefCacheKeys = new[] { "Interface", "Operation", "Security" }
                .Select(category => $"{UserPreferenceCacheKeyPrefix}{userId}_{category}");

            foreach (var key in prefCacheKeys)
            {
                _memoryCache.Remove(key);
            }

            _logger.LogDebug("用户上下文缓存清理完成: {UserId}", userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理用户上下文缓存时发生错误: {UserId}", userId);
        }
    }

    /// <summary>
    /// 触发用户上下文变更事件
    /// </summary>
    private void OnUserContextChanged(UserContextChangedEventArgs args)
    {
        try
        {
            UserContextChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发用户上下文变更事件时发生错误");
        }
    }

    /// <summary>
    /// 触发用户偏好设置变更事件
    /// </summary>
    private void OnUserPreferenceChanged(UserPreferenceChangedEventArgs args)
    {
        try
        {
            UserPreferenceChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发用户偏好设置变更事件时发生错误");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            _currentUserContext?.Dispose();
            _contextLock?.Dispose();

            _logger.LogDebug("用户上下文服务资源释放完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放用户上下文服务资源时发生错误");
        }
    }
}