using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的执行步骤记录配置
/// </summary>
public class SqliteExecutionStepRecordConfiguration : IEntityTypeConfiguration<ExecutionStepRecord>
{
    public void Configure(EntityTypeBuilder<ExecutionStepRecord> builder)
    {
        // 主键与外键（Guid->string）
        builder.HasKey(e => e.StepRecordId);
        builder.Property(e => e.StepRecordId)
            .HasConversion(g => g.ToString(), s => Guid.Parse(s));

        builder.Property(e => e.ExecutionId)
            .HasConversion(g => g.ToString(), s => Guid.Parse(s));

        // 基本字段
        builder.Property(e => e.StepId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.StepOrder)
            .HasColumnType("INTEGER");

        builder.Property(e => e.StepDescription)
            .HasMaxLength(500);

        builder.Property(e => e.AgentId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ActionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Parameters)
            .HasColumnType("TEXT");

        builder.Property(e => e.OutputData)
            .HasColumnType("TEXT");

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.StartTime)
            .HasColumnType("TEXT");

        builder.Property(e => e.EndTime)
            .HasColumnType("TEXT");

        builder.Property(e => e.ExecutionTime)
            .HasColumnType("INTEGER");

        builder.Property(e => e.IsSuccessful)
            .HasConversion<int>();

        builder.Property(e => e.RetryCount)
            .HasColumnType("INTEGER");

        // 枚举 ExecutionStatus 存储为整数
        builder.Property(e => e.StepStatus)
            .HasConversion(
                s => s.Id,
                v => Enumeration.FromValue<ExecutionStatus>(v)
            );

        // 值对象 ResourceUsage 拥有类型 + JSON列
        builder.OwnsOne(e => e.ResourceUsage, usage =>
        {
            usage.Property(u => u.CpuUsagePercent).HasColumnType("REAL");
            usage.Property(u => u.MemoryUsageBytes).HasColumnType("INTEGER");
            usage.Property(u => u.DiskIOBytes).HasColumnType("INTEGER");
            usage.Property(u => u.NetworkIOBytes).HasColumnType("INTEGER");

            usage.Property(u => u.CustomMetrics)
                .HasColumnType("TEXT")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? new Dictionary<string, double>() :
                         JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, double>()
                );
        });

        // 关系：多对一 TaskExecutionHistory
        builder.HasOne(e => e.Execution)
            .WithMany(h => h.ExecutionSteps)
            .HasForeignKey(e => e.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引与唯一约束
        builder.HasIndex(e => new { e.ExecutionId, e.StepOrder })
            .IsUnique()
            .HasDatabaseName("IX_SQLite_ExecutionStepRecords_Execution_Order");

        builder.HasIndex(e => new { e.AgentId, e.ActionName, e.StartTime })
            .HasDatabaseName("IX_SQLite_ExecutionStepRecords_Agent_Action_Time");

        builder.HasIndex(e => new { e.StepStatus, e.StartTime })
            .HasDatabaseName("IX_SQLite_ExecutionStepRecords_Status_Time");

        builder.ToTable("ExecutionStepRecords");

        // SQLite CHECK 约束以防止超长数据被写入（进一步保护，双保险）
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExecutionStepRecords_StepId_Length",
            "length(StepId) <= 100"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExecutionStepRecords_AgentId_Length",
            "length(AgentId) <= 100"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExecutionStepRecords_ActionName_Length",
            "length(ActionName) <= 100"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExecutionStepRecords_StepDescription_Length",
            "StepDescription IS NULL OR length(StepDescription) <= 500"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExecutionStepRecords_ErrorMessage_Length",
            "ErrorMessage IS NULL OR length(ErrorMessage) <= 1000"));
    }
}
