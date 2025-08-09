using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

/// <summary>
/// PreferenceService 扩展测试 - 覆盖类型安全、导入导出、事件、批量、并发等高级场景
/// </summary>
public class PreferenceServiceAdvancedTests
{
    private readonly Mock<IUserPreferenceRepository> _repo;
    private readonly Mock<ILogger<PreferenceService>> _logger;
    private readonly PreferenceService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public PreferenceServiceAdvancedTests()
    {
        _repo = new Mock<IUserPreferenceRepository>(MockBehavior.Strict);
        _logger = new Mock<ILogger<PreferenceService>>();
        _service = new PreferenceService(_repo.Object, _logger.Object);
    }

    #region 类型安全读取
    [Fact]
    public async Task GetPreferenceAsync_ReturnsTypedValues_ForVariousTypes()
    {
        // Arrange bool
        var prefBool = new UserPreferences(_userId, "UI", "ShowSidebar", "true", "Boolean");
        var prefInt = new UserPreferences(_userId, "UI", "FontSize", "18", "Integer");
        var prefDouble = new UserPreferences(_userId, "Operation", "TimeoutSeconds", "2.5", "Double");
        var date = DateTime.UtcNow.Date;
        var prefDate = new UserPreferences(_userId, "Operation", "LastOpen", date.ToString("O"), "DateTime");
        var jsonObj = new { Model = "gpt-4", Temp = 0.7 };
        var json = System.Text.Json.JsonSerializer.Serialize(jsonObj);
        var prefJson = new UserPreferences(_userId, "Operation", "LLMConfig", json, "JSON");

        _repo.Setup(r => r.GetByKeyAsync(_userId, "UI", "ShowSidebar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefBool);
        _repo.Setup(r => r.GetByKeyAsync(_userId, "UI", "FontSize", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefInt);
        _repo.Setup(r => r.GetByKeyAsync(_userId, "Operation", "TimeoutSeconds", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefDouble);
        _repo.Setup(r => r.GetByKeyAsync(_userId, "Operation", "LastOpen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefDate);
        _repo.Setup(r => r.GetByKeyAsync(_userId, "Operation", "LLMConfig", It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefJson);

        // Act
        var b = await _service.GetPreferenceAsync(_userId, "UI", "ShowSidebar", false);
        var i = await _service.GetPreferenceAsync(_userId, "UI", "FontSize", 12);
        var d = await _service.GetPreferenceAsync(_userId, "Operation", "TimeoutSeconds", 1.0);
        var dt = await _service.GetPreferenceAsync(_userId, "Operation", "LastOpen", DateTime.MinValue);
        var cfg = await _service.GetPreferenceAsync<object>(_userId, "Operation", "LLMConfig", new { });

        // Assert
        Assert.True(b);
        Assert.Equal(18, i);
        Assert.Equal(2.5, d);
        Assert.Equal(date, dt.Date);
        Assert.NotNull(cfg);
    }
    #endregion

    #region GetAllPreferences
    [Fact]
    public async Task GetAllPreferencesAsync_ReturnsGroupedByCategory()
    {
        var list = new List<UserPreferences>
        {
            new UserPreferences(_userId, "UI", "Theme", "Dark", "String"),
            new UserPreferences(_userId, "UI", "Font", "16", "Integer"),
            new UserPreferences(_userId, "Language", "UILanguage", "zh-CN", "String"),
        };
        _repo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var result = await _service.GetAllPreferencesAsync(_userId);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result["UI"].Count);
        Assert.Single(result["Language"]);
    }
    #endregion

    #region 批量设置
    [Fact]
    public async Task SetPreferencesBatchAsync_AddsAllPreferences()
    {
        var input = new Dictionary<string, Dictionary<string, object>>
        {
            ["UI"] = new() { ["Theme"] = "Dark", ["FontSize"] = 18 },
            ["Operation"] = new() { ["TimeoutSeconds"] = 2.5 }
        };
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserPreferences>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<UserPreferences> prefs, CancellationToken _) => prefs);
        var count = await _service.SetPreferencesBatchAsync(_userId, input);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task SetPreferencesBatchAsync_ReturnsZero_OnEmptyInput()
    {
        var count = await _service.SetPreferencesBatchAsync(_userId, new Dictionary<string, Dictionary<string, object>>());
        Assert.Equal(0, count);
    }
    #endregion

    #region 导出
    [Fact]
    public async Task ExportPreferencesAsync_ExcludesSystemDefaults_WhenFlagFalse()
    {
        var prefs = new List<UserPreferences>
        {
            new UserPreferences(_userId, "UI", "Theme", "Dark", "String", isSystemDefault:false),
            new UserPreferences(_userId, "UI", "Layout", "Grid", "String", isSystemDefault:true),
            new UserPreferences(_userId, "Language", "UILanguage", "en-US", "String", isSystemDefault:false)
        };
        _repo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        var export = await _service.ExportPreferencesAsync(_userId, includeSystemDefaults: false);
        Assert.True(export.Preferences.ContainsKey("UI"));
        Assert.Single(export.Preferences["UI"]); // system default excluded
    }

    [Fact]
    public async Task ExportPreferencesAsync_IncludesSystemDefaults_WhenFlagTrue()
    {
        var prefs = new List<UserPreferences>
        {
            new UserPreferences(_userId, "UI", "Theme", "Dark", "String", isSystemDefault:false),
            new UserPreferences(_userId, "UI", "Layout", "Grid", "String", isSystemDefault:true)
        };
        _repo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        var export = await _service.ExportPreferencesAsync(_userId, includeSystemDefaults: true);
        Assert.Equal(2, export.Preferences["UI"].Count);
    }
    #endregion

    #region 导入
    [Fact]
    public async Task ImportPreferencesAsync_Overwrites_WhenFlagTrue()
    {
        var import = new PreferenceExportData
        {
            UserId = _userId.ToString(),
            Preferences = new()
            {
                ["UI"] = new()
                {
                    ["Theme"] = new PreferenceExportItem { Value = "Dark", ValueType = "String" }
                }
            }
        };
        _repo.Setup(r => r.GetByKeyAsync(_userId, "UI", "Theme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences(_userId, "UI", "Theme", "Light", "String"));
        _repo.Setup(r => r.SetPreferenceAsync(_userId, "UI", "Theme", "Dark", "String", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences(_userId, "UI", "Theme", "Dark", "String"));
        var count = await _service.ImportPreferencesAsync(_userId, import, overwriteExisting: true);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ImportPreferencesAsync_SkipsExisting_WhenOverwriteFalse()
    {
        var import = new PreferenceExportData
        {
            UserId = _userId.ToString(),
            Preferences = new()
            {
                ["UI"] = new()
                {
                    ["Theme"] = new PreferenceExportItem { Value = "Dark", ValueType = "String" }
                }
            }
        };
        _repo.Setup(r => r.GetByKeyAsync(_userId, "UI", "Theme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences(_userId, "UI", "Theme", "Light", "String"));
        var count = await _service.ImportPreferencesAsync(_userId, import, overwriteExisting: false);
        Assert.Equal(0, count);
    }
    #endregion

    #region 事件通知
    [Fact]
    public async Task DeletePreferenceAsync_RaisesDeletedEvent()
    {
        var category = "UI"; var key = "Theme";
        var pref = new UserPreferences(_userId, category, key, "Dark", "String");
        _repo.Setup(r => r.GetByKeyAsync(_userId, category, key, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _repo.Setup(r => r.DeleteAsync(pref.PreferenceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        PreferenceChangedEventArgs? args = null;
        _service.PreferenceChanged += (_, e) => args = e;
        var ok = await _service.DeletePreferenceAsync(_userId, category, key);
        Assert.True(ok);
        Assert.NotNull(args);
        Assert.Equal(PreferenceChangeType.Deleted, args!.ChangeType);
    }

    [Fact]
    public async Task ResetAllPreferencesAsync_RaisesResetEvent()
    {
        _repo.Setup(r => r.ResetToDefaultsAsync(_userId, null, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        PreferenceChangedEventArgs? args = null;
        _service.PreferenceChanged += (_, e) => args = e;
        var count = await _service.ResetAllPreferencesAsync(_userId);
        Assert.Equal(10, count);
        Assert.NotNull(args);
        Assert.Equal(PreferenceChangeType.Reset, args!.ChangeType);
        Assert.Equal("*", args.Category);
    }

    [Fact]
    public async Task ResetCategoryPreferencesAsync_RaisesResetEvent()
    {
        var category = "UI";
        _repo.Setup(r => r.ResetToDefaultsAsync(_userId, category, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        PreferenceChangedEventArgs? args = null;
        _service.PreferenceChanged += (_, e) => args = e;
        var count = await _service.ResetCategoryPreferencesAsync(_userId, category);
        Assert.Equal(5, count);
        Assert.NotNull(args);
        Assert.Equal(PreferenceChangeType.Reset, args!.ChangeType);
        Assert.Equal(category, args.Category);
    }
    #endregion

    #region 异常与默认值
    [Fact]
    public async Task GetPreferenceAsync_ReturnsDefault_WhenRepositoryThrows()
    {
        _repo.Setup(r => r.GetByKeyAsync(_userId, "UI", "Theme", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));
        var result = await _service.GetPreferenceAsync(_userId, "UI", "Theme", "Light");
        Assert.Equal("Light", result);
    }
    #endregion

    #region 并发
    [Fact]
    public async Task SetPreferenceAsync_IsThreadSafe_ForConcurrentCalls()
    {
        var category = "UI"; var key = "Theme";
        _repo.Setup(r => r.GetByKeyAsync(_userId, category, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _repo.Setup(r => r.SetPreferenceAsync(_userId, category, key, It.IsAny<string>(), "String", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid u, string c, string k, string v, string vt, string? d, CancellationToken ct) => new UserPreferences(_userId, c, k, v, vt));

        var tasks = Enumerable.Range(0, 20).Select(i => _service.SetPreferenceAsync(_userId, category, key, $"Val{i}"));
        await Task.WhenAll(tasks);
        // 验证写入次数 == 20
        _repo.Verify(r => r.SetPreferenceAsync(_userId, category, key, It.IsAny<string>(), "String", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(20));
    }
    #endregion
}
