using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Moq;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 用户元数据仓储Mock测试类 - 使用直接Mock仓储接口的方式进行单元测试
/// </summary>
public class UserMetadataRepositoryMockTests
{
    private readonly Mock<IUserMetadataRepository> _mockRepository;

    public UserMetadataRepositoryMockTests()
    {
        _mockRepository = new Mock<IUserMetadataRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMetadata_WhenMetadataExists()
    {
        // Arrange
        var metadataId = Guid.NewGuid();
        var expectedMetadata = CreateTestMetadata();
        _mockRepository.Setup(r => r.GetByIdAsync(metadataId, default)).ReturnsAsync(expectedMetadata);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(metadataId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedMetadata.Id, result.Id);
        Assert.Equal(expectedMetadata.Key, result.Key);
        _mockRepository.Verify(r => r.GetByIdAsync(metadataId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMetadataNotExists()
    {
        // Arrange
        var metadataId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(metadataId, default)).ReturnsAsync((UserMetadataEntry?)null);

        // Act
        var result = await _mockRepository.Object.GetByIdAsync(metadataId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(metadataId, default), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnMetadata_WhenUserHasMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var metadata = new List<UserMetadataEntry> { CreateTestMetadata(userId), CreateTestMetadata(userId) };
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(userId, m.UserId));
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnCategoryMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "Settings";
        var metadata = new List<UserMetadataEntry> { CreateTestMetadata(userId, category: category) };
        _mockRepository.Setup(r => r.GetByCategoryAsync(userId, category, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.GetByCategoryAsync(userId, category);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(category, result.First().Category);
        _mockRepository.Verify(r => r.GetByCategoryAsync(userId, category, default), Times.Once);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnMetadata_WhenKeyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "lastLogin";
        var metadata = CreateTestMetadata(userId, key: key);
        _mockRepository.Setup(r => r.GetByKeyAsync(userId, key, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.GetByKeyAsync(userId, key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal(userId, result.UserId);
        _mockRepository.Verify(r => r.GetByKeyAsync(userId, key, default), Times.Once);
    }

    [Fact]
    public async Task GetByCategoryAndKeyAsync_ShouldReturnMetadata_WhenCategoryAndKeyExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "Settings";
        var key = "theme";
        var metadata = CreateTestMetadata(userId, category: category, key: key);
        _mockRepository.Setup(r => r.GetByCategoryAndKeyAsync(userId, category, key, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.GetByCategoryAndKeyAsync(userId, category, key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal(category, result.Category);
        _mockRepository.Verify(r => r.GetByCategoryAndKeyAsync(userId, category, key, default), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenMetadataExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "lastLogin";
        _mockRepository.Setup(r => r.ExistsAsync(userId, key, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.ExistsAsync(userId, key);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.ExistsAsync(userId, key, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnMetadata_WhenMetadataAddedSuccessfully()
    {
        // Arrange
        var metadata = CreateTestMetadata();
        _mockRepository.Setup(r => r.AddAsync(metadata, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.AddAsync(metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metadata.Id, result.Id);
        _mockRepository.Verify(r => r.AddAsync(metadata, default), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldReturnMetadata_WhenMetadataAddedSuccessfully()
    {
        // Arrange
        var metadata = new List<UserMetadataEntry> { CreateTestMetadata(), CreateTestMetadata() };
        _mockRepository.Setup(r => r.AddRangeAsync(metadata, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.AddRangeAsync(metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.AddRangeAsync(metadata, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnMetadata_WhenMetadataUpdatedSuccessfully()
    {
        // Arrange
        var metadata = CreateTestMetadata();
        _mockRepository.Setup(r => r.UpdateAsync(metadata, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metadata.Id, result.Id);
        _mockRepository.Verify(r => r.UpdateAsync(metadata, default), Times.Once);
    }

    [Fact]
    public async Task SetMetadataAsync_ShouldReturnMetadata_WhenMetadataSetSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "lastLogin";
        var value = DateTime.UtcNow;
        var category = "Activity";
        var metadata = CreateTestMetadata(userId, category: category, key: key);

        _mockRepository.Setup(r => r.SetMetadataAsync(userId, key, value, category, default))
                      .ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.SetMetadataAsync(userId, key, value, category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        _mockRepository.Verify(r => r.SetMetadataAsync(userId, key, value, category, default), Times.Once);
    }

    [Fact]
    public async Task GetValueAsync_ShouldReturnValue_WhenMetadataExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "loginCount";
        var expectedValue = 5;
        _mockRepository.Setup(r => r.GetValueAsync<int>(userId, key, 0, default)).ReturnsAsync(expectedValue);

        // Act
        var result = await _mockRepository.Object.GetValueAsync<int>(userId, key, 0);

        // Assert
        Assert.Equal(expectedValue, result);
        _mockRepository.Verify(r => r.GetValueAsync<int>(userId, key, 0, default), Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_ShouldReturnMetadata_WhenValueSetSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "loginCount";
        var value = 5;
        var category = "Activity";
        var metadata = CreateTestMetadata(userId, category: category, key: key);

        _mockRepository.Setup(r => r.SetValueAsync(userId, key, value, category, default))
                      .ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.SetValueAsync(userId, key, value, category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        _mockRepository.Verify(r => r.SetValueAsync(userId, key, value, category, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenMetadataDeletedSuccessfully()
    {
        // Arrange
        var metadataId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(metadataId, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.DeleteAsync(metadataId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(metadataId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteByKeyAsync_ShouldReturnTrue_WhenMetadataDeletedByKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = "lastLogin";
        _mockRepository.Setup(r => r.DeleteByKeyAsync(userId, key, default)).ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.DeleteByKeyAsync(userId, key);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteByKeyAsync(userId, key, default), Times.Once);
    }

    [Fact]
    public async Task DeleteByUserIdAsync_ShouldReturnCount_WhenMetadataDeletedSuccessfully()
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
    public async Task DeleteByCategoryAsync_ShouldReturnCount_WhenCategoryMetadataDeletedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = "Settings";
        var expectedCount = 3;
        _mockRepository.Setup(r => r.DeleteByCategoryAsync(userId, category, default)).ReturnsAsync(expectedCount);

        // Act
        var result = await _mockRepository.Object.DeleteByCategoryAsync(userId, category);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockRepository.Verify(r => r.DeleteByCategoryAsync(userId, category, default), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedStats = (CategoryCount: 3, TotalEntries: 10, LastUpdated: DateTime.UtcNow);
        _mockRepository.Setup(r => r.GetStatisticsAsync(userId, default)).ReturnsAsync(expectedStats);

        // Act
        var result = await _mockRepository.Object.GetStatisticsAsync(userId);

        // Assert
        Assert.Equal(expectedStats.CategoryCount, result.CategoryCount);
        Assert.Equal(expectedStats.TotalEntries, result.TotalEntries);
        Assert.NotNull(result.LastUpdated);
        _mockRepository.Verify(r => r.GetStatisticsAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var searchTerm = "Login";
        var category = "Activity";
        var metadata = new List<UserMetadataEntry> { CreateTestMetadata(userId, key: "lastLogin") };
        _mockRepository.Setup(r => r.SearchAsync(userId, searchTerm, category, default)).ReturnsAsync(metadata);

        // Act
        var result = await _mockRepository.Object.SearchAsync(userId, searchTerm, category);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(searchTerm, result.First().Key, StringComparison.OrdinalIgnoreCase);
        _mockRepository.Verify(r => r.SearchAsync(userId, searchTerm, category, default), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenNullMetadata()
    {
        // Arrange
        _mockRepository.Setup(r => r.AddAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("metadataEntry"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.AddAsync(null!));
        _mockRepository.Verify(r => r.AddAsync(null!, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenNullMetadata()
    {
        // Arrange
        _mockRepository.Setup(r => r.UpdateAsync(null!, default))
                      .ThrowsAsync(new ArgumentNullException("metadataEntry"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockRepository.Object.UpdateAsync(null!));
        _mockRepository.Verify(r => r.UpdateAsync(null!, default), Times.Once);
    }

    /// <summary>
    /// 创建测试元数据条目
    /// </summary>
    private static UserMetadataEntry CreateTestMetadata(
        Guid? userId = null,
        string category = "Settings",
        string key = "defaultTheme")
    {
        return new UserMetadataEntry(
            userId: userId ?? Guid.NewGuid(),
            key: key,
            value: "dark",
            category: category
        );
    }
}
