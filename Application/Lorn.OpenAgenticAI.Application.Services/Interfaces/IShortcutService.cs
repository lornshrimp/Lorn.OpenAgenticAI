using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 快捷键服务接口，管理用户快捷键配置
/// </summary>
public interface IShortcutService
{
    /// <summary>
    /// 获取用户的所有快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>快捷键列表</returns>
    Task<IEnumerable<ShortcutDto>> GetUserShortcutsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户启用的快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启用的快捷键列表</returns>
    Task<IEnumerable<ShortcutDto>> GetEnabledShortcutsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据分类获取快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定分类的快捷键列表</returns>
    Task<IEnumerable<ShortcutDto>> GetShortcutsByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>快捷键详情</returns>
    Task<ShortcutDto?> GetShortcutByIdAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新的快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">创建快捷键请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    Task<CreateShortcutResult> CreateShortcutAsync(Guid userId, CreateShortcutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="request">更新快捷键请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<UpdateShortcutResult> UpdateShortcutAsync(Guid shortcutId, UpdateShortcutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功标识</returns>
    Task<bool> DeleteShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作成功标识</returns>
    Task<bool> EnableShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用快捷键
    /// </summary>
    /// <param name="shortcutId">快捷键ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作成功标识</returns>
    Task<bool> DisableShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查按键组合是否冲突
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="keyCombination">按键组合</param>
    /// <param name="excludeShortcutId">排除的快捷键ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>冲突检查结果</returns>
    Task<KeyCombinationConflictResult> CheckKeyCombinationConflictAsync(Guid userId, string keyCombination, Guid? excludeShortcutId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取推荐的快捷键组合
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐的快捷键组合列表</returns>
    Task<IEnumerable<string>> GetRecommendedKeyCombinationsAsync(Guid userId, string actionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">搜索请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<IEnumerable<ShortcutDto>> SearchShortcutsAsync(Guid userId, SearchShortcutsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新快捷键排序
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="sortOrderUpdates">排序更新列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功标识</returns>
    Task<bool> UpdateShortcutSortOrdersAsync(Guid userId, IEnumerable<ShortcutSortOrderUpdate> sortOrderUpdates, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行快捷键动作
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="keyCombination">按键组合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<ShortcutExecutionResult> ExecuteShortcutAsync(Guid userId, string keyCombination, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户快捷键分类
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类列表</returns>
    Task<IEnumerable<string>> GetShortcutCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最常用的快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最常用的快捷键列表</returns>
    Task<IEnumerable<ShortcutDto>> GetMostUsedShortcutsAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出用户快捷键配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出的快捷键配置</returns>
    Task<ShortcutConfigurationExport> ExportShortcutConfigurationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导入用户快捷键配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="configurationData">配置数据</param>
    /// <param name="mergeMode">合并模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导入结果</returns>
    Task<ImportShortcutConfigurationResult> ImportShortcutConfigurationAsync(Guid userId, ShortcutConfigurationExport configurationData, ImportMergeMode mergeMode = ImportMergeMode.Merge, CancellationToken cancellationToken = default);
}

/// <summary>
/// 快捷键DTO
/// </summary>
public record ShortcutDto(
    Guid Id,
    string Name,
    string KeyCombination,
    string ActionType,
    string ActionData,
    string? Description,
    string Category,
    bool IsEnabled,
    bool IsGlobal,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    int UsageCount,
    int SortOrder);

/// <summary>
/// 创建快捷键请求
/// </summary>
public record CreateShortcutRequest(
    string Name,
    string KeyCombination,
    string ActionType,
    string ActionData = "",
    string? Description = null,
    string Category = "",
    bool IsGlobal = false,
    int SortOrder = 0);

/// <summary>
/// 更新快捷键请求
/// </summary>
public record UpdateShortcutRequest(
    string? Name = null,
    string? KeyCombination = null,
    string? ActionType = null,
    string? ActionData = null,
    string? Description = null,
    string? Category = null,
    bool? IsGlobal = null,
    int? SortOrder = null);

/// <summary>
/// 创建快捷键结果
/// </summary>
public record CreateShortcutResult(
    bool Success,
    Guid? ShortcutId,
    string? ErrorMessage,
    KeyCombinationConflictResult? ConflictInfo = null);

/// <summary>
/// 更新快捷键结果
/// </summary>
public record UpdateShortcutResult(
    bool Success,
    string? ErrorMessage,
    KeyCombinationConflictResult? ConflictInfo = null);

/// <summary>
/// 按键组合冲突结果
/// </summary>
public record KeyCombinationConflictResult(
    bool HasConflict,
    ShortcutDto? ConflictingShortcut,
    IEnumerable<string> SuggestedAlternatives);

/// <summary>
/// 搜索快捷键请求
/// </summary>
public record SearchShortcutsRequest(
    string SearchTerm,
    string? Category = null,
    string? ActionType = null,
    bool? IsEnabled = null);

/// <summary>
/// 快捷键排序更新
/// </summary>
public record ShortcutSortOrderUpdate(Guid ShortcutId, int SortOrder);

/// <summary>
/// 快捷键执行结果
/// </summary>
public record ShortcutExecutionResult(
    bool Success,
    string? ErrorMessage,
    object? ExecutionData = null);

/// <summary>
/// 快捷键配置导出
/// </summary>
public record ShortcutConfigurationExport(
    Guid UserId,
    DateTime ExportedAt,
    IEnumerable<ShortcutDto> Shortcuts);

/// <summary>
/// 导入快捷键配置结果
/// </summary>
public record ImportShortcutConfigurationResult(
    bool Success,
    int ImportedCount,
    int SkippedCount,
    int ErrorCount,
    IEnumerable<string> Errors);

/// <summary>
/// 导入合并模式
/// </summary>
public enum ImportMergeMode
{
    /// <summary>
    /// 合并模式，保留现有快捷键，添加新的
    /// </summary>
    Merge,

    /// <summary>
    /// 替换模式，先删除所有现有快捷键，再导入新的
    /// </summary>
    Replace,

    /// <summary>
    /// 跳过冲突模式，只导入不冲突的快捷键
    /// </summary>
    SkipConflicts
}
