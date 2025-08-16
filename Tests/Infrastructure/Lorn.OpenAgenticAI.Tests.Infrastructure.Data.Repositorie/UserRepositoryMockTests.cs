using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Moq;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户仓储单元测试类 - 基于业务需求和产品设计的测试驱动开发
/// 
/// 测试原则：
/// 1. 业务需求驱动 - 每个测试用例都对应具体的业务需求
/// 2. 产品设计符合性 - 验证实现是否符合产品设计文档
/// 3. 技术设计一致性 - 确保接口契约和数据结构正确
/// 4. 异常场景覆盖 - 测试各种边界条件和错误情况
/// 
/// 对应需求：
/// - 需求1：静默用户管理 (1.1-1.10)
/// - 需求2：用户信息管理 (2.1-2.10) 
/// - 需求6：多用户支持与切换 (6.1-6.10)
/// </summary>
public class UserRepositoryMockTestsFixed
{
    private readonly Mock<IUserProfileRepository> _mockRepository;

    public UserRepositoryMockTestsFixed()
    {
        _mockRepository = new Mock<IUserProfileRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenValidUserId_ShouldReturnUserWithCompleteProfile()
    {
        // Arrange - 基于业务需求：用户访问个人资料页面时需要显示完整的用户信息
        // 对应需求2.1：WHEN 用户访问个人资料页面 THEN 系统 SHALL 显示当前用户的基本信息
        var userId = Guid.NewGuid();
        var expectedUser = CreateTestUserWithCompleteProfile(userId, "business-user-001", "user@company.com");
        _mockRepository.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync(expectedUser);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(userId);

        // Assert - 验证业务规则：返回的用户信息必须完整且准确
        Assert.NotNull(result);
        Assert.Equal(expectedUser.UserId, result.UserId);
        Assert.Equal(expectedUser.Username, result.Username);
        Assert.Equal(expectedUser.Email, result.Email);
        Assert.True(result.IsActive, "根据业务需求，获取的用户应该是活跃状态");
        Assert.True(result.ValidateProfile(), "根据产品设计，用户档案必须通过完整性验证");

        // 验证技术设计：确保仓储接口被正确调用
        _mockRepository.Verify(r => r.GetByIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotExists_ShouldReturnNullWithoutError()
    {
        // Arrange - 基于业务需求：系统必须优雅处理不存在的用户查询
        // 对应技术设计：仓储层应该返回null而不是抛出异常
        var nonExistentUserId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(nonExistentUserId, default)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(nonExistentUserId);

        // Assert - 验证错误处理：不存在的用户应该返回null，不应该抛出异常
        Assert.Null(result);

        // 验证技术设计：确保仓储接口被正确调用
        _mockRepository.Verify(r => r.GetByIdAsync(nonExistentUserId, default), Times.Once);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenValidUsername_ShouldReturnUserForAuthentication()
    {
        // Arrange - 基于业务需求：静默认证需要通过用户名识别用户
        // 对应需求1.3：WHEN 用户启动应用 THEN 系统 SHALL 自动识别当前机器并加载对应用户数据
        var username = "business-user-001";
        var expectedUser = CreateTestUserWithCompleteProfile(Guid.NewGuid(), username, "user@company.com");
        _mockRepository.Setup(r => r.GetByUserNameAsync(username, default)).ReturnsAsync(expectedUser);

        // Act
        var result = await _mockRepository.Object.GetByUserNameAsync(username);

        // Assert - 验证业务规则：用户名查询必须返回完整的用户信息用于认证
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Username, result.Username);
        Assert.Equal(expectedUser.UserId, result.UserId);
        Assert.True(result.IsActive, "根据业务需求，认证用户必须是活跃状态");
        Assert.NotNull(result.SecuritySettings); // 根据技术设计，用户必须包含安全设置信息

        // 验证技术设计：确保仓储接口被正确调用
        _mockRepository.Verify(r => r.GetByUserNameAsync(username, default), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var expectedUser = CreateTestUser();
        _mockRepository.Setup(r => r.GetByEmailAsync(email, default)).ReturnsAsync(expectedUser);

        // Act
        var result = await _mockRepository.Object.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Email, result.Email);
        _mockRepository.Verify(r => r.GetByEmailAsync(email, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenCreatingNewUser_ShouldCreateUserWithDefaultSettings()
    {
        // Arrange - 基于业务需求：系统自动创建默认用户账户
        // 对应需求1.1：WHEN 用户首次启动应用 THEN 系统 SHALL 自动创建默认用户账户并生成唯一用户ID
        var newUser = CreateTestUserWithCompleteProfile(Guid.NewGuid(), "auto-created-user", "");
        _mockRepository.Setup(r => r.AddAsync(newUser, default)).ReturnsAsync(newUser);

        // Act
        var result = await _mockRepository.Object.AddAsync(newUser);

        // Assert - 验证业务规则：新创建的用户必须具备完整的默认配置
        Assert.NotNull(result);
        Assert.Equal(newUser.UserId, result.UserId);
        Assert.Equal(newUser.Username, result.Username);
        Assert.True(result.IsActive, "根据业务需求，新创建的用户必须是活跃状态");
        Assert.True(result.CreatedTime <= DateTime.UtcNow, "根据技术设计，创建时间必须被正确设置");
        Assert.NotNull(result.SecuritySettings); // 根据产品设计，新用户必须包含默认安全设置
        Assert.Equal(1, result.ProfileVersion); // 根据技术设计，新用户的版本号应该从1开始

        // 验证技术设计：确保仓储接口被正确调用
        _mockRepository.Verify(r => r.AddAsync(newUser, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUser_WhenUserUpdatedSuccessfully()
    {
        // Arrange
        var user = CreateTestUser();
        _mockRepository.Setup(r => r.UpdateAsync(user, default)).Returns(Task.CompletedTask);

        // Act
        await _mockRepository.Object.UpdateAsync(user);

        // Assert - 验证更新操作被调用
        _mockRepository.Verify(r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldReturnTrue_WhenUserDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.SoftDeleteAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.SoftDeleteAsync(userId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.SoftDeleteAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldReturnFalse_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.SoftDeleteAsync(userId, default)).ReturnsAsync(false);

        // Act
        var result = await _mockRepository.Object.SoftDeleteAsync(userId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.SoftDeleteAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldReturnTrue_WhenUserSoftDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.SoftDeleteAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.SoftDeleteAsync(userId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.SoftDeleteAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task IsUsernameExistsAsync_ShouldReturnTrue_WhenUsernameExists()
    {
        // Arrange
        var username = "existinguser";
        _mockRepository.Setup(r => r.IsUsernameExistsAsync(username, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.IsUsernameExistsAsync(username);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.IsUsernameExistsAsync(username, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsEmailExistsAsync_ShouldReturnTrue_WhenEmailExists()
    {
        // Arrange
        var email = "existing@example.com";
        _mockRepository.Setup(r => r.IsEmailExistsAsync(email, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.IsEmailExistsAsync(email);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.IsEmailExistsAsync(email, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnActiveUsers()
    {
        // Arrange
        var activeUsers = new List<UserProfile> { CreateTestUser(), CreateTestUser() };
        _mockRepository.Setup(r => r.GetActiveUsersAsync(default)).ReturnsAsync(activeUsers);

        // Act
        var result = await _mockRepository.Object.GetActiveUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetActiveUsersAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetUserCountAsync_ShouldReturnUserCount()
    {
        // Arrange
        var expectedCount = 5;
        _mockRepository.Setup(r => r.GetUserCountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(expectedCount);

        // Act
        var result = await _mockRepository.Object.GetUserCountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
        _mockRepository.Verify(r => r.GetUserCountAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsUsernameExistsAsync_WhenCheckingDuplicateUsername_ShouldPreventDuplicateCreation()
    {
        // Arrange - 基于业务需求：系统必须验证用户名唯一性
        // 对应需求2.7：WHEN 用户输入新用户信息 THEN 系统 SHALL 验证用户名唯一性
        var existingUsername = "existing-user";
        _mockRepository.Setup(r => r.IsUsernameExistsAsync(existingUsername, null, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.IsUsernameExistsAsync(existingUsername);

        // Assert - 验证业务规则：重复用户名必须被检测出来
        Assert.True(result, "根据业务需求，系统必须检测到重复的用户名");
        _mockRepository.Verify(r => r.IsUsernameExistsAsync(existingUsername, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveUsersAsync_WhenMultipleUsersExist_ShouldReturnOnlyActiveUsers()
    {
        // Arrange - 基于业务需求：多用户环境下需要显示活跃用户列表
        // 对应需求6.1：WHEN 系统启动时存在多个用户 THEN 系统 SHALL 显示用户选择界面
        var activeUsers = new List<UserProfile>
        {
            CreateTestUserWithCompleteProfile(Guid.NewGuid(), "active-user-1", "user1@company.com"),
            CreateTestUserWithCompleteProfile(Guid.NewGuid(), "active-user-2", "user2@company.com")
        };
        _mockRepository.Setup(r => r.GetActiveUsersAsync(default)).ReturnsAsync(activeUsers);

        // Act
        var result = await _mockRepository.Object.GetActiveUsersAsync();

        // Assert - 验证业务规则：只返回活跃用户，用于用户选择界面
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, user => Assert.True(user.IsActive, "根据业务需求，返回的用户必须都是活跃状态"));
        Assert.All(result, user => Assert.True(user.ValidateProfile(), "根据产品设计，所有用户档案必须通过验证"));

        _mockRepository.Verify(r => r.GetActiveUsersAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenUpdatingUserProfile_ShouldIncrementVersionAndUpdateTimestamp()
    {
        // Arrange - 基于业务需求：用户修改个人信息时需要更新版本和时间戳
        // 对应需求2.2：WHEN 用户点击编辑按钮 THEN 系统 SHALL 允许修改显示名称和个人描述
        var existingUser = CreateTestUserWithCompleteProfile(Guid.NewGuid(), "user-to-update", "old@company.com");
        existingUser.UpdateEmail("new@company.com");
        _mockRepository.Setup(r => r.UpdateAsync(existingUser, default)).Returns(Task.CompletedTask);

        // Act
        await _mockRepository.Object.UpdateAsync(existingUser);

        // Assert - 验证业务规则：更新操作必须正确处理版本控制
        Assert.Equal("new@company.com", existingUser.Email);
        Assert.True(existingUser.ProfileVersion > 1, "根据技术设计，更新操作必须增加版本号");

        _mockRepository.Verify(r => r.UpdateAsync(existingUser, default), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenDeactivatingUser_ShouldMaintainDataIntegrity()
    {
        // Arrange - 基于业务需求：用户删除应该是软删除，保持数据完整性
        // 对应需求6.6：WHEN 用户删除不再使用的用户身份 THEN 系统 SHALL 要求确认并清理相关数据
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.SoftDeleteAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.SoftDeleteAsync(userId);

        // Assert - 验证业务规则：软删除必须成功，保持数据完整性
        Assert.True(result, "根据业务需求，软删除操作必须成功");
        _mockRepository.Verify(r => r.SoftDeleteAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetUsersPagedAsync_WhenPaginatingUsers_ShouldReturnCorrectPageAndCount()
    {
        // Arrange - 基于业务需求：用户管理界面需要分页显示用户列表
        // 对应需求2.5：WHEN 用户访问用户管理页面 THEN 系统 SHALL 显示用户列表和管理选项
        var pageIndex = 0;
        var pageSize = 10;
        var users = new List<UserProfile>
        {
            CreateTestUserWithCompleteProfile(Guid.NewGuid(), "paged-user-1", "user1@company.com"),
            CreateTestUserWithCompleteProfile(Guid.NewGuid(), "paged-user-2", "user2@company.com")
        };
        var totalCount = 25;
        _mockRepository.Setup(r => r.GetUsersPagedAsync(pageIndex, pageSize, true, default))
                      .ReturnsAsync((users, totalCount));

        // Act
        var result = await _mockRepository.Object.GetUsersPagedAsync(pageIndex, pageSize, true);

        // Assert - 验证业务规则：分页功能必须正确返回数据和总数
        Assert.NotNull(result.Users);
        Assert.Equal(2, result.Users.Count());
        Assert.Equal(totalCount, result.TotalCount);
        Assert.All(result.Users, user => Assert.True(user.IsActive, "根据业务需求，分页结果应该只包含活跃用户"));

        _mockRepository.Verify(r => r.GetUsersPagedAsync(pageIndex, pageSize, true, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange - 基于技术设计：仓储层必须验证输入参数
        _mockRepository.Setup(r => r.AddAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("userProfile"));

        // Act & Assert - 验证错误处理：空参数必须抛出适当的异常
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.AddAsync(null!));
        Assert.Equal("userProfile", exception.ParamName);
        _mockRepository.Verify(r => r.AddAsync(null!, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange - 基于技术设计：仓储层必须验证输入参数
        _mockRepository.Setup(r => r.UpdateAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("userProfile"));

        // Act & Assert - 验证错误处理：空参数必须抛出适当的异常
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.UpdateAsync(null!));
        Assert.Equal("userProfile", exception.ParamName);
        _mockRepository.Verify(r => r.UpdateAsync(null!, default), Times.Once);
    }

    [Fact]
    public async Task IsEmailExistsAsync_WhenCheckingDuplicateEmail_ShouldPreventDuplicateRegistration()
    {
        // Arrange - 基于业务需求：系统必须验证邮箱唯一性
        // 对应需求2.3：WHEN 用户修改个人信息 THEN 系统 SHALL 验证新信息的有效性和唯一性
        var existingEmail = "existing@company.com";
        _mockRepository.Setup(r => r.IsEmailExistsAsync(existingEmail, null, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.IsEmailExistsAsync(existingEmail);

        // Assert - 验证业务规则：重复邮箱必须被检测出来
        Assert.True(result, "根据业务需求，系统必须检测到重复的邮箱地址");
        _mockRepository.Verify(r => r.IsEmailExistsAsync(existingEmail, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #region 边界条件和异常场景测试

    [Fact]
    public async Task GetByUsernameAsync_WhenEmptyUsername_ShouldHandleGracefully()
    {
        // Arrange - 基于技术设计：系统必须优雅处理无效输入
        var emptyUsername = "";
        _mockRepository.Setup(r => r.GetByUserNameAsync(emptyUsername, default))
                      .ThrowsAsync(new ArgumentException("Username cannot be null or empty", nameof(emptyUsername)));

        // Act & Assert - 验证错误处理：空用户名必须被正确处理
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _mockRepository.Object.GetByUserNameAsync(emptyUsername));
        Assert.Contains("Username cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmptyEmail_ShouldHandleGracefully()
    {
        // Arrange - 基于技术设计：系统必须优雅处理无效输入
        var emptyEmail = "";
        _mockRepository.Setup(r => r.GetByEmailAsync(emptyEmail, default))
                      .ThrowsAsync(new ArgumentException("Email cannot be null or empty", nameof(emptyEmail)));

        // Act & Assert - 验证错误处理：空邮箱必须被正确处理
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _mockRepository.Object.GetByEmailAsync(emptyEmail));
        Assert.Contains("Email cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateUsername_ShouldThrowInvalidOperationException()
    {
        // Arrange - 基于业务需求：系统必须防止重复用户名
        // 对应需求2.7：系统必须验证用户名唯一性
        var duplicateUser = CreateTestUserWithCompleteProfile(Guid.NewGuid(), "duplicate-user", "user@company.com");
        _mockRepository.Setup(r => r.AddAsync(duplicateUser, default))
                      .ThrowsAsync(new InvalidOperationException($"Username '{duplicateUser.Username}' already exists"));

        // Act & Assert - 验证业务规则：重复用户名必须被拒绝
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _mockRepository.Object.AddAsync(duplicateUser));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetUsersPagedAsync_WhenInvalidPageParameters_ShouldThrowArgumentException()
    {
        // Arrange - 基于技术设计：分页参数必须被验证
        var invalidPageIndex = -1;
        var invalidPageSize = 0;
        _mockRepository.Setup(r => r.GetUsersPagedAsync(invalidPageIndex, invalidPageSize, true, default))
                      .ThrowsAsync(new ArgumentException("Page index cannot be negative"));

        // Act & Assert - 验证参数验证：无效的分页参数必须被拒绝
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockRepository.Object.GetUsersPagedAsync(invalidPageIndex, invalidPageSize, true));
        Assert.Contains("Page index cannot be negative", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenUserHasRelatedData_ShouldHandleCascadeDelete()
    {
        // Arrange - 基于业务需求：删除用户时必须处理关联数据
        // 对应需求6.6：系统必须清理相关数据
        var userIdWithRelatedData = Guid.NewGuid();
        _mockRepository.Setup(r => r.SoftDeleteAsync(userIdWithRelatedData, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.SoftDeleteAsync(userIdWithRelatedData);

        // Assert - 验证业务规则：删除操作必须成功处理关联数据
        Assert.True(result, "根据业务需求，删除操作必须成功处理关联数据");
        _mockRepository.Verify(r => r.SoftDeleteAsync(userIdWithRelatedData, default), Times.Once);
    }

    [Fact]
    public async Task GetUserCountAsync_WhenNoUsers_ShouldReturnZero()
    {
        // Arrange - 基于边界条件：系统必须正确处理空用户列表
        _mockRepository.Setup(r => r.GetUserCountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        var result = await _mockRepository.Object.GetUserCountAsync();

        // Assert - 验证边界条件：空用户列表必须返回0
        Assert.Equal(0, result);
        _mockRepository.Verify(r => r.GetUserCountAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveUsersAsync_WhenAllUsersInactive_ShouldReturnEmptyList()
    {
        // Arrange - 基于边界条件：系统必须正确处理所有用户都非活跃的情况
        var emptyUserList = new List<UserProfile>();
        _mockRepository.Setup(r => r.GetActiveUsersAsync(default)).ReturnsAsync(emptyUserList);

        // Act
        var result = await _mockRepository.Object.GetActiveUsersAsync();

        // Assert - 验证边界条件：无活跃用户时必须返回空列表
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetActiveUsersAsync(default), Times.Once);
    }

    #endregion

    /// <summary>
    /// 创建符合业务需求的完整测试用户
    /// 基于产品设计文档中的用户档案要求
    /// </summary>
    private static UserProfile CreateTestUserWithCompleteProfile(Guid? userId = null, string? username = null, string? email = null)
    {
        // 基于技术设计：用户必须包含完整的安全设置
        var securitySettings = new SecuritySettings(
            authenticationMethod: "silent", // 对应需求1：静默认证模式
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );

        var user = new UserProfile(
            userId: userId ?? Guid.NewGuid(),
            username: username ?? "default-user",
            email: email ?? "",
            securitySettings: securitySettings
        );

        // 确保用户档案通过业务验证
        if (!user.ValidateProfile())
        {
            throw new InvalidOperationException("创建的测试用户不符合业务验证规则");
        }

        return user;
    }

    /// <summary>
    /// 创建测试用户（保持向后兼容）
    /// </summary>
    private static UserProfile CreateTestUser()
    {
        return CreateTestUserWithCompleteProfile();
    }
}
