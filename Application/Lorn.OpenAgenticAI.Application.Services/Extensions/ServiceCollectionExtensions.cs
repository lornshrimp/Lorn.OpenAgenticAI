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

        return services;
    }
}