using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户元数据仓储接口，定义用户元数据访问契约
/// </summary>
public interface IUserMetadataRepository
{
    /// <summary>
    /// 根据元数据ID获取用户元数据条目
    /// </summary>
    /// <param name="id">元数据ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户元数据条目，如果不存在则返回null</returns>
    Task<UserMetadataEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID获取所有元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户的所有元数据条目</returns>
    Task<IEnumerable<UserMetadataEntry>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和分类获取元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">元数据分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的元数据条目列表</returns>
    Task<IEnumerable<UserMetadataEntry>> GetByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和键获取特定元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>特定的元数据条目，如果不存在则返回null</returns>
    Task<UserMetadataEntry?> GetByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID、分类和键获取特定元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">元数据分类</param>
    /// <param name="key">元数据键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>特定的元数据条目，如果不存在则返回null</returns>
    Task<UserMetadataEntry?> GetByCategoryAndKeyAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查元数据条目是否存在
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果元数据条目存在则返回true</returns>
    Task<bool> ExistsAsync(Guid userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加新的元数据条目
    /// </summary>
    /// <param name="metadataEntry">用户元数据条目</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的元数据条目</returns>
    Task<UserMetadataEntry> AddAsync(UserMetadataEntry metadataEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加元数据条目
    /// </summary>
    /// <param name="metadataEntries">元数据条目列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的元数据条目列表</returns>
    Task<IEnumerable<UserMetadataEntry>> AddRangeAsync(IEnumerable<UserMetadataEntry> metadataEntries, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新元数据条目
    /// </summary>
    /// <param name="metadataEntry">用户元数据条目</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的元数据条目</returns>
    Task<UserMetadataEntry> UpdateAsync(UserMetadataEntry metadataEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置或更新元数据条目（如果不存在则创建，存在则更新）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="value">元数据值</param>
    /// <param name="category">元数据分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置的元数据条目</returns>
    Task<UserMetadataEntry> SetMetadataAsync(
        Guid userId,
        string key,
        object value,
        string category = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取元数据值（泛型版本）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>元数据值，如果不存在则返回默认值</returns>
    Task<T?> GetValueAsync<T>(Guid userId, string key, T? defaultValue = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置元数据值（泛型版本）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="value">元数据值</param>
    /// <param name="category">元数据分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置的元数据条目</returns>
    Task<UserMetadataEntry> SetValueAsync<T>(
        Guid userId,
        string key,
        T value,
        string category = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除元数据条目
    /// </summary>
    /// <param name="id">元数据ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和键删除元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="key">元数据键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的元数据条目数量</returns>
    Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户指定分类的所有元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">元数据分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的元数据条目数量</returns>
    Task<int> DeleteByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取元数据的统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息（分类数量、总元数据数量等）</returns>
    Task<(int CategoryCount, int TotalEntries, DateTime? LastUpdated)> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索元数据条目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="searchTerm">搜索词（在键和值中搜索）</param>
    /// <param name="category">分类过滤（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的元数据条目列表</returns>
    Task<IEnumerable<UserMetadataEntry>> SearchAsync(
        Guid userId,
        string searchTerm,
        string? category = null,
        CancellationToken cancellationToken = default);
}