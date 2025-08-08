using Microsoft.Extensions.DependencyInjection;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Application.Services.Services;

namespace Lorn.OpenAgenticAI.Application.Services.Extensions;

/// <summary>
/// 服务集合扩展方法，用于注册应用服务
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册用户账户与个性化功能相关的应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUserAccountServices(this IServiceCollection services)
    {
        // 注册静默认证服务
        services.AddScoped<ISilentAuthenticationService, SilentAuthenticationService>();

        // 注册用户管理服务
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }

    /// <summary>
    /// 注册偏好设置相关服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPreferenceServices(this IServiceCollection services)
    {
        // 注册核心偏好设置服务
        services.AddScoped<IPreferenceService, PreferenceService>();

        // 注册偏好设置通知服务
        services.AddSingleton<IPreferenceNotificationService, PreferenceNotificationService>();
        services.AddHostedService<PreferenceNotificationService>(provider =>
            (PreferenceNotificationService)provider.GetRequiredService<IPreferenceNotificationService>());

        // 注册偏好设置应用服务
        services.AddScoped<IPreferenceApplyService, PreferenceApplyService>();

        // 注册综合偏好设置管理服务
        services.AddScoped<PreferenceManagementService>();

        return services;
    }

    /// <summary>
    /// 注册所有应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册用户账户服务
        services.AddUserAccountServices();

        // 注册偏好设置服务
        services.AddPreferenceServices();

        return services;
    }
}