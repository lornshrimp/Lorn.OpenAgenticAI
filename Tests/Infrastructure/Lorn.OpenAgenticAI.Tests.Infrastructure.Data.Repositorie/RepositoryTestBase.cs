using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 仓储测试基类，提供测试基础设施
/// </summary>
public abstract class RepositoryTestBase : IDisposable
{
    protected OpenAgenticAIDbContext DbContext { get; private set; }

    protected RepositoryTestBase()
    {
        // 创建内存数据库上下文适配器
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new TestDbContextAdapter(options);

        // 确保数据库被创建
        DbContext.Database.EnsureCreated();
    }

    protected Mock<ILogger<T>> GetMockLogger<T>() => new Mock<ILogger<T>>();

    protected static SecuritySettings CreateTestSecuritySettings()
    {
        return new SecuritySettings(
            authenticationMethod: "Standard",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );
    }

    protected static UserProfile CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        return new UserProfile(
            Guid.NewGuid(),
            username,
            email,
            CreateTestSecuritySettings()
        );
    }

    protected static UserPreferences CreateTestUserPreference(
        Guid? userId = null,
        string category = "UI",
        string key = "theme",
        string value = "dark",
        string valueType = "String")
    {
        return new UserPreferences(
            userId ?? Guid.NewGuid(),
            category,
            key,
            value,
            valueType
        );
    }

    protected static UserMetadataEntry CreateTestUserMetadata(
        Guid? userId = null,
        string key = "test_key",
        string value = "test_value",
        string category = "")
    {
        return new UserMetadataEntry(
            userId ?? Guid.NewGuid(),
            key,
            value,
            category
        );
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
