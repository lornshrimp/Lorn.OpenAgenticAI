using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 偏好设置变更通知服务接口
/// </summary>
public interface IPreferenceNotificationService
{
    /// <summary>
    /// 注册偏好设置变更监听器
    /// </summary>
    /// <param name="category">偏好分类（可选，为null时监听所有分类）</param>
    /// <param name="handler">变更处理器</param>
    void Subscribe(string? category, Func<PreferenceChangedEventArgs, Task> handler);

    /// <summary>
    /// 取消偏好设置变更监听器
    /// </summary>
    /// <param name="category">偏好分类</param>
    /// <param name="handler">变更处理器</param>
    void Unsubscribe(string? category, Func<PreferenceChangedEventArgs, Task> handler);

    /// <summary>
    /// 通知偏好设置变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    Task NotifyPreferenceChangedAsync(PreferenceChangedEventArgs eventArgs);

    /// <summary>
    /// 启动通知服务
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止通知服务
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 偏好设置实时应用服务接口
/// </summary>
public interface IPreferenceApplyService
{
    /// <summary>
    /// 应用界面相关偏好设置变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    Task ApplyUIPreferenceAsync(PreferenceChangedEventArgs eventArgs);

    /// <summary>
    /// 应用语言相关偏好设置变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    Task ApplyLanguagePreferenceAsync(PreferenceChangedEventArgs eventArgs);

    /// <summary>
    /// 应用操作相关偏好设置变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    Task ApplyOperationPreferenceAsync(PreferenceChangedEventArgs eventArgs);

    /// <summary>
    /// 应用快捷键相关偏好设置变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    Task ApplyShortcutPreferenceAsync(PreferenceChangedEventArgs eventArgs);

    /// <summary>
    /// 检查是否需要重启应用以应用变更
    /// </summary>
    /// <param name="eventArgs">变更事件参数</param>
    /// <returns>是否需要重启</returns>
    bool RequiresRestart(PreferenceChangedEventArgs eventArgs);
}
