using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.LLM;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的模型提供者配置
/// </summary>
public class SqliteModelProviderConfiguration : IEntityTypeConfiguration<ModelProvider>
{
    public void Configure(EntityTypeBuilder<ModelProvider> builder)
    {
        // 主键配置
        builder.HasKey(e => e.ProviderId);
        builder.Property(e => e.ProviderId)
            .HasConversion<string>(); // Guid转换为字符串

        // 字符串字段配置
        builder.Property(e => e.ProviderName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        builder.Property(e => e.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(e => e.ApiKeyUrl)
            .HasMaxLength(500);

        builder.Property(e => e.DocsUrl)
            .HasMaxLength(500);

        builder.Property(e => e.ModelsUrl)
            .HasMaxLength(500);

        // 外键配置
        builder.Property(e => e.ProviderTypeId)
            .HasConversion<string>();

        builder.Property(e => e.CreatedBy)
            .HasConversion<string>();

        // 时间字段配置
        builder.Property(e => e.CreatedTime)
            .HasColumnType("TEXT");

        builder.Property(e => e.UpdatedTime)
            .HasColumnType("TEXT");

        // 布尔字段配置
        builder.Property(e => e.IsPrebuilt)
            .HasConversion<int>();

        // 复杂对象配置（JSON序列化）
        builder.Property(e => e.DefaultApiConfiguration)
            .HasColumnType("TEXT")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new ApiConfiguration("", null, AuthenticationMethod.None, null, 30, null, null, null) :
                     System.Text.Json.JsonSerializer.Deserialize<ApiConfiguration>(v, (System.Text.Json.JsonSerializerOptions?)null) ??
                     new ApiConfiguration("", null, AuthenticationMethod.None, null, 30, null, null, null)
            );

        builder.Property(e => e.Status)
            .HasColumnType("TEXT")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     ServiceStatus.Unknown :
                     System.Text.Json.JsonSerializer.Deserialize<ServiceStatus>(v, (System.Text.Json.JsonSerializerOptions?)null) ??
                     ServiceStatus.Unknown
            );

        // SQLite特定索引
        builder.HasIndex(e => e.ProviderName)
            .HasDatabaseName("IX_SQLite_ModelProvider_Name");

        builder.HasIndex(e => e.ProviderTypeId)
            .HasDatabaseName("IX_SQLite_ModelProvider_Type");

        builder.HasIndex(e => e.IsPrebuilt)
            .HasDatabaseName("IX_SQLite_ModelProvider_Prebuilt");
    }
}
