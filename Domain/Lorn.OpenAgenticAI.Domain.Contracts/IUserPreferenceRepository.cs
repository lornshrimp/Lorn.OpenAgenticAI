using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户偏好设置仓储接口，定义用户偏好数据访问契约
/// </summary>
public interface IUserPreferenceRepository
{
    /// <summary>
    /// 根据偏好ID获取用户偏好设置
    /// </summary>
    /// <param name="preferenceId">偏好ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户偏好设置，如果不存在则返回null</returns>
    Task<UserPreferences?> GetByIdAsync(Guid preferenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID获取所有偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户的所有偏好设置</returns>
    Task<IEnumerable<UserPreferences>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和分类获取偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的偏好设置列表</returns>
    Task<IEnumerable<UserPreferences>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID、分类和键获取特定偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>特定的偏好设置，如果不存在则返回null</returns>
    Task<UserPreferences?> GetByKeyAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统默认偏好设置
    /// </summary>
    /// <param name="category">偏好分类（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统默认偏好设置列表</returns>
    Task<IEnumerable<UserPreferences>> GetSystemDefaultsAsync(string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查偏好设置是否存在
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果偏好设置存在则返回true</returns>
    Task<bool> ExistsAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加新的偏好设置
    /// </summary>
    /// <param name="preference">用户偏好设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的偏好设置</returns>
    Task<UserPreferences> AddAsync(UserPreferences preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加偏好设置
    /// </summary>
    /// <param name="preferences">偏好设置列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的偏好设置列表</returns>
    Task<IEnumerable<UserPreferences>> AddRangeAsync(IEnumerable<UserPreferences> preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新偏好设置
    /// </summary>
    /// <param name="preference">用户偏好设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的偏好设置</returns>
    Task<UserPreferences> UpdateAsync(UserPreferences preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置或更新偏好设置（如果不存在则创建，存在则更新）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="value">偏好值</param>
    /// <param name="valueType">值类型</param>
    /// <param name="description">描述</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置的偏好设置</returns>
    Task<UserPreferences> SetPreferenceAsync(
        Guid userId,
        string category,
        string key,
        string value,
        string valueType = "String",
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除偏好设置
    /// </summary>
    /// <param name="preferenceId">偏好ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid preferenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的偏好设置数量</returns>
    Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户指定分类的所有偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的偏好设置数量</returns>
    Task<int> DeleteByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置用户偏好设置为系统默认值
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类（可选，如果为null则重置所有分类）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置的偏好设置数量</returns>
    Task<int> ResetToDefaultsAsync(Guid userId, string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取偏好设置的统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息（分类数量、总偏好数量等）</returns>
    Task<(int CategoryCount, int TotalPreferences, DateTime? LastUpdated)> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
}