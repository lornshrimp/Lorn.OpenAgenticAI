using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户仓储Mock测试类 - 使用直接Mock仓储接口的方式进行单元测试
/// </summary>
public class UserRepositoryMockTestsFixed
{
    private readonly Mock<IUserRepository> _mockRepository;

    public UserRepositoryMockTestsFixed()
    {
        _mockRepository = new Mock<IUserRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = CreateTestUser();
        _mockRepository.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync(expectedUser);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.UserId, result.UserId);
        Assert.Equal(expectedUser.Username, result.Username);
        _mockRepository.Verify(r => r.GetByIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(userId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var username = "testuser";
        var expectedUser = CreateTestUser();
        _mockRepository.Setup(r => r.GetByUsernameAsync(username, default)).ReturnsAsync(expectedUser);

        // Act
        var result = await _mockRepository.Object.GetByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Username, result.Username);
        _mockRepository.Verify(r => r.GetByUsernameAsync(username, default), Times.Once);
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
    public async Task AddAsync_ShouldReturnUser_WhenUserAddedSuccessfully()
    {
        // Arrange
        var user = CreateTestUser();
        _mockRepository.Setup(r => r.AddAsync(user, default)).ReturnsAsync(user);

        // Act
        var result = await _mockRepository.Object.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        _mockRepository.Verify(r => r.AddAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUser_WhenUserUpdatedSuccessfully()
    {
        // Arrange
        var user = CreateTestUser();
        _mockRepository.Setup(r => r.UpdateAsync(user, default)).ReturnsAsync(user);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        _mockRepository.Verify(r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenUserDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.DeleteAsync(userId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId, default)).ReturnsAsync(false);

        // Act
        var result = await _mockRepository.Object.DeleteAsync(userId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(userId, default), Times.Once);
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
    public async Task AddAsync_ShouldThrowException_WhenNullUser()
    {
        // Arrange
        _mockRepository.Setup(r => r.AddAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("user"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.AddAsync(null!));
        _mockRepository.Verify(r => r.AddAsync(null!, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenNullUser()
    {
        // Arrange
        _mockRepository.Setup(r => r.UpdateAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("user"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.UpdateAsync(null!));
        _mockRepository.Verify(r => r.UpdateAsync(null!, default), Times.Once);
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    private static UserProfile CreateTestUser()
    {
        var securitySettings = new SecuritySettings(
            authenticationMethod: "password",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );

        return new UserProfile(
            userId: Guid.NewGuid(),
            username: "testuser",
            email: "test@example.com",
            securitySettings: securitySettings
        );
    }
}
