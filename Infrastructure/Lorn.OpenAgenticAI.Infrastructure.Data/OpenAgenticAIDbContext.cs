using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Lorn.OpenAgenticAI.Domain.Models.LLM;
using Lorn.OpenAgenticAI.Domain.Models.MCP;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.Workflow;
using Lorn.OpenAgenticAI.Domain.Models.Monitoring;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Infrastructure.Data.Configurations;

namespace Lorn.OpenAgenticAI.Infrastructure.Data;

/// <summary>
/// 智能体平台数据库上下文抽象基类
/// 提供数据库无关的基础设施实现
/// </summary>
public abstract class OpenAgenticAIDbContext : DbContext
{
    protected OpenAgenticAIDbContext(DbContextOptions options) : base(options)
    {
    }

    #region DbSets - 业务实体集合

    // 用户管理相关
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<UserMetadataEntry> UserMetadataEntries { get; set; } = null!;

    // 任务执行相关
    public DbSet<TaskExecutionHistory> TaskExecutionHistories { get; set; } = null!;
    public DbSet<StepExecutionTimeEntry> StepExecutionTimeEntries { get; set; } = null!;
    public DbSet<ResourceUtilizationEntry> ResourceUtilizationEntries { get; set; } = null!;

    // 工作流模板相关
    public DbSet<WorkflowTemplate> WorkflowTemplates { get; set; } = null!;
    public DbSet<WorkflowTemplateStep> WorkflowTemplateSteps { get; set; } = null!;
    public DbSet<StepParameterEntry> StepParameterEntries { get; set; } = null!;
    public DbSet<WorkflowMetadataEntry> WorkflowMetadataEntries { get; set; } = null!;

    // LLM管理相关
    public DbSet<ModelProvider> ModelProviders { get; set; } = null!;
    public DbSet<Model> Models { get; set; } = null!;
    public DbSet<ModelParameterEntry> ModelParameterEntries { get; set; } = null!;
    public DbSet<QualityThresholdEntry> QualityThresholdEntries { get; set; } = null!;
    public DbSet<UsageQuotaCustomLimitEntry> UsageQuotaCustomLimitEntries { get; set; } = null!;
    public DbSet<PricingSpecialEntry> PricingSpecialEntries { get; set; } = null!;
    public DbSet<ApiHeaderEntry> ApiHeaderEntries { get; set; } = null!;
    public DbSet<ProviderUserConfiguration> ProviderUserConfigurations { get; set; } = null!;
    public DbSet<ProviderCustomSettingEntry> ProviderCustomSettingEntries { get; set; } = null!;
    public DbSet<ModelUserConfiguration> ModelUserConfigurations { get; set; } = null!;

    // MCP配置相关
    public DbSet<MCPConfiguration> MCPConfigurations { get; set; } = null!;

    // 监控和性能相关
    public DbSet<PerformanceMetricsRecord> PerformanceMetrics { get; set; } = null!;
    public DbSet<MetricTagEntry> MetricTagEntries { get; set; } = null!;
    public DbSet<MetricContextEntry> MetricContextEntries { get; set; } = null!;

    #endregion

    #region 模型配置

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用通用配置
        ApplyCommonConfigurations(modelBuilder);

        // 应用实体配置
        ApplyEntityConfigurations(modelBuilder);

        // 应用关系配置
        ApplyRelationshipConfigurations(modelBuilder);

