using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Lorn.OpenAgenticAI.Infrastructure.Data;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;

/// <summary>
/// 设计时 DbContext 工厂，用于 EF Core 迁移
/// </summary>
public class SqliteOpenAgenticAIDbContextFactory : IDesignTimeDbContextFactory<SqliteOpenAgenticAIDbContext>
{
    public SqliteOpenAgenticAIDbContext CreateDbContext(string[] args)
    {
        // 创建配置构建器
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 配置 DbContext 选项
        var optionsBuilder = new DbContextOptionsBuilder<SqliteOpenAgenticAIDbContext>();

        // 获取连接字符串，如果没有配置则使用默认的内存数据库
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                             ?? "Data Source=OpenAgenticAI.db";

        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(SqliteOpenAgenticAIDbContext).Assembly.FullName);
            options.CommandTimeout(30);
        });

        // 在开发环境启用敏感数据日志记录
        if (args.Contains("--verbose") || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        return new SqliteOpenAgenticAIDbContext(optionsBuilder.Options);
    }
}
