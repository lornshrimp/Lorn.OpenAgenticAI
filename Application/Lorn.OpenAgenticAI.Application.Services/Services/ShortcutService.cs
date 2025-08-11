using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 快捷键服务实现，管理用户快捷键配置
/// </summary>
public class ShortcutService : IShortcutService
{
    private readonly IUserShortcutRepository _shortcutRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ShortcutService> _logger;

    public ShortcutService(
        IUserShortcutRepository shortcutRepository,
        IUserRepository userRepository,
        ILogger<ShortcutService> logger)
    {
        _shortcutRepository = shortcutRepository ?? throw new ArgumentNullException(nameof(shortcutRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ShortcutDto>> GetUserShortcutsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.GetByUserIdAsync(userId, cancellationToken);
            return shortcuts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user shortcuts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ShortcutDto>> GetEnabledShortcutsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.GetEnabledByUserIdAsync(userId, cancellationToken);
            return shortcuts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enabled shortcuts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ShortcutDto>> GetShortcutsByCategoryAsync(Guid userId, string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.GetByCategoryAsync(userId, category, cancellationToken);
            return shortcuts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shortcuts by category {Category} for user {UserId}", category, userId);
            throw;
        }
    }

    public async Task<ShortcutDto?> GetShortcutByIdAsync(Guid shortcutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcut = await _shortcutRepository.GetByIdAsync(shortcutId, cancellationToken);
            return shortcut != null ? MapToDto(shortcut) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shortcut {ShortcutId}", shortcutId);
            throw;
        }
    }

    public async Task<CreateShortcutResult> CreateShortcutAsync(Guid userId, CreateShortcutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证用户存在
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new CreateShortcutResult(false, null, "User not found");
            }

            // 检查按键组合冲突
            var conflictResult = await CheckKeyCombinationConflictAsync(userId, request.KeyCombination, null, cancellationToken);
            if (conflictResult.HasConflict)
            {
                // 确保建议列表已生成（若为空再尝试生成）
                if (conflictResult.SuggestedAlternatives == null || !conflictResult.SuggestedAlternatives.Any())
                {
                    var suggestions = await GenerateKeyCombinationSuggestions(userId, request.KeyCombination, cancellationToken);
                    conflictResult = new KeyCombinationConflictResult(true, conflictResult.ConflictingShortcut, suggestions);
                }
                return new CreateShortcutResult(false, null, "Key combination conflicts with existing shortcut", conflictResult);
            }

            // 创建快捷键
            var shortcut = new UserShortcut(
                userId,
                request.Name,
                request.KeyCombination,
                request.ActionType,
                request.ActionData,
                request.Description,
                request.Category,
                request.IsGlobal,
                request.SortOrder);

            var success = await _shortcutRepository.AddAsync(shortcut, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Created shortcut {ShortcutName} for user {UserId}", request.Name, userId);
                return new CreateShortcutResult(true, shortcut.Id, null);
            }

            return new CreateShortcutResult(false, null, "Failed to create shortcut");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shortcut for user {UserId}", userId);
            return new CreateShortcutResult(false, null, $"Error creating shortcut: {ex.Message}");
        }
    }

    public async Task<UpdateShortcutResult> UpdateShortcutAsync(Guid shortcutId, UpdateShortcutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcut = await _shortcutRepository.GetByIdAsync(shortcutId, cancellationToken);
            if (shortcut == null)
            {
                return new UpdateShortcutResult(false, "Shortcut not found");
            }

            // 如果更新了按键组合，检查冲突
            KeyCombinationConflictResult? conflictResult = null;
            if (!string.IsNullOrWhiteSpace(request.KeyCombination) && request.KeyCombination != shortcut.KeyCombination)
            {
                conflictResult = await CheckKeyCombinationConflictAsync(shortcut.UserId, request.KeyCombination, shortcutId, cancellationToken);
                if (conflictResult.HasConflict)
                {
                    return new UpdateShortcutResult(false, "Key combination conflicts with existing shortcut", conflictResult);
                }
            }

            // 更新快捷键
            shortcut.UpdateShortcut(
                request.Name,
                request.KeyCombination,
                request.ActionType,
                request.ActionData,
                request.Description,
                request.Category,
                request.IsGlobal);

            if (request.SortOrder.HasValue)
            {
                shortcut.UpdateSortOrder(request.SortOrder.Value);
            }

            var success = await _shortcutRepository.UpdateAsync(shortcut, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Updated shortcut {ShortcutId}", shortcutId);
                return new UpdateShortcutResult(true, null);
            }

            return new UpdateShortcutResult(false, "Failed to update shortcut");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shortcut {ShortcutId}", shortcutId);
            return new UpdateShortcutResult(false, $"Error updating shortcut: {ex.Message}");
        }
    }

    public async Task<bool> DeleteShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _shortcutRepository.DeleteAsync(shortcutId, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Deleted shortcut {ShortcutId}", shortcutId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shortcut {ShortcutId}", shortcutId);
            return false;
        }
    }

    public async Task<bool> EnableShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcut = await _shortcutRepository.GetByIdAsync(shortcutId, cancellationToken);
            if (shortcut == null)
            {
                return false;
            }

            shortcut.Enable();
            var success = await _shortcutRepository.UpdateAsync(shortcut, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Enabled shortcut {ShortcutId}", shortcutId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable shortcut {ShortcutId}", shortcutId);
            return false;
        }
    }

    public async Task<bool> DisableShortcutAsync(Guid shortcutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcut = await _shortcutRepository.GetByIdAsync(shortcutId, cancellationToken);
            if (shortcut == null)
            {
                return false;
            }

            shortcut.Disable();
            var success = await _shortcutRepository.UpdateAsync(shortcut, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Disabled shortcut {ShortcutId}", shortcutId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable shortcut {ShortcutId}", shortcutId);
            return false;
        }
    }

    public async Task<KeyCombinationConflictResult> CheckKeyCombinationConflictAsync(Guid userId, string keyCombination, Guid? excludeShortcutId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var conflictingShortcut = await _shortcutRepository.CheckKeyCombinationConflictAsync(userId, keyCombination, excludeShortcutId, cancellationToken);

            if (conflictingShortcut != null)
            {
                var suggestions = await GenerateKeyCombinationSuggestions(userId, keyCombination, cancellationToken);
                return new KeyCombinationConflictResult(true, MapToDto(conflictingShortcut), suggestions);
            }

            return new KeyCombinationConflictResult(false, null, Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check key combination conflict for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetRecommendedKeyCombinationsAsync(Guid userId, string actionType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _shortcutRepository.GetRecommendedKeyCombinationsAsync(userId, actionType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommended key combinations for user {UserId} and action type {ActionType}", userId, actionType);
            throw;
        }
    }

    public async Task<IEnumerable<ShortcutDto>> SearchShortcutsAsync(Guid userId, SearchShortcutsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.SearchShortcutsAsync(
                userId,
                request.SearchTerm,
                request.Category,
                request.ActionType,
                request.IsEnabled,
                cancellationToken);

            return shortcuts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search shortcuts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateShortcutSortOrdersAsync(Guid userId, IEnumerable<ShortcutSortOrderUpdate> sortOrderUpdates, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateDict = sortOrderUpdates.ToDictionary(x => x.ShortcutId, x => x.SortOrder);
            var success = await _shortcutRepository.UpdateSortOrdersAsync(userId, updateDict, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Updated sort orders for {Count} shortcuts for user {UserId}", updateDict.Count, userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shortcut sort orders for user {UserId}", userId);
            return false;
        }
    }

    public async Task<ShortcutExecutionResult> ExecuteShortcutAsync(Guid userId, string keyCombination, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcut = await _shortcutRepository.GetByKeyCombinationAsync(userId, keyCombination, cancellationToken);
            if (shortcut == null)
            {
                return new ShortcutExecutionResult(false, "Shortcut not found");
            }

            if (!shortcut.IsEnabled)
            {
                return new ShortcutExecutionResult(false, "Shortcut is disabled");
            }

            // 记录使用
            shortcut.RecordUsage();
            await _shortcutRepository.UpdateAsync(shortcut, cancellationToken);

            // 执行快捷键动作 - 这里只是示例，实际执行逻辑需要根据具体的动作类型实现
            var executionData = await ExecuteShortcutAction(shortcut, cancellationToken);

            _logger.LogInformation("Executed shortcut {ShortcutName} for user {UserId}", shortcut.Name, userId);
            return new ShortcutExecutionResult(true, null, executionData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute shortcut with key combination {KeyCombination} for user {UserId}", keyCombination, userId);
            return new ShortcutExecutionResult(false, $"Error executing shortcut: {ex.Message}");
        }
    }

    public async Task<IEnumerable<string>> GetShortcutCategoriesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _shortcutRepository.GetCategoriesAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shortcut categories for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ShortcutDto>> GetMostUsedShortcutsAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.GetMostUsedAsync(userId, count, cancellationToken);
            return shortcuts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most used shortcuts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ShortcutConfigurationExport> ExportShortcutConfigurationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcuts = await _shortcutRepository.GetByUserIdAsync(userId, cancellationToken);
            var shortcutDtos = shortcuts.Select(MapToDto);

            return new ShortcutConfigurationExport(userId, DateTime.UtcNow, shortcutDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export shortcut configuration for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ImportShortcutConfigurationResult> ImportShortcutConfigurationAsync(Guid userId, ShortcutConfigurationExport configurationData, ImportMergeMode mergeMode = ImportMergeMode.Merge, CancellationToken cancellationToken = default)
    {
        try
        {
            var errors = new List<string>();
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            // 如果是替换模式，先删除所有现有快捷键
            if (mergeMode == ImportMergeMode.Replace)
            {
                await _shortcutRepository.DeleteByUserIdAsync(userId, cancellationToken);
            }

            foreach (var shortcutDto in configurationData.Shortcuts)
            {
                try
                {
                    // SkipConflicts 模式：仅做一次直接仓储冲突检查（测试中已对仓储方法进行 Mock）
                    if (mergeMode == ImportMergeMode.SkipConflicts)
                    {
                        var conflict = await _shortcutRepository.CheckKeyCombinationConflictAsync(userId, shortcutDto.KeyCombination, null, cancellationToken);
                        if (conflict != null)
                        {
                            skippedCount++;
                            continue; // 不导入冲突项
                        }
                    }

                    var shortcut = new UserShortcut(
                        userId,
                        shortcutDto.Name,
                        shortcutDto.KeyCombination,
                        shortcutDto.ActionType,
                        shortcutDto.ActionData,
                        shortcutDto.Description,
                        shortcutDto.Category,
                        shortcutDto.IsGlobal,
                        shortcutDto.SortOrder);

                    var success = await _shortcutRepository.AddAsync(shortcut, cancellationToken);
                    if (success)
                    {
                        importedCount++;
                    }
                    else
                    {
                        errorCount++;
                        errors.Add($"Failed to import shortcut: {shortcutDto.Name}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"Error importing shortcut {shortcutDto.Name}: {ex.Message}");
                }
            }

            _logger.LogInformation("Imported {ImportedCount} shortcuts for user {UserId}, skipped {SkippedCount}, errors {ErrorCount}",
                importedCount, userId, skippedCount, errorCount);

            return new ImportShortcutConfigurationResult(
                errorCount == 0,
                importedCount,
                skippedCount,
                errorCount,
                errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shortcut configuration for user {UserId}", userId);
            throw;
        }
    }

    #region Private Methods

    private static ShortcutDto MapToDto(UserShortcut shortcut)
    {
        return new ShortcutDto(
            shortcut.Id,
            shortcut.Name,
            shortcut.KeyCombination,
            shortcut.ActionType,
            shortcut.ActionData,
            shortcut.Description,
            shortcut.Category,
            shortcut.IsEnabled,
            shortcut.IsGlobal,
            shortcut.CreatedAt,
            shortcut.LastUsedAt,
            shortcut.UsageCount,
            shortcut.SortOrder);
    }

    private async Task<IEnumerable<string>> GenerateKeyCombinationSuggestions(Guid userId, string originalCombination, CancellationToken cancellationToken)
    {
        // 生成替代方案的逻辑
        var suggestions = new List<string>();
        var baseCombination = originalCombination;

        // 尝试添加不同的修饰键
        var modifiers = new[] { "Ctrl", "Alt", "Shift", "Win" };
        var keys = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        foreach (var modifier in modifiers)
        {
            foreach (var key in keys.Take(5)) // 只取前5个字母作为示例
            {
                var suggestion = $"{modifier}+{key}";
                var conflict = await _shortcutRepository.CheckKeyCombinationConflictAsync(userId, suggestion, null, cancellationToken);
                if (conflict == null)
                {
                    suggestions.Add(suggestion);
                    if (suggestions.Count >= 5) break; // 最多返回5个建议
                }
            }
            if (suggestions.Count >= 5) break;
        }

        return suggestions;
    }

    private async Task<object?> ExecuteShortcutAction(UserShortcut shortcut, CancellationToken cancellationToken)
    {
        // 这里应该根据不同的动作类型执行相应的操作
        // 这只是一个示例实现，实际应用中需要集成到具体的动作执行系统
        return await Task.FromResult<object?>(new { ActionType = shortcut.ActionType, ActionData = shortcut.ActionData });
    }

    #endregion
}
