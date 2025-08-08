using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 快速访问面板服务接口，管理用户的快速访问配置
/// </summary>


namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 快速访问面板服务实现
/// </summary>
public class QuickAccessService : IQuickAccessService
{
    private readonly IPreferenceService _preferenceService;
    private readonly IFavoriteService _favoriteService;
    private readonly IShortcutService _shortcutService;
    private readonly ILogger<QuickAccessService> _logger;

    private const string QuickAccessCategory = "QuickAccess";
    private const string PanelConfigKey = "PanelConfig";
    private const string ItemsConfigKey = "Items";

    public QuickAccessService(
        IPreferenceService preferenceService,
        IFavoriteService favoriteService,
        IShortcutService shortcutService,
        ILogger<QuickAccessService> logger)
    {
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _favoriteService = favoriteService ?? throw new ArgumentNullException(nameof(favoriteService));
        _shortcutService = shortcutService ?? throw new ArgumentNullException(nameof(shortcutService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QuickAccessPanelDto> GetQuickAccessPanelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取面板配置
            var panelConfig = await _preferenceService.GetPreferenceAsync<QuickAccessPanelConfig>(
                userId,
                QuickAccessCategory,
                PanelConfigKey,
                GetDefaultPanelConfig(),
                cancellationToken);

            // 获取快速访问项目
            var itemsJson = await _preferenceService.GetPreferenceAsync<string>(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                "[]",
                cancellationToken);

            var items = DeserializeQuickAccessItems(itemsJson);

            return new QuickAccessPanelDto(
                userId,
                panelConfig.IsEnabled,
                panelConfig.Layout,
                panelConfig.MaxItems,
                items,
                panelConfig.LastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quick access panel for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateQuickAccessPanelAsync(Guid userId, UpdateQuickAccessPanelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentConfig = await _preferenceService.GetPreferenceAsync<QuickAccessPanelConfig>(
                userId,
                QuickAccessCategory,
                PanelConfigKey,
                GetDefaultPanelConfig(),
                cancellationToken);

            var updatedConfig = currentConfig with
            {
                IsEnabled = request.IsEnabled ?? currentConfig.IsEnabled,
                Layout = request.Layout ?? currentConfig.Layout,
                MaxItems = request.MaxItems ?? currentConfig.MaxItems,
                LastUpdated = DateTime.UtcNow
            };

            var success = await _preferenceService.SetPreferenceAsync(
                userId,
                QuickAccessCategory,
                PanelConfigKey,
                updatedConfig,
                "Quick access panel configuration",
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("Updated quick access panel configuration for user {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quick access panel for user {UserId}", userId);
            return false;
        }
    }

    public async Task<AddQuickAccessItemResult> AddQuickAccessItemAsync(Guid userId, AddQuickAccessItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取当前项目列表
            var itemsJson = await _preferenceService.GetPreferenceAsync<string>(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                "[]",
                cancellationToken);

            var items = DeserializeQuickAccessItems(itemsJson).ToList();

            // 检查是否已存在
            if (items.Any(x => x.ItemType == request.ItemType && x.ItemId == request.ItemId))
            {
                return new AddQuickAccessItemResult(false, "Item already exists in quick access panel");
            }

            // 获取面板配置以检查最大项目数量
            var panelConfig = await _preferenceService.GetPreferenceAsync<QuickAccessPanelConfig>(
                userId,
                QuickAccessCategory,
                PanelConfigKey,
                GetDefaultPanelConfig(),
                cancellationToken);

            if (items.Count >= panelConfig.MaxItems)
            {
                return new AddQuickAccessItemResult(false, $"Quick access panel is full (max {panelConfig.MaxItems} items)");
            }

            // 添加新项目
            var newItem = new QuickAccessItemDto(
                request.ItemType,
                request.ItemId,
                request.ItemName,
                request.IconPath,
                request.Description,
                request.SortOrder,
                true,
                DateTime.UtcNow);

            items.Add(newItem);

            // 按排序顺序排序
            items = items.OrderBy(x => x.SortOrder).ThenBy(x => x.AddedAt).ToList();

            // 保存更新的项目列表
            var updatedItemsJson = SerializeQuickAccessItems(items);
            var success = await _preferenceService.SetPreferenceAsync(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                updatedItemsJson,
                "Quick access items configuration",
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("Added quick access item {ItemName} for user {UserId}", request.ItemName, userId);
                return new AddQuickAccessItemResult(true, null);
            }

            return new AddQuickAccessItemResult(false, "Failed to save quick access item");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add quick access item for user {UserId}", userId);
            return new AddQuickAccessItemResult(false, $"Error adding quick access item: {ex.Message}");
        }
    }

    public async Task<bool> RemoveQuickAccessItemAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取当前项目列表
            var itemsJson = await _preferenceService.GetPreferenceAsync<string>(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                "[]",
                cancellationToken);

            var items = DeserializeQuickAccessItems(itemsJson).ToList();

            // 移除指定项目
            var itemToRemove = items.FirstOrDefault(x => x.ItemType == itemType && x.ItemId == itemId);
            if (itemToRemove == null)
            {
                return true; // 项目不存在，视为成功
            }

            items.Remove(itemToRemove);

            // 保存更新的项目列表
            var updatedItemsJson = SerializeQuickAccessItems(items);
            var success = await _preferenceService.SetPreferenceAsync(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                updatedItemsJson,
                "Quick access items configuration",
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("Removed quick access item {ItemName} for user {UserId}", itemToRemove.ItemName, userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove quick access item for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<QuickAccessItemDto>> GetRecommendedQuickAccessItemsAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var recommendations = new List<QuickAccessItemDto>();

            // 获取最常用的快捷键
            var mostUsedShortcuts = await _shortcutService.GetMostUsedShortcutsAsync(userId, count / 2, cancellationToken);
            foreach (var shortcut in mostUsedShortcuts)
            {
                recommendations.Add(new QuickAccessItemDto(
                    "Shortcut",
                    shortcut.Id.ToString(),
                    shortcut.Name,
                    null,
                    $"Shortcut: {shortcut.KeyCombination}",
                    0,
                    true,
                    DateTime.UtcNow));
            }

            // 获取最常访问的收藏
            var mostAccessedFavorites = await _favoriteService.GetMostAccessedFavoritesAsync(userId, count / 2, cancellationToken);
            foreach (var favorite in mostAccessedFavorites)
            {
                recommendations.Add(new QuickAccessItemDto(
                    favorite.ItemType,
                    favorite.ItemId,
                    favorite.ItemName,
                    null,
                    favorite.Description,
                    0,
                    true,
                    DateTime.UtcNow));
            }

            return recommendations.Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommended quick access items for user {UserId}", userId);
            return Enumerable.Empty<QuickAccessItemDto>();
        }
    }

    public async Task<bool> ResetQuickAccessPanelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 重置面板配置
            var defaultConfig = GetDefaultPanelConfig();
            var configSuccess = await _preferenceService.SetPreferenceAsync(
                userId,
                QuickAccessCategory,
                PanelConfigKey,
                defaultConfig,
                "Quick access panel configuration",
                cancellationToken);

            // 重置项目列表
            var itemsSuccess = await _preferenceService.SetPreferenceAsync(
                userId,
                QuickAccessCategory,
                ItemsConfigKey,
                "[]",
                "Quick access items configuration",
                cancellationToken);

            var success = configSuccess && itemsSuccess;
            if (success)
            {
                _logger.LogInformation("Reset quick access panel for user {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset quick access panel for user {UserId}", userId);
            return false;
        }
    }

    #region Private Methods

    private static QuickAccessPanelConfig GetDefaultPanelConfig()
    {
        return new QuickAccessPanelConfig(
            IsEnabled: true,
            Layout: "Grid",
            MaxItems: 12,
            LastUpdated: DateTime.UtcNow);
    }

    private static IEnumerable<QuickAccessItemDto> DeserializeQuickAccessItems(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return Enumerable.Empty<QuickAccessItemDto>();
            }

            return System.Text.Json.JsonSerializer.Deserialize<QuickAccessItemDto[]>(json) ?? Enumerable.Empty<QuickAccessItemDto>();
        }
        catch
        {
            return Enumerable.Empty<QuickAccessItemDto>();
        }
    }

    private static string SerializeQuickAccessItems(IEnumerable<QuickAccessItemDto> items)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(items);
        }
        catch
        {
            return "[]";
        }
    }

    #endregion
}

/// <summary>
/// 快速访问面板配置
/// </summary>
internal record QuickAccessPanelConfig(
    bool IsEnabled,
    string Layout,
    int MaxItems,
    DateTime LastUpdated);
