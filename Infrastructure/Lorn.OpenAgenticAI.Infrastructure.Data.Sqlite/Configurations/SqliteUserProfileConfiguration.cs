using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的用户档案配置
/// </summary>
public class SqliteUserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // SQLite特定的配置

        // 主键配置
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId)
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str)
            );

        // 字符串长度限制（SQLite推荐）
        builder.Property(x => x.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        // 并发控制配置
        builder.Property(x => x.ProfileVersion)
            .IsConcurrencyToken();

        // 注意：Metadata 属性已经移除，现在通过 UserMetadataEntry 实体单独存储
        // 如果需要JSON存储，应该在 UserMetadataEntry 的配置中处理

        // 值对象配置 - SecuritySettings
        builder.OwnsOne(x => x.SecuritySettings, securityBuilder =>
        {
            securityBuilder.Property(s => s.AuthenticationMethod)
                .HasMaxLength(50)
                .IsRequired();

            securityBuilder.Property(s => s.SessionTimeoutMinutes)
                .HasDefaultValue(30);

            securityBuilder.Property(s => s.RequireTwoFactor)
                .HasDefaultValue(false);

            // 额外设置的JSON存储
            securityBuilder.Property(s => s.AdditionalSettings)
                .HasColumnType("TEXT")
                .HasConversion(
                    dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions)null!),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions)null!) ?? new Dictionary<string, string>()
                );
        });

        // SQLite特定索引
        builder.HasIndex(x => x.Username)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.CreatedTime);

        builder.HasIndex(x => x.IsActive);

        // SQLite不支持复杂的外键级联，使用限制级联
        builder.HasMany(x => x.UserPreferences)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ExecutionHistories)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.WorkflowTemplates)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 表名配置
        builder.ToTable("UserProfiles");
    }
}
