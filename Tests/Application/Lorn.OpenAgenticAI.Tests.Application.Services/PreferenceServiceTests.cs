using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Extensions;
using Lorn.OpenAgenticAI.Application.Services.Constants;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// 偏好设置服务单元测试
/// </summary>
public class PreferenceServiceTests
{
    private readonly Mock<IUserPreferenceRepository> _mockRepository;
    private readonly Mock<ILogger<PreferenceService>> _mockLogger;
    private readonly PreferenceService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PreferenceServiceTests()
    {
        _mockRepository = new Mock<IUserPreferenceRepository>();
        _mockLogger = new Mock<ILogger<PreferenceService>>();
        _service = new PreferenceService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPreferenceAsync_ReturnsDefaultValue_WhenPreferenceNotFound()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        var key = PreferenceConstants.UI.THEME;
        var defaultValue = "Auto";

        _mockRepository.Setup(r => r.GetByKeyAsync(_testUserId, category, key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((UserPreferences?)null);

        // Act
        var result = await _service.GetPreferenceAsync(_testUserId, category, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public async Task GetPreferenceAsync_ReturnsStoredValue_WhenPreferenceExists()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        var key = PreferenceConstants.UI.THEME;
        var storedValue = "Dark";
        var defaultValue = "Auto";

        var preference = new UserPreferences(_testUserId, category, key, storedValue, "String");
        _mockRepository.Setup(r => r.GetByKeyAsync(_testUserId, category, key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(preference);

        // Act
        var result = await _service.GetPreferenceAsync(_testUserId, category, key, defaultValue);

        // Assert
        Assert.Equal(storedValue, result);
    }

    [Fact]
    public async Task SetPreferenceAsync_CallsRepository_WithCorrectParameters()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        var key = PreferenceConstants.UI.THEME;
        var value = "Dark";
        var description = "用户界面主题设置";

        _mockRepository.Setup(r => r.GetByKeyAsync(_testUserId, category, key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((UserPreferences?)null);

        _mockRepository.Setup(r => r.SetPreferenceAsync(
                _testUserId, category, key, value, "String", description, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new UserPreferences(_testUserId, category, key, value, "String"));

        // Act
        var result = await _service.SetPreferenceAsync(_testUserId, category, key, value, description);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.SetPreferenceAsync(
            _testUserId, category, key, value, "String", description, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCategoryPreferencesAsync_ReturnsEmptyDictionary_WhenNoPreferences()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        _mockRepository.Setup(r => r.GetByCategoryAsync(_testUserId, category, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<UserPreferences>());

        // Act
        var result = await _service.GetCategoryPreferencesAsync(_testUserId, category);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCategoryPreferencesAsync_ReturnsCorrectPreferences_WhenPreferencesExist()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        var preferences = new List<UserPreferences>
        {
            new UserPreferences(_testUserId, category, "Theme", "Dark", "String"),
            new UserPreferences(_testUserId, category, "FontSize", "16", "Integer")
        };

        _mockRepository.Setup(r => r.GetByCategoryAsync(_testUserId, category, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(preferences);

        // Act
        var result = await _service.GetCategoryPreferencesAsync(_testUserId, category);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Dark", result["Theme"]);
        Assert.Equal("16", result["FontSize"]);
    }

    [Theory]
    [InlineData("UI", "Theme", "Light")]
    [InlineData("UI", "Theme", "Dark")]
    [InlineData("UI", "Theme", "Auto")]
    public async Task ExtensionMethods_SetTheme_WorksCorrectly(string category, string key, string theme)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByKeyAsync(_testUserId, category, key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((UserPreferences?)null);

        _mockRepository.Setup(r => r.SetPreferenceAsync(
                _testUserId, category, key, theme, "String", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new UserPreferences(_testUserId, category, key, theme, "String"));

        // Act
        var result = await _service.SetThemeAsync(_testUserId, theme);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExtensionMethods_SetTheme_ThrowsException_ForInvalidTheme()
    {
        // Arrange
        var invalidTheme = "InvalidTheme";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetThemeAsync(_testUserId, invalidTheme));
    }

    [Fact]
    public async Task ResetCategoryPreferencesAsync_CallsRepository_WithCorrectParameters()
    {
        // Arrange
        var category = PreferenceConstants.UI.CATEGORY;
        var expectedResetCount = 5;

        _mockRepository.Setup(r => r.ResetToDefaultsAsync(_testUserId, category, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedResetCount);

        // Act
        var result = await _service.ResetCategoryPreferencesAsync(_testUserId, category);

        // Assert
        Assert.Equal(expectedResetCount, result);
        _mockRepository.Verify(r => r.ResetToDefaultsAsync(_testUserId, category, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var expectedStats = (CategoryCount: 3, TotalPreferences: 15, LastUpdated: DateTime.UtcNow);
        var preferences = new List<UserPreferences>
        {
            new UserPreferences(_testUserId, "UI", "Theme", "Dark", "String"),
            new UserPreferences(_testUserId, "UI", "FontSize", "16", "Integer"),
            new UserPreferences(_testUserId, "Language", "UILanguage", "en-US", "String"),
            new UserPreferences(_testUserId, "Operation", "DefaultLLMModel", "gpt-4", "String")
        };

        _mockRepository.Setup(r => r.GetStatisticsAsync(_testUserId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedStats);

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(preferences);

        // Act
        var result = await _service.GetStatisticsAsync(_testUserId);

        // Assert
        Assert.Equal(expectedStats.CategoryCount, result.CategoryCount);
        Assert.Equal(expectedStats.TotalPreferences, result.TotalPreferences);
        Assert.Equal(expectedStats.LastUpdated, result.LastUpdated);
        Assert.Equal(3, result.PreferencesByCategory.Count);
    }

    [Fact]
    public void PreferenceChanged_Event_IsTriggered_OnSetPreference()
    {
        // Arrange
        var eventTriggered = false;
        PreferenceChangedEventArgs? capturedEventArgs = null;

        _service.PreferenceChanged += (sender, e) =>
        {
            eventTriggered = true;
            capturedEventArgs = e;
        };

        var category = PreferenceConstants.UI.CATEGORY;
        var key = PreferenceConstants.UI.THEME;
        var value = "Dark";

        _mockRepository.Setup(r => r.GetByKeyAsync(_testUserId, category, key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((UserPreferences?)null);

        _mockRepository.Setup(r => r.SetPreferenceAsync(
                _testUserId, category, key, value, "String", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new UserPreferences(_testUserId, category, key, value, "String"));

        // Act
        // Act
        var task = _service.SetPreferenceAsync(_testUserId, category, key, value);
        task.GetAwaiter().GetResult();

        // Assert (after awaiting to ensure event fired)
        Assert.True(eventTriggered);
        Assert.NotNull(capturedEventArgs);
        Assert.Equal(_testUserId, capturedEventArgs!.UserId);
        Assert.Equal(category, capturedEventArgs.Category);
        Assert.Equal(key, capturedEventArgs.Key);
        Assert.Equal(value, capturedEventArgs.NewValue);
        Assert.Equal(PreferenceChangeType.Created, capturedEventArgs.ChangeType);
    }
}
