using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 测试专用的数据库上下文适配器，包装独立的测试DbContext来兼容OpenAgenticAIDbContext接口
/// </summary>
public class TestDbContextAdapter : OpenAgenticAIDbContext
{
    private readonly TestDbContextCore _coreContext;

    public TestDbContextAdapter(DbContextOptions options) : base(options)
    {
        var coreOptions = new DbContextOptionsBuilder<TestDbContextCore>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _coreContext = new TestDbContextCore(coreOptions);
    }

    // 重写父类的DbSet属性，代理到内部测试上下文
    public new DbSet<UserProfile> UserProfiles => _coreContext.UserProfiles;
    public new DbSet<UserPreferences> UserPreferences => _coreContext.UserPreferences;
    public new DbSet<UserMetadataEntry> UserMetadataEntries => _coreContext.UserMetadataEntries;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 不调用基类配置，直接使用内部上下文的配置
        _coreContext.ConfigureModel(modelBuilder);
    }

    public override void Dispose()
    {
        _coreContext?.Dispose();
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_coreContext != null)
        {
            await _coreContext.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    // 重写必要的DbContext方法，代理到内部上下文
    public override int SaveChanges()
    {
        return _coreContext.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _coreContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// 核心测试DbContext，独立实现仅包含用户管理相关实体
/// </summary>
public class TestDbContextCore : DbContext
{
    public TestDbContextCore(DbContextOptions<TestDbContextCore> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<UserMetadataEntry> UserMetadataEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        // 配置用户档案
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);

            // 简化的安全设置映射
            entity.OwnsOne(u => u.SecuritySettings, settings =>
            {
                settings.Property(s => s.AuthenticationMethod).HasColumnName("AuthenticationMethod");
                settings.Property(s => s.SessionTimeoutMinutes).HasColumnName("SessionTimeoutMinutes");
                settings.Property(s => s.RequireTwoFactor).HasColumnName("RequireTwoFactor");
                settings.Property(s => s.PasswordLastChanged).HasColumnName("PasswordLastChanged");
            });
        });

        // 配置用户偏好
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(p => p.PreferenceId);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.PreferenceCategory).IsRequired().HasMaxLength(100);
            entity.Property(p => p.PreferenceKey).IsRequired().HasMaxLength(200);
            entity.Property(p => p.PreferenceValue).IsRequired();
            entity.HasIndex(p => new { p.UserId, p.PreferenceCategory, p.PreferenceKey }).IsUnique();
        });

        // 配置用户元数据
        modelBuilder.Entity<UserMetadataEntry>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.UserId).IsRequired();
            entity.Property(m => m.Key).IsRequired().HasMaxLength(200);
            entity.Property(m => m.ValueJson).IsRequired();
            entity.HasIndex(m => new { m.UserId, m.Key }).IsUnique();
        });
    }
}
