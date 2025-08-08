using Lorn.OpenAgenticAI.Application.Services.Constants;
using Lorn.OpenAgenticAI.Application.Services.Extensions;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 综合偏好设置管理服务，整合偏好设置CRUD和实时应用功能
/// </summary>
public class PreferenceManagementService : IDisposable
{
    private readonly IPreferenceService _preferenceService;
    private readonly IPreferenceNotificationService _notificationService;
    private readonly IPreferenceApplyService _applyService;
    private readonly ILogger<PreferenceManagementService> _logger;
    private bool _disposed = false;

    public PreferenceManagementService(
        IPreferenceService preferenceService,
        IPreferenceNotificationService notificationService,
        IPreferenceApplyService applyService,
        ILogger<PreferenceManagementService> logger)
    {
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _applyService = applyService ?? throw new ArgumentNullException(nameof(applyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeService();
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    private void InitializeService()
    {
        // 订阅偏好设置变更事件
        _preferenceService.PreferenceChanged += OnPreferenceChanged;

        // 注册分类特定的变更处理器
        _notificationService.Subscribe(PreferenceConstants.UI.CATEGORY, HandleUIPreferenceChange);
        _notificationService.Subscribe(PreferenceConstants.Language.CATEGORY, HandleLanguagePreferenceChange);
        _notificationService.Subscribe(PreferenceConstants.Operation.CATEGORY, HandleOperationPreferenceChange);
        _notificationService.Subscribe(PreferenceConstants.Shortcuts.CATEGORY, HandleShortcutPreferenceChange);

        _logger.LogInformation("PreferenceManagementService initialized successfully");
    }

    /// <summary>
    /// 初始化用户默认偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="forceReset">是否强制重置为默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化的偏好设置数量</returns>
    public async Task<int> InitializeUserPreferencesAsync(Guid userId, bool forceReset = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing preferences for user {UserId}, forceReset: {ForceReset}", userId, forceReset);

            if (forceReset)
            {
                // 重置所有偏好设置为默认值
                await _preferenceService.ResetAllPreferencesAsync(userId, cancellationToken);
            }

            // 使用扩展方法初始化默认偏好设置
            var initCount = await _preferenceService.InitializeDefaultPreferencesAsync(userId, cancellationToken);

            _logger.LogInformation("Successfully initialized {Count} preferences for user {UserId}", initCount, userId);
            return initCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing preferences for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// 获取用户的完整偏好设置配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整的偏好设置配置</returns>
    public async Task<UserPreferenceProfile> GetUserPreferenceProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting preference profile for user {UserId}", userId);

            var allPreferences = await _preferenceService.GetAllPreferencesAsync(userId, cancellationToken);
            var statistics = await _preferenceService.GetStatisticsAsync(userId, cancellationToken);

            var profile = new UserPreferenceProfile
            {
                UserId = userId,
                LastUpdated = statistics.LastUpdated ?? DateTime.UtcNow,
                Categories = allPreferences.ToDictionary(
                    kv => kv.Key,
                    kv => new PreferenceCategoryInfo
                    {
                        Name = kv.Key,
                        Preferences = kv.Value,
                        Count = kv.Value.Count
                    }),
                Statistics = statistics
            };

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preference profile for user {UserId}", userId);
            return new UserPreferenceProfile { UserId = userId, LastUpdated = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// 批量更新偏好设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="updates">偏好设置更新</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    public async Task<PreferenceUpdateResult> UpdatePreferencesAsync(Guid userId, Dictionary<string, Dictionary<string, object>> updates, CancellationToken cancellationToken = default)
    {
        var result = new PreferenceUpdateResult();

        try
        {
            _logger.LogInformation("Updating preferences for user {UserId}, {CategoryCount} categories", userId, updates.Count);

            foreach (var category in updates)
            {
                foreach (var preference in category.Value)
                {
                    try
                    {
                        var success = await _preferenceService.SetPreferenceAsync(userId, category.Key, preference.Key, preference.Value, null, cancellationToken);

                        if (success)
                        {
                            result.SuccessfulUpdates.Add(new PreferenceUpdate
                            {
                                Category = category.Key,
                                Key = preference.Key,
                                Value = preference.Value
                            });
                        }
                        else
                        {
                            result.FailedUpdates.Add(new PreferenceUpdate
                            {
                                Category = category.Key,
                                Key = preference.Key,
                                Value = preference.Value,
                                Error = "Update failed"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedUpdates.Add(new PreferenceUpdate
                        {
                            Category = category.Key,
                            Key = preference.Key,
                            Value = preference.Value,
                            Error = ex.Message
                        });
                    }
                }
            }

            _logger.LogInformation("Preference update completed for user {UserId}: {SuccessCount} successful, {FailCount} failed",
                userId, result.SuccessfulUpdates.Count, result.FailedUpdates.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            result.FailedUpdates.Add(new PreferenceUpdate { Error = ex.Message });
            return result;
        }
    }

    /// <summary>
    /// 验证偏好设置值
    /// </summary>
    /// <param name="category">分类</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns>验证结果</returns>
    public PreferenceValidationResult ValidatePreference(string category, string key, object value)
    {
        try
        {
            switch (category)
            {
                case PreferenceConstants.UI.CATEGORY:
                    return ValidateUIPreference(key, value);

                case PreferenceConstants.Language.CATEGORY:
                    return ValidateLanguagePreference(key, value);

                case PreferenceConstants.Operation.CATEGORY:
                    return ValidateOperationPreference(key, value);

                case PreferenceConstants.Shortcuts.CATEGORY:
                    return ValidateShortcutPreference(key, value);

                default:
                    return new PreferenceValidationResult { IsValid = true };
            }
        }
        catch (Exception ex)
        {
            return new PreferenceValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #region 事件处理器

    /// <summary>
    /// 处理偏好设置变更事件
    /// </summary>
    private async void OnPreferenceChanged(object? sender, PreferenceChangedEventArgs e)
    {
        try
        {
            await _notificationService.NotifyPreferenceChangedAsync(e);

            // 检查是否需要重启
            if (_applyService.RequiresRestart(e))
            {
                _logger.LogWarning("Preference change requires application restart: {Category}.{Key}", e.Category, e.Key);
                // 这里可以触发重启提示事件
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling preference change for user {UserId}, category: {Category}, key: {Key}",
                e.UserId, e.Category, e.Key);
        }
    }

    /// <summary>
    /// 处理UI偏好设置变更
    /// </summary>
    private async Task HandleUIPreferenceChange(PreferenceChangedEventArgs e)
    {
        await _applyService.ApplyUIPreferenceAsync(e);
    }

    /// <summary>
    /// 处理语言偏好设置变更
    /// </summary>
    private async Task HandleLanguagePreferenceChange(PreferenceChangedEventArgs e)
    {
        await _applyService.ApplyLanguagePreferenceAsync(e);
    }

    /// <summary>
    /// 处理操作偏好设置变更
    /// </summary>
    private async Task HandleOperationPreferenceChange(PreferenceChangedEventArgs e)
    {
        await _applyService.ApplyOperationPreferenceAsync(e);
    }

    /// <summary>
    /// 处理快捷键偏好设置变更
    /// </summary>
    private async Task HandleShortcutPreferenceChange(PreferenceChangedEventArgs e)
    {
        await _applyService.ApplyShortcutPreferenceAsync(e);
    }

    #endregion

    #region 验证方法

    private PreferenceValidationResult ValidateUIPreference(string key, object value)
    {
        switch (key)
        {
            case PreferenceConstants.UI.THEME:
                var theme = value?.ToString();
                if (theme != null && !PreferenceConstants.UI.Options.THEMES.Contains(theme))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Invalid theme: {theme}. Valid options: {string.Join(", ", PreferenceConstants.UI.Options.THEMES)}"
                    };
                }
                break;

            case PreferenceConstants.UI.FONT_SIZE:
                if (int.TryParse(value?.ToString(), out var fontSize) && !PreferenceConstants.UI.Options.FONT_SIZES.Contains(fontSize))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Invalid font size: {fontSize}. Valid options: {string.Join(", ", PreferenceConstants.UI.Options.FONT_SIZES)}"
                    };
                }
                break;

            case PreferenceConstants.UI.LAYOUT:
                var layout = value?.ToString();
                if (layout != null && !PreferenceConstants.UI.Options.LAYOUTS.Contains(layout))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Invalid layout: {layout}. Valid options: {string.Join(", ", PreferenceConstants.UI.Options.LAYOUTS)}"
                    };
                }
                break;
        }

        return new PreferenceValidationResult { IsValid = true };
    }

    private PreferenceValidationResult ValidateLanguagePreference(string key, object value)
    {
        switch (key)
        {
            case PreferenceConstants.Language.UI_LANGUAGE:
            case PreferenceConstants.Language.INPUT_LANGUAGE:
            case PreferenceConstants.Language.OUTPUT_LANGUAGE:
                var language = value?.ToString();
                if (language != null && !PreferenceConstants.Language.Options.SUPPORTED_LANGUAGES.Contains(language))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Unsupported language: {language}. Supported languages: {string.Join(", ", PreferenceConstants.Language.Options.SUPPORTED_LANGUAGES)}"
                    };
                }
                break;
        }

        return new PreferenceValidationResult { IsValid = true };
    }

    private PreferenceValidationResult ValidateOperationPreference(string key, object value)
    {
        switch (key)
        {
            case PreferenceConstants.Operation.DEFAULT_LLM_MODEL:
                var model = value?.ToString();
                if (model != null && !PreferenceConstants.Operation.Options.LLM_MODELS.Contains(model))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Unsupported LLM model: {model}. Supported models: {string.Join(", ", PreferenceConstants.Operation.Options.LLM_MODELS)}"
                    };
                }
                break;

            case PreferenceConstants.Operation.TASK_TIMEOUT:
                if (int.TryParse(value?.ToString(), out var timeout) && !PreferenceConstants.Operation.Options.TIMEOUT_OPTIONS.Contains(timeout))
                {
                    return new PreferenceValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Invalid timeout: {timeout}. Valid options: {string.Join(", ", PreferenceConstants.Operation.Options.TIMEOUT_OPTIONS)}"
                    };
                }
                break;
        }

        return new PreferenceValidationResult { IsValid = true };
    }

