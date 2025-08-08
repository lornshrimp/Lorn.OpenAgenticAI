using System.Collections.Concurrent;
using Lorn.OpenAgenticAI.Application.Services.Constants;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 偏好设置变更通知服务实现
/// </summary>
public class PreferenceNotificationService : BackgroundService, IPreferenceNotificationService
{
    private readonly ILogger<PreferenceNotificationService> _logger;
    private readonly ConcurrentDictionary<string, List<Func<PreferenceChangedEventArgs, Task>>> _categoryHandlers = new();
    private readonly List<Func<PreferenceChangedEventArgs, Task>> _globalHandlers = new();
    private readonly object _lockObject = new();

    public PreferenceNotificationService(ILogger<PreferenceNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 注册偏好设置变更监听器
    /// </summary>
    public void Subscribe(string? category, Func<PreferenceChangedEventArgs, Task> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_lockObject)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                _globalHandlers.Add(handler);
                _logger.LogDebug("Registered global preference change handler");
            }
            else
            {
                if (!_categoryHandlers.ContainsKey(category))
                {
                    _categoryHandlers[category] = new List<Func<PreferenceChangedEventArgs, Task>>();
                }
                _categoryHandlers[category].Add(handler);
                _logger.LogDebug("Registered preference change handler for category: {Category}", category);
            }
        }
    }

    /// <summary>
    /// 取消偏好设置变更监听器
    /// </summary>
    public void Unsubscribe(string? category, Func<PreferenceChangedEventArgs, Task> handler)
    {
        if (handler == null)
        {
            return;
        }

        lock (_lockObject)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                _globalHandlers.Remove(handler);
                _logger.LogDebug("Unregistered global preference change handler");
            }
            else if (_categoryHandlers.ContainsKey(category))
            {
                _categoryHandlers[category].Remove(handler);
                if (!_categoryHandlers[category].Any())
                {
                    _categoryHandlers.TryRemove(category, out _);
                }
                _logger.LogDebug("Unregistered preference change handler for category: {Category}", category);
            }
        }
    }

    /// <summary>
    /// 通知偏好设置变更
    /// </summary>
    public async Task NotifyPreferenceChangedAsync(PreferenceChangedEventArgs eventArgs)
    {
        if (eventArgs == null)
        {
            return;
        }

        _logger.LogDebug("Notifying preference change: User {UserId}, Category {Category}, Key {Key}, ChangeType {ChangeType}",
            eventArgs.UserId, eventArgs.Category, eventArgs.Key, eventArgs.ChangeType);

        var tasks = new List<Task>();

        // 执行全局监听器
        lock (_lockObject)
        {
            foreach (var handler in _globalHandlers.ToList())
            {
                tasks.Add(SafeExecuteHandler(handler, eventArgs));
            }

            // 执行分类特定监听器
            if (_categoryHandlers.ContainsKey(eventArgs.Category))
            {
                foreach (var handler in _categoryHandlers[eventArgs.Category].ToList())
                {
                    tasks.Add(SafeExecuteHandler(handler, eventArgs));
                }
            }
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 启动通知服务
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PreferenceNotificationService");
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// 停止通知服务
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PreferenceNotificationService");
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// 后台服务执行
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PreferenceNotificationService is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            // 这里可以添加周期性的清理工作或健康检查
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    /// <summary>
    /// 安全执行处理器
    /// </summary>
    private async Task SafeExecuteHandler(Func<PreferenceChangedEventArgs, Task> handler, PreferenceChangedEventArgs eventArgs)
    {
        try
        {
            await handler(eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing preference change handler for User {UserId}, Category {Category}, Key {Key}",
                eventArgs.UserId, eventArgs.Category, eventArgs.Key);
        }
    }
}

/// <summary>
/// 偏好设置实时应用服务实现
/// </summary>
public class PreferenceApplyService : IPreferenceApplyService
{
    private readonly ILogger<PreferenceApplyService> _logger;

    public PreferenceApplyService(ILogger<PreferenceApplyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 应用界面相关偏好设置变更
    /// </summary>
    public async Task ApplyUIPreferenceAsync(PreferenceChangedEventArgs eventArgs)
    {
        _logger.LogDebug("Applying UI preference change: {Key} = {NewValue}", eventArgs.Key, eventArgs.NewValue);

        try
        {
            switch (eventArgs.Key)
            {
                case PreferenceConstants.UI.THEME:
                    await ApplyThemeChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.UI.FONT_SIZE:
                    if (int.TryParse(eventArgs.NewValue?.ToString(), out var fontSize))
                    {
                        await ApplyFontSizeChangeAsync(fontSize);
                    }
                    break;

                case PreferenceConstants.UI.LAYOUT:
                    await ApplyLayoutChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.UI.SCALE_FACTOR:
                    if (double.TryParse(eventArgs.NewValue?.ToString(), out var scaleFactor))
                    {
                        await ApplyScaleFactorChangeAsync(scaleFactor);
                    }
                    break;

                case PreferenceConstants.UI.WINDOW_OPACITY:
                    if (double.TryParse(eventArgs.NewValue?.ToString(), out var opacity))
                    {
                        await ApplyWindowOpacityChangeAsync(opacity);
                    }
                    break;

                case PreferenceConstants.UI.ENABLE_ANIMATIONS:
                    if (bool.TryParse(eventArgs.NewValue?.ToString(), out var enableAnimations))
                    {
                        await ApplyAnimationSettingChangeAsync(enableAnimations);
                    }
                    break;

                default:
                    _logger.LogDebug("No specific handler for UI preference: {Key}", eventArgs.Key);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying UI preference change: {Key}", eventArgs.Key);
        }
    }

    /// <summary>
    /// 应用语言相关偏好设置变更
    /// </summary>
    public async Task ApplyLanguagePreferenceAsync(PreferenceChangedEventArgs eventArgs)
    {
        _logger.LogDebug("Applying language preference change: {Key} = {NewValue}", eventArgs.Key, eventArgs.NewValue);

        try
        {
            switch (eventArgs.Key)
            {
                case PreferenceConstants.Language.UI_LANGUAGE:
                    await ApplyUILanguageChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.Language.DATETIME_FORMAT:
                    await ApplyDateTimeFormatChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.Language.NUMBER_FORMAT:
                    await ApplyNumberFormatChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.Language.TIMEZONE:
                    await ApplyTimezoneChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                default:
                    _logger.LogDebug("No specific handler for language preference: {Key}", eventArgs.Key);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying language preference change: {Key}", eventArgs.Key);
        }
    }

    /// <summary>
    /// 应用操作相关偏好设置变更
    /// </summary>
    public async Task ApplyOperationPreferenceAsync(PreferenceChangedEventArgs eventArgs)
    {
        _logger.LogDebug("Applying operation preference change: {Key} = {NewValue}", eventArgs.Key, eventArgs.NewValue);

        try
        {
            switch (eventArgs.Key)
            {
                case PreferenceConstants.Operation.DEFAULT_LLM_MODEL:
                    await ApplyDefaultLLMModelChangeAsync(eventArgs.NewValue?.ToString());
                    break;

                case PreferenceConstants.Operation.TASK_TIMEOUT:
                    if (int.TryParse(eventArgs.NewValue?.ToString(), out var timeout))
                    {
                        await ApplyTaskTimeoutChangeAsync(timeout);
                    }
                    break;

                case PreferenceConstants.Operation.AUTO_SAVE_INTERVAL:
                    if (int.TryParse(eventArgs.NewValue?.ToString(), out var interval))
                    {
                        await ApplyAutoSaveIntervalChangeAsync(interval);
                    }
                    break;

                case PreferenceConstants.Operation.ENABLE_AUTO_SAVE:
                    if (bool.TryParse(eventArgs.NewValue?.ToString(), out var enableAutoSave))
                    {
                        await ApplyAutoSaveSettingChangeAsync(enableAutoSave);
                    }
                    break;

                case PreferenceConstants.Operation.MAX_CONCURRENT_TASKS:
                    if (int.TryParse(eventArgs.NewValue?.ToString(), out var maxTasks))
                    {
                        await ApplyMaxConcurrentTasksChangeAsync(maxTasks);
                    }
                    break;

                default:
                    _logger.LogDebug("No specific handler for operation preference: {Key}", eventArgs.Key);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying operation preference change: {Key}", eventArgs.Key);
        }
    }

    /// <summary>
    /// 应用快捷键相关偏好设置变更
    /// </summary>
    public async Task ApplyShortcutPreferenceAsync(PreferenceChangedEventArgs eventArgs)
    {
        _logger.LogDebug("Applying shortcut preference change: {Key} = {NewValue}", eventArgs.Key, eventArgs.NewValue);

        try
        {
            await ApplyShortcutChangeAsync(eventArgs.Key, eventArgs.NewValue?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying shortcut preference change: {Key}", eventArgs.Key);
        }
    }

    /// <summary>
    /// 检查是否需要重启应用以应用变更
    /// </summary>
    public bool RequiresRestart(PreferenceChangedEventArgs eventArgs)
    {
        return eventArgs.Category switch
        {
            PreferenceConstants.Language.CATEGORY when eventArgs.Key == PreferenceConstants.Language.UI_LANGUAGE => true,
            PreferenceConstants.Operation.CATEGORY when eventArgs.Key == PreferenceConstants.Operation.DEFAULT_WORK_DIRECTORY => true,
            _ => false
        };
    }

    #region 私有方法 - UI变更应用

    private async Task ApplyThemeChangeAsync(string? theme)
    {
        _logger.LogInformation("Applying theme change to: {Theme}", theme);
        // 这里应该调用UI框架的主题切换方法
        // 例如：Application.Current.RequestedTheme = theme;
        await Task.CompletedTask;
    }

    private async Task ApplyFontSizeChangeAsync(int fontSize)
    {
        _logger.LogInformation("Applying font size change to: {FontSize}", fontSize);
        // 这里应该调用UI框架的字体大小变更方法
        await Task.CompletedTask;
    }

    private async Task ApplyLayoutChangeAsync(string? layout)
    {
        _logger.LogInformation("Applying layout change to: {Layout}", layout);
        // 这里应该调用UI框架的布局变更方法
        await Task.CompletedTask;
    }

    private async Task ApplyScaleFactorChangeAsync(double scaleFactor)
    {
        _logger.LogInformation("Applying scale factor change to: {ScaleFactor}", scaleFactor);
        // 这里应该调用UI框架的缩放变更方法
        await Task.CompletedTask;
    }

    private async Task ApplyWindowOpacityChangeAsync(double opacity)
    {
        _logger.LogInformation("Applying window opacity change to: {Opacity}", opacity);
        // 这里应该调用UI框架的透明度变更方法
        await Task.CompletedTask;
    }

    private async Task ApplyAnimationSettingChangeAsync(bool enableAnimations)
    {
        _logger.LogInformation("Applying animation setting change to: {EnableAnimations}", enableAnimations);
        // 这里应该调用UI框架的动画设置方法
        await Task.CompletedTask;
    }

    #endregion

    #region 私有方法 - 语言变更应用

    private async Task ApplyUILanguageChangeAsync(string? language)
    {
        _logger.LogInformation("Applying UI language change to: {Language}", language);
        // 这里应该调用本地化框架的语言切换方法
        await Task.CompletedTask;
    }

    private async Task ApplyDateTimeFormatChangeAsync(string? format)
    {
        _logger.LogInformation("Applying datetime format change to: {Format}", format);
        // 这里应该更新全局的日期时间格式设置
        await Task.CompletedTask;
    }

    private async Task ApplyNumberFormatChangeAsync(string? format)
    {
        _logger.LogInformation("Applying number format change to: {Format}", format);
        // 这里应该更新全局的数字格式设置
        await Task.CompletedTask;
    }

    private async Task ApplyTimezoneChangeAsync(string? timezone)
    {
        _logger.LogInformation("Applying timezone change to: {Timezone}", timezone);
        // 这里应该更新系统时区设置
        await Task.CompletedTask;
    }

    #endregion

    #region 私有方法 - 操作变更应用

    private async Task ApplyDefaultLLMModelChangeAsync(string? model)
    {
        _logger.LogInformation("Applying default LLM model change to: {Model}", model);
        // 这里应该通知LLM服务更新默认模型
        await Task.CompletedTask;
    }

    private async Task ApplyTaskTimeoutChangeAsync(int timeout)
    {
        _logger.LogInformation("Applying task timeout change to: {Timeout} seconds", timeout);
        // 这里应该更新任务执行引擎的超时设置
        await Task.CompletedTask;
    }

    private async Task ApplyAutoSaveIntervalChangeAsync(int interval)
    {
        _logger.LogInformation("Applying auto-save interval change to: {Interval} seconds", interval);
        // 这里应该更新自动保存服务的间隔设置
        await Task.CompletedTask;
    }

    private async Task ApplyAutoSaveSettingChangeAsync(bool enableAutoSave)
    {
        _logger.LogInformation("Applying auto-save setting change to: {EnableAutoSave}", enableAutoSave);
        // 这里应该启用或禁用自动保存服务
        await Task.CompletedTask;
    }

    private async Task ApplyMaxConcurrentTasksChangeAsync(int maxTasks)
    {
        _logger.LogInformation("Applying max concurrent tasks change to: {MaxTasks}", maxTasks);
        // 这里应该更新任务执行引擎的并发数限制
        await Task.CompletedTask;
    }

    #endregion

    #region 私有方法 - 快捷键变更应用

    private async Task ApplyShortcutChangeAsync(string shortcutKey, string? keyBinding)
    {
        _logger.LogInformation("Applying shortcut change: {ShortcutKey} = {KeyBinding}", shortcutKey, keyBinding);
        // 这里应该更新UI框架的快捷键绑定
        await Task.CompletedTask;
    }

    #endregion
}
