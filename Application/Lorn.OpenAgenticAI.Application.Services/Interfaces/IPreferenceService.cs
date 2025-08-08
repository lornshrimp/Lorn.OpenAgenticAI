using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 偏好设置服务接口，提供类型安全的个性化配置管理
/// </summary>
public interface IPreferenceService
{
    /// <summary>
    /// 获取用户偏好设置，支持类型安全和默认值
    /// </summary>
    /// <typeparam name="T">偏好值类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>偏好值</returns>
    Task<T> GetPreferenceAsync<T>(Guid userId, string category, string key, T defaultValue = default!, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置用户偏好设置，支持类型安全
    /// </summary>
    /// <typeparam name="T">偏好值类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="value">偏好值</param>
    /// <param name="description">偏好描述（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置成功标识</returns>
    Task<bool> SetPreferenceAsync<T>(Guid userId, string category, string key, T value, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定分类的所有偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>偏好设置字典</returns>
    Task<Dictionary<string, object?>> GetCategoryPreferencesAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户所有偏好设置，按分类组织
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类化的偏好设置</returns>
    Task<Dictionary<string, Dictionary<string, object?>>> GetAllPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置指定分类的偏好设置为默认值
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置的偏好设置数量</returns>
    Task<int> ResetCategoryPreferencesAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置用户所有偏好设置为默认值
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置的偏好设置数量</returns>
    Task<int> ResetAllPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量设置偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="preferences">偏好设置字典（分类->键值对）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置成功的偏好数量</returns>
    Task<int> SetPreferencesBatchAsync(Guid userId, Dictionary<string, Dictionary<string, object>> preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">偏好分类</param>
    /// <param name="key">偏好键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeletePreferenceAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取偏好设置统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<PreferenceStatistics> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出用户偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="includeSystemDefaults">是否包含系统默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出的配置数据</returns>
    Task<PreferenceExportData> ExportPreferencesAsync(Guid userId, bool includeSystemDefaults = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导入用户偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="importData">导入的配置数据</param>
    /// <param name="overwriteExisting">是否覆盖现有配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导入的偏好设置数量</returns>
    Task<int> ImportPreferencesAsync(Guid userId, PreferenceExportData importData, bool overwriteExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 事件：偏好设置变更通知
    /// </summary>
    event EventHandler<PreferenceChangedEventArgs>? PreferenceChanged;
}

/// <summary>
/// 偏好设置统计信息
/// </summary>
public record PreferenceStatistics(
    int CategoryCount,
    int TotalPreferences,
    DateTime? LastUpdated,
    Dictionary<string, int> PreferencesByCategory
);

/// <summary>
/// 偏好设置导出数据
/// </summary>
public class PreferenceExportData
{
    public string UserId { get; set; } = string.Empty;
    public DateTime ExportTime { get; set; }
    public string Version { get; set; } = "1.0";
    public Dictionary<string, Dictionary<string, PreferenceExportItem>> Preferences { get; set; } = new();
}

/// <summary>
/// 偏好设置导出项
/// </summary>
public class PreferenceExportItem
{
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 偏好设置变更事件参数
/// </summary>
public class PreferenceChangedEventArgs : EventArgs
{
    public Guid UserId { get; }
    public string Category { get; }
    public string Key { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public PreferenceChangeType ChangeType { get; }

    public PreferenceChangedEventArgs(Guid userId, string category, string key, object? oldValue, object? newValue, PreferenceChangeType changeType)
    {
        UserId = userId;
        Category = category;
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        ChangeType = changeType;
    }
}

/// <summary>
/// 偏好设置变更类型
/// </summary>
public enum PreferenceChangeType
{
    /// <summary>创建</summary>
    Created,
    /// <summary>更新</summary>
    Updated,
    /// <summary>删除</summary>
    Deleted,
    /// <summary>重置</summary>
    Reset
}
