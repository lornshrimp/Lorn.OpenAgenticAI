using Microsoft.Extensions.DependencyInjection;
using Lorn.OpenAgenticAI.Domain.Contracts;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;

/// <summary>
/// 仓储服务注册扩展
/// </summary>
public static class RepositoryServiceExtensions
{
    /// <summary>
    /// 注册用户管理相关的仓储服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUserRepositories(this IServiceCollection services)
    {
        // 注册用户仓储
        services.AddScoped<IUserRepository, UserRepository>();

        // 注册用户偏好设置仓储
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();

        // 注册用户元数据仓储
        services.AddScoped<IUserMetadataRepository, UserMetadataRepository>();

        // 注册用户安全日志仓储
        services.AddScoped<IUserSecurityLogRepository, UserSecurityLogRepository>();

        return services;
    }

    /// <summary>
    /// 注册所有仓储服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllRepositories(this IServiceCollection services)
    {
        // 注册用户管理相关仓储
        services.AddUserRepositories();

        // TODO: 在后续任务中添加其他仓储的注册
        // services.AddWorkflowRepositories();
        // services.AddLLMRepositories();
        // services.AddMCPRepositories();
        // services.AddMonitoringRepositories();

        return services;
    }
}
