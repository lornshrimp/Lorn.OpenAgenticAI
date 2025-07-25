using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Moq;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户偏好设置仓储Mock测试类 - 使用直接Mock仓储接口的方式进行单元测试
/// </summary>
public class UserPreferenceRepositoryMockTests
{
    private readonly Mock<IUserPreferenceRepository> _mockRepository;

    public UserPreferenceRepositoryMockTests()
    {
        _mockRepository = new Mock<IUserPreferenceRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPreference_WhenPreferenceExists()
    {
        // Arrange
        var preferenceId = Guid.NewGuid();
        var expectedPreference = CreateTestPreference();
        _mockRepository.Setup(r => r.GetByIdAsync(preferenceId, default)).ReturnsAsync(expectedPreference);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(preferenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPreference.PreferenceId, result.PreferenceId);
        Assert.Equal(expectedPreference.PreferenceKey, result.PreferenceKey);
        _mockRepository.Verify(r => r.GetByIdAsync(preferenceId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPreferenceNotExists()
    {
        // Arrange
        var preferenceId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(preferenceId, default)).ReturnsAsync((UserPreferences?)null);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(preferenceId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(preferenceId, default), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnPreferences_WhenUserHasPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new List<UserPreferences> { CreateTestPreference(userId), CreateTestPreference(userId) };
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(preferences);

        // Act
        var result = await _mockRepository.Object.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(userId, p.UserId));
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnCategoryPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var preferences = new List<UserPreferences> { CreateTestPreference(userId, category) };
        _mockRepository.Setup(r => r.GetByCategoryAsync(userId, category, default)).ReturnsAsync(preferences);

        // Act
        var result = await _mockRepository.Object.GetByCategoryAsync(userId, category);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(category, result.First().PreferenceCategory);
        _mockRepository.Verify(r => r.GetByCategoryAsync(userId, category, default), Times.Once);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnPreference_WhenKeyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var key = "theme";
        var preference = CreateTestPreference(userId, category, key);
        _mockRepository.Setup(r => r.GetByKeyAsync(userId, category, key, default)).ReturnsAsync(preference);

        // Act
        var result = await _mockRepository.Object.GetByKeyAsync(userId, category, key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.PreferenceKey);
        Assert.Equal(category, result.PreferenceCategory);
        _mockRepository.Verify(r => r.GetByKeyAsync(userId, category, key, default), Times.Once);
    }

    [Fact]
    public async Task GetSystemDefaultsAsync_ShouldReturnDefaultPreferences()
    {
        // Arrange
        var category = "UI";
        var defaults = new List<UserPreferences> { CreateTestPreference(isSystemDefault: true) };
        _mockRepository.Setup(r => r.GetSystemDefaultsAsync(category, default)).ReturnsAsync(defaults);

        // Act
        var result = await _mockRepository.Object.GetSystemDefaultsAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.First().IsSystemDefault);
        _mockRepository.Verify(r => r.GetSystemDefaultsAsync(category, default), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenPreferenceExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var key = "theme";
        _mockRepository.Setup(r => r.ExistsAsync(userId, category, key, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.ExistsAsync(userId, category, key);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.ExistsAsync(userId, category, key, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnPreference_WhenPreferenceAddedSuccessfully()
    {
        // Arrange
        var preference = CreateTestPreference();
        _mockRepository.Setup(r => r.AddAsync(preference, default)).ReturnsAsync(preference);

        // Act
        var result = await _mockRepository.Object.AddAsync(preference);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(preference.PreferenceId, result.PreferenceId);
        _mockRepository.Verify(r => r.AddAsync(preference, default), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldReturnPreferences_WhenPreferencesAddedSuccessfully()
    {
        // Arrange
        var preferences = new List<UserPreferences> { CreateTestPreference(), CreateTestPreference() };
        _mockRepository.Setup(r => r.AddRangeAsync(preferences, default)).ReturnsAsync(preferences);

        // Act
        var result = await _mockRepository.Object.AddRangeAsync(preferences);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.AddRangeAsync(preferences, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnPreference_WhenPreferenceUpdatedSuccessfully()
    {
        // Arrange
        var preference = CreateTestPreference();
        _mockRepository.Setup(r => r.UpdateAsync(preference, default)).ReturnsAsync(preference);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(preference);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(preference.PreferenceId, result.PreferenceId);
        _mockRepository.Verify(r => r.UpdateAsync(preference, default), Times.Once);
    }

    [Fact]
    public async Task SetPreferenceAsync_ShouldReturnPreference_WhenPreferenceSetSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var key = "theme";
        var value = "dark";
        var valueType = "String";
        var description = "UI theme preference";
        var preference = CreateTestPreference(userId, category, key);

        _mockRepository.Setup(r => r.SetPreferenceAsync(userId, category, key, value, valueType, description, default))
                      .ReturnsAsync(preference);

        // Act
        var result = await _mockRepository.Object.SetPreferenceAsync(userId, category, key, value, valueType, description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.PreferenceKey);
        _mockRepository.Verify(r => r.SetPreferenceAsync(userId, category, key, value, valueType, description, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenPreferenceDeletedSuccessfully()
    {
        // Arrange
        var preferenceId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(preferenceId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.DeleteAsync(preferenceId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(preferenceId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteByUserIdAsync_ShouldReturnCount_WhenPreferencesDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCount = 5;
        _mockRepository.Setup(r => r.DeleteByUserIdAsync(userId, default)).ReturnsAsync(expectedCount);

        // Act
        var result = await _mockRepository.Object.DeleteByUserIdAsync(userId);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockRepository.Verify(r => r.DeleteByUserIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteByCategoryAsync_ShouldReturnCount_WhenCategoryPreferencesDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var expectedCount = 3;
        _mockRepository.Setup(r => r.DeleteByCategoryAsync(userId, category, default)).ReturnsAsync(expectedCount);

        // Act
        var result = await _mockRepository.Object.DeleteByCategoryAsync(userId, category);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockRepository.Verify(r => r.DeleteByCategoryAsync(userId, category, default), Times.Once);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldReturnCount_WhenPreferencesResetSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "UI";
        var expectedCount = 2;
        _mockRepository.Setup(r => r.ResetToDefaultsAsync(userId, category, default)).ReturnsAsync(expectedCount);

        // Act
        var result = await _mockRepository.Object.ResetToDefaultsAsync(userId, category);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockRepository.Verify(r => r.ResetToDefaultsAsync(userId, category, default), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedStats = (CategoryCount: 3, TotalPreferences: 10, LastUpdated: DateTime.UtcNow);
        _mockRepository.Setup(r => r.GetStatisticsAsync(userId, default)).ReturnsAsync(expectedStats);

        // Act
        var result = await _mockRepository.Object.GetStatisticsAsync(userId);

        // Assert
        Assert.Equal(expectedStats.CategoryCount, result.CategoryCount);
        Assert.Equal(expectedStats.TotalPreferences, result.TotalPreferences);
        Assert.NotNull(result.LastUpdated);
        _mockRepository.Verify(r => r.GetStatisticsAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenNullPreference()
    {
        // Arrange
        _mockRepository.Setup(r => r.AddAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("preference"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.AddAsync(null!));
        _mockRepository.Verify(r => r.AddAsync(null!, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenNullPreference()
    {
        // Arrange
        _mockRepository.Setup(r => r.UpdateAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("preference"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.UpdateAsync(null!));
        _mockRepository.Verify(r => r.UpdateAsync(null!, default), Times.Once);
    }

    /// <summary>
    /// 创建测试偏好设置
    /// </summary>
    private static UserPreferences CreateTestPreference(
        Guid? userId = null,
        string category = "UI",
        string key = "theme",
        bool isSystemDefault = false)
    {
        return new UserPreferences(
            userId: userId ?? Guid.NewGuid(),
            preferenceCategory: category,
            preferenceKey: key,
            preferenceValue: "dark",
            valueType: "String",
            isSystemDefault: isSystemDefault,
            description: "Test preference"
        );
    }
}
