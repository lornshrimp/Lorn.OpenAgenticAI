using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// 用户上下文服务单元测试
/// </summary>
public class UserContextServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPreferenceService> _mockPreferenceService;
    private readonly Mock<ISilentAuthenticationService> _mockAuthenticationService;
    private readonly Mock<ISecurityLogService> _mockSecurityLogService;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<UserContextService>> _mockLogger;
    private readonly UserContextService _userContextService;

    private readonly UserProfile _testUser;
    private readonly UserContext _testUserContext;

    public UserContextServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPreferenceService = new Mock<IPreferenceService>();
        _mockAuthenticationService = new Mock<ISilentAuthenticationService>();
        _mockSecurityLogService = new Mock<ISecurityLogService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<UserContextService>>();

        _userContextService = new UserContextService(
            _mockUserRepository.Object,
            _mockPreferenceService.Object,
            _mockAuthenticationService.Object,
            _mockSecurityLogService.Object,
            _memoryCache,
            _mockLogger.Object);

        // 创建测试用户
        var securitySettings = new SecuritySettings(
            "SilentAuthentication",
            30,
            false,
            DateTime.UtcNow,
            new Dictionary<string, string>());

        _testUser = new UserProfile(
            Guid.NewGuid(),
            "TestUser",
            "test@example.com",
            securitySettings);

        // 创建测试用户上下文
        _testUserContext = new UserContext
        {
            UserId = _testUser.UserId,
            UserProfile = _testUser,
            SessionToken = "test-session-token",
            SessionId = "test-session-id",
            SessionExpirationTime = DateTime.UtcNow.AddHours(1),
            MachineId = "test-machine-id",
            IsDefaultUser = true
        };
    }

    public void Dispose()
    {
        _userContextService?.Dispose();
        _memoryCache?.Dispose();
    }

    #region 获取当前用户上下文测试

    [Fact]
    public async Task GetCurrentUserContextAsync_WhenNoContextSet_ShouldReturnNull()
    {
        // Act
        var result = await _userContextService.GetCurrentUserContextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserContextAsync_WhenContextSet_ShouldReturnContext()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // Act
        var result = await _userContextService.GetCurrentUserContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserContext.UserId, result!.UserId);
        Assert.Equal(_testUser.Username, result.UserProfile.Username);
    }

    #endregion

    #region 设置当前用户上下文测试

    [Fact]
    public async Task SetCurrentUserContextAsync_WhenValidContext_ShouldSetSuccessfully()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // Assert
        var result = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(result);
        Assert.Equal(_testUserContext.UserId, result!.UserId);

        // 验证安全日志记录
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            _testUserContext.UserId,
            SecurityEventType.UserLogin,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            true,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCurrentUserContextAsync_WhenNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _userContextService.SetCurrentUserContextAsync(null!));
    }

    [Fact]
    public async Task SetCurrentUserContextAsync_WhenInvalidContext_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidContext = new UserContext
        {
            UserId = Guid.Empty, // 无效的用户ID
            UserProfile = _testUser
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _userContextService.SetCurrentUserContextAsync(invalidContext));
    }

    #endregion

    #region 用户上下文切换测试

    [Fact]
    public async Task SwitchUserContextAsync_WhenValidUserId_ShouldSwitchSuccessfully()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var authResult = AuthenticationResult.Success(
            _testUser,
            "new-session-token",
            "new-session-id",
            DateTime.UtcNow.AddHours(1));

        _mockAuthenticationService.Setup(x => x.SwitchUserAsync(
            targetUserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userContextService.SwitchUserContextAsync(targetUserId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.NewUserContext);
        Assert.Equal(_testUser.UserId, result.NewUserContext!.UserId);

        // 验证认证服务调用
        _mockAuthenticationService.Verify(x => x.SwitchUserAsync(
            targetUserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SwitchUserContextAsync_WhenEmptyUserId_ShouldReturnFailure()
    {
        // Act
        var result = await _userContextService.SwitchUserContextAsync(Guid.Empty);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("INVALID_USER_ID", result.ErrorCode);
    }

    #endregion

    #region 获取当前用户ID测试

    [Fact]
    public async Task GetCurrentUserId_WhenContextSet_ShouldReturnUserId()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // Act
        var result = _userContextService.GetCurrentUserId();

        // Assert
        Assert.Equal(_testUserContext.UserId, result);
    }

    [Fact]
    public void GetCurrentUserId_WhenNoContext_ShouldReturnNull()
    {
        // Act
        var result = _userContextService.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 用户偏好设置测试

    [Fact]
    public async Task GetCurrentUserPreferenceAsync_WhenContextSet_ShouldReturnPreference()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var expectedValue = "dark";
        _mockPreferenceService.Setup(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _userContextService.GetCurrentUserPreferenceAsync<string>(
            "Interface", "Theme", "light");

        // Assert
        Assert.Equal(expectedValue, result);

        // 验证偏好设置服务调用
        _mockPreferenceService.Verify(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            "light",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserPreferenceAsync_WhenNoContext_ShouldReturnDefaultValue()
    {
        // Act
        var result = await _userContextService.GetCurrentUserPreferenceAsync<string>(
            "Interface", "Theme", "light");

        // Assert
        Assert.Equal("light", result);
    }

    #endregion

    #region 活跃用户上下文检查测试

    [Fact]
    public async Task HasActiveUserContext_WhenContextSet_ShouldReturnTrue()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // Act
        var result = _userContextService.HasActiveUserContext();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasActiveUserContext_WhenNoContext_ShouldReturnFalse()
    {
        // Act
        var result = _userContextService.HasActiveUserContext();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UserContext 辅助类测试

    [Fact]
    public void UserContext_IsValid_WhenValidContext_ShouldReturnTrue()
    {
        // Act
        var result = _testUserContext.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void UserContext_IsValid_WhenEmptyUserId_ShouldReturnFalse()
    {
        // Arrange
        var invalidContext = new UserContext
        {
            UserId = Guid.Empty,
            UserProfile = _testUser
        };

        // Act
        var result = invalidContext.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UserContext_IsValid_WhenExpiredSession_ShouldReturnFalse()
    {
        // Arrange
        var expiredContext = new UserContext
        {
            UserId = _testUser.UserId,
            UserProfile = _testUser,
            SessionExpirationTime = DateTime.UtcNow.AddMinutes(-1) // 已过期
        };

        // Act
        var result = expiredContext.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UserContext_IsSessionNearExpiry_WhenNearExpiry_ShouldReturnTrue()
    {
        // Arrange
        var nearExpiryContext = new UserContext
        {
            UserId = _testUser.UserId,
            UserProfile = _testUser,
            SessionExpirationTime = DateTime.UtcNow.AddMinutes(15) // 15分钟后过期
        };

        // Act
        var result = nearExpiryContext.IsSessionNearExpiry(30); // 30分钟阈值

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void UserContext_UpdateLastModified_ShouldUpdateTimestamp()
    {
        // Arrange
        var originalTime = _testUserContext.LastUpdatedAt;

        // Act
        Thread.Sleep(10); // 确保时间差异
        _testUserContext.UpdateLastModified();

        // Assert
        Assert.True(_testUserContext.LastUpdatedAt > originalTime);
    }

    #endregion

    #region 线程安全测试

    [Fact]
    public async Task GetCurrentUserContextAsync_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var results = new ConcurrentBag<UserContext?>();
        var tasks = new List<Task>();

        // Act - 并发访问用户上下文
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var context = await _userContextService.GetCurrentUserContextAsync();
                results.Add(context);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(50, results.Count);
        Assert.All(results, context =>
        {
            Assert.NotNull(context);
            Assert.Equal(_testUserContext.UserId, context!.UserId);
        });
    }

    [Fact]
    public async Task SetCurrentUserContextAsync_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act - 并发设置用户上下文
        for (int i = 0; i < 10; i++)
        {
            var contextIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var testContext = new UserContext
                    {
                        UserId = Guid.NewGuid(),
                        UserProfile = _testUser,
                        SessionToken = $"test-session-token-{contextIndex}",
                        SessionId = $"test-session-id-{contextIndex}",
                        SessionExpirationTime = DateTime.UtcNow.AddHours(1),
                        MachineId = "test-machine-id",
                        IsDefaultUser = true
                    };

                    await _userContextService.SetCurrentUserContextAsync(testContext);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - 不应该有异常
        Assert.Empty(exceptions);

        // 验证最终状态一致
        var finalContext = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(finalContext);
    }

    [Fact]
    public async Task GetCurrentUserId_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var results = new ConcurrentBag<Guid?>();
        var tasks = new List<Task>();

        // Act - 并发获取用户ID
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var userId = _userContextService.GetCurrentUserId();
                results.Add(userId);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Count);
        Assert.All(results, userId =>
        {
            Assert.Equal(_testUserContext.UserId, userId);
        });
    }

    #endregion

    #region 用户切换时的上下文清理和重建测试

    [Fact]
    public async Task SwitchUserContextAsync_ShouldClearOldContextAndSetNewContext()
    {
        // Arrange
        var oldUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();

        var oldSecuritySettings = new SecuritySettings(
            "SilentAuthentication",
            30,
            false,
            DateTime.UtcNow,
            new Dictionary<string, string>());

        var oldUser = new UserProfile(
            oldUserId,
            "OldUser",
            "old@example.com",
            oldSecuritySettings);

        var oldContext = new UserContext
        {
            UserId = oldUserId,
            UserProfile = oldUser,
            SessionToken = "old-session-token",
            SessionId = "old-session-id",
            SessionExpirationTime = DateTime.UtcNow.AddHours(1),
            MachineId = "test-machine-id",
            IsDefaultUser = false
        };

        var newUser = new UserProfile(
            newUserId,
            "NewUser",
            "new@example.com",
            oldSecuritySettings);

        var authResult = AuthenticationResult.Success(
            newUser,
            "new-session-token",
            "new-session-id",
            DateTime.UtcNow.AddHours(1));

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAuthenticationService.Setup(x => x.SwitchUserAsync(
            newUserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // 设置初始上下文
        await _userContextService.SetCurrentUserContextAsync(oldContext);

        // 添加一些缓存的偏好设置
        oldContext.CachedPreferences["Interface"] = new Dictionary<string, object>
        {
            ["Theme"] = "dark",
            ["Language"] = "zh-CN"
        };

        // Act - 切换用户
        var result = await _userContextService.SwitchUserContextAsync(newUserId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.NewUserContext);
        Assert.Equal(newUserId, result.NewUserContext!.UserId);
        Assert.Equal("NewUser", result.NewUserContext.UserProfile.Username);

        // 验证新上下文已设置
        var currentContext = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(currentContext);
        Assert.Equal(newUserId, currentContext!.UserId);

        // 验证旧上下文的缓存已清理（新上下文应该没有旧的偏好设置）
        Assert.Empty(currentContext.CachedPreferences);
    }

    [Fact]
    public async Task ClearCurrentUserContextAsync_ShouldClearAllContextData()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // 添加一些缓存数据
        _testUserContext.CachedPreferences["Interface"] = new Dictionary<string, object>
        {
            ["Theme"] = "dark"
        };

        // 验证上下文已设置
        Assert.True(_userContextService.HasActiveUserContext());
        Assert.NotNull(await _userContextService.GetCurrentUserContextAsync());

        // Act - 清理上下文
        await _userContextService.ClearCurrentUserContextAsync();

        // Assert
        Assert.False(_userContextService.HasActiveUserContext());
        Assert.Null(await _userContextService.GetCurrentUserContextAsync());
        Assert.Null(_userContextService.GetCurrentUserId());

        // 验证安全日志记录
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            _testUserContext.UserId,
            SecurityEventType.UserLogout,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            true,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 上下文缓存机制测试

    [Fact]
    public async Task GetCurrentUserPreferenceAsync_ShouldCachePreferences()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var expectedValue = "dark";
        _mockPreferenceService.Setup(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act - 第一次调用，应该从服务获取
        var result1 = await _userContextService.GetCurrentUserPreferenceAsync<string>(
            "Interface", "Theme", "light");

        // Act - 第二次调用，应该从缓存获取
        var result2 = await _userContextService.GetCurrentUserPreferenceAsync<string>(
            "Interface", "Theme", "light");

        // Assert
        Assert.Equal(expectedValue, result1);
        Assert.Equal(expectedValue, result2);

        // 验证偏好设置服务只被调用一次（第二次从缓存获取）
        _mockPreferenceService.Verify(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            "light",
            It.IsAny<CancellationToken>()), Times.Once);

        // 验证缓存中有数据
        var context = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(context);
        Assert.True(context!.CachedPreferences.ContainsKey("Interface"));
        Assert.True(context.CachedPreferences["Interface"].ContainsKey("Theme"));
        Assert.Equal(expectedValue, context.CachedPreferences["Interface"]["Theme"]);
    }

    [Fact]
    public async Task SetCurrentUserPreferenceAsync_ShouldUpdateCache()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        _mockPreferenceService.Setup(x => x.SetPreferenceAsync(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            "dark",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - 设置偏好设置
        var success = await _userContextService.SetCurrentUserPreferenceAsync(
            "Interface", "Theme", "dark");

        // Assert
        Assert.True(success);

        // 验证缓存已更新
        var context = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(context);
        Assert.True(context!.CachedPreferences.ContainsKey("Interface"));
        Assert.True(context.CachedPreferences["Interface"].ContainsKey("Theme"));
        Assert.Equal("dark", context.CachedPreferences["Interface"]["Theme"]);
    }

    [Fact]
    public async Task RefreshCurrentUserContextAsync_ShouldClearAndReloadCache()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // 添加一些缓存数据
        _testUserContext.CachedPreferences["Interface"] = new Dictionary<string, object>
        {
            ["Theme"] = "old-theme"
        };

        // 模拟用户仓储返回更新的用户信息
        _mockUserRepository.Setup(x => x.GetByIdAsync(_testUserContext.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        // 模拟偏好设置服务返回新的偏好设置
        _mockPreferenceService.Setup(x => x.GetCategoryPreferencesAsync(
            _testUserContext.UserId,
            "Interface",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object?> { ["Theme"] = "new-theme" });

        _mockPreferenceService.Setup(x => x.GetCategoryPreferencesAsync(
            _testUserContext.UserId,
            "Operation",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        _mockPreferenceService.Setup(x => x.GetCategoryPreferencesAsync(
            _testUserContext.UserId,
            "Security",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        // Act - 刷新上下文
        await _userContextService.RefreshCurrentUserContextAsync();

        // Assert
        var context = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(context);

        // 验证缓存已更新
        Assert.True(context!.CachedPreferences.ContainsKey("Interface"));
        Assert.Equal("new-theme", context.CachedPreferences["Interface"]["Theme"]);
    }

    #endregion

    #region 性能测试

    [Fact]
    public async Task GetCurrentUserContextAsync_PerformanceTest_ShouldBeEfficient()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var stopwatch = Stopwatch.StartNew();
        var iterations = 1000;

        // Act - 执行大量获取操作
        for (int i = 0; i < iterations; i++)
        {
            var context = await _userContextService.GetCurrentUserContextAsync();
            Assert.NotNull(context);
        }

        stopwatch.Stop();

        // Assert - 平均每次调用应该很快（小于1ms）
        var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.True(averageTime < 1.0, $"平均响应时间 {averageTime}ms 超过预期的 1ms");
    }

    [Fact]
    public async Task GetCurrentUserId_PerformanceTest_ShouldBeVeryFast()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 设置上下文
        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        var stopwatch = Stopwatch.StartNew();
        var iterations = 10000;

        // Act - 执行大量同步获取操作
        for (int i = 0; i < iterations; i++)
        {
            var userId = _userContextService.GetCurrentUserId();
            Assert.Equal(_testUserContext.UserId, userId);
        }

        stopwatch.Stop();

        // Assert - 平均每次调用应该非常快（小于0.1ms）
        var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.True(averageTime < 0.1, $"平均响应时间 {averageTime}ms 超过预期的 0.1ms");
    }

    #endregion

    #region 异常情况和恢复机制测试

    [Fact]
    public async Task GetCurrentUserContextAsync_WhenExceptionOccurs_ShouldReturnNullAndLogError()
    {
        // Arrange - 模拟内部异常
        var mockMemoryCache = new Mock<IMemoryCache>();
        mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Throws(new InvalidOperationException("内存缓存异常"));

        var faultyService = new UserContextService(
            _mockUserRepository.Object,
            _mockPreferenceService.Object,
            _mockAuthenticationService.Object,
            _mockSecurityLogService.Object,
            mockMemoryCache.Object,
            _mockLogger.Object);

        // Act
        var result = await faultyService.GetCurrentUserContextAsync();

        // Assert
        Assert.Null(result);

        // 验证错误日志记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取当前用户上下文时发生错误")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        faultyService.Dispose();
    }

    [Fact]
    public async Task SetCurrentUserContextAsync_WhenSecurityLogFails_ShouldStillSetContext()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("安全日志服务异常"));

        // Act & Assert - 应该抛出UserContextException
        var exception = await Assert.ThrowsAsync<UserContextException>(async () =>
            await _userContextService.SetCurrentUserContextAsync(_testUserContext));

        Assert.Contains("设置用户上下文失败", exception.Message);
    }

    [Fact]
    public async Task SwitchUserContextAsync_WhenAuthenticationFails_ShouldReturnFailureResult()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var authResult = AuthenticationResult.Failure("认证失败", "AUTH_FAILED");

        _mockAuthenticationService.Setup(x => x.SwitchUserAsync(
            targetUserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _userContextService.SwitchUserContextAsync(targetUserId);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("认证失败", result.ErrorMessage);
        Assert.Equal("AUTH_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshCurrentUserContextAsync_WhenUserNotFound_ShouldClearContext()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // 模拟用户不存在
        _mockUserRepository.Setup(x => x.GetByIdAsync(_testUserContext.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        await _userContextService.RefreshCurrentUserContextAsync();

        // Assert - 上下文应该被清理
        Assert.False(_userContextService.HasActiveUserContext());
        Assert.Null(await _userContextService.GetCurrentUserContextAsync());
    }

    [Fact]
    public async Task RefreshCurrentUserContextAsync_WhenSessionRefreshFails_ShouldContinueWithOldSession()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 创建一个即将过期的会话
        var nearExpiryContext = new UserContext
        {
            UserId = _testUser.UserId,
            UserProfile = _testUser,
            SessionToken = "expiring-session-token",
            SessionId = "expiring-session-id",
            SessionExpirationTime = DateTime.UtcNow.AddMinutes(15), // 15分钟后过期
            MachineId = "test-machine-id",
            IsDefaultUser = true
        };

        await _userContextService.SetCurrentUserContextAsync(nearExpiryContext);

        _mockUserRepository.Setup(x => x.GetByIdAsync(nearExpiryContext.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        // 模拟会话刷新失败
        _mockAuthenticationService.Setup(x => x.RefreshSessionAsync(
            "expiring-session-token",
            "test-machine-id",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionRefreshResult.Failure("会话刷新失败"));

        _mockPreferenceService.Setup(x => x.GetCategoryPreferencesAsync(
            nearExpiryContext.UserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        // Act
        await _userContextService.RefreshCurrentUserContextAsync();

        // Assert - 上下文应该仍然存在，但会话令牌未更新
        var context = await _userContextService.GetCurrentUserContextAsync();
        Assert.NotNull(context);
        Assert.Equal("expiring-session-token", context!.SessionToken); // 会话令牌未更新
    }

    [Fact]
    public async Task GetCurrentUserPreferenceAsync_WhenPreferenceServiceFails_ShouldReturnDefaultValue()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        _mockPreferenceService.Setup(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("偏好设置服务异常"));

        // Act
        var result = await _userContextService.GetCurrentUserPreferenceAsync<string>(
            "Interface", "Theme", "light");

        // Assert
        Assert.Equal("light", result); // 应该返回默认值
    }

    #endregion

    #region 事件通知测试

    [Fact]
    public async Task SetCurrentUserContextAsync_ShouldTriggerUserContextChangedEvent()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UserContextChangedEventArgs? eventArgs = null;
        _userContextService.UserContextChanged += (sender, args) => eventArgs = args;

        // Act
        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs!.OldUserContext);
        Assert.NotNull(eventArgs.NewUserContext);
        Assert.Equal(_testUserContext.UserId, eventArgs.NewUserContext!.UserId);
        Assert.Equal(UserContextChangeType.UserLogin, eventArgs.ChangeType);
    }

    [Fact]
    public async Task SwitchUserContextAsync_ShouldTriggerUserContextChangedEvent()
    {
        // Arrange
        var oldUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();

        var oldUser = new UserProfile(
            oldUserId,
            "OldUser",
            "old@example.com",
            new SecuritySettings("SilentAuthentication", 30, false, DateTime.UtcNow, []));

        var newUser = new UserProfile(
            newUserId,
            "NewUser",
            "new@example.com",
            new SecuritySettings("SilentAuthentication", 30, false, DateTime.UtcNow, []));

        var oldContext = new UserContext
        {
            UserId = oldUserId,
            UserProfile = oldUser,
            SessionToken = "old-session-token",
            SessionId = "old-session-id",
            SessionExpirationTime = DateTime.UtcNow.AddHours(1),
            MachineId = "test-machine-id",
            IsDefaultUser = false
        };

        var authResult = AuthenticationResult.Success(
            newUser,
            "new-session-token",
            "new-session-id",
            DateTime.UtcNow.AddHours(1));

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAuthenticationService.Setup(x => x.SwitchUserAsync(
            newUserId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        await _userContextService.SetCurrentUserContextAsync(oldContext);

        UserContextChangedEventArgs? eventArgs = null;
        _userContextService.UserContextChanged += (sender, args) => eventArgs = args;

        // Act
        await _userContextService.SwitchUserContextAsync(newUserId);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs!.OldUserContext);
        Assert.NotNull(eventArgs.NewUserContext);
        Assert.Equal(oldUserId, eventArgs.OldUserContext!.UserId);
        Assert.Equal(newUserId, eventArgs.NewUserContext!.UserId);
        Assert.Equal(UserContextChangeType.UserSwitch, eventArgs.ChangeType);
    }

    [Fact]
    public async Task SetCurrentUserPreferenceAsync_ShouldTriggerUserPreferenceChangedEvent()
    {
        // Arrange
        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(),
            It.IsAny<SecurityEventType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _userContextService.SetCurrentUserContextAsync(_testUserContext);

        _mockPreferenceService.Setup(x => x.GetPreferenceAsync<string>(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("light");

        _mockPreferenceService.Setup(x => x.SetPreferenceAsync(
            _testUserContext.UserId,
            "Interface",
            "Theme",
            "dark",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        UserPreferenceChangedEventArgs? eventArgs = null;
        _userContextService.UserPreferenceChanged += (sender, args) => eventArgs = args;

        // Act
        await _userContextService.SetCurrentUserPreferenceAsync("Interface", "Theme", "dark");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(_testUserContext.UserId, eventArgs!.UserId);
        Assert.Equal("Interface", eventArgs.Category);
        Assert.Equal("Theme", eventArgs.Key);
        Assert.Equal("light", eventArgs.OldValue);
        Assert.Equal("dark", eventArgs.NewValue);
    }

    #endregion
}