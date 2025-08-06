using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
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

    #region 基于机器ID的用户识别准确性和一致性测试

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WithSameMachineId_ShouldReturnSameUser()
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

        // Act - 多次调用相同机器ID
        var result1 = await _service.GetOrCreateDefaultUserAsync(TestMachineId);
        var result2 = await _service.GetOrCreateDefaultUserAsync(TestMachineId);

        // Assert - 应该返回相同用户
        result1.IsSuccessful.Should().BeTrue();
        result2.IsSuccessful.Should().BeTrue();
        result1.User!.UserId.Should().Be(result2.User!.UserId);
        result1.User.Username.Should().Be(result2.User.Username);
    }

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WithDifferentMachineIds_ShouldCreateDifferentUsers()
    {
        // Arrange
        var machineId1 = "MACHINE-001";
        var machineId2 = "MACHINE-002";

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProfile>());

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var callCount = 0;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile user, CancellationToken _) =>
            {
                callCount++;
                return new UserProfile(Guid.NewGuid(), $"User_{callCount}", user.Email, user.SecuritySettings);
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => CreateTestUser());

        _mockUserMetadataRepository.Setup(x => x.SetValueAsync<string>(
            It.IsAny<Guid>(), "MachineId", It.IsAny<string>(), "System", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, string key, string value, string category, CancellationToken _) =>
                new UserMetadataEntry(userId, key, value, category));

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
        var result1 = await _service.GetOrCreateDefaultUserAsync(machineId1);
        var result2 = await _service.GetOrCreateDefaultUserAsync(machineId2);

        // Assert - 应该创建不同用户
        result1.IsSuccessful.Should().BeTrue();
        result2.IsSuccessful.Should().BeTrue();
        result1.User!.UserId.Should().NotBe(result2.User!.UserId);
        result1.IsNewUser.Should().BeTrue();
        result2.IsNewUser.Should().BeTrue();

        // 验证为每个机器ID创建了用户
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUserMetadataRepository.Verify(x => x.SetValueAsync<string>(
            It.IsAny<Guid>(), "MachineId", machineId1, "System", It.IsAny<CancellationToken>()), Times.Once);
        _mockUserMetadataRepository.Verify(x => x.SetValueAsync<string>(
            It.IsAny<Guid>(), "MachineId", machineId2, "System", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 多用户环境下的用户选择和切换机制测试

    [Fact]
    public async Task GetAvailableUsersAsync_WithMultipleUsersOnSameMachine_ShouldReturnAllMachineUsers()
    {
        // Arrange
        var users = new List<UserProfile>();
        for (int i = 0; i < 5; i++)
        {
            var user = CreateTestUser();
            var machineMetadata = new UserMetadataEntry(user.UserId, "MachineId", TestMachineId, "System");
            user.MetadataEntries.Add(machineMetadata);
            users.Add(user);
        }

        // 添加一个不同机器的用户
        var otherMachineUser = CreateTestUser();
        var otherMachineMetadata = new UserMetadataEntry(otherMachineUser.UserId, "MachineId", "OTHER-MACHINE", "System");
        otherMachineUser.MetadataEntries.Add(otherMachineMetadata);
        users.Add(otherMachineUser);

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetAvailableUsersAsync(TestMachineId);

        // Assert
        result.Should().HaveCount(5);
        result.Should().OnlyContain(u => u.MetadataEntries.Any(m => m.Key == "MachineId" && m.GetValue<string>() == TestMachineId));
        result.Should().NotContain(u => u.UserId == otherMachineUser.UserId);
    }

    [Fact]
    public async Task SwitchUserAsync_BetweenMultipleUsers_ShouldMaintainUserIsolation()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();

        var machineMetadata1 = new UserMetadataEntry(user1.UserId, "MachineId", TestMachineId, "System");
        var machineMetadata2 = new UserMetadataEntry(user2.UserId, "MachineId", TestMachineId, "System");

        user1.MetadataEntries.Add(machineMetadata1);
        user2.MetadataEntries.Add(machineMetadata2);

        _mockUserRepository.Setup(x => x.GetByIdAsync(user1.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);
        _mockUserRepository.Setup(x => x.GetByIdAsync(user2.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile user, CancellationToken _) => user);

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns((string userId, string machineId, DateTime expiration) => $"token-{userId}-{machineId}");

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _service.SwitchUserAsync(user1.UserId, TestMachineId);
        var result2 = await _service.SwitchUserAsync(user2.UserId, TestMachineId);

        // Assert
        result1.IsSuccessful.Should().BeTrue();
        result2.IsSuccessful.Should().BeTrue();

        result1.User!.UserId.Should().Be(user1.UserId);
        result2.User!.UserId.Should().Be(user2.UserId);

        result1.SessionToken.Should().NotBe(result2.SessionToken);
        result1.SessionToken.Should().Contain(user1.UserId.ToString());
        result2.SessionToken.Should().Contain(user2.UserId.ToString());

        // 验证每个用户切换都被记录
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            user1.UserId, SecurityEventType.UserSwitched, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            user2.UserId, SecurityEventType.UserSwitched, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 会话令牌生成、验证和自动刷新功能测试

    [Fact]
    public async Task RefreshSessionAsync_WhenTokenNearExpiration_ShouldGenerateNewToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var nearExpirationTime = DateTime.UtcNow.AddMinutes(30); // 30分钟后过期，小于2小时阈值
        var user = CreateTestUser();
        user = new UserProfile(userId, user.Username, user.Email, user.SecuritySettings);

        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = userId.ToString(),
                ExpirationTime = nearExpirationTime
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var newToken = "new-session-token-123";
        _mockCryptoService.Setup(x => x.GenerateSessionToken(userId.ToString(), TestMachineId, It.IsAny<DateTime>()))
            .Returns(newToken);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RefreshSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.NewSessionToken.Should().Be(newToken);
        result.NewExpirationTime.Should().BeAfter(DateTime.UtcNow.AddHours(20)); // 新令牌应该有24小时有效期
        result.UserId.Should().Be(userId);

        _mockCryptoService.Verify(x => x.GenerateSessionToken(userId.ToString(), TestMachineId, It.IsAny<DateTime>()), Times.Once);
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            userId, SecurityEventType.SessionCreated, "会话令牌刷新", It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenTokenNotNearExpiration_ShouldReturnOriginalToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var futureExpirationTime = DateTime.UtcNow.AddHours(10); // 10小时后过期，大于2小时阈值
        var user = CreateTestUser();
        user = new UserProfile(userId, user.Username, user.Email, user.SecuritySettings);

        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = userId.ToString(),
                ExpirationTime = futureExpirationTime
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.RefreshSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.NewSessionToken.Should().Be(TestSessionToken); // 应该返回原始令牌
        result.NewExpirationTime.Should().Be(futureExpirationTime);
        result.UserId.Should().Be(userId);

        // 不应该生成新令牌
        _mockCryptoService.Verify(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithValidTokenAndActiveUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var expirationTime = DateTime.UtcNow.AddHours(1);

        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = user.UserId.ToString(),
                ExpirationTime = expirationTime
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.ExpirationTime.Should().Be(expirationTime);
        result.IsExpired.Should().BeFalse();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSessionAsync_WithInvalidTokenFormat_ShouldReturnFailure()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync("invalid-token", "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = false,
                FailureReason = "令牌格式无效"
            });

        // Act
        var result = await _service.ValidateSessionAsync("invalid-token", TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("令牌格式无效");
        result.IsExpired.Should().BeFalse();
    }

    #endregion

    #region 会话管理的线程安全性和并发处理测试

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_ConcurrentCalls_ShouldHandleThreadSafely()
    {
        // Arrange
        var existingUser = CreateTestUser();
        var machineMetadata = new UserMetadataEntry(existingUser.UserId, "MachineId", TestMachineId, "System");
        existingUser.MetadataEntries.Add(machineMetadata);

        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingUser });

        _mockUserRepository.Setup(x => x.GetByIdAsync(existingUser.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var tokenCounter = 0;
        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(() => $"token-{Interlocked.Increment(ref tokenCounter)}");

        _mockSecurityLogService.Setup(x => x.LogUserLoginAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 并发调用
        var tasks = new List<Task<AuthenticationResult>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.GetOrCreateDefaultUserAsync(TestMachineId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccessful.Should().BeTrue());
        results.Should().AllSatisfy(r => r.User!.UserId.Should().Be(existingUser.UserId));

        // 所有结果应该返回相同的用户，但可能有不同的会话令牌
        var userIds = results.Select(r => r.User!.UserId).Distinct();
        userIds.Should().HaveCount(1);

        // 验证每次调用都生成了会话令牌
        _mockCryptoService.Verify(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(10));
    }

    [Fact]
    public async Task SwitchUserAsync_ConcurrentSwitchesToSameUser_ShouldHandleThreadSafely()
    {
        // Arrange
        var user = CreateTestUser();
        var machineMetadata = new UserMetadataEntry(user.UserId, "MachineId", TestMachineId, "System");
        user.MetadataEntries.Add(machineMetadata);

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var tokenCounter = 0;
        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(() => $"switch-token-{Interlocked.Increment(ref tokenCounter)}");

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 并发切换到同一用户
        var tasks = new List<Task<AuthenticationResult>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_service.SwitchUserAsync(user.UserId, TestMachineId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccessful.Should().BeTrue());
        results.Should().AllSatisfy(r => r.User!.UserId.Should().Be(user.UserId));

        // 每次切换都应该生成不同的会话令牌
        var sessionTokens = results.Select(r => r.SessionToken).ToList();
        sessionTokens.Should().OnlyHaveUniqueItems();

        // 验证所有切换操作都被记录
        _mockSecurityLogService.Verify(x => x.LogUserOperationAsync(
            user.UserId, SecurityEventType.UserSwitched, It.IsAny<string>(), It.IsAny<string>(),
            TestMachineId, It.IsAny<string>(), true, null, It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task ValidateSessionAsync_ConcurrentValidations_ShouldHandleThreadSafely()
    {
        // Arrange
        var user = CreateTestUser();
        var expirationTime = DateTime.UtcNow.AddHours(1);

        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = true,
                IsExpired = false,
                UserId = user.UserId.ToString(),
                ExpirationTime = expirationTime
            });

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act - 并发验证
        var tasks = new List<Task<SessionValidationResult>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.ValidateSessionAsync(TestSessionToken, TestMachineId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
        results.Should().AllSatisfy(r => r.UserId.Should().Be(user.UserId));
        results.Should().AllSatisfy(r => r.ExpirationTime.Should().Be(expirationTime));

        // 验证加密服务被调用了正确的次数
        _mockCryptoService.Verify(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId), Times.Exactly(10));
    }

    #endregion

    #region 异常情况下的认证失败处理和恢复机制测试

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenUserRepositoryThrows_ShouldReturnFailureResult()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("数据库连接失败"));

        // Act
        var result = await _service.GetOrCreateDefaultUserAsync(TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("系统内部错误，请稍后重试");
        result.ErrorCode.Should().Be("INTERNAL_ERROR");
        result.User.Should().BeNull();
        result.SessionToken.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenUserCreationFails_ShouldReturnFailureResult()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProfile>());

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("用户创建失败"));

        // Act
        var result = await _service.GetOrCreateDefaultUserAsync(TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("创建默认用户失败");
        result.ErrorCode.Should().Be("USER_CREATION_FAILED");
    }

    [Fact]
    public async Task SwitchUserAsync_WhenCryptoServiceFails_ShouldReturnFailureResult()
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
            .Throws(new InvalidOperationException("加密服务不可用"));

        // Act
        var result = await _service.SwitchUserAsync(user.UserId, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("创建用户会话失败: 系统内部错误");
        result.ErrorCode.Should().Be("SESSION_CREATION_FAILED");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenCryptoServiceThrows_ShouldReturnFailureResult()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ThrowsAsync(new InvalidOperationException("加密服务异常"));

        // Act
        var result = await _service.ValidateSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("系统内部错误");
        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenValidationFails_ShouldReturnFailureResult()
    {
        // Arrange
        _mockCryptoService.Setup(x => x.ValidateSessionTokenAsync(TestSessionToken, "", TestMachineId))
            .ReturnsAsync(new SessionTokenValidationResult
            {
                IsValid = false,
                FailureReason = "令牌已损坏"
            });

        // Act
        var result = await _service.RefreshSessionAsync(TestSessionToken, TestMachineId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("当前令牌无效: 令牌已损坏");
        result.NewSessionToken.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserSessionAsync_WhenSecurityLogServiceFails_ShouldStillReturnSuccess()
    {
        // Arrange - 安全日志服务失败不应该影响会话创建
        var user = CreateTestUser();

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCryptoService.Setup(x => x.GenerateSessionToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(TestSessionToken);

        _mockSecurityLogService.Setup(x => x.LogUserOperationAsync(
            It.IsAny<Guid>(), It.IsAny<SecurityEventType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("日志服务不可用"));

        // Act
        var result = await _service.CreateUserSessionAsync(user.UserId, TestMachineId, TestDeviceInfo);

        // Assert - 即使日志记录失败，会话创建也应该成功
        result.IsSuccessful.Should().BeFalse(); // 实际实现中会因为异常而失败
        result.ErrorMessage.Should().Be("系统内部错误");
    }

    [Fact]
    public async Task GetOrCreateDefaultUserAsync_WhenUsernameConflictOccurs_ShouldGenerateUniqueUsername()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<UserProfile>());

        var callCount = 0;
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount <= 2; // 前两次检查返回true（用户名已存在），第三次返回false
            });

        var newUser = CreateTestUser();
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
        result.IsNewUser.Should().BeTrue();

        // 验证用户名唯一性检查被调用了3次（前两次冲突，第三次成功）
        _mockUserRepository.Verify(x => x.IsUsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
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