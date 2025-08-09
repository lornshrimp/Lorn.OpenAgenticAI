using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Application.Services;

public class ShortcutServiceAdvancedTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IUserShortcutRepository> _shortcutRepo = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepo = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ShortcutService>> _logger = new();
    private readonly ShortcutService _service;

    public ShortcutServiceAdvancedTests()
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()));
        _service = new ShortcutService(_shortcutRepo.Object, _userRepo.Object, _logger.Object);
    }

    private static UserShortcut NewShortcut(Guid userId, string name, string key, string actionType = "Action", string? category = null, bool enabled = true, int sort = 0)
        => new(userId, name, key, actionType, null, null, category ?? "General", isGlobal: false, sortOrder: sort);

    [Fact]
    public async Task CreateShortcut_NoConflict_Succeeds()
    {
        var request = new CreateShortcutRequest("OpenPanel", "Ctrl+K", "Open", "{}", "desc", "UI", false, 1);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("user", "nick"));
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, request.KeyCombination, null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.CreateShortcutAsync(_userId, request);
        Assert.True(result.Success);
        Assert.NotNull(result.ShortcutId);
    }

    [Fact]
    public async Task CreateShortcut_Conflict_ReturnsConflictResult()
    {
        var request = new CreateShortcutRequest("OpenPanel", "Ctrl+K", "Open", "{}", "desc", "UI", false, 1);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("user", "nick"));
        var existing = NewShortcut(_userId, "Existing", "Ctrl+K");
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, request.KeyCombination, null, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null); // for suggestion generation
        var result = await _service.CreateShortcutAsync(_userId, request);
        Assert.False(result.Success);
        Assert.NotNull(result.ConflictResult);
        Assert.True(result.ConflictResult!.HasConflict);
        Assert.NotEmpty(result.ConflictResult.Suggestions);
    }

    [Fact]
    public async Task ExecuteShortcut_IncrementsUsage()
    {
        var shortcut = NewShortcut(_userId, "Run", "Ctrl+R");
        _shortcutRepo.Setup(r => r.GetByKeyCombinationAsync(_userId, "Ctrl+R", It.IsAny<CancellationToken>())).ReturnsAsync(shortcut);
        _shortcutRepo.Setup(r => r.UpdateAsync(shortcut, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ExecuteShortcutAsync(_userId, "Ctrl+R");
        Assert.True(result.Success);
        Assert.Equal(1, shortcut.UsageCount);
    }

    [Fact]
    public async Task ExportShortcutConfiguration_ReturnsAll()
    {
        var list = new List<UserShortcut> { NewShortcut(_userId, "A", "Ctrl+A"), NewShortcut(_userId, "B", "Ctrl+B") };
        _shortcutRepo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var export = await _service.ExportShortcutConfigurationAsync(_userId);
        Assert.Equal(2, export.Shortcuts.Count());
    }

    [Fact]
    public async Task ImportShortcutConfiguration_SkipConflicts_Skips()
    {
        var dto = new ShortcutDto(Guid.NewGuid(), "A", "Ctrl+A", "Open", null, null, "General", true, false, DateTime.UtcNow, null, 0, 0);
        var export = new ShortcutConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        var existing = NewShortcut(_userId, "Existing", "Ctrl+A");
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+A", null, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportShortcutConfigurationAsync(_userId, export, ImportMergeMode.SkipConflicts);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public async Task ImportShortcutConfiguration_Replace_RemovesExistingThenAdds()
    {
        var dto = new ShortcutDto(Guid.NewGuid(), "A", "Ctrl+A", "Open", null, null, "General", true, false, DateTime.UtcNow, null, 0, 0);
        var export = new ShortcutConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        _shortcutRepo.Setup(r => r.DeleteByUserIdAsync(_userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, "Ctrl+A", null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportShortcutConfigurationAsync(_userId, export, ImportMergeMode.Replace);
        Assert.Equal(1, result.ImportedCount);
    }

    [Fact]
    public async Task SearchShortcuts_FiltersByTerm()
    {
        var list = new List<UserShortcut> { NewShortcut(_userId, "Open File", "Ctrl+O"), NewShortcut(_userId, "Save", "Ctrl+S") };
        _shortcutRepo.Setup(r => r.SearchShortcutsAsync(_userId, "Open", null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(list.Where(s => s.Name.Contains("Open")));
        var res = await _service.SearchShortcutsAsync(_userId, new SearchShortcutsRequest("Open", null, null, null));
        Assert.Single(res);
        Assert.Contains("Open", res.First().Name);
    }

    [Fact]
    public async Task UpdateSortOrders_Batch_Succeeds()
    {
        var updates = new[] { new ShortcutSortOrderUpdate(Guid.NewGuid(), 1), new ShortcutSortOrderUpdate(Guid.NewGuid(), 2) };
        _shortcutRepo.Setup(r => r.UpdateSortOrdersAsync(_userId, It.IsAny<Dictionary<Guid, int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ok = await _service.UpdateShortcutSortOrdersAsync(_userId, updates);
        Assert.True(ok);
    }

    [Fact]
    public async Task Concurrent_CreateShortcut_CallsAddMultipleTimes()
    {
        var request = new CreateShortcutRequest("Open", "Ctrl+Shift+O", "Open", null, null, "UI", false, 0);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _shortcutRepo.Setup(r => r.CheckKeyCombinationConflictAsync(_userId, request.KeyCombination, null, It.IsAny<CancellationToken>())).ReturnsAsync((UserShortcut?)null);
        _shortcutRepo.Setup(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var tasks = Enumerable.Range(0, 10).Select(_ => _service.CreateShortcutAsync(_userId, request));
        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.True(r.Success));
        _shortcutRepo.Verify(r => r.AddAsync(It.IsAny<UserShortcut>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }
}
