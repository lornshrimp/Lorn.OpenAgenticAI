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

public class FavoriteServiceAdvancedTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IUserFavoriteRepository> _favoriteRepo = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepo = new(MockBehavior.Strict);
    private readonly Mock<ILogger<FavoriteService>> _logger = new();
    private readonly FavoriteService _service;

    public FavoriteServiceAdvancedTests()
    {
        // 默认返回一个用户配置，避免后续测试遗漏 Setup 导致 null
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile("default", "user"));
        _service = new FavoriteService(_favoriteRepo.Object, _userRepo.Object, _logger.Object);
    }

    private static UserFavorite NewFavorite(Guid userId, string type, string itemId, string name, string? category = null, int sort = 0)
        => new(userId, type, itemId, name, category ?? "General", string.Empty, null, sort);

    [Fact]
    public async Task AddFavorite_New_Succeeds()
    {
        var req = new AddFavoriteRequest("Doc", "1", "Spec", "Docs", new[] { "ref" }, "desc", 1);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _favoriteRepo.Setup(r => r.GetByUserItemAsync(_userId, req.ItemType, req.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync((UserFavorite?)null);
        _favoriteRepo.Setup(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var res = await _service.AddFavoriteAsync(_userId, req);
        Assert.True(res.Success);
        Assert.NotNull(res.FavoriteId);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsAlreadyFavorited()
    {
        var req = new AddFavoriteRequest("Doc", "1", "Spec", "Docs", null, null, 0);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        var existing = NewFavorite(_userId, req.ItemType, req.ItemId, req.ItemName);
        _favoriteRepo.Setup(r => r.GetByUserItemAsync(_userId, req.ItemType, req.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var res = await _service.AddFavoriteAsync(_userId, req);
        Assert.False(res.Success);
        Assert.Equal("Item is already favorited", res.ErrorMessage);
        Assert.Equal(existing.Id, res.FavoriteId);
    }

    [Fact]
    public async Task ToggleFavorite_AddThenRemove_Works()
    {
        var toggleReq = new ToggleFavoriteRequest("Doc", "1", "Spec", "Docs", null, null);
        // 第一次 Toggle：
        //   ToggleFavoriteAsync -> GetByUserItemAsync (null) 触发 AddFavoriteAsync
        //   AddFavoriteAsync 内部再次调用 GetByUserItemAsync (仍需返回 null)
        // 第二次 Toggle：
        //   ToggleFavoriteAsync -> GetByUserItemAsync (返回已存在 Favorited 实例) 触发删除
        var existing = NewFavorite(_userId, toggleReq.ItemType, toggleReq.ItemId, toggleReq.ItemName);
        _favoriteRepo.SetupSequence(r => r.GetByUserItemAsync(_userId, toggleReq.ItemType, toggleReq.ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorite?)null) // Toggle 外层第一次判定
            .ReturnsAsync((UserFavorite?)null) // AddFavoriteAsync 内部再次判定
            .ReturnsAsync(existing);           // 第二次 Toggle 外层判定已存在
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _favoriteRepo.Setup(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _favoriteRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var addResult = await _service.ToggleFavoriteAsync(_userId, toggleReq);
        Assert.True(addResult.Success);
        Assert.True(addResult.IsNowFavorited);
        var removeResult = await _service.ToggleFavoriteAsync(_userId, toggleReq);
        Assert.True(removeResult.Success);
        Assert.False(removeResult.IsNowFavorited);
    }

    [Fact]
    public async Task UpdateFavorite_ModifiesFieldsAndTags()
    {
        var fav = NewFavorite(_userId, "Doc", "1", "Spec", "Docs");
        _favoriteRepo.Setup(r => r.GetByIdAsync(fav.Id, It.IsAny<CancellationToken>())).ReturnsAsync(fav);
        _favoriteRepo.Setup(r => r.UpdateAsync(fav, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var req = new UpdateFavoriteRequest("Spec2", "Docs2", new[] { "t1", "t2" }, "desc", 5);
        var res = await _service.UpdateFavoriteAsync(fav.Id, req);
        Assert.True(res.Success);
        Assert.Equal(5, fav.SortOrder);
        Assert.Contains("t1", fav.GetTagsList());
    }

    [Fact]
    public async Task SearchFavorites_ByNameAndTag()
    {
        var f1 = NewFavorite(_userId, "Doc", "1", "Spec Alpha", "Docs"); f1.SetTags(new[] { "alpha" });
        var f2 = NewFavorite(_userId, "Doc", "2", "Spec Beta", "Docs"); f2.SetTags(new[] { "beta" });
        _favoriteRepo.Setup(r => r.SearchFavoritesAsync(_userId, "Alpha", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { f1 });
        var res = await _service.SearchFavoritesAsync(_userId, new SearchFavoritesRequest("Alpha", null, null, null));
        Assert.Single(res);
        Assert.Contains("Alpha", res.First().ItemName);
    }

    [Fact]
    public async Task GetCategories_ReturnsList()
    {
        _favoriteRepo.Setup(r => r.GetCategoriesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "Docs", "UI" });
        var cats = await _service.GetFavoriteCategoriesAsync(_userId);
        Assert.Contains("Docs", cats);
    }

    [Fact]
    public async Task ExportFavoriteConfiguration_ReturnsAll()
    {
        var favs = new[] { NewFavorite(_userId, "Doc", "1", "A"), NewFavorite(_userId, "Doc", "2", "B") };
        _favoriteRepo.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(favs);
        var export = await _service.ExportFavoriteConfigurationAsync(_userId);
        Assert.Equal(2, export.Favorites.Count());
    }

    [Fact]
    public async Task ImportFavoriteConfiguration_SkipExisting_Skips()
    {
        var dto = new FavoriteDto(Guid.NewGuid(), "Doc", "1", "Spec", "Docs", new[] { "t" }, "desc", 0, DateTime.UtcNow, DateTime.UtcNow, 0, true);
        var export = new FavoriteConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        _favoriteRepo.Setup(r => r.DeleteByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _favoriteRepo.Setup(r => r.IsItemFavoritedAsync(_userId, dto.ItemType, dto.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportFavoriteConfigurationAsync(_userId, export, ImportMergeMode.SkipConflicts);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public async Task ImportFavoriteConfiguration_Replace_Imports()
    {
        var dto = new FavoriteDto(Guid.NewGuid(), "Doc", "1", "Spec", "Docs", Array.Empty<string>(), null, 0, DateTime.UtcNow, DateTime.UtcNow, 0, true);
        var export = new FavoriteConfigurationExport(_userId, DateTime.UtcNow, new[] { dto });
        _favoriteRepo.Setup(r => r.DeleteByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _favoriteRepo.Setup(r => r.IsItemFavoritedAsync(_userId, dto.ItemType, dto.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _favoriteRepo.Setup(r => r.GetByUserItemAsync(_userId, dto.ItemType, dto.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync((UserFavorite?)null);
        _favoriteRepo.Setup(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.ImportFavoriteConfigurationAsync(_userId, export, ImportMergeMode.Replace);
        Assert.Equal(1, result.ImportedCount);
    }

    [Fact]
    public async Task BatchAddFavorites_MixedResults()
    {
        var reqs = new[] { new AddFavoriteRequest("Doc", "1", "A", "Docs", null, null, 0), new AddFavoriteRequest("Doc", "2", "B", "Docs", null, null, 0) };
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _favoriteRepo.SetupSequence(r => r.GetByUserItemAsync(_userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorite?)null)
            .ReturnsAsync(NewFavorite(_userId, "Doc", "2", "B"));
        _favoriteRepo.Setup(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.BatchAddFavoritesAsync(_userId, reqs);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount); // duplicate
    }

    [Fact]
    public async Task UpdateFavoriteSortOrders_Batch_Succeeds()
    {
        _favoriteRepo.Setup(r => r.UpdateSortOrdersAsync(_userId, It.IsAny<Dictionary<Guid, int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ok = await _service.UpdateFavoriteSortOrdersAsync(_userId, new[] { new FavoriteSortOrderUpdate(Guid.NewGuid(), 1) });
        Assert.True(ok);
    }

    [Fact]
    public async Task Concurrent_AddFavorite_Parallel()
    {
        var req = new AddFavoriteRequest("Doc", "X", "ItemX", "Docs", null, null, 0);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile("u", "n"));
        _favoriteRepo.Setup(r => r.GetByUserItemAsync(_userId, req.ItemType, req.ItemId, It.IsAny<CancellationToken>())).ReturnsAsync((UserFavorite?)null);
        _favoriteRepo.Setup(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var tasks = Enumerable.Range(0, 5).Select(_ => _service.AddFavoriteAsync(_userId, req));
        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.True(r.Success));
        _favoriteRepo.Verify(r => r.AddAsync(It.IsAny<UserFavorite>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}