        // 应用索引和约束
        ApplyIndexesAndConstraints(modelBuilder);
    }

    /// <summary>
    /// 应用通用配置
    /// </summary>
    protected virtual void ApplyCommonConfigurations(ModelBuilder modelBuilder)
    {
        // 忽略枚举类型 - 这些是值对象，不需要单独的表
        modelBuilder.Ignore<AuthenticationMethod>();
        modelBuilder.Ignore<Currency>();
        modelBuilder.Ignore<ExecutionStatus>();
        modelBuilder.Ignore<MCPProtocolType>();

        // 忽略值对象类型
        modelBuilder.Ignore<ApiConfiguration>();
        modelBuilder.Ignore<ModelParameters>();
        modelBuilder.Ignore<QualitySettings>();

        // 忽略非实体类型（不实现IAggregateRoot或IEntity的类）
        modelBuilder.Ignore<ExecutionStepRecord>();
        modelBuilder.Ignore<ProviderType>();
        modelBuilder.Ignore<ConfigurationTemplate>();
        modelBuilder.Ignore<ErrorEventRecord>();

        // 全局删除行为配置
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // 配置默认的字符串长度
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
        {
            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(200); // 默认字符串长度
            }
        }
    }

    /// <summary>
    /// 应用实体特定配置
    /// </summary>
    protected virtual void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        // UserProfile 配置
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // UserMetadataEntry 配置
        modelBuilder.Entity<UserMetadataEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ValueType).HasMaxLength(200);
            entity.HasIndex(e => new { e.UserId, e.Key }).IsUnique();
        });

        // ModelParameterEntry 配置
        modelBuilder.Entity<ModelParameterEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(200);
            entity.HasIndex(e => new { e.ConfigurationId, e.Key }).IsUnique();
        });

        // QualityThresholdEntry 配置
        modelBuilder.Entity<QualityThresholdEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThresholdName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => new { e.ConfigurationId, e.ThresholdName }).IsUnique();
        });

        // ModelProvider 配置
        modelBuilder.Entity<ModelProvider>(entity =>
        {
            entity.HasKey(e => e.ProviderId);
            entity.Property(e => e.ProviderName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.ProviderName);

            // 复杂类型将在具体数据库实现中配置
            // entity.OwnsOne(e => e.DefaultApiConfiguration);
            // entity.OwnsOne(e => e.Status);
        });

        // Model 配置
        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(e => e.ModelId);
            entity.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ModelName).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.ProviderId, e.ModelName }).IsUnique();
        });

        // ModelUserConfiguration 配置
        modelBuilder.Entity<ModelUserConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigurationId);
            entity.HasIndex(e => new { e.UserId, e.ModelId, e.ProviderId }).IsUnique();
        });

        // ProviderUserConfiguration 配置
        modelBuilder.Entity<ProviderUserConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigurationId);
            entity.HasIndex(e => new { e.UserId, e.ProviderId }).IsUnique();
        });

        // UserPreferences 配置
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.PreferenceId);
            entity.Property(e => e.PreferenceCategory).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PreferenceKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(50);
            entity.HasIndex(e => new { e.UserId, e.PreferenceCategory, e.PreferenceKey }).IsUnique();
        });

        // Entry实体配置
        modelBuilder.Entity<ProviderCustomSettingEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId);
            entity.Property(e => e.SettingKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(200);
        });

        modelBuilder.Entity<MetricTagEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId);
            entity.Property(e => e.TagKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TagValue).HasMaxLength(500);
        });

        modelBuilder.Entity<MetricContextEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId);
            entity.Property(e => e.ContextKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(200);
        });

        // TaskExecutionHistory 配置
        modelBuilder.Entity<TaskExecutionHistory>(entity =>
        {
            entity.HasKey(e => e.ExecutionId);
            entity.Property(e => e.RequestType).HasMaxLength(200);
            entity.HasIndex(e => new { e.UserId, e.StartTime });

            // ExecutionStatus会在特定数据库配置中处理
        });

        // WorkflowTemplate 配置
        modelBuilder.Entity<WorkflowTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);
            entity.Property(e => e.TemplateName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.TemplateName);
        });

        // WorkflowTemplateStep 配置
        modelBuilder.Entity<WorkflowTemplateStep>(entity =>
        {
            entity.HasKey(e => e.StepId);
            entity.Property(e => e.StepName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StepType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RequiredCapability).HasMaxLength(100);
            entity.HasIndex(e => new { e.TemplateId, e.StepOrder }).IsUnique();
        });

        // MCPConfiguration 配置
        modelBuilder.Entity<MCPConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigurationId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.CreatedBy, e.Name });
        });

        // PerformanceMetricsRecord 配置
        modelBuilder.Entity<PerformanceMetricsRecord>(entity =>
        {
            entity.HasKey(e => e.MetricId);
            entity.HasIndex(e => new { e.MetricType, e.MetricTimestamp });
        });
    }

    /// <summary>
    /// 应用关系配置
    /// </summary>
    protected virtual void ApplyRelationshipConfigurations(ModelBuilder modelBuilder)
    {
        // UserProfile -> UserMetadataEntries (一对多)
        modelBuilder.Entity<UserMetadataEntry>()
            .HasOne(m => m.User)
            .WithMany(u => u.MetadataEntries)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ModelProvider -> Models (一对多)
        modelBuilder.Entity<Model>()
            .HasOne(m => m.Provider)
            .WithMany(p => p.Models)
            .HasForeignKey(m => m.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ModelProvider -> ProviderUserConfigurations (一对多)
        modelBuilder.Entity<ProviderUserConfiguration>()
            .HasOne(c => c.Provider)
            .WithMany(p => p.UserConfigurations)
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserProfile -> TaskExecutionHistories (一对多)
        modelBuilder.Entity<TaskExecutionHistory>()
            .HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserProfile -> MCPConfigurations (一对多)
        modelBuilder.Entity<MCPConfiguration>()
            .HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ProviderUserConfiguration -> ProviderCustomSettingEntries (一对多)
        modelBuilder.Entity<ProviderCustomSettingEntry>()
            .HasOne(e => e.Configuration)
            .WithMany(c => c.CustomSettingEntries)
            .HasForeignKey(e => e.ConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // PerformanceMetricsRecord -> MetricTagEntries (一对多)
        modelBuilder.Entity<MetricTagEntry>()
            .HasOne(e => e.Metric)
            .WithMany(m => m.TagEntries)
            .HasForeignKey(e => e.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        // PerformanceMetricsRecord -> MetricContextEntries (一对多)
        modelBuilder.Entity<MetricContextEntry>()
            .HasOne(e => e.Metric)
            .WithMany(m => m.ContextEntries)
            .HasForeignKey(e => e.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowTemplate -> WorkflowTemplateSteps (一对多)
        modelBuilder.Entity<WorkflowTemplateStep>()
            .HasOne(s => s.Template)
            .WithMany(t => t.TemplateSteps)
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// 应用索引和约束配置
    /// </summary>
    protected virtual void ApplyIndexesAndConstraints(ModelBuilder modelBuilder)
    {
        // 性能优化索引
        modelBuilder.Entity<TaskExecutionHistory>()
            .HasIndex(e => new { e.UserId, e.StartTime, e.ExecutionStatus });

        modelBuilder.Entity<PerformanceMetricsRecord>()
            .HasIndex(e => new { e.MetricType, e.MetricName, e.MetricTimestamp });

        modelBuilder.Entity<MCPConfiguration>()
            .HasIndex(e => new { e.CreatedBy, e.IsEnabled, e.Type });

        // Entry实体性能索引
        modelBuilder.Entity<MetricTagEntry>()
            .HasIndex(e => new { e.MetricId, e.TagKey });

        modelBuilder.Entity<MetricContextEntry>()
            .HasIndex(e => new { e.MetricId, e.ContextKey });

        modelBuilder.Entity<ProviderCustomSettingEntry>()
            .HasIndex(e => new { e.ConfigurationId, e.SettingKey });
    }

    #endregion

    #region 数据变更跟踪

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 在保存前更新审计字段
        UpdateAuditFields();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // 在保存前更新审计字段
        UpdateAuditFields();

        return base.SaveChanges();
    }

    /// <summary>
    /// 更新审计字段
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // 设置创建时间
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedTime"))
                {
                    entry.Property("CreatedTime").CurrentValue = DateTime.UtcNow;
                }
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                // 设置更新时间
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedTime"))
                {
                    entry.Property("UpdatedTime").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }

    #endregion

    #region 数据库健康检查

    /// <summary>
    /// 检查数据库连接状态
    /// </summary>
    public virtual async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取数据库版本信息
    /// </summary>
    public virtual async Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 子类可以重写此方法提供特定数据库的版本信息
            return await Task.FromResult("Database Version: Available");
        }
        catch
        {
            return "Version information unavailable";
        }
    }

    #endregion
}
