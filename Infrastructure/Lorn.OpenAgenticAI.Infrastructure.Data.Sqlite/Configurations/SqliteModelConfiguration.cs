using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.LLM;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的模型配置
/// </summary>
public class SqliteModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        // 主键配置
        builder.HasKey(e => e.ModelId);
        builder.Property(e => e.ModelId)
            .HasConversion<string>(); // Guid转换为字符串

        // 字符串字段配置
        builder.Property(e => e.ModelName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.ModelGroup)
            .HasMaxLength(100);

        // 外键配置
        builder.Property(e => e.ProviderId)
            .HasConversion<string>();

        builder.Property(e => e.CreatedBy)
            .HasConversion<string>();

        // 时间字段配置
        builder.Property(e => e.CreatedTime)
            .HasColumnType("TEXT");

        builder.Property(e => e.ReleaseDate)
            .HasColumnType("TEXT");

        // 布尔字段配置
        builder.Property(e => e.IsLatestVersion)
            .HasConversion<int>();

        builder.Property(e => e.IsPrebuilt)
            .HasConversion<int>();

        // 数值字段配置
        builder.Property(e => e.ContextLength)
            .HasColumnType("INTEGER");

        builder.Property(e => e.MaxOutputTokens)
            .HasColumnType("INTEGER");

        // 集合字段配置（JSON序列化）
        builder.Property(e => e.SupportedCapabilities)
            .HasColumnType("TEXT")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ModelCapability>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ModelCapability>()
            );

        // 复杂对象配置（JSON序列化）
        builder.Property(e => e.PricingInfo)
            .HasColumnType("TEXT")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new PricingInfo(Currency.USD, 0m, 0m, null, null, null, null) :
                     System.Text.Json.JsonSerializer.Deserialize<PricingInfo>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new PricingInfo(Currency.USD, 0m, 0m, null, null, null, null)
            );

        builder.Property(e => e.PerformanceMetrics)
            .HasColumnType("TEXT")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new PerformanceMetrics(0.0, 0.0, 0.0, 0, 0.0) :
                     System.Text.Json.JsonSerializer.Deserialize<PerformanceMetrics>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new PerformanceMetrics(0.0, 0.0, 0.0, 0, 0.0)
            );

        // SQLite特定索引
        builder.HasIndex(e => new { e.ProviderId, e.ModelName })
            .IsUnique()
            .HasDatabaseName("IX_SQLite_Model_Provider_Name");

        builder.HasIndex(e => e.ModelName)
            .HasDatabaseName("IX_SQLite_Model_Name");

        builder.HasIndex(e => e.IsLatestVersion)
            .HasDatabaseName("IX_SQLite_Model_LatestVersion");

        builder.HasIndex(e => e.IsPrebuilt)
            .HasDatabaseName("IX_SQLite_Model_Prebuilt");

        builder.HasIndex(e => e.ModelGroup)
            .HasDatabaseName("IX_SQLite_Model_Group");
    }
}
