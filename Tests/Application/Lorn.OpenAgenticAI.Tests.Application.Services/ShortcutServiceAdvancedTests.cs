using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

public class ShortcutServiceAdvancedTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IUserShortcutRepository> _shortcutRepo = new(MockBehavior.Strict);
    private readonly Mock<IUserProfileRepository> _userRepo = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ShortcutService>> _logger = new();
    private readonly ShortcutService _service;

    public ShortcutServiceAdvancedTests()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile("default", "user"));
        _service = new ShortcutService(_shortcutRepo.Object, _userRepo.Object, _logger.Object);
    }

    private static UserShortcut NewShortcut(Guid userId, string name, string key, string actionType = "Action", string? category = null, bool enabled = true, int sort = 0)
    {
        var shortcut = new UserShortcut(userId, name, key, actionType, actionData: "{}", description: "test", category: category ?? "General", isGlobal: false, sortOrder: sort);
        if (!enabled) shortcut.Disable();
        return shortcut;
    }

    [Fact]
    public async Task CreateShortcut_NoConflict_Succeeds()
    {
        var request = new CreateShortcutRequest("OpenPanel", "Ctrl+K", "Open", "{}", "desc", "UI", false, 1);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("user", "nick"));
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, request.KeyCombination, null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.CreateShortcutAsync(_userId, request);
        result.Success.Should().BeTrue();
        result.ShortcutId.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateShortcut_Conflict_ReturnsConflictResult()
    {
        var request = new CreateShortcutRequest("OpenPanel", "Ctrl+K", "Open", "{}", "desc", "UI", false, 1);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("user", "nick"));
        var existing = NewShortcut(_userId, "Existing", "Ctrl+K");
        // 仅对目标组合返回冲突，其他组合（建议生成时尝试的不同组合）返回 null
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+K", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, It.Is<string>(k => k != "Ctrl+K"), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserShortcut?)null);

        // 直接调用冲突检测以确认 mock 生效
        var conflictCheck = await _service.CheckKeyCombinationConflictAsync(_userId, "Ctrl+K");
        conflictCheck.HasConflict.Should().BeTrue();
        conflictCheck.ConflictingShortcut.Should().NotBeNull();
        var result = await _service.CreateShortcutAsync(_userId, request);
        result.Success.Should().BeFalse();
        // 如果 ConflictInfo 仍为 null，说明 Create 内部调用未触发；添加调用计数验证
        _shortcutRepo.Verify(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+K", null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        // 放宽断言，暂允 ConflictInfo null（用于定位问题），但记录失败时信息
        if (result.ConflictInfo == null)
        {
            // 强化失败提示
            Assert.Fail("Expected conflict result with ConflictInfo not null");
        }
        // 冲突时不应调用 AddAsync
        _shortcutRepo.Verify(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteShortcut_IncrementsUsage()
    {
        var shortcut = NewShortcut(_userId, "Run", "Ctrl+R");
        _shortcutRepo.Setup(r => r.GetByKeyCombinationAsync(_userId, "Ctrl+R", It.IsAny<CancellationToken>())).ReturnsAsync(shortcut);
        _shortcutRepo.Setup(r => r.UpdateAsync(shortcut, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ExecuteShortcutAsync(_userId, "Ctrl+R");
        result.Success.Should().BeTrue();
        shortcut.UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task ExportShortcutConfiguration_ReturnsAll()
    {
        var list = new List<UserShortcut> { NewShortcut(_userId, "A", "Ctrl+A"), NewShortcut(_userId, "B", "Ctrl+B") };
        _shortcutRepo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var export = await _service.ExportShortcutConfigurationAsync(_userId);
        export.Shortcuts.Count().Should().Be(2);
    }

    [Fact]
    public async Task ImportShortcutConfiguration_SkipConflicts_Skips()
    {
        var dto = new ShortcutDto(Guid.NewGuid(), "A", "Ctrl+A", "Open", string.Empty, string.Empty, "General", true, false, DateTime.UtcNow, null, 0, 0);
        var export = new ShortcutConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        var existing = NewShortcut(_userId, "Existing", "Ctrl+A");
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+A", null, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportShortcutConfigurationAsync(_userId, export, ImportMergeMode.SkipConflicts);
        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportShortcutConfiguration_Replace_RemovesExistingThenAdds()
    {
        var dto = new ShortcutDto(Guid.NewGuid(), "A", "Ctrl+A", "Open", string.Empty, string.Empty, "General", true, false, DateTime.UtcNow, null, 0, 0);
        var export = new ShortcutConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        _shortcutRepo.Setup(r => r.DeleteByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+A", null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportShortcutConfigurationAsync(_userId, export, ImportMergeMode.Replace);
        result.ImportedCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchShortcuts_FiltersByTerm()
    {
        var list = new List<UserShortcut> { NewShortcut(_userId, "Open File", "Ctrl+O"), NewShortcut(_userId, "Save", "Ctrl+S") };
        _shortcutRepo.Setup(r => r.SearchShortcutsAsync(_userId, "Open", null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(list.Where(s => s.Name.Contains("Open")));
        var res = await _service.SearchShortcutsAsync(_userId, new SearchShortcutsRequest("Open", null, null, null));
        res.Should().ContainSingle();
        res.First().Name.Should().Contain("Open");
    }

    [Fact]
    public async Task UpdateSortOrders_Batch_Succeeds()
    {
        var updates = new[] { new ShortcutSortOrderUpdate(Guid.NewGuid(), 1), new ShortcutSortOrderUpdate(Guid.NewGuid(), 2) };
        _shortcutRepo.Setup(r => r.UpdateSortOrdersAsync(_userId, It.IsAny<Dictionary<Guid, int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ok = await _service.UpdateShortcutSortOrdersAsync(_userId, updates);
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task Concurrent_CreateShortcut_CallsAddMultipleTimes()
    {
        var request = new CreateShortcutRequest("Open", "Ctrl+Shift+O", "Open", string.Empty, string.Empty, "UI", false, 0);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, request.KeyCombination, null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var tasks = Enumerable.Range(0, 10).Select(_ => _service.CreateShortcutAsync(_userId, request));
        var results = await Task.WhenAll(tasks);
        results.Should().OnlyContain(r => r.Success);
        _shortcutRepo.Verify(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }
}
