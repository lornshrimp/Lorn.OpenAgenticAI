using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 收藏服务接口，管理用户收藏内容
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// 获取用户的所有收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>收藏列表</returns>
    Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定类型的收藏列表</returns>
    Task<IEnumerable<FavoriteDto>> GetFavoritesByTypeAsync(Guid userId, string itemType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据分类获取收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的收藏列表</returns>
    Task<IEnumerable<FavoriteDto>> GetFavoritesByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取收藏
    /// </summary>
    /// <param name="favoriteId">收藏ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>收藏详情</returns>
    Task<FavoriteDto?> GetFavoriteByIdAsync(Guid favoriteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">添加收藏请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加结果</returns>
    Task<AddFavoriteResult> AddFavoriteAsync(Guid userId, AddFavoriteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新收藏
    /// </summary>
    /// <param name="favoriteId">收藏ID</param>
    /// <param name="request">更新收藏请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<UpdateFavoriteResult> UpdateFavoriteAsync(Guid favoriteId, UpdateFavoriteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除收藏
    /// </summary>
    /// <param name="favoriteId">收藏ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> RemoveFavoriteAsync(Guid favoriteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据项目信息删除收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="itemId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> RemoveFavoriteByItemAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查项目是否已收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="itemId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已收藏</returns>
    Task<bool> IsItemFavoritedAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">切换收藏请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换结果</returns>
    Task<ToggleFavoriteResult> ToggleFavoriteAsync(Guid userId, ToggleFavoriteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">搜索请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<IEnumerable<FavoriteDto>> SearchFavoritesAsync(Guid userId, SearchFavoritesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取收藏分类
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    Task<IEnumerable<string>> GetFavoriteCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取收藏标签
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签列表</returns>
    Task<IEnumerable<string>> GetFavoriteTagsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最常访问的收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最常访问的收藏列表</returns>
    Task<IEnumerable<FavoriteDto>> GetMostAccessedFavoritesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近添加的收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近添加的收藏列表</returns>
    Task<IEnumerable<FavoriteDto>> GetRecentlyAddedFavoritesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录收藏访问
    /// </summary>
    /// <param name="favoriteId">收藏ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>记录成功标识</returns>
    Task<bool> RecordFavoriteAccessAsync(Guid favoriteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新收藏排序
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="sortOrderUpdates">排序更新列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateFavoriteSortOrdersAsync(Guid userId, IEnumerable<FavoriteSortOrderUpdate> sortOrderUpdates, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="requests">批量添加请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量添加结果</returns>
    Task<BatchAddFavoritesResult> BatchAddFavoritesAsync(Guid userId, IEnumerable<AddFavoriteRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出收藏配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出的收藏配置</returns>
    Task<FavoriteConfigurationExport> ExportFavoriteConfigurationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导入收藏配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="configurationData">配置数据</param>
    /// <param name="mergeMode">合并模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导入结果</returns>
    Task<ImportFavoriteConfigurationResult> ImportFavoriteConfigurationAsync(Guid userId, FavoriteConfigurationExport configurationData, ImportMergeMode mergeMode = ImportMergeMode.Merge, CancellationToken cancellationToken = default);
}

/// <summary>
/// 收藏DTO
/// </summary>
public record FavoriteDto(
    Guid Id,
    string ItemType,
    string ItemId,
    string ItemName,
    string Category,
    IEnumerable<string> Tags,
    string? Description,
    int SortOrder,
    DateTime CreatedAt,
    DateTime LastAccessedAt,
    int AccessCount,
    bool IsEnabled);

/// <summary>
/// 添加收藏请求
/// </summary>
public record AddFavoriteRequest(
    string ItemType,
    string ItemId,
    string ItemName,
    string Category = "",
    IEnumerable<string>? Tags = null,
    string? Description = null,
    int SortOrder = 0);

/// <summary>
/// 更新收藏请求
/// </summary>
public record UpdateFavoriteRequest(
    string? ItemName = null,
    string? Category = null,
    IEnumerable<string>? Tags = null,
    string? Description = null,
    int? SortOrder = null);

/// <summary>
/// 切换收藏请求
/// </summary>
public record ToggleFavoriteRequest(
    string ItemType,
    string ItemId,
    string ItemName,
    string Category = "",
    IEnumerable<string>? Tags = null,
    string? Description = null);

/// <summary>
/// 搜索收藏请求
/// </summary>
public record SearchFavoritesRequest(
    string SearchTerm,
    string? ItemType = null,
    string? Category = null,
    IEnumerable<string>? Tags = null);

/// <summary>
/// 添加收藏结果
/// </summary>
public record AddFavoriteResult(
    bool Success,
    Guid? FavoriteId,
    string? ErrorMessage);

/// <summary>
/// 更新收藏结果
/// </summary>
public record UpdateFavoriteResult(
    bool Success,
    string? ErrorMessage);

/// <summary>
/// 切换收藏结果
/// </summary>
public record ToggleFavoriteResult(
    bool Success,
    bool IsNowFavorited,
    Guid? FavoriteId,
    string? ErrorMessage);

/// <summary>
/// 收藏排序更新
/// </summary>
public record FavoriteSortOrderUpdate(Guid FavoriteId, int SortOrder);

/// <summary>
/// 批量添加收藏结果
/// </summary>
public record BatchAddFavoritesResult(
    bool Success,
    int AddedCount,
    int SkippedCount,
    int ErrorCount,
    IEnumerable<Guid> AddedFavoriteIds,
    IEnumerable<string> Errors);

/// <summary>
/// 收藏配置导出
/// </summary>
public record FavoriteConfigurationExport(
    Guid UserId,
    DateTime ExportedAt,
    IEnumerable<FavoriteDto> Favorites);

/// <summary>
/// 导入收藏配置结果
/// </summary>
public record ImportFavoriteConfigurationResult(
    bool Success,
    int ImportedCount,
    int SkippedCount,
    int ErrorCount,
    IEnumerable<string> Errors);

/// <summary>
/// 收藏项目类型常量
/// </summary>
public static class FavoriteItemTypes
{
    public const string Workflow = "Workflow";
    public const string Template = "Template";
    public const string Agent = "Agent";
    public const string Document = "Document";
    public const string Project = "Project";
    public const string Tool = "Tool";
    public const string Command = "Command";
    public const string Query = "Query";
    public const string Script = "Script";
    public const string Configuration = "Configuration";
}
