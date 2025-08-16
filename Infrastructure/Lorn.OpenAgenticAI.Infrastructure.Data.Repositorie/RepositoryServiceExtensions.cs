using Microsoft.Extensions.DependencyInjection;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;

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
        // 注册统一的用户档案仓储
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }

    /// <summary>
    /// 注册任务执行相关仓储
    /// </summary>
    public static IServiceCollection AddExecutionRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskExecutionRepository, TaskExecutionRepository>();
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

        // 注册任务执行相关仓储
        services.AddExecutionRepositories();

        // TODO: 在后续任务中添加其他仓储的注册
        // services.AddWorkflowRepositories();
        // services.AddLLMRepositories();
        // services.AddMCPRepositories();
        // services.AddMonitoringRepositories();

        return services;
    }
}
