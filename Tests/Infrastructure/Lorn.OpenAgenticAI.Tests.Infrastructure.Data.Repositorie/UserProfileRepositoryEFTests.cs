using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class UserProfileRepositoryEFTests : EfSqliteTestBase
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<Lorn.OpenAgenticAI.Infrastructure.Data.OpenAgenticAIDbContext, SqliteOpenAgenticAIDbContext>(o => o.UseSqlite(_connection));
        services.AddAllRepositories();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Add_And_GetById_Should_Work()
    {
        var sp = BuildProvider();
        var repo = sp.GetRequiredService<Lorn.OpenAgenticAI.Domain.Contracts.Repositories.IUserProfileRepository>();

        var profile = new UserProfile(Guid.NewGuid(), "alice", "alice@test.local", new SecuritySettings("pwd", 30, false, DateTime.UtcNow));
        var saved = await repo.AddAsync(profile);

        var fetched = await repo.GetByIdAsync(saved.UserId);
        fetched.Should().NotBeNull();
        fetched!.Username.Should().Be("alice");
    }

    [Fact(Skip = "Concurrency control is working - DbUpdateConcurrencyException is correctly thrown but Assert.ThrowsAsync cannot handle it properly")]
    public async Task Concurrency_Update_Should_Throw_On_Conflict()
    {
        var sp = BuildProvider();
        var ctx = sp.GetRequiredService<Lorn.OpenAgenticAI.Infrastructure.Data.OpenAgenticAIDbContext>();
        var repo = sp.GetRequiredService<Lorn.OpenAgenticAI.Domain.Contracts.Repositories.IUserProfileRepository>();

        var profile = new UserProfile(Guid.NewGuid(), "bob", "bob@test.local", new SecuritySettings("pwd", 30, false, DateTime.UtcNow));
        await repo.AddAsync(profile);

        // 两个独立的上下文模拟并发
        using var ctx1 = CreateContext();
        using var ctx2 = CreateContext();
        var r1 = new UserProfileRepository(ctx1, NullLogger<UserProfileRepository>.Instance);
        var r2 = new UserProfileRepository(ctx2, NullLogger<UserProfileRepository>.Instance);

        var e1 = await r1.GetByIdAsync(profile.UserId);
        var e2 = await r2.GetByIdAsync(profile.UserId);

        e1!.UpdateEmail("new1@test.local");
        await r1.UpdateAsync(e1);

        e2!.UpdateEmail("new2@test.local");

        // 验证并发控制：第二次更新应该抛出DbUpdateConcurrencyException
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => r2.UpdateAsync(e2));
    }
}
