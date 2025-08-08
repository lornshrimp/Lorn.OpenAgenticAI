using Lorn.OpenAgenticAI.Application.Services.Constants;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Application.Services.Extensions;

/// <summary>
/// 偏好设置扩展方法，提供强类型的偏好设置访问
/// </summary>
public static class PreferenceServiceExtensions
{
    /// <summary>
    /// 获取界面主题设置
    /// </summary>
    public static async Task<string> GetThemeAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.THEME, PreferenceConstants.UI.Defaults.THEME, cancellationToken);
    }

    /// <summary>
    /// 设置界面主题
    /// </summary>
    public static async Task<bool> SetThemeAsync(this IPreferenceService service, Guid userId, string theme, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.UI.Options.THEMES.Contains(theme))
        {
            throw new ArgumentException($"Invalid theme: {theme}. Valid themes: {string.Join(", ", PreferenceConstants.UI.Options.THEMES)}", nameof(theme));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.THEME, theme, "用户界面主题设置", cancellationToken);
    }

    /// <summary>
    /// 获取字体大小设置
    /// </summary>
    public static async Task<int> GetFontSizeAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.FONT_SIZE, PreferenceConstants.UI.Defaults.FONT_SIZE, cancellationToken);
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    public static async Task<bool> SetFontSizeAsync(this IPreferenceService service, Guid userId, int fontSize, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.UI.Options.FONT_SIZES.Contains(fontSize))
        {
            throw new ArgumentException($"Invalid font size: {fontSize}. Valid sizes: {string.Join(", ", PreferenceConstants.UI.Options.FONT_SIZES)}", nameof(fontSize));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.FONT_SIZE, fontSize, "界面字体大小设置", cancellationToken);
    }

    /// <summary>
    /// 获取布局设置
    /// </summary>
    public static async Task<string> GetLayoutAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.LAYOUT, PreferenceConstants.UI.Defaults.LAYOUT, cancellationToken);
    }

    /// <summary>
    /// 设置布局
    /// </summary>
    public static async Task<bool> SetLayoutAsync(this IPreferenceService service, Guid userId, string layout, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.UI.Options.LAYOUTS.Contains(layout))
        {
            throw new ArgumentException($"Invalid layout: {layout}. Valid layouts: {string.Join(", ", PreferenceConstants.UI.Options.LAYOUTS)}", nameof(layout));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.UI.CATEGORY, PreferenceConstants.UI.LAYOUT, layout, "界面布局设置", cancellationToken);
    }

    /// <summary>
    /// 获取界面语言设置
    /// </summary>
    public static async Task<string> GetUILanguageAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.Language.CATEGORY, PreferenceConstants.Language.UI_LANGUAGE, PreferenceConstants.Language.Defaults.UI_LANGUAGE, cancellationToken);
    }

    /// <summary>
    /// 设置界面语言
    /// </summary>
    public static async Task<bool> SetUILanguageAsync(this IPreferenceService service, Guid userId, string language, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.Language.Options.SUPPORTED_LANGUAGES.Contains(language))
        {
            throw new ArgumentException($"Unsupported language: {language}. Supported languages: {string.Join(", ", PreferenceConstants.Language.Options.SUPPORTED_LANGUAGES)}", nameof(language));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.Language.CATEGORY, PreferenceConstants.Language.UI_LANGUAGE, language, "界面显示语言设置", cancellationToken);
    }

    /// <summary>
    /// 获取默认LLM模型设置
    /// </summary>
    public static async Task<string> GetDefaultLLMModelAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.DEFAULT_LLM_MODEL, PreferenceConstants.Operation.Defaults.DEFAULT_LLM_MODEL, cancellationToken);
    }

    /// <summary>
    /// 设置默认LLM模型
    /// </summary>
    public static async Task<bool> SetDefaultLLMModelAsync(this IPreferenceService service, Guid userId, string model, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.Operation.Options.LLM_MODELS.Contains(model))
        {
            throw new ArgumentException($"Unsupported LLM model: {model}. Supported models: {string.Join(", ", PreferenceConstants.Operation.Options.LLM_MODELS)}", nameof(model));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.DEFAULT_LLM_MODEL, model, "默认大语言模型设置", cancellationToken);
    }

    /// <summary>
    /// 获取任务超时时间设置
    /// </summary>
    public static async Task<int> GetTaskTimeoutAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.TASK_TIMEOUT, PreferenceConstants.Operation.Defaults.TASK_TIMEOUT, cancellationToken);
    }

    /// <summary>
    /// 设置任务超时时间
    /// </summary>
    public static async Task<bool> SetTaskTimeoutAsync(this IPreferenceService service, Guid userId, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.Operation.Options.TIMEOUT_OPTIONS.Contains(timeoutSeconds))
        {
            throw new ArgumentException($"Invalid timeout: {timeoutSeconds}. Valid options: {string.Join(", ", PreferenceConstants.Operation.Options.TIMEOUT_OPTIONS)}", nameof(timeoutSeconds));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.TASK_TIMEOUT, timeoutSeconds, "任务执行超时时间设置", cancellationToken);
    }

    /// <summary>
    /// 获取自动保存启用状态
    /// </summary>
    public static async Task<bool> GetAutoSaveEnabledAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.ENABLE_AUTO_SAVE, PreferenceConstants.Operation.Defaults.ENABLE_AUTO_SAVE, cancellationToken);
    }

    /// <summary>
    /// 设置自动保存启用状态
    /// </summary>
    public static async Task<bool> SetAutoSaveEnabledAsync(this IPreferenceService service, Guid userId, bool enabled, CancellationToken cancellationToken = default)
    {
        return await service.SetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.ENABLE_AUTO_SAVE, enabled, "自动保存功能启用设置", cancellationToken);
    }

    /// <summary>
    /// 获取自动保存间隔
    /// </summary>
    public static async Task<int> GetAutoSaveIntervalAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        return await service.GetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.AUTO_SAVE_INTERVAL, PreferenceConstants.Operation.Defaults.AUTO_SAVE_INTERVAL, cancellationToken);
    }

    /// <summary>
    /// 设置自动保存间隔
    /// </summary>
    public static async Task<bool> SetAutoSaveIntervalAsync(this IPreferenceService service, Guid userId, int intervalSeconds, CancellationToken cancellationToken = default)
    {
        if (!PreferenceConstants.Operation.Options.SAVE_INTERVALS.Contains(intervalSeconds))
        {
            throw new ArgumentException($"Invalid interval: {intervalSeconds}. Valid options: {string.Join(", ", PreferenceConstants.Operation.Options.SAVE_INTERVALS)}", nameof(intervalSeconds));
        }

        return await service.SetPreferenceAsync(userId, PreferenceConstants.Operation.CATEGORY, PreferenceConstants.Operation.AUTO_SAVE_INTERVAL, intervalSeconds, "自动保存时间间隔设置", cancellationToken);
    }

    /// <summary>
    /// 获取快捷键设置
    /// </summary>
    public static async Task<string> GetShortcutAsync(this IPreferenceService service, Guid userId, string shortcutKey, CancellationToken cancellationToken = default)
    {
        var defaultValue = shortcutKey switch
        {
            PreferenceConstants.Shortcuts.NEW_TASK => PreferenceConstants.Shortcuts.Defaults.NEW_TASK,
            PreferenceConstants.Shortcuts.SAVE_WORK => PreferenceConstants.Shortcuts.Defaults.SAVE_WORK,
            PreferenceConstants.Shortcuts.OPEN_WORKFLOW => PreferenceConstants.Shortcuts.Defaults.OPEN_WORKFLOW,
            PreferenceConstants.Shortcuts.RUN_TASK => PreferenceConstants.Shortcuts.Defaults.RUN_TASK,
            PreferenceConstants.Shortcuts.STOP_TASK => PreferenceConstants.Shortcuts.Defaults.STOP_TASK,
            PreferenceConstants.Shortcuts.OPEN_SETTINGS => PreferenceConstants.Shortcuts.Defaults.OPEN_SETTINGS,
            PreferenceConstants.Shortcuts.OPEN_HELP => PreferenceConstants.Shortcuts.Defaults.OPEN_HELP,
            PreferenceConstants.Shortcuts.EXIT_APP => PreferenceConstants.Shortcuts.Defaults.EXIT_APP,
            _ => string.Empty
        };

        return await service.GetPreferenceAsync(userId, PreferenceConstants.Shortcuts.CATEGORY, shortcutKey, defaultValue, cancellationToken);
    }

    /// <summary>
    /// 设置快捷键
    /// </summary>
    public static async Task<bool> SetShortcutAsync(this IPreferenceService service, Guid userId, string shortcutKey, string keyBinding, CancellationToken cancellationToken = default)
    {
        return await service.SetPreferenceAsync(userId, PreferenceConstants.Shortcuts.CATEGORY, shortcutKey, keyBinding, $"快捷键设置: {shortcutKey}", cancellationToken);
    }

    /// <summary>
    /// 批量初始化默认偏好设置
    /// </summary>
    public static async Task<int> InitializeDefaultPreferencesAsync(this IPreferenceService service, Guid userId, CancellationToken cancellationToken = default)
    {
        var defaultPreferences = new Dictionary<string, Dictionary<string, object>>
        {
            // 界面设置
            [PreferenceConstants.UI.CATEGORY] = new()
            {
                [PreferenceConstants.UI.THEME] = PreferenceConstants.UI.Defaults.THEME,
                [PreferenceConstants.UI.FONT_SIZE] = PreferenceConstants.UI.Defaults.FONT_SIZE,
                [PreferenceConstants.UI.LAYOUT] = PreferenceConstants.UI.Defaults.LAYOUT,
                [PreferenceConstants.UI.SHOW_SIDEBAR] = PreferenceConstants.UI.Defaults.SHOW_SIDEBAR,
                [PreferenceConstants.UI.SHOW_TOOLBAR] = PreferenceConstants.UI.Defaults.SHOW_TOOLBAR,
                [PreferenceConstants.UI.SHOW_STATUSBAR] = PreferenceConstants.UI.Defaults.SHOW_STATUSBAR,
                [PreferenceConstants.UI.WINDOW_OPACITY] = PreferenceConstants.UI.Defaults.WINDOW_OPACITY,
                [PreferenceConstants.UI.ENABLE_ANIMATIONS] = PreferenceConstants.UI.Defaults.ENABLE_ANIMATIONS,
                [PreferenceConstants.UI.SCALE_FACTOR] = PreferenceConstants.UI.Defaults.SCALE_FACTOR,
                [PreferenceConstants.UI.COLOR_SCHEME] = PreferenceConstants.UI.Defaults.COLOR_SCHEME
            },
            // 语言设置
            [PreferenceConstants.Language.CATEGORY] = new()
            {
                [PreferenceConstants.Language.UI_LANGUAGE] = PreferenceConstants.Language.Defaults.UI_LANGUAGE,
                [PreferenceConstants.Language.INPUT_LANGUAGE] = PreferenceConstants.Language.Defaults.INPUT_LANGUAGE,
                [PreferenceConstants.Language.OUTPUT_LANGUAGE] = PreferenceConstants.Language.Defaults.OUTPUT_LANGUAGE,
                [PreferenceConstants.Language.DATETIME_FORMAT] = PreferenceConstants.Language.Defaults.DATETIME_FORMAT,
                [PreferenceConstants.Language.NUMBER_FORMAT] = PreferenceConstants.Language.Defaults.NUMBER_FORMAT,
                [PreferenceConstants.Language.CURRENCY_FORMAT] = PreferenceConstants.Language.Defaults.CURRENCY_FORMAT,
                [PreferenceConstants.Language.TIMEZONE] = PreferenceConstants.Language.Defaults.TIMEZONE
            },
            // 操作设置
            [PreferenceConstants.Operation.CATEGORY] = new()
            {
                [PreferenceConstants.Operation.DEFAULT_LLM_MODEL] = PreferenceConstants.Operation.Defaults.DEFAULT_LLM_MODEL,
                [PreferenceConstants.Operation.TASK_TIMEOUT] = PreferenceConstants.Operation.Defaults.TASK_TIMEOUT,
                [PreferenceConstants.Operation.AUTO_SAVE_INTERVAL] = PreferenceConstants.Operation.Defaults.AUTO_SAVE_INTERVAL,
                [PreferenceConstants.Operation.MAX_CONCURRENT_TASKS] = PreferenceConstants.Operation.Defaults.MAX_CONCURRENT_TASKS,
                [PreferenceConstants.Operation.ENABLE_AUTO_SAVE] = PreferenceConstants.Operation.Defaults.ENABLE_AUTO_SAVE,
                [PreferenceConstants.Operation.ENABLE_CONFIRMATION] = PreferenceConstants.Operation.Defaults.ENABLE_CONFIRMATION,
                [PreferenceConstants.Operation.ENABLE_OPERATION_LOG] = PreferenceConstants.Operation.Defaults.ENABLE_OPERATION_LOG,
                [PreferenceConstants.Operation.DEFAULT_WORK_DIRECTORY] = PreferenceConstants.Operation.Defaults.DEFAULT_WORK_DIRECTORY,
                [PreferenceConstants.Operation.TEMP_CLEANUP_INTERVAL] = PreferenceConstants.Operation.Defaults.TEMP_CLEANUP_INTERVAL,
                [PreferenceConstants.Operation.ENABLE_SMART_SUGGESTIONS] = PreferenceConstants.Operation.Defaults.ENABLE_SMART_SUGGESTIONS,
                [PreferenceConstants.Operation.RESPONSE_SPEED_PRIORITY] = PreferenceConstants.Operation.Defaults.RESPONSE_SPEED_PRIORITY
            },
            // 快捷键设置
            [PreferenceConstants.Shortcuts.CATEGORY] = new()
            {
                [PreferenceConstants.Shortcuts.NEW_TASK] = PreferenceConstants.Shortcuts.Defaults.NEW_TASK,
                [PreferenceConstants.Shortcuts.SAVE_WORK] = PreferenceConstants.Shortcuts.Defaults.SAVE_WORK,
                [PreferenceConstants.Shortcuts.OPEN_WORKFLOW] = PreferenceConstants.Shortcuts.Defaults.OPEN_WORKFLOW,
                [PreferenceConstants.Shortcuts.RUN_TASK] = PreferenceConstants.Shortcuts.Defaults.RUN_TASK,
                [PreferenceConstants.Shortcuts.STOP_TASK] = PreferenceConstants.Shortcuts.Defaults.STOP_TASK,
                [PreferenceConstants.Shortcuts.OPEN_SETTINGS] = PreferenceConstants.Shortcuts.Defaults.OPEN_SETTINGS,
                [PreferenceConstants.Shortcuts.OPEN_HELP] = PreferenceConstants.Shortcuts.Defaults.OPEN_HELP,
                [PreferenceConstants.Shortcuts.EXIT_APP] = PreferenceConstants.Shortcuts.Defaults.EXIT_APP
            },
            // 收藏设置
            [PreferenceConstants.Favorites.CATEGORY] = new()
            {
                [PreferenceConstants.Favorites.WORKFLOWS] = PreferenceConstants.Favorites.Defaults.WORKFLOWS,
                [PreferenceConstants.Favorites.AGENTS] = PreferenceConstants.Favorites.Defaults.AGENTS,
                [PreferenceConstants.Favorites.TEMPLATES] = PreferenceConstants.Favorites.Defaults.TEMPLATES,
                [PreferenceConstants.Favorites.RECENT_ITEMS] = PreferenceConstants.Favorites.Defaults.RECENT_ITEMS,
                [PreferenceConstants.Favorites.QUICK_ACCESS_ITEMS] = PreferenceConstants.Favorites.Defaults.QUICK_ACCESS_ITEMS
            }
        };

        return await service.SetPreferencesBatchAsync(userId, defaultPreferences, cancellationToken);
    }
}
