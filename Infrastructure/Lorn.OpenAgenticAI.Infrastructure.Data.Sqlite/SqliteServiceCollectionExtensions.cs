using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.SeedData;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;

/// <summary>
/// SQLite数据库服务注册扩展
/// </summary>
public static class SqliteServiceCollectionExtensions
{
    /// <summary>
    /// 添加SQLite数据库支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="configureOptions">额外配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSqliteDatabase(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        // 验证连接字符串
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("连接字符串不能为空", nameof(connectionString));
        }

        // 注册DbContext
        services.AddDbContext<OpenAgenticAIDbContext, SqliteOpenAgenticAIDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
                // 启用外键约束
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // 开发环境配置
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(true);
            options.EnableServiceProviderCaching();

            // 应用额外配置
            configureOptions?.Invoke(options);
        });

        // 注册数据库初始化相关服务
        services.AddScoped<SqliteDatabaseMigrator>();
        services.AddScoped<SqliteSeedDataService>();
        services.AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// 从配置文件添加SQLite数据库支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <param name="configureOptions">额外配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSqliteDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName) ??
                              "Data Source=openagentai.db";

        return services.AddSqliteDatabase(connectionString, configureOptions);
    }

    /// <summary>
    /// 添加SQLite内存数据库（用于测试）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="configureOptions">额外配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSqliteInMemoryDatabase(
        this IServiceCollection services,
        string databaseName = "TestDatabase",
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        services.AddDbContext<OpenAgenticAIDbContext, SqliteOpenAgenticAIDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(true);
            options.EnableDetailedErrors(true);
            options.LogTo(Console.WriteLine, LogLevel.Information);

            configureOptions?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// 添加生产环境的SQLite数据库配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSqliteDatabaseForProduction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddSqliteDatabase(configuration, "DefaultConnection", options =>
        {
            // 生产环境优化配置
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
            options.EnableServiceProviderCaching(true);
        });
    }

    /// <summary>
    /// 确保数据库已创建并应用迁移
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="createDatabase">是否创建数据库</param>
    /// <param name="migrateDatabase">是否应用迁移</param>
    /// <returns>异步任务</returns>
    public static async Task EnsureDatabaseAsync(
        this IServiceProvider serviceProvider,
        bool createDatabase = true,
        bool migrateDatabase = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OpenAgenticAIDbContext>();

        try
        {
            if (createDatabase)
            {
                await context.Database.EnsureCreatedAsync();
            }

            if (migrateDatabase && context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<OpenAgenticAIDbContext>>();
            logger?.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 验证数据库连接
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>连接是否成功</returns>
    public static async Task<bool> ValidateDatabaseConnectionAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OpenAgenticAIDbContext>();
            return await context.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
