using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

public class QuickAccessServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IPreferenceService> _pref = new(MockBehavior.Strict);
    private readonly Mock<IFavoriteService> _fav = new(MockBehavior.Strict);
    private readonly Mock<IShortcutService> _shortcut = new(MockBehavior.Strict);
    private readonly Mock<ILogger<QuickAccessService>> _logger = new();
    private readonly QuickAccessService _service;

    public QuickAccessServiceTests()
    {
        _service = new QuickAccessService(_pref.Object, _fav.Object, _shortcut.Object, _logger.Object);
    }

    private void SetupDefaultPanel()
    {
        _pref.Setup(p => p.GetPreferenceAsync<QuickAccessPanelConfig>(
            _userId,
            "QuickAccess",
            "PanelConfig",
            It.IsAny<QuickAccessPanelConfig>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickAccessPanelConfig(true, "Grid", 12, DateTime.UtcNow));
    }

    private void SetupItems(string json = "[]")
    {
        _pref.Setup(p => p.GetPreferenceAsync<string>(
            _userId,
            "QuickAccess",
            "Items",
            "[]",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
    }

    [Fact]
    public async Task GetPanel_Defaults_ReturnsConfig()
    {
        SetupDefaultPanel();
        SetupItems();
        var panel = await _service.GetQuickAccessPanelAsync(_userId);
        panel.IsEnabled.Should().BeTrue();
        panel.Layout.Should().Be("Grid");
        panel.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_AddsAndSorts()
    {
        SetupItems();
        SetupDefaultPanel();
        _pref.Setup(p => p.SetPreferenceAsync(_userId, "QuickAccess", "Items", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.AddQuickAccessItemAsync(_userId, new AddQuickAccessItemRequest("Doc", "1", "Spec", null, null, 2));
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddItem_Duplicate_Fails()
    {
        var item = new QuickAccessItemDto("Doc", "1", "Spec", null, null, 1, true, DateTime.UtcNow);
        SetupItems(JsonSerializer.Serialize(new[] { item }));
        SetupDefaultPanel();
        var result = await _service.AddQuickAccessItemAsync(_userId, new AddQuickAccessItemRequest("Doc", "1", "Spec", null, null, 1));
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddItem_FullCapacity_Fails()
    {
        var max = 2;
        _pref.Setup(p => p.GetPreferenceAsync<QuickAccessPanelConfig>(_userId, "QuickAccess", "PanelConfig", It.IsAny<QuickAccessPanelConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickAccessPanelConfig(true, "Grid", max, DateTime.UtcNow));
        var items = Enumerable.Range(0, max).Select(i => new QuickAccessItemDto("Doc", i.ToString(), "Item" + i, null, null, i, true, DateTime.UtcNow)).ToArray();
        SetupItems(JsonSerializer.Serialize(items));
        var result = await _service.AddQuickAccessItemAsync(_userId, new AddQuickAccessItemRequest("Doc", "X", "New", null, null, 99));
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Recommend_MergesShortcutAndFavorites()
    {
        _shortcut.Setup(s => s.GetMostUsedShortcutsAsync(_userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ShortcutDto(Guid.NewGuid(), "Run", "Ctrl+R", "Action", string.Empty, string.Empty, "General", true, false, DateTime.UtcNow, null, 0, 10) });
        _fav.Setup(f => f.GetMostAccessedFavoritesAsync(_userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new FavoriteDto(Guid.NewGuid(), "Doc", "1", "Spec", "Docs", Array.Empty<string>(), "desc", 0, DateTime.UtcNow, DateTime.UtcNow, 5, true) });
        var recs = await _service.GetRecommendedQuickAccessItemsAsync(_userId, 6);
        recs.Should().Contain(r => r.ItemType == "Shortcut");
        recs.Should().Contain(r => r.ItemType == "Doc");
    }

    [Fact]
    public async Task Reset_RestoresDefaults()
    {
        _pref.Setup(p => p.SetPreferenceAsync(_userId, "QuickAccess", "PanelConfig", It.IsAny<QuickAccessPanelConfig>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _pref.Setup(p => p.SetPreferenceAsync(_userId, "QuickAccess", "Items", "[]", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ok = await _service.ResetQuickAccessPanelAsync(_userId);
        ok.Should().BeTrue();
    }
}
