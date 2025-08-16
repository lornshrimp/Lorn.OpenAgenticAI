using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// 精简版用户上下文服务测试：仅覆盖当前实现的核心职责（设置/获取/切换/清理/刷新/事件）。
/// </summary>
public class UserContextServiceTests : IDisposable
{
    private readonly Mock<IUserDataService> _userData = new();
    private readonly Mock<ISilentAuthenticationService> _auth = new();
    private readonly Mock<ISecurityLogService> _securityLog = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<UserContextService>> _logger = new();
    private readonly UserContextService _svc;

    private readonly UserProfile _profile;
    private readonly UserContext _ctx;

    public UserContextServiceTests()
    {
        _svc = new UserContextService(_userData.Object, _auth.Object, _securityLog.Object, _cache, _logger.Object);
        var security = new SecuritySettings("SilentAuthentication", 30, false, DateTime.UtcNow, []);
        _profile = new UserProfile(Guid.NewGuid(), "TestUser", "test@example.com", security);
        _ctx = new UserContext { UserId = _profile.Id, UserProfile = _profile, IsDefaultUser = true };
    }

    public void Dispose()
    {
        _svc.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task GetCurrentUserContext_NoSet_ReturnsNull()
    {
        var result = await _svc.GetCurrentUserContextAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetCurrentUserContext_Valid_PersistsAndLogs()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(
            _ctx.UserId,
            SecurityEventType.UserLogin,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            true,
            null,
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _svc.SetCurrentUserContextAsync(_ctx);
        var stored = await _svc.GetCurrentUserContextAsync();
        Assert.NotNull(stored);
        Assert.Equal(_ctx.UserId, stored!.UserId);

        _securityLog.VerifyAll();
    }

    [Fact]
    public async Task SetCurrentUserContext_Invalid_Throws()
    {
        var invalid = new UserContext { UserId = Guid.Empty, UserProfile = _profile };
        await Assert.ThrowsAsync<ArgumentException>(() => _svc.SetCurrentUserContextAsync(invalid));
    }

    [Fact]
    public async Task GetCurrentUserId_AfterSet_ReturnsId()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);
        var id = _svc.GetCurrentUserId();
        Assert.Equal(_ctx.UserId, id);
    }

    [Fact]
    public void GetCurrentUserId_NoContext_ReturnsNull()
    {
        Assert.Null(_svc.GetCurrentUserId());
    }

    [Fact]
    public async Task HasActiveUserContext_TrueAfterSet()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);
        Assert.True(_svc.HasActiveUserContext());
    }

    [Fact]
    public void HasActiveUserContext_FalseWhenNone()
    {
        Assert.False(_svc.HasActiveUserContext());
    }

    [Fact]
    public async Task ClearCurrentUserContext_RemovesStateAndLogs()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);
        _securityLog.Setup(l => l.LogUserOperationAsync(
            _ctx.UserId,
            SecurityEventType.UserLogout,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            true,
            null,
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.ClearCurrentUserContextAsync();
        Assert.False(_svc.HasActiveUserContext());
        Assert.Null(_svc.GetCurrentUserId());
    }

    [Fact]
    public async Task RefreshCurrentUserContext_ProfileUpdated_RaisesEvent()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);

        var newProfile = new UserProfile(_profile.Id, "NewName", _profile.Email, _profile.SecuritySettings);
        _userData.Setup(d => d.GetUserProfileAsync(_profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newProfile);

        UserContextChangedEventArgs? evt = null;
        _svc.UserContextChanged += (_, e) => evt = e;
        await _svc.RefreshCurrentUserContextAsync();
        Assert.NotNull(evt);
        Assert.Equal(UserContextChangeType.ContextRefresh, evt!.ChangeType);
        var after = await _svc.GetCurrentUserContextAsync();
        Assert.Equal("NewName", after!.UserProfile.Username);
    }

    [Fact]
    public async Task RefreshCurrentUserContext_UserMissing_ClearsContext()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);
        _userData.Setup(d => d.GetUserProfileAsync(_profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);
        await _svc.RefreshCurrentUserContextAsync();
        Assert.False(_svc.HasActiveUserContext());
    }

    [Fact]
    public async Task SetCurrentUserContext_RaisesLoginEvent()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        UserContextChangedEventArgs? evt = null;
        _svc.UserContextChanged += (_, e) => evt = e;
        await _svc.SetCurrentUserContextAsync(_ctx);
        Assert.NotNull(evt);
        Assert.Equal(UserContextChangeType.UserLogin, evt!.ChangeType);
        Assert.Null(evt.OldUserContext);
        Assert.Equal(_ctx.UserId, evt.NewUserContext!.UserId);
    }

    [Fact]
    public async Task SwitchUserContext_ValidProfile_SetsNewContextAndEvent()
    {
        // existing context
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);

        var another = new UserProfile(Guid.NewGuid(), "Another", "a@b.com", _profile.SecuritySettings); // constructor sets active
        _userData.Setup(d => d.GetUserProfileAsync(another.Id, It.IsAny<CancellationToken>())).ReturnsAsync(another);

        UserContextChangedEventArgs? evt = null;
        _svc.UserContextChanged += (_, e) => evt = e;
        var result = await _svc.SwitchUserContextAsync(another.Id);
        Assert.True(result.IsSuccessful);
        Assert.NotNull(evt);
        Assert.Equal(UserContextChangeType.UserSwitch, evt!.ChangeType);
        Assert.Equal(another.Id, evt.NewUserContext!.UserId);
    }

    [Fact]
    public async Task Concurrent_ReadsAfterSet_AllReturnSameContext()
    {
        _securityLog.Setup(l => l.LogUserOperationAsync(It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), true, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _svc.SetCurrentUserContextAsync(_ctx);

        const int parallel = 20;
        var tasks = new Task<UserContext?>[parallel];
        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = _svc.GetCurrentUserContextAsync();
        }
        var results = await Task.WhenAll(tasks);
        foreach (var ctx in results)
        {
            Assert.NotNull(ctx);
            Assert.Equal(_ctx.UserId, ctx!.UserId);
        }
    }
}
