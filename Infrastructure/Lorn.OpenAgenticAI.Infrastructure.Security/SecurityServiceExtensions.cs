using Microsoft.Extensions.DependencyInjection;
using Lorn.OpenAgenticAI.Domain.Contracts;

namespace Lorn.OpenAgenticAI.Infrastructure.Security;

/// <summary>
/// 安全服务依赖注入扩展
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// 注册安全相关服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddScoped<ICryptoService, CryptoService>();

        return services;
    }
}