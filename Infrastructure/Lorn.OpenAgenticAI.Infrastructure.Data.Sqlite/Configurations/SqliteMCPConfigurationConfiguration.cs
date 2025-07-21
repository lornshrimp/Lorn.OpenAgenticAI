using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.MCP;
using System.Text.Json;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的MCP配置
/// </summary>
public class SqliteMCPConfigurationConfiguration : IEntityTypeConfiguration<MCPConfiguration>
{
    public void Configure(EntityTypeBuilder<MCPConfiguration> builder)
    {
        // 主键配置
        builder.HasKey(e => e.ConfigurationId);
        builder.Property(e => e.ConfigurationId)
            .HasConversion<string>();

        // 外键配置
        builder.Property(e => e.CreatedBy)
            .HasConversion<string>();

        // 字符串字段配置
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Command)
            .HasMaxLength(500)
            .IsRequired();

        // 时间字段配置
        builder.Property(e => e.CreatedTime)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.UpdatedTime)
            .HasColumnType("TEXT");

        builder.Property(e => e.LastUsedTime)
            .HasColumnType("TEXT");

        // 数值字段配置
        builder.Property(e => e.TimeoutSeconds)
            .HasColumnType("INTEGER");

        // 布尔字段配置
        builder.Property(e => e.IsEnabled)
            .HasConversion<int>();

        // 枚举字段配置
        builder.Property(e => e.Type)
            .HasConversion<string>();

        // 复杂对象配置（JSON序列化）
        builder.Property(e => e.Arguments)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<ArgumentItem>() :
                     JsonSerializer.Deserialize<List<ArgumentItem>>(v, (JsonSerializerOptions?)null) ?? new List<ArgumentItem>()
            );

        builder.Property(e => e.EnvironmentVariables)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<EnvironmentVariable>() :
                     JsonSerializer.Deserialize<List<EnvironmentVariable>>(v, (JsonSerializerOptions?)null) ?? new List<EnvironmentVariable>()
            );

        builder.Property(e => e.Tags)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<string>() :
                     JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(e => e.ProviderInfo)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     null :
                     JsonSerializer.Deserialize<ProviderInfo>(v, (JsonSerializerOptions?)null)
            );

        builder.Property(e => e.AdapterConfiguration)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     null :
                     JsonSerializer.Deserialize<ProtocolAdapterConfiguration>(v, (JsonSerializerOptions?)null)
            );

        // SQLite特定索引
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_SQLite_MCPConfiguration_Name");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_SQLite_MCPConfiguration_Type");

        builder.HasIndex(e => e.IsEnabled)
            .HasDatabaseName("IX_SQLite_MCPConfiguration_IsEnabled");

        builder.HasIndex(e => new { e.Type, e.IsEnabled })
            .HasDatabaseName("IX_SQLite_MCPConfiguration_Type_Enabled");
    }
}
