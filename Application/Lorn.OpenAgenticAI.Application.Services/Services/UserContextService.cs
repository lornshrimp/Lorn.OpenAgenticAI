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
    private readonly IUserDataService _userDataService;
    private readonly ISilentAuthenticationService _authenticationService;
    private readonly ISecurityLogService _securityLogService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<UserContextService> _logger;

    private readonly ThreadLocal<UserContext?> _currentUserContext = new();
    private readonly ConcurrentDictionary<int, UserContext> _contextCache = new();
    private volatile UserContext? _globalUserContext;
    private readonly ReaderWriterLockSlim _contextLock = new();

    private const string UserContextCacheKeyPrefix = "UserContext_";
    private static readonly TimeSpan ContextCacheExpiration = TimeSpan.FromMinutes(30);

    public event EventHandler<UserContextChangedEventArgs>? UserContextChanged;

    public UserContextService(
        IUserDataService userDataService,
        ISilentAuthenticationService authenticationService,
        ISecurityLogService securityLogService,
        IMemoryCache memoryCache,
        ILogger<UserContextService> logger)
    {
        _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _securityLogService = securityLogService ?? throw new ArgumentNullException(nameof(securityLogService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserContext?> GetCurrentUserContextAsync()
    {
        try
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var local = _currentUserContext.Value;
            if (local != null && local.IsValid()) return local;

            if (_contextCache.TryGetValue(threadId, out var cached) && cached.IsValid())
            {
                _currentUserContext.Value = cached;
                return cached;
            }

            _contextLock.EnterReadLock();
            try
            {
                if (_globalUserContext != null && _globalUserContext.IsValid())
                {
                    _currentUserContext.Value = _globalUserContext;
                    _contextCache[threadId] = _globalUserContext;
                    return _globalUserContext;
                }
            }
            finally { _contextLock.ExitReadLock(); }

            if (_memoryCache.TryGetValue($"{UserContextCacheKeyPrefix}Global", out UserContext? memoryCtx) && memoryCtx != null && memoryCtx.IsValid())
            {
                await SetCurrentUserContextAsync(memoryCtx);
                return memoryCtx;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户上下文失败");
            return null;
        }
    }

    public async Task SetCurrentUserContextAsync(UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext == null) throw new ArgumentNullException(nameof(userContext));
        if (!userContext.IsValid()) throw new ArgumentException("用户上下文无效", nameof(userContext));

        var old = await GetCurrentUserContextAsync();
        var threadId = Thread.CurrentThread.ManagedThreadId;

        _currentUserContext.Value = userContext;
        _contextCache[threadId] = userContext;
        _contextLock.EnterWriteLock();
        try { _globalUserContext = userContext; }
        finally { _contextLock.ExitWriteLock(); }
        _memoryCache.Set($"{UserContextCacheKeyPrefix}Global", userContext, ContextCacheExpiration);

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

        OnUserContextChanged(new UserContextChangedEventArgs
        {
            OldUserContext = old,
            NewUserContext = userContext,
            ChangeType = old == null ? UserContextChangeType.UserLogin : UserContextChangeType.UserSwitch
        });
    }

    public async Task<UserContextSwitchResult> SwitchUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _userDataService.GetUserProfileAsync(userId, cancellationToken);
            if (profile == null || !profile.IsActive)
            {
                return UserContextSwitchResult.Failure("目标用户不存在或未激活", "USER_NOT_FOUND");
            }
            var newCtx = new UserContext
            {
                UserId = profile.Id,
                UserProfile = profile,
                IsDefaultUser = profile.IsDefault
            };
            await SetCurrentUserContextAsync(newCtx, cancellationToken);
            return UserContextSwitchResult.Success(newCtx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户上下文失败: {UserId}", userId);
            return UserContextSwitchResult.Failure("切换失败", "SWITCH_ERROR");
        }
    }

    public async Task ClearCurrentUserContextAsync(CancellationToken cancellationToken = default)
    {
        var old = await GetCurrentUserContextAsync();
        if (old == null) return;

        _currentUserContext.Value = null;
        _contextCache.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
        _contextLock.EnterWriteLock();
        try
        {
            if (_globalUserContext?.UserId == old.UserId) _globalUserContext = null;
        }
        finally { _contextLock.ExitWriteLock(); }
        _memoryCache.Remove($"{UserContextCacheKeyPrefix}Global");

        await _securityLogService.LogUserOperationAsync(
            old.UserId,
            SecurityEventType.UserLogout,
            "清理用户上下文",
            $"清理用户 {old.UserProfile.Username} 的上下文",
            old.MachineId,
            old.SessionId,
            true,
            null,
            cancellationToken);

        OnUserContextChanged(new UserContextChangedEventArgs
        {
            OldUserContext = old,
            NewUserContext = null,
            ChangeType = UserContextChangeType.UserLogout
        });
    }

    public Guid? GetCurrentUserId()
    {
        var local = _currentUserContext.Value;
        if (local != null && local.IsValid()) return local.UserId;
        _contextLock.EnterReadLock();
        try { return _globalUserContext?.UserId; }
        finally { _contextLock.ExitReadLock(); }
    }

    public async Task<UserProfile?> GetCurrentUserProfileAsync()
    {
        var ctx = await GetCurrentUserContextAsync();
        return ctx?.UserProfile;
    }

    public bool HasActiveUserContext()
    {
        var local = _currentUserContext.Value;
        if (local != null && local.IsValid()) return true;
        _contextLock.EnterReadLock();
        try { return _globalUserContext != null && _globalUserContext.IsValid(); }
        finally { _contextLock.ExitReadLock(); }
    }

    public async Task RefreshCurrentUserContextAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentUserContextAsync();
        if (current == null) return;
        var profile = await _userDataService.GetUserProfileAsync(current.UserId, cancellationToken);
        if (profile == null)
        {
            await ClearCurrentUserContextAsync(cancellationToken);
            return;
        }
        current.UserProfile = profile;
        current.UpdateLastModified();
        OnUserContextChanged(new UserContextChangedEventArgs
        {
            OldUserContext = current,
            NewUserContext = current,
            ChangeType = UserContextChangeType.ContextRefresh
        });
    }

    private void OnUserContextChanged(UserContextChangedEventArgs args)
    {
        try { UserContextChanged?.Invoke(this, args); }
        catch (Exception ex) { _logger.LogError(ex, "触发用户上下文变更事件失败"); }
    }


    public void Dispose()
    {
        _currentUserContext?.Dispose();
        _contextLock?.Dispose();
    }
}