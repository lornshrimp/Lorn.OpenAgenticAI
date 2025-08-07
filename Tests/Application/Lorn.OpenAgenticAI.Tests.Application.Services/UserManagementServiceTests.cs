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
/// 用户管理服务单元测试
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserSecurityLogRepository> _mockSecurityLogRepository;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private readonly UserManagementService _service;

    private const string TestUsername = "testuser";
    private const string TestEmail = "test@example.com";
    private const string TestMachineId = "TEST-MACHINE-001";

    public UserManagementServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSecurityLogRepository = new Mock<IUserSecurityLogRepository>();
        _mockLogger = new Mock<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _mockUserRepository.Object,
            _mockSecurityLogRepository.Object,
            _mockLogger.Object);
    }

    #region Helper Methods

    /// <summary>
    /// 创建测试用户
    /// </summary>
    private static UserProfile CreateTestUser(Guid? userId = null, string? username = null, string? email = null)
    {
        var securitySettings = new SecuritySettings(
            authenticationMethod: "Silent",
            sessionTimeoutMinutes: 1440,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );

        return new UserProfile(
            userId ?? Guid.NewGuid(),
            username ?? TestUsername,
            email ?? TestEmail,
            securitySettings
        );
    }

    /// <summary>
    /// 创建测试安全日志
    /// </summary>
    private static UserSecurityLog CreateTestSecurityLog(Guid userId, SecurityEventType eventType = SecurityEventType.UserLogin)
    {
        return UserSecurityLog.CreateOperationLog(
            userId,
            eventType,
            "测试操作",
            "测试详情",
            TestMachineId,
            null,
            true,
            null
        );
    }

    #endregion

    #region GetUserProfileAsync Tests

    [Fact]
    public async Task GetUserProfileAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetUserProfileAsync(Guid.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenUserExists_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetUserProfileAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserProfile.Should().NotBeNull();
        result.UserProfile!.UserId.Should().Be(user.UserId);
        result.UserProfile.Username.Should().Be(user.Username);
        result.UserProfile.Email.Should().Be(user.Email);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSecurityLogRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("数据库连接失败"));

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("获取用户档案信息失败");
        result.ErrorCode.Should().Be("GET_PROFILE_ERROR");
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var request = new UpdateUserProfileRequest { Username = "newusername" };

        // Act
        var result = await _service.UpdateUserProfileAsync(Guid.Empty, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenRequestIsNull_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, null!);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("更新用户档案失败");
        result.ErrorCode.Should().Be("UPDATE_PROFILE_ERROR");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenValidationFails_ShouldReturnValidationFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest
        {
            Username = "ab", // 太短，验证失败
            Email = "invalid-email" // 无效邮箱格式
        };

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
        result.ValidationErrors.Should().Contain("用户名长度必须在3-50个字符之间");
        result.ValidationErrors.Should().Contain("邮箱地址格式无效");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest { Username = "newusername" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUsernameExists_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateTestUser(userId, "oldusername");
        var request = new UpdateUserProfileRequest { Username = "existingusername" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existingusername", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名已存在");
        result.ErrorCode.Should().Be("USERNAME_EXISTS");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenEmailExists_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateTestUser(userId, "testuser", "old@example.com");
        var request = new UpdateUserProfileRequest { Email = "existing@example.com" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("existing@example.com", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("邮箱地址已存在");
        result.ErrorCode.Should().Be("EMAIL_EXISTS");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateTestUser(userId, "oldusername", "old@example.com");
        var request = new UpdateUserProfileRequest
        {
            Username = "newusername",
            Email = "new@example.com"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newusername", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("new@example.com", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile user, CancellationToken _) => user);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UpdatedProfile.Should().NotBeNull();
        result.UpdatedProfile!.Username.Should().Be("newusername");

        // 验证调用了更新方法
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUsernameAlreadyExistsExceptionThrown_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateTestUser(userId);
        var request = new UpdateUserProfileRequest { Username = "existingusername" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existingusername", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameAlreadyExistsException("用户名已存在", "USERNAME_EXISTS"));

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USERNAME_EXISTS");
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WhenRequestIsNull_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CreateUserAsync(null!);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("创建用户失败");
        result.ErrorCode.Should().Be("CREATE_USER_ERROR");
    }

    [Fact]
    public async Task CreateUserAsync_WhenValidationFails_ShouldReturnValidationFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "", // 空用户名
            Email = "invalid-email" // 无效邮箱
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
        result.ValidationErrors.Should().Contain("用户名不能为空");
        result.ValidationErrors.Should().Contain("邮箱地址格式无效");
    }

    [Fact]
    public async Task CreateUserAsync_WhenUsernameExists_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "existinguser",
            Email = "test@example.com"
        };

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existinguser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名已存在");
        result.ErrorCode.Should().Be("USERNAME_EXISTS");
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailExists_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "newuser",
            Email = "existing@example.com"
        };

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("existing@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("邮箱地址已存在");
        result.ErrorCode.Should().Be("EMAIL_EXISTS");
    }

    [Fact]
    public async Task CreateUserAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "newuser",
            Email = "new@example.com"
        };

        var createdUser = CreateTestUser(Guid.NewGuid(), "newuser", "new@example.com");

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("new@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CreatedUser.Should().NotBeNull();
        result.CreatedUser!.Username.Should().Be("newuser");
        result.CreatedUser.Email.Should().Be("new@example.com");
        result.UserId.Should().Be(createdUser.UserId);

        // 验证调用了添加方法
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenUsernameAlreadyExistsExceptionThrown_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "newuser",
            Email = "new@example.com"
        };

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("new@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameAlreadyExistsException("用户名已存在", "USERNAME_EXISTS"));

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USERNAME_EXISTS");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.DeleteUserAsync(Guid.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenSoftDelete_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.SoftDeleteAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteUserAsync(user.UserId, hardDelete: false);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.DeletedUserId.Should().Be(user.UserId);
        result.IsHardDelete.Should().BeFalse();

        // 验证调用了软删除方法
        _mockUserRepository.Verify(x => x.SoftDeleteAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenHardDelete_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.DeleteAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockSecurityLogRepository.Setup(x => x.DeleteUserLogsAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteUserAsync(user.UserId, hardDelete: true);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.DeletedUserId.Should().Be(user.UserId);
        result.IsHardDelete.Should().BeTrue();

        // 验证调用了硬删除方法
        _mockUserRepository.Verify(x => x.DeleteAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        // 验证删除了用户日志
        _mockSecurityLogRepository.Verify(x => x.DeleteUserLogsAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenDeleteFails_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.SoftDeleteAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("删除用户失败");
        result.ErrorCode.Should().Be("DELETE_USER_ERROR");
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.ActivateUserAsync(Guid.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task ActivateUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.ActivateUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateUserAsync_WhenUserAlreadyActive_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        // 用户默认是激活状态
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ActivateUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.OperationType.Should().Be("激活用户");

        // 验证没有调用更新方法（因为用户已经是激活状态）
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateUserAsync_WhenUserInactive_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate(); // 停用用户

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ActivateUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.OperationType.Should().Be("激活用户");

        // 验证调用了更新方法
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.DeactivateUserAsync(Guid.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.DeactivateUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserAlreadyInactive_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate(); // 停用用户

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.DeactivateUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.OperationType.Should().Be("停用用户");

        // 验证没有调用更新方法（因为用户已经是停用状态）
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserActive_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        // 用户默认是激活状态

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeactivateUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.OperationType.Should().Be("停用用户");

        // 验证调用了更新方法
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);

        // 验证记录了操作日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WhenRequestIsNull_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetUsersAsync(null!);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("获取用户列表失败");
        result.ErrorCode.Should().Be("GET_USERS_ERROR");
    }

    [Fact]
    public async Task GetUsersAsync_WhenPageIndexIsNegative_ShouldReturnFailure()
    {
        // Arrange
        var request = new GetUsersRequest { PageIndex = -1 };

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("页索引不能为负数");
        result.ErrorCode.Should().Be("INVALID_PAGE_INDEX");
    }

    [Fact]
    public async Task GetUsersAsync_WhenPageSizeInvalid_ShouldReturnFailure()
    {
        // Arrange
        var request = new GetUsersRequest { PageSize = 0 };

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("页大小必须在1-100之间");
        result.ErrorCode.Should().Be("INVALID_PAGE_SIZE");
    }

    [Fact]
    public async Task GetUsersAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser(Guid.NewGuid(), "user1", "user1@example.com"),
            CreateTestUser(Guid.NewGuid(), "user2", "user2@example.com")
        };

        var request = new GetUsersRequest
        {
            PageIndex = 0,
            PageSize = 10,
            ActiveOnly = true
        };

        _mockUserRepository.Setup(x => x.GetUsersPagedAsync(0, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 2));

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Users.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageIndex.Should().Be(0);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetUsersAsync_WhenSearchKeywordProvided_ShouldFilterResults()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser(Guid.NewGuid(), "testuser1", "test1@example.com"),
            CreateTestUser(Guid.NewGuid(), "user2", "user2@example.com"),
            CreateTestUser(Guid.NewGuid(), "testuser3", "test3@example.com")
        };

        var request = new GetUsersRequest
        {
            PageIndex = 0,
            PageSize = 10,
            SearchKeyword = "test"
        };

        _mockUserRepository.Setup(x => x.GetUsersPagedAsync(0, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 3));

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Users.Should().HaveCount(2); // 只有包含"test"的用户
        result.Users.Should().OnlyContain(u => u.Username.Contains("test") || u.Email.Contains("test"));
    }

    #endregion

    #region ValidateUsernameAsync Tests

    [Fact]
    public async Task ValidateUsernameAsync_WhenUsernameIsEmpty_ShouldReturnNotAvailable()
    {
        // Act
        var result = await _service.ValidateUsernameAsync("");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名不能为空");
    }

    [Fact]
    public async Task ValidateUsernameAsync_WhenUsernameTooShort_ShouldReturnNotAvailable()
    {
        // Act
        var result = await _service.ValidateUsernameAsync("ab");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名长度必须在3-50个字符之间");
    }

    [Fact]
    public async Task ValidateUsernameAsync_WhenUsernameInvalidFormat_ShouldReturnNotAvailable()
    {
        // Act
        var result = await _service.ValidateUsernameAsync("user@name");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名只能包含字母、数字、下划线和连字符");
    }

    [Fact]
    public async Task ValidateUsernameAsync_WhenUsernameExists_ShouldReturnNotAvailableWithSuggestions()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existinguser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existinguser1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("existinguser2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateUsernameAsync("existinguser");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名已存在");
        result.SuggestedUsernames.Should().NotBeEmpty();
        result.SuggestedUsernames.Should().Contain("existinguser1");
    }

    [Fact]
    public async Task ValidateUsernameAsync_WhenUsernameAvailable_ShouldReturnAvailable()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateUsernameAsync("newuser");

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.Message.Should().Be("用户名可用");
    }

    #endregion

    #region ValidateEmailAsync Tests

    [Fact]
    public async Task ValidateEmailAsync_WhenEmailIsEmpty_ShouldReturnNotAvailable()
    {
        // Act
        var result = await _service.ValidateEmailAsync("");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("邮箱地址不能为空");
    }

    [Fact]
    public async Task ValidateEmailAsync_WhenEmailInvalidFormat_ShouldReturnNotAvailable()
    {
        // Act
        var result = await _service.ValidateEmailAsync("invalid-email");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("邮箱地址格式无效");
    }

    [Fact]
    public async Task ValidateEmailAsync_WhenEmailExists_ShouldReturnNotAvailable()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("existing@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateEmailAsync("existing@example.com");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("邮箱地址已存在");
    }

    [Fact]
    public async Task ValidateEmailAsync_WhenEmailAvailable_ShouldReturnAvailable()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("new@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateEmailAsync("new@example.com");

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.Message.Should().Be("邮箱地址可用");
    }

    #endregion

    #region GetUserOperationHistoryAsync Tests

    [Fact]
    public async Task GetUserOperationHistoryAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var request = new GetOperationHistoryRequest();

        // Act
        var result = await _service.GetUserOperationHistoryAsync(Guid.Empty, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task GetUserOperationHistoryAsync_WhenRequestIsNull_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetUserOperationHistoryAsync(Guid.NewGuid(), null!);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("获取用户操作历史失败");
        result.ErrorCode.Should().Be("GET_OPERATION_HISTORY_ERROR");
    }

    [Fact]
    public async Task GetUserOperationHistoryAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new GetOperationHistoryRequest();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserOperationHistoryAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetUserOperationHistoryAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var logs = new[]
        {
            CreateTestSecurityLog(user.UserId, SecurityEventType.UserLogin),
            CreateTestSecurityLog(user.UserId, SecurityEventType.UserProfileUpdated)
        };

        var request = new GetOperationHistoryRequest
        {
            PageIndex = 0,
            PageSize = 10
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockSecurityLogRepository.Setup(x => x.GetUserLogsAsync(
            user.UserId, null, null, null, null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);
        _mockSecurityLogRepository.Setup(x => x.GetUserLogCountAsync(
            user.UserId, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.GetUserOperationHistoryAsync(user.UserId, request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.OperationHistory.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageIndex.Should().Be(0);
        result.PageSize.Should().Be(10);
    }

    #endregion

    #region GetUserStatisticsAsync Tests

    [Fact]
    public async Task GetUserStatisticsAsync_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetUserStatisticsAsync(Guid.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户ID不能为空");
        result.ErrorCode.Should().Be("INVALID_USER_ID");
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserStatisticsAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不存在");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var lastLoginTime = DateTime.UtcNow.AddHours(-1);
        var lastActivityTime = DateTime.UtcNow.AddMinutes(-30);

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockSecurityLogRepository.Setup(x => x.GetLastLoginTimeAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastLoginTime);
        _mockSecurityLogRepository.Setup(x => x.GetLastActivityTimeAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastActivityTime);
        _mockSecurityLogRepository.Setup(x => x.GetUserLogCountAsync(
            user.UserId, null, null, It.Is<IEnumerable<SecurityEventType>>(e => e.Contains(SecurityEventType.UserLogin)), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _mockSecurityLogRepository.Setup(x => x.GetUserLogCountAsync(
            user.UserId, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);
        _mockSecurityLogRepository.Setup(x => x.GetUserLogsAsync(
            user.UserId, null, null, null, null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateTestSecurityLog(user.UserId),
                CreateTestSecurityLog(user.UserId)
            });

        // Act
        var result = await _service.GetUserStatisticsAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CreatedTime.Should().Be(user.CreatedTime);
        result.LastLoginTime.Should().Be(lastLoginTime);
        result.LastActivityTime.Should().Be(lastActivityTime);
        result.TotalLogins.Should().Be(5);
        result.TotalOperations.Should().Be(20);
        result.ActiveDays.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region 并发操作测试

    [Fact]
    public async Task UpdateUserProfileAsync_ConcurrentUpdates_ShouldHandleCorrectly()
    {
        // Arrange
        var user = CreateTestUser();
        var request1 = new UpdateUserProfileRequest { Username = "newusername1" };
        var request2 = new UpdateUserProfileRequest { Username = "newusername2" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync(It.IsAny<string>(), user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 并发执行更新操作
        var task1 = _service.UpdateUserProfileAsync(user.UserId, request1);
        var task2 = _service.UpdateUserProfileAsync(user.UserId, request2);

        var results = await Task.WhenAll(task1, task2);

        // Assert - 两个操作都应该成功（在实际场景中可能需要更复杂的并发控制）
        results[0].IsSuccessful.Should().BeTrue();
        results[1].IsSuccessful.Should().BeTrue();

        // 验证更新方法被调用了两次
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateUserAsync_ConcurrentCreations_ShouldHandleCorrectly()
    {
        // Arrange
        var request1 = new CreateUserRequest { Username = "user1", Email = "user1@example.com" };
        var request2 = new CreateUserRequest { Username = "user2", Email = "user2@example.com" };

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("user1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("user2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("user1@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("user2@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var callCount = 0;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile user, CancellationToken _) =>
            {
                callCount++;
                return new UserProfile(Guid.NewGuid(), $"user{callCount}", user.Email, user.SecuritySettings);
            });

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 并发执行创建操作
        var task1 = _service.CreateUserAsync(request1);
        var task2 = _service.CreateUserAsync(request2);

        var results = await Task.WhenAll(task1, task2);

        // Assert - 两个操作都应该成功
        results[0].IsSuccessful.Should().BeTrue();
        results[1].IsSuccessful.Should().BeTrue();
        results[0].CreatedUser!.UserId.Should().NotBe(results[1].CreatedUser!.UserId);

        // 验证添加方法被调用了两次
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region 异常处理和数据回滚测试

    [Fact]
    public async Task UpdateUserProfileAsync_WhenRepositoryThrowsException_ShouldReturnFailureAndLogError()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new UpdateUserProfileRequest { Username = "newusername" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newusername", user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("数据库连接失败"));

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateUserProfileAsync(user.UserId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("更新用户档案失败");
        result.ErrorCode.Should().Be("UPDATE_PROFILE_ERROR");

        // 验证记录了错误日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(
            It.Is<UserSecurityLog>(log => !log.IsSuccessful && log.ErrorCode == "UPDATE_PROFILE_ERROR"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateUserRequest { Username = "newuser", Email = "new@example.com" };

        _mockUserRepository.Setup(x => x.IsUsernameExistsAsync("newuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.IsEmailExistsAsync("new@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("数据库连接失败"));

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("创建用户失败");
        result.ErrorCode.Should().Be("CREATE_USER_ERROR");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenRepositoryThrowsException_ShouldReturnFailureAndLogError()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.SoftDeleteAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("数据库连接失败"));

        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSecurityLogRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteUserAsync(user.UserId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("删除用户失败");
        result.ErrorCode.Should().Be("DELETE_USER_ERROR");

        // 验证记录了错误日志
        _mockSecurityLogRepository.Verify(x => x.AddAsync(
            It.Is<UserSecurityLog>(log => !log.IsSuccessful && log.ErrorCode == "DELETE_USER_ERROR"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogUserOperationAsync_WhenSecurityLogRepositoryThrowsException_ShouldNotAffectMainOperation()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // 模拟安全日志记录失败
        _mockSecurityLogRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("日志记录失败"));

        // Act
        var result = await _service.GetUserProfileAsync(user.UserId);

        // Assert - 主要操作应该仍然成功
        result.IsSuccessful.Should().BeTrue();
        result.UserProfile.Should().NotBeNull();

        // 验证尝试记录了日志（即使失败）
        _mockSecurityLogRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}