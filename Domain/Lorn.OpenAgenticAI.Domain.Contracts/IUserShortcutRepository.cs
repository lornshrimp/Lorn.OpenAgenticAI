using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户快捷键仓储接口，定义快捷键数据访问契约
/// </summary>
public interface IUserShortcutRepository
{
    /// <summary>
    /// 根据ID获取快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>快捷键，如果不存在则返回null</returns>
    Task<UserShortcut?> GetByIdAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户启用的快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启用的快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和分类获取快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据按键组合查找快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="keyCombination">按键组合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的快捷键，如果不存在则返回null</returns>
    Task<UserShortcut?> GetByKeyCombinationAsync(Guid userId, string keyCombination, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查按键组合是否冲突
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="keyCombination">按键组合</param>
    /// <param name="excludeShortcutId">排除的快捷键ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果冲突返回冲突的快捷键，否则返回null</returns>
    Task<UserShortcut?> CheckKeyCombinationConflictAsync(Guid userId, string keyCombination, Guid? excludeShortcutId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全局快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全局快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetGlobalShortcutsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据动作类型获取快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定动作类型的快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetByActionTypeAsync(Guid userId, string actionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchTerm">搜索词（搜索名称、描述、按键组合）</param>
    /// <param name="category">分类（可选）</param>
    /// <param name="actionType">动作类型（可选）</param>
    /// <param name="isEnabled">是否启用（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> SearchShortcutsAsync(
        Guid userId,
        string searchTerm,
        string? category = null,
        string? actionType = null,
        bool? isEnabled = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户快捷键的所有分类
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户最常用的快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最常用的快捷键列表</returns>
    Task<IEnumerable<UserShortcut>> GetMostUsedAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统推荐的快捷键组合（不冲突的）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐的快捷键组合列表</returns>
    Task<IEnumerable<string>> GetRecommendedKeyCombinationsAsync(Guid userId, string actionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加快捷键
    /// </summary>
    /// <param name="shortcut">快捷键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加成功标识</returns>
    Task<bool> AddAsync(UserShortcut shortcut, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新快捷键
    /// </summary>
    /// <param name="shortcut">快捷键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateAsync(UserShortcut shortcut, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeleteAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新快捷键排序
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="shortcutOrders">快捷键ID和排序的映射</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateSortOrdersAsync(Guid userId, Dictionary<Guid, int> shortcutOrders, CancellationToken cancellationToken = default);
}