    private PreferenceValidationResult ValidateShortcutPreference(string key, object value)
    {
        // 快捷键验证逻辑可以在这里实现
        // 例如检查快捷键格式、冲突等
        return new PreferenceValidationResult { IsValid = true };
    }

    #endregion

    #region IDisposable实现

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // 取消事件订阅
            if (_preferenceService != null)
            {
                _preferenceService.PreferenceChanged -= OnPreferenceChanged;
            }

            // 取消通知服务订阅
            _notificationService?.Unsubscribe(PreferenceConstants.UI.CATEGORY, HandleUIPreferenceChange);
            _notificationService?.Unsubscribe(PreferenceConstants.Language.CATEGORY, HandleLanguagePreferenceChange);
            _notificationService?.Unsubscribe(PreferenceConstants.Operation.CATEGORY, HandleOperationPreferenceChange);
            _notificationService?.Unsubscribe(PreferenceConstants.Shortcuts.CATEGORY, HandleShortcutPreferenceChange);

            _disposed = true;
            _logger.LogInformation("PreferenceManagementService disposed successfully");
        }
    }

    #endregion
}

#region 辅助类型

/// <summary>
/// 用户偏好设置配置文件
/// </summary>
public class UserPreferenceProfile
{
    public Guid UserId { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, PreferenceCategoryInfo> Categories { get; set; } = new();
    public PreferenceStatistics Statistics { get; set; } = new(0, 0, null, new Dictionary<string, int>());
}

/// <summary>
/// 偏好设置分类信息
/// </summary>
public class PreferenceCategoryInfo
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Preferences { get; set; } = new();
    public int Count { get; set; }
}

/// <summary>
/// 偏好设置更新结果
/// </summary>
public class PreferenceUpdateResult
{
    public List<PreferenceUpdate> SuccessfulUpdates { get; set; } = new();
    public List<PreferenceUpdate> FailedUpdates { get; set; } = new();
    public bool HasErrors => FailedUpdates.Any();
    public int TotalUpdates => SuccessfulUpdates.Count + FailedUpdates.Count;
}

/// <summary>
/// 偏好设置更新
/// </summary>
public class PreferenceUpdate
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 偏好设置验证结果
/// </summary>
public class PreferenceValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
