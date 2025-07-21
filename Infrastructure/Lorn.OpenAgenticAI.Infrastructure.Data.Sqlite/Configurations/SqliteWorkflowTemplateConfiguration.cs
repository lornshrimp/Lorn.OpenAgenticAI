using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.Workflow;
using System.Text.Json;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的工作流模板配置
/// </summary>
public class SqliteWorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        // 主键配置
        builder.HasKey(e => e.TemplateId);
        builder.Property(e => e.TemplateId)
            .HasConversion<string>();

        // 外键配置
        builder.Property(e => e.UserId)
            .HasConversion<string>();

        // 字符串字段配置
        builder.Property(e => e.TemplateName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        // 时间字段配置
        builder.Property(e => e.CreatedTime)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.LastModifiedTime)
            .HasColumnType("TEXT");

        // 数值字段配置
        builder.Property(e => e.UsageCount)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(e => e.Rating)
            .HasColumnType("REAL");

        builder.Property(e => e.EstimatedExecutionTime)
            .HasColumnType("INTEGER");

        // 布尔字段配置
        builder.Property(e => e.IsPublic)
            .HasConversion<int>();

        builder.Property(e => e.IsSystemTemplate)
            .HasConversion<int>();

        // 复杂对象配置（JSON序列化）
        builder.Property(e => e.TemplateDefinition)
            .HasColumnType("TEXT")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Domain.Models.ValueObjects.WorkflowDefinition>(v, (JsonSerializerOptions?)null) ?? new Domain.Models.ValueObjects.WorkflowDefinition("json", "{}", null, null)
            );

        builder.Property(e => e.RequiredCapabilities)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<string>() :
                     JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(e => e.Tags)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<string>() :
                     JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(e => e.TemplateVersion)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Domain.Models.ValueObjects.Version>(v, (JsonSerializerOptions?)null) ?? new Domain.Models.ValueObjects.Version(1, 0, 0, null)
            );

        builder.Property(e => e.ThumbnailData)
            .HasColumnType("BLOB");

        // SQLite特定索引
        builder.HasIndex(e => e.TemplateName)
            .IsUnique()
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_Name");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_Category");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_UserId");

        builder.HasIndex(e => e.IsPublic)
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_IsPublic");

        builder.HasIndex(e => e.IsSystemTemplate)
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_IsSystemTemplate");

        builder.HasIndex(e => new { e.Category, e.IsPublic, e.IsSystemTemplate })
            .HasDatabaseName("IX_SQLite_WorkflowTemplate_Category_Public_System");
    }
}
