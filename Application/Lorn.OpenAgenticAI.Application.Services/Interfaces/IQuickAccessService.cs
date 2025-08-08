using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

public interface IQuickAccessService
{
    /// <summary>
    /// 获取用户的快速访问面板配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>快速访问面板配置</returns>
    Task<QuickAccessPanelDto> GetQuickAccessPanelAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新快速访问面板配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuickAccessPanelAsync(Guid userId, UpdateQuickAccessPanelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加项目到快速访问面板
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">添加请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加结果</returns>
    Task<AddQuickAccessItemResult> AddQuickAccessItemAsync(Guid userId, AddQuickAccessItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从快速访问面板移除项目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">项目类型</param>
    /// <param name="itemId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>移除成功标识</returns>
    Task<bool> RemoveQuickAccessItemAsync(Guid userId, string itemType, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取推荐的快速访问项目
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐的快速访问项目</returns>
    Task<IEnumerable<QuickAccessItemDto>> GetRecommendedQuickAccessItemsAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置快速访问面板为默认配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置成功标识</returns>
    Task<bool> ResetQuickAccessPanelAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 快速访问面板DTO
/// </summary>
public record QuickAccessPanelDto(
    Guid UserId,
    bool IsEnabled,
    string Layout,
    int MaxItems,
    IEnumerable<QuickAccessItemDto> Items,
    DateTime LastUpdated);

/// <summary>
/// 快速访问项目DTO
/// </summary>
public record QuickAccessItemDto(
    string ItemType,
    string ItemId,
    string ItemName,
    string? IconPath,
    string? Description,
    int SortOrder,
    bool IsEnabled,
    DateTime AddedAt);

/// <summary>
/// 更新快速访问面板请求
/// </summary>
public record UpdateQuickAccessPanelRequest(
    bool? IsEnabled = null,
    string? Layout = null,
    int? MaxItems = null);

/// <summary>
/// 添加快速访问项目请求
/// </summary>
public record AddQuickAccessItemRequest(
    string ItemType,
    string ItemId,
    string ItemName,
    string? IconPath = null,
    string? Description = null,
    int SortOrder = 0);

/// <summary>
/// 添加快速访问项目结果
/// </summary>
public record AddQuickAccessItemResult(
    bool Success,
    string? ErrorMessage);