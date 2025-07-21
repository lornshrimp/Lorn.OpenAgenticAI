using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using System.Text.Json;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的任务执行历史配置
/// </summary>
public class SqliteTaskExecutionHistoryConfiguration : IEntityTypeConfiguration<TaskExecutionHistory>
{
    public void Configure(EntityTypeBuilder<TaskExecutionHistory> builder)
    {
        // 主键配置
        builder.HasKey(e => e.ExecutionId);
        builder.Property(e => e.ExecutionId)
            .HasConversion<string>();

        // 外键配置
        builder.Property(e => e.UserId)
            .HasConversion<string>();

        // 字符串字段配置
        builder.Property(e => e.RequestId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.UserInput)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.RequestType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ResultSummary)
            .HasColumnType("TEXT");

        builder.Property(e => e.LlmProvider)
            .HasMaxLength(100);

        builder.Property(e => e.LlmModel)
            .HasMaxLength(100);

        // 时间字段配置
        builder.Property(e => e.StartTime)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(e => e.EndTime)
            .HasColumnType("TEXT");

        // 数值字段配置
        builder.Property(e => e.TotalExecutionTime)
            .HasColumnType("INTEGER");

        builder.Property(e => e.TokenUsage)
            .HasColumnType("INTEGER");

        builder.Property(e => e.EstimatedCost)
            .HasColumnType("REAL");

        builder.Property(e => e.ErrorCount)
            .HasColumnType("INTEGER");

        // 布尔字段配置
        builder.Property(e => e.IsSuccessful)
            .HasConversion<int>();

        // 枚举字段配置
        builder.Property(e => e.ExecutionStatus)
            .HasConversion<string>();

        // 复杂对象配置（JSON序列化）
        builder.Property(e => e.Tags)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new List<string>() :
                     JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(e => e.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ?
                     new Dictionary<string, object>() :
                     JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            );

        // SQLite特定索引
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_UserId");

        builder.HasIndex(e => e.ExecutionStatus)
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_Status");

        builder.HasIndex(e => e.StartTime)
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_StartTime");

        builder.HasIndex(e => e.IsSuccessful)
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_IsSuccessful");

        builder.HasIndex(e => new { e.UserId, e.StartTime })
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_User_StartTime");

        builder.HasIndex(e => e.RequestType)
            .HasDatabaseName("IX_SQLite_TaskExecutionHistory_RequestType");
    }
}
