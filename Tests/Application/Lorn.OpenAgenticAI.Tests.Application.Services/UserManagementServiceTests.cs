using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Exceptions;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Shared.Contracts.DTOs;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// UserManagementService 单元测试 (重构版本 - 适配IUserDataService)
/// </summary>
public class UserManagementServiceTests_New
{
    private readonly Mock<IUserDataService> _mockUserDataService;
    private readonly Mock<IUserSecurityLogRepository> _mockSecurityLogRepository;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private readonly UserManagementService _service;

    private const string TestUsername = "testuser";
    private const string TestEmail = "test@example.com";
    private const string TestMachineId = "TEST-MACHINE-001";

    public UserManagementServiceTests_New()
    {
        _mockUserDataService = new Mock<IUserDataService>();
        _mockSecurityLogRepository = new Mock<IUserSecurityLogRepository>();
        _mockLogger = new Mock<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _mockUserDataService.Object,
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
            userId: userId ?? Guid.NewGuid(),
            username: username ?? TestUsername,
            email: email ?? TestEmail,
            securitySettings: securitySettings
        );
    }

    #endregion

    #region ChangeUsernameAsync Tests

    [Fact]
    public async Task ChangeUsernameAsync_ValidNewUsername_ReturnsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "olduser", "old@example.com");
        _mockUserDataService.Setup(x => x.UsernameExistsAsync("newuser", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.Is<UserProfile>(p => p.UserId == userId && p.Username == "newuser"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.ChangeUsernameAsync(userId, "newuser");

        // Assert
        result.IsAvailable.Should().BeTrue();
        _mockUserDataService.Verify(x => x.UpdateUserProfileAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUsernameAsync_UserNotFound_ReturnsNotAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.UsernameExistsAsync("newuser", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.ChangeUsernameAsync(userId, "newuser");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task ChangeUsernameAsync_ExistingUsername_ReturnsNotAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.UsernameExistsAsync("dupuser", userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.ChangeUsernameAsync(userId, "dupuser");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名已存在");
    }

    #endregion

    #region ChangeEmailAsync Tests

    [Fact]
    public async Task ChangeEmailAsync_ValidNewEmail_ReturnsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "user1", "old@example.com");
        _mockUserDataService.Setup(x => x.EmailExistsAsync("new@example.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.Is<UserProfile>(p => p.UserId == userId && p.Email == "new@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.ChangeEmailAsync(userId, "new@example.com");

        // Assert
        result.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeEmailAsync_UserNotFound_ReturnsNotAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.EmailExistsAsync("new@example.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.ChangeEmailAsync(userId, "new@example.com");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task ChangeEmailAsync_ExistingEmail_ReturnsNotAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.EmailExistsAsync("dup@example.com", userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.ChangeEmailAsync(userId, "dup@example.com");

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("邮箱地址已存在");
    }

    #endregion

    #region SetDefaultUserAsync Tests

    [Fact]
    public async Task SetDefaultUserAsync_ValidUser_SetsAsDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "user1", "user1@test.com");
        var other = CreateTestUser(Guid.NewGuid(), "user2", "user2@test.com");
        other.SetAsDefault();
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.GetAllUserProfilesAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserProfile> { user, other });
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.SetDefaultUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        user.IsDefault.Should().BeTrue();
        other.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultUserAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.SetDefaultUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    #endregion

    #region UpdatePreferencesAsync Tests

    [Fact]
    public async Task UpdatePreferencesAsync_AddAndRemovePreferences_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "prefuser", "pref@test.com");
        user.UserPreferences.Add(new UserPreferences(user.UserId, "ui", "theme", "light", "String", false, null));
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile u, CancellationToken _) => u);

        var request = new UpdatePreferencesRequest
        {
            Preferences = new Dictionary<string, Dictionary<string, object>>
            {
                ["ui"] = new() { ["theme"] = "dark", ["fontSize"] = 14 },
                ["editor"] = new() { ["tabSize"] = 4 }
            },
            RemoveItems = new Dictionary<string, List<string>>
            {
                ["obsolete"] = new() // empty -> entire category removal (if existed)
            }
        };

        // Act
        var result = await _service.UpdatePreferencesAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        user.UserPreferences.Should().Contain(p => p.PreferenceCategory == "ui" && p.PreferenceKey == "theme" && p.PreferenceValue == "dark");
        user.UserPreferences.Should().Contain(p => p.PreferenceCategory == "ui" && p.PreferenceKey == "fontSize");
        user.UserPreferences.Should().Contain(p => p.PreferenceCategory == "editor" && p.PreferenceKey == "tabSize");
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);
        var request = new UpdatePreferencesRequest();

        // Act
        var result = await _service.UpdatePreferencesAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    #endregion
    #region GetUserProfileAsync Tests

    [Fact]
    public async Task GetUserProfileAsync_ValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = CreateTestUser(userId);

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserProfile.Should().NotBeNull();
        result.UserProfile!.UserId.Should().Be(userId);
        result.UserProfile.Username.Should().Be(TestUsername);
    }

    [Fact]
    public async Task GetUserProfileAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
        result.ErrorMessage.Should().Be("用户不存在");
    }

    [Fact]
    public async Task GetUserProfileAsync_EmptyUserId_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.Empty;

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_USER_ID");
        result.ErrorMessage.Should().Be("用户ID不能为空");
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateTestUser(userId, "oldusername", "old@example.com");
        // updatedUser 将在 service 逻辑中对 existingUser 实例直接修改后再传入 UpdateUserProfileAsync

        var request = new UpdateUserProfileRequest
        {
            Username = "newusername",
            Email = "new@example.com"
        };

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _mockUserDataService.Setup(x => x.UsernameExistsAsync("newusername", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.EmailExistsAsync("new@example.com", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.Is<UserProfile>(p => p.UserId == userId && p.Username == "newusername" && p.Email == "new@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UpdatedProfile.Should().NotBeNull();
        result.UpdatedProfile!.Username.Should().Be("newusername");
        result.UpdatedProfile.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest { Username = "newusername" };

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
        result.ErrorMessage.Should().Be("用户不存在");
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = TestUsername,
            Email = TestEmail
        };

        var createdUser = CreateTestUser();
        _mockUserDataService.Setup(x => x.UsernameExistsAsync(TestUsername, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.EmailExistsAsync(TestEmail, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserDataService.Setup(x => x.CreateUserProfileAsync(It.Is<UserProfile>(p => p.Username == TestUsername && p.Email == TestEmail), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CreatedUser.Should().NotBeNull();
        result.CreatedUser!.Username.Should().Be(TestUsername);
        result.CreatedUser.Email.Should().Be(TestEmail);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ValidUserId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserDataService.Setup(x => x.DeleteUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.DeletedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
        result.ErrorMessage.Should().Be("用户不存在");
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_ValidUserId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.Deactivate(); // 先设置为非活跃状态

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.Is<UserProfile>(p => p.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.ActivateUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(userId);
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_ValidUserId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);

        _mockUserDataService.Setup(x => x.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserDataService.Setup(x => x.UpdateUserProfileAsync(It.Is<UserProfile>(p => p.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile u, CancellationToken _) => u);

        // Act
        var result = await _service.DeactivateUserAsync(userId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.UserId.Should().Be(userId);
    }

    #endregion

    #region GetUsersPagedAsync Tests

    [Fact]
    public async Task GetUsersPagedAsync_ValidRequest_ReturnsUserList()
    {
        // Arrange
        var request = new GetUsersRequest
        {
            PageIndex = 0,
            PageSize = 10,
            ActiveOnly = true
        };

        var users = new List<UserProfile>
        {
            CreateTestUser(Guid.NewGuid(), "user1", "user1@test.com"),
            CreateTestUser(Guid.NewGuid(), "user2", "user2@test.com")
        };

        _mockUserDataService.Setup(x => x.GetAllUserProfilesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Users.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Users.Count().Should().Be(2);
    }

    #endregion

    #region ValidateUsernameAsync Tests

    [Fact]
    public async Task ValidateUsernameAsync_ValidUsername_ReturnsAvailable()
    {
        // Arrange
        var username = "validuser";
        _mockUserDataService.Setup(x => x.UsernameExistsAsync(username, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateUsernameAsync(username);

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.Message.Should().Be("用户名可用");
    }

    [Fact]
    public async Task ValidateUsernameAsync_ExistingUsername_ReturnsNotAvailable()
    {
        // Arrange
        var username = "existinguser";
        _mockUserDataService.Setup(x => x.UsernameExistsAsync(username, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // 生成建议需要 GetAllUserProfilesAsync
        _mockUserDataService.Setup(x => x.GetAllUserProfilesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { CreateTestUser(Guid.NewGuid(), username, "existing@test.com") });

        // Act
        var result = await _service.ValidateUsernameAsync(username);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("用户名已存在");
        result.SuggestedUsernames.Should().NotBeNull();
        result.SuggestedUsernames!.Should().NotBeEmpty();
    }

    #endregion

    #region ValidateEmailAsync Tests

    [Fact]
    public async Task ValidateEmailAsync_ValidEmail_ReturnsAvailable()
    {
        // Arrange
        var email = "new@test.com";
        _mockUserDataService.Setup(x => x.EmailExistsAsync(email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateEmailAsync(email);

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.Message.Should().Be("邮箱地址可用");
    }

    [Fact]
    public async Task ValidateEmailAsync_ExistingEmail_ReturnsNotAvailable()
    {
        // Arrange
        var email = "existing@test.com";
        _mockUserDataService.Setup(x => x.EmailExistsAsync(email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateEmailAsync(email);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Message.Should().Be("邮箱地址已存在");
    }

    #endregion
}
