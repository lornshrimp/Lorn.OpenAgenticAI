using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// 静默认证服务单元测试
/// </summary>
public class SilentAuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserMetadataRepository> _mockUserMetadataRepository;
    private readonly Mock<ICryptoService> _mockCryptoService;
    private readonly Mock<ISecurityLogService> _mockSecurityLogService;
    private readonly Mock<ILogger<SilentAuthenticationService>> _mockLogger;
    private readonly SilentAuthenticationService _service;

    private const string TestMachineId = "TEST-MACHINE-001";
    private const string TestSessionToken = "test-session-token-123";
    private const string TestDeviceInfo = "Windows 11 Pro";

    public SilentAuthenticationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserMetadataRepository = new Mock<IUserMetadataRepository>();
        _mockCryptoService = new Mock<ICryptoService>();
        _mockSecurityLogService = new Mock<ISecurityLogService>();
        _mockLogger = new Mock<ILogger<SilentAuthenticationService>>();

        _service = new SilentAuthenticationService(
            _mockUserRepository.Object,
            _mockUserMetadataRepository.Object,
            _mockCryptoService.Object,
            _mockSecurityLogService.Object,
            _mockLogger.Object);
    }

    #region GetOrCreateDefaultUserAsync Tests

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenMachineIdIsEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        var result = await _service.GetOrCreateDefaultUserAsync(string.Empty);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenExistingUserFound_ShouldReturnExistingUser()
    {
        // Arrange
        var existingUser = CreateTestUser();
        var machineMetadata = new UserMetadataEntry(existingUser.UserId, "MachineId", TestMachineId, "System");
        existingUser.MetadataEntries.Add(machineMetadata);

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingUser });

        _mockUserRepository.Setup(x => x.GetByIdAsync(existingUser.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(TestSessionToken);

        _mockSecurityLogService.Setup(x => x.LogUserLoginAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetOrCreateDefaultUserAsync(TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be(existingUser.UserId);
        result.SessionToken.Should().Be(TestSessionToken);
        result.IsNewUser.Should().BeFalse();

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSecurityLogService.Verify(x => x.LogUserLoginAsync(
            existingUser.UserId, TestMachineId, It.IsAny<string>(), It.IsAny<string>(),
            true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenNoExistingUser_ShouldCreateNewUser()
    {
        // Arrange
        var newUser = CreateTestUser();

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProfile>());

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        _mockUserRepository.Setup(x => x.GetByIdAsync(newUser.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        _mockUserMetadataRepository.Setup(x => x.SetValueAsync<string>(
            It.IsAny<Guid>(), "MachineId", TestMachineId, "System", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMetadataEntry(newUser.UserId, "MachineId", TestMachineId, "System"));

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(TestSessionToken);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockSecurityLogService.Setup(x => x.LogUserLoginAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetOrCreateDefaultUserAsync(TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.SessionToken.Should().Be(TestSessionToken);
        result.IsNewUser.Should().BeTrue();

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserMetadataRepository.Verify(x => x.SetValueAsync<string>(
            It.IsAny<Guid>(), "MachineId", TestMachineId, "System", It.IsAny<CancellationToken>()), Times.Once);
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), SecurityEventType.UserCreated, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SwitchUserAsync Tests

    [Fact]
    public async Task SwitchUserAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.SwitchUserAsync(Guid.Empty, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SwitchUserAsync_WhenMachineIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.SwitchUserAsync(Guid.NewGuid(), string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SwitchUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.SwitchUserAsync(userId, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task SwitchUserAsync_WhenUserInactive_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.SwitchUserAsync(user.UserId, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_INACTIVE");
    }

    [Fact]
    public async Task SwitchUserAsync_WhenMachineIdMismatch_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var machineMetadata = new UserMetadataEntry(user.UserId, "MachineId", "DIFFERENT-MACHINE", "System");
        user.MetadataEntries.Add(machineMetadata);

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.SwitchUserAsync(user.UserId, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("MACHINE_ID_MISMATCH");
    }

    [Fact]
    public async Task SwitchUserAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var machineMetadata = new UserMetadataEntry(user.UserId, "MachineId", TestMachineId, "System");
        user.MetadataEntries.Add(machineMetadata);

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(TestSessionToken);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SwitchUserAsync(user.UserId, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be(user.UserId);
        result.SessionToken.Should().Be(TestSessionToken);
        result.IsNewUser.Should().BeFalse();

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            user.UserId, SecurityEventType.UserSwitched, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ValidateSessionAsync Tests

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionTokenIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ValidateSessionAsync(string.Empty, TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("会话令牌不能为空");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenMachineIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, string.Empty);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("机器ID不能为空");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenTokenInvalid_ShouldReturnFailure()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult { IsValid = false, FailureReason = "令牌格式无效" });

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("令牌格式无效");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenTokenExpired_ShouldReturnFailure()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult { IsValid = true, IsExpired = true });

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.FailureReason.Should().Be("会话已过期");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = userId.ToString(),
                ExpirationTime = DateTime.UtcNow.AddHours(1)
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("用户不存在或已停用");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = user.UserId.ToString(),
                ExpirationTime = DateTime.UtcNow.AddHours(1)
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.ExpirationTime.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region RefreshSessionAsync Tests

    [Fact]
    public async Task RefreshSessionAsync_WhenSessionTokenIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.RefreshSessionAsync(string.Empty, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("会话令牌不能为空");
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenMachineIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.RefreshSessionAsync(TestSessionToken, string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("机器ID不能为空");
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenCurrentTokenInvalid_ShouldReturnFailure()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult { IsValid = false, FailureReason = "令牌无效" });

        // Act
        var result = await _service.RefreshSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("当前令牌无效");
    }

    #endregion

    #region GetAvailableUsersAsync Tests

    [Fact]
    public async Task GetAvailableUsersAsync_WhenMachineIdIsEmpty_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetAvailableUsersAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableUsersAsync_WhenValidMachineId_ShouldReturnMachineUsers()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        var user3 = CreateTestUser();

        var machineMetadata1 = new UserMetadataEntry(user1.UserId, "MachineId", TestMachineId, "System");
        var machineMetadata2 = new UserMetadataEntry(user2.UserId, "MachineId", TestMachineId, "System");
        var machineMetadata3 = new UserMetadataEntry(user3.UserId, "MachineId", "DIFFERENT-MACHINE", "System");

        user1.MetadataEntries.Add(machineMetadata1);
        user2.MetadataEntries.Add(machineMetadata2);
        user3.MetadataEntries.Add(machineMetadata3);

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user1, user2, user3 });

        // Act
        var result = await _service.GetAvailableUsersAsync(TestMachineId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.UserId == user1.UserId);
        result.Should().Contain(u => u.UserId == user2.UserId);
        result.Should().NotContain(u => u.UserId == user3.UserId);
    }

    #endregion

    #region CreateUserSessionAsync Tests

    [Fact]
    public async Task CreateUserSessionAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CreateUserSessionAsync(Guid.Empty, TestMachineId, TestDeviceInfo);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WhenMachineIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CreateUserSessionAsync(Guid.NewGuid(), string.Empty, TestDeviceInfo);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("机器ID不能为空");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.CreateUserSessionAsync(userId, TestMachineId, TestDeviceInfo);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WhenUserInactive_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateUserSessionAsync(user.UserId, TestMachineId, TestDeviceInfo);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户已被停用");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(TestSessionToken);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateUserSessionAsync(user.UserId, TestMachineId, TestDeviceInfo);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.SessionToken.Should().Be(TestSessionToken);
        result.UserId.Should().Be(user.UserId);
        result.ExpirationTime.Should().BeAfter(DateTime.UtcNow);

        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            user.UserId, SecurityEventType.SessionCreated, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region EndUserSessionAsync Tests

    [Fact]
    public async Task EndUserSessionAsync_WhenSessionTokenIsEmpty_ShouldReturnFalse()
    {
        // Act
        var result = await _service.EndUserSessionAsync(string.Empty, TestMachineId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EndUserSessionAsync_WhenMachineIdIsEmpty_ShouldReturnFalse()
    {
        // Act
        var result = await _service.EndUserSessionAsync(TestSessionToken, string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EndUserSessionAsync_WhenValidSession_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var user = CreateTestUser();
        user = new UserProfile(userId, user.Username, user.Email, user.SecuritySettings);

        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                UserId = userId.ToString(),
                ExpirationTime = DateTime.UtcNow.AddHours(1)
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockSecurityLogService.Setup(x => x.LogUserLogoutAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EndUserSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.Should().BeTrue();
        _mockSecurityLogService.Verify(x => x.LogUserLogoutAsync(
            userId, TestMachineId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetActiveSessionsAsync Tests

    [Fact]
    public async Task GetActiveSessionsAsync_WhenUserIdIsEmpty_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetActiveSessionsAsync(Guid.Empty, TestMachineId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_WhenMachineIdIsEmpty_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetActiveSessionsAsync(Guid.NewGuid(), string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_WhenValidParameters_ShouldReturnEmpty()
    {
        // Note: Current implementation returns empty as there's no session storage yet
        // Act
        var result = await _service.GetActiveSessionsAsync(Guid.NewGuid(), TestMachineId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建测试用户
    /// </summary>
    private static UserProfile CreateTestUser()
    {
        var securitySettings = new SecuritySettings(
            "SilentAuthentication",
            1440, // 24 hours
            false,
            DateTime.UtcNow,
            new Dictionary<string, string>());

        return new UserProfile(
            Guid.NewGuid(),
            $"TestUser_{Guid.NewGuid():N}",
            "test@example.com",
            securitySettings);
    }

    #endregion
}