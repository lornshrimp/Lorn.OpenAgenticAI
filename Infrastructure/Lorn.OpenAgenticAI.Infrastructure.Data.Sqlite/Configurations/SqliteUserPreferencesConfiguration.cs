using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Configurations;

/// <summary>
/// SQLite特定的用户偏好配置
/// </summary>
public class SqliteUserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        // 主键和外键
        builder.HasKey(e => e.PreferenceId);
        builder.Property(e => e.PreferenceId)
            .HasConversion(g => g.ToString(), s => Guid.Parse(s));

        builder.Property(e => e.UserId)
            .HasConversion(g => g.ToString(), s => Guid.Parse(s));

        // 字段
        builder.Property(e => e.PreferenceCategory)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PreferenceKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PreferenceValue)
            .HasColumnType("TEXT");

        builder.Property(e => e.ValueType)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.LastUpdatedTime)
            .HasColumnType("TEXT");

        // 关系
        builder.HasOne(e => e.User)
            .WithMany(u => u.UserPreferences)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引与唯一约束
        builder.HasIndex(e => new { e.UserId, e.PreferenceCategory, e.PreferenceKey })
            .IsUnique()
            .HasDatabaseName("IX_SQLite_UserPreferences_User_Category_Key");

        builder.ToTable("UserPreferences");
    }
}
