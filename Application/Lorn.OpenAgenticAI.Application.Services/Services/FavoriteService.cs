using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 收藏服务实现，管理用户收藏内容
/// </summary>
public class FavoriteService : IFavoriteService
{
    private readonly IUserFavoriteRepository _favoriteRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(
        IUserFavoriteRepository favoriteRepository,
        IUserRepository userRepository,
        ILogger<FavoriteService> logger)
    {
        _favoriteRepository = favoriteRepository ?? throw new ArgumentNullException(nameof(favoriteRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetByUserIdAsync(userId, cancellationToken);
            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user favorites for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteDto>> GetFavoritesByTypeAsync(Guid userId, string itemType, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetByUserIdAndTypeAsync(userId, itemType, cancellationToken);
            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorites by type {ItemType} for user {UserId}", itemType, userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteDto>> GetFavoritesByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetByCategoryAsync(userId, category, cancellationToken);
            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorites by category {Category} for user {UserId}", category, userId);
            throw;
        }
    }

    public async Task<FavoriteDto?> GetFavoriteByIdAsync(Guid favoriteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorite = await _favoriteRepository.GetByIdAsync(favoriteId, cancellationToken);
            return favorite != null ? MapToDto(favorite) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorite {FavoriteId}", favoriteId);
            throw;
        }
    }

    public async Task<AddFavoriteResult> AddFavoriteAsync(Guid userId, AddFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证用户存在
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new AddFavoriteResult(false, null, "User not found");
            }

            // 检查是否已经收藏
            var existingFavorite = await _favoriteRepository.GetByUserItemAsync(userId, request.ItemType, request.ItemId, cancellationToken);
            if (existingFavorite != null)
            {
                return new AddFavoriteResult(false, existingFavorite.Id, "Item is already favorited");
            }

            // 创建收藏
            var favorite = new UserFavorite(
                userId,
                request.ItemType,
                request.ItemId,
                request.ItemName,
                request.Category,
                string.Empty, // 先设置为空字符串
                null,
                request.SortOrder);

            // 设置标签
            if (request.Tags != null)
            {
                favorite.SetTags(request.Tags);
            }

            var success = await _favoriteRepository.AddAsync(favorite, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Added favorite {ItemName} for user {UserId}", request.ItemName, userId);
                return new AddFavoriteResult(true, favorite.Id, null);
            }

            return new AddFavoriteResult(false, null, "Failed to add favorite");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add favorite for user {UserId}", userId);
            return new AddFavoriteResult(false, null, $"Error adding favorite: {ex.Message}");
        }
    }

    public async Task<UpdateFavoriteResult> UpdateFavoriteAsync(Guid favoriteId, UpdateFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorite = await _favoriteRepository.GetByIdAsync(favoriteId, cancellationToken);
            if (favorite == null)
            {
                return new UpdateFavoriteResult(false, "Favorite not found");
            }

            // 更新收藏信息
            favorite.UpdateFavorite(
                request.ItemName,
                request.Category,
                null, // Tags will be updated separately
                request.Description);

            // 更新标签
            if (request.Tags != null)
            {
                favorite.SetTags(request.Tags);
            }

            // 更新排序
            if (request.SortOrder.HasValue)
            {
                favorite.UpdateSortOrder(request.SortOrder.Value);
            }

            var success = await _favoriteRepository.UpdateAsync(favorite, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Updated favorite {FavoriteId}", favoriteId);
                return new UpdateFavoriteResult(true, null);
            }

            return new UpdateFavoriteResult(false, "Failed to update favorite");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update favorite {FavoriteId}", favoriteId);
            return new UpdateFavoriteResult(false, $"Error updating favorite: {ex.Message}");
        }
    }

    public async Task<bool> RemoveFavoriteAsync(Guid favoriteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _favoriteRepository.DeleteAsync(favoriteId, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Removed favorite {FavoriteId}", favoriteId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove favorite {FavoriteId}", favoriteId);
            return false;
        }
    }

    public async Task<bool> RemoveFavoriteByItemAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorite = await _favoriteRepository.GetByUserItemAsync(userId, itemType, itemId, cancellationToken);
            if (favorite == null)
            {
                return true; // 已经不存在，视为成功
            }

            return await RemoveFavoriteAsync(favorite.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove favorite by item for user {UserId}, type {ItemType}, id {ItemId}", userId, itemType, itemId);
            return false;
        }
    }

    public async Task<bool> IsItemFavoritedAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _favoriteRepository.IsItemFavoritedAsync(userId, itemType, itemId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if item is favorited for user {UserId}, type {ItemType}, id {ItemId}", userId, itemType, itemId);
            return false;
        }
    }

    public async Task<ToggleFavoriteResult> ToggleFavoriteAsync(Guid userId, ToggleFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingFavorite = await _favoriteRepository.GetByUserItemAsync(userId, request.ItemType, request.ItemId, cancellationToken);

            if (existingFavorite != null)
            {
                // 已收藏，取消收藏
                var removeSuccess = await RemoveFavoriteAsync(existingFavorite.Id, cancellationToken);
                return new ToggleFavoriteResult(removeSuccess, false, null, removeSuccess ? null : "Failed to remove favorite");
            }
            else
            {
                // 未收藏，添加收藏
                var addRequest = new AddFavoriteRequest(
                    request.ItemType,
                    request.ItemId,
                    request.ItemName,
                    request.Category,
                    request.Tags,
                    request.Description);

                var addResult = await AddFavoriteAsync(userId, addRequest, cancellationToken);
                return new ToggleFavoriteResult(addResult.Success, addResult.Success, addResult.FavoriteId, addResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite for user {UserId}", userId);
            return new ToggleFavoriteResult(false, false, null, $"Error toggling favorite: {ex.Message}");
        }
    }

    public async Task<IEnumerable<FavoriteDto>> SearchFavoritesAsync(Guid userId, SearchFavoritesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.SearchFavoritesAsync(
                userId,
                request.SearchTerm,
                request.ItemType,
                request.Category,
                request.Tags,
                cancellationToken);

            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search favorites for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetFavoriteCategoriesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _favoriteRepository.GetCategoriesAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorite categories for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetFavoriteTagsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _favoriteRepository.GetTagsAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorite tags for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteDto>> GetMostAccessedFavoritesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetMostAccessedAsync(userId, count, cancellationToken);
            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most accessed favorites for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteDto>> GetRecentlyAddedFavoritesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetRecentlyAddedAsync(userId, count, cancellationToken);
            return favorites.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recently added favorites for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RecordFavoriteAccessAsync(Guid favoriteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorite = await _favoriteRepository.GetByIdAsync(favoriteId, cancellationToken);
            if (favorite == null)
            {
                return false;
            }

            favorite.RecordAccess();
            var success = await _favoriteRepository.UpdateAsync(favorite, cancellationToken);

            if (success)
            {
                _logger.LogDebug("Recorded access for favorite {FavoriteId}", favoriteId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record favorite access {FavoriteId}", favoriteId);
            return false;
        }
    }

    public async Task<bool> UpdateFavoriteSortOrdersAsync(Guid userId, IEnumerable<FavoriteSortOrderUpdate> sortOrderUpdates, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateDict = sortOrderUpdates.ToDictionary(x => x.FavoriteId, x => x.SortOrder);
            var success = await _favoriteRepository.UpdateSortOrdersAsync(userId, updateDict, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Updated sort orders for {Count} favorites for user {UserId}", updateDict.Count, userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update favorite sort orders for user {UserId}", userId);
            return false;
        }
    }

    public async Task<BatchAddFavoritesResult> BatchAddFavoritesAsync(Guid userId, IEnumerable<AddFavoriteRequest> requests, CancellationToken cancellationToken = default)
    {
        try
        {
            var addedFavoriteIds = new List<Guid>();
            var errors = new List<string>();
            int addedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            foreach (var request in requests)
            {
                try
                {
                    var result = await AddFavoriteAsync(userId, request, cancellationToken);
                    if (result.Success && result.FavoriteId.HasValue)
                    {
                        addedCount++;
                        addedFavoriteIds.Add(result.FavoriteId.Value);
                    }
                    else if (result.ErrorMessage?.Contains("already favorited") == true)
                    {
                        skippedCount++;
                    }
                    else
                    {
                        errorCount++;
                        errors.Add($"Failed to add favorite {request.ItemName}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"Error adding favorite {request.ItemName}: {ex.Message}");
                }
            }

            _logger.LogInformation("Batch added {AddedCount} favorites for user {UserId}, skipped {SkippedCount}, errors {ErrorCount}",
                addedCount, userId, skippedCount, errorCount);

            return new BatchAddFavoritesResult(
                errorCount == 0,
                addedCount,
                skippedCount,
                errorCount,
                addedFavoriteIds,
                errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add favorites for user {UserId}", userId);
            throw;
        }
    }

    public async Task<FavoriteConfigurationExport> ExportFavoriteConfigurationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var favorites = await _favoriteRepository.GetByUserIdAsync(userId, cancellationToken);
            var favoriteDtos = favorites.Select(MapToDto);

            return new FavoriteConfigurationExport(userId, DateTime.UtcNow, favoriteDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export favorite configuration for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ImportFavoriteConfigurationResult> ImportFavoriteConfigurationAsync(Guid userId, FavoriteConfigurationExport configurationData, ImportMergeMode mergeMode = ImportMergeMode.Merge, CancellationToken cancellationToken = default)
    {
        try
        {
            var errors = new List<string>();
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            // 如果是替换模式，先删除所有现有收藏
            if (mergeMode == ImportMergeMode.Replace)
            {
                await _favoriteRepository.DeleteByUserIdAsync(userId, cancellationToken);
            }

            foreach (var favoriteDto in configurationData.Favorites)
            {
                try
                {
                    // 检查冲突
                    if (mergeMode == ImportMergeMode.SkipConflicts || mergeMode == ImportMergeMode.Merge)
                    {
                        var isAlreadyFavorited = await IsItemFavoritedAsync(userId, favoriteDto.ItemType, favoriteDto.ItemId, cancellationToken);
                        if (isAlreadyFavorited)
                        {
                            if (mergeMode == ImportMergeMode.SkipConflicts)
                            {
                                skippedCount++;
                                continue;
                            }
                            // 对于Merge模式，我们可以选择更新现有收藏或跳过
                            skippedCount++;
                            continue;
                        }
                    }

                    var addRequest = new AddFavoriteRequest(
                        favoriteDto.ItemType,
                        favoriteDto.ItemId,
                        favoriteDto.ItemName,
                        favoriteDto.Category,
                        favoriteDto.Tags,
                        favoriteDto.Description,
                        favoriteDto.SortOrder);

                    var result = await AddFavoriteAsync(userId, addRequest, cancellationToken);
                    if (result.Success)
                    {
                        importedCount++;
                    }
                    else
                    {
                        errorCount++;
                        errors.Add($"Failed to import favorite: {favoriteDto.ItemName} - {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"Error importing favorite {favoriteDto.ItemName}: {ex.Message}");
                }
            }

            _logger.LogInformation("Imported {ImportedCount} favorites for user {UserId}, skipped {SkippedCount}, errors {ErrorCount}",
                importedCount, userId, skippedCount, errorCount);

            return new ImportFavoriteConfigurationResult(
                errorCount == 0,
                importedCount,
                skippedCount,
                errorCount,
                errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import favorite configuration for user {UserId}", userId);
            throw;
        }
    }

    #region Private Methods

    private static FavoriteDto MapToDto(UserFavorite favorite)
    {
        return new FavoriteDto(
            favorite.Id,
            favorite.ItemType,
            favorite.ItemId,
            favorite.ItemName,
            favorite.Category,
            favorite.GetTagsList(),
            favorite.Description,
            favorite.SortOrder,
            favorite.CreatedAt,
            favorite.LastAccessedAt,
            favorite.AccessCount,
            favorite.IsEnabled);
    }

    #endregion
}
