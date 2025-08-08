using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户收藏仓储接口，定义收藏数据访问契约
/// </summary>
public interface IUserFavoriteRepository
{
    /// <summary>
    /// 根据ID获取收藏项
    /// </summary>
    /// <param name="favoriteId">收藏项ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>收藏项，如果不存在则返回null</returns>
    Task<UserFavorite?> GetByIdAsync(Guid favoriteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和项目类型获取收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定类型的收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> GetByUserIdAndTypeAsync(Guid userId, string itemType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和分类获取收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定项目是否已被用户收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="itemId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果已收藏返回true，否则返回false</returns>
    Task<bool> IsItemFavoritedAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID、项目类型和项目ID获取收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="itemId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>收藏项，如果不存在则返回null</returns>
    Task<UserFavorite?> GetByUserItemAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索用户收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="itemType">项目类型（可选）</param>
    /// <param name="category">分类（可选）</param>
    /// <param name="tags">标签列表（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> SearchFavoritesAsync(
        Guid userId,
        string searchTerm,
        string? itemType = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户收藏的所有分类
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户收藏的所有标签
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签列表</returns>
    Task<IEnumerable<string>> GetTagsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最常访问的收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最常访问的收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> GetMostAccessedAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最近添加的收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近添加的收藏项列表</returns>
    Task<IEnumerable<UserFavorite>> GetRecentlyAddedAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加收藏项
    /// </summary>
    /// <param name="favorite">收藏项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加成功标识</returns>
    Task<bool> AddAsync(UserFavorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新收藏项
    /// </summary>
    /// <param name="favorite">收藏项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateAsync(UserFavorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除收藏项
    /// </summary>
    /// <param name="favoriteId">收藏项ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeleteAsync(Guid favoriteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有收藏项
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新收藏项排序
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="favoriteOrders">收藏项ID和排序的映射</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateSortOrdersAsync(Guid userId, Dictionary<Guid, int> favoriteOrders, CancellationToken cancellationToken = default);
}
