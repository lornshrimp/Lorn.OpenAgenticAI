using Microsoft.EntityFrameworkCore;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;

/// <summary>
/// SQLite特定的DbContext配置
/// </summary>
public class SqliteOpenAgenticAIDbContext : OpenAgenticAIDbContext
{
    public SqliteOpenAgenticAIDbContext(DbContextOptions<OpenAgenticAIDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 调用基类配置
        base.OnModelCreating(modelBuilder);

        // 应用SQLite特定配置
        ApplySqliteConfigurations(modelBuilder);

        // SQLite特定的全局设置
        ApplySqliteConventions(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // SQLite特定的配置
        if (optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(options =>
            {
                // 启用外键约束
                options.CommandTimeout(30);
            });
        }
    }

    /// <summary>
    /// 应用SQLite特定的实体配置
    /// </summary>
    /// <param name="modelBuilder"></param>
    private void ApplySqliteConfigurations(ModelBuilder modelBuilder)
    {
        // 应用SQLite特定的配置类
        modelBuilder.ApplyConfiguration(new SqliteUserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new SqliteModelProviderConfiguration());
        modelBuilder.ApplyConfiguration(new SqliteModelConfiguration());
        modelBuilder.ApplyConfiguration(new SqliteTaskExecutionHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new SqliteWorkflowTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new SqliteMCPConfigurationConfiguration());
    }

    /// <summary>
    /// 应用SQLite特定的约定
    /// </summary>
    /// <param name="modelBuilder"></param>
    private void ApplySqliteConventions(ModelBuilder modelBuilder)
    {
        // SQLite不支持真正的布尔类型，转换为整数
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(bool) || property.ClrType == typeof(bool?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.BoolToZeroOneConverter<int>());
                }

                // Guid转换为字符串存储
                if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.GuidToStringConverter());
                }

                // DateTime确保使用TEXT格式
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("TEXT");
                }

                // Decimal精度配置
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetColumnType("TEXT");
                    // 使用自定义转换器
                    var converter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal, string>(
                        v => v.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        v => decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
                    property.SetValueConverter(converter);
                }
            }
        }

        // 禁用级联删除（SQLite支持有限）
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
