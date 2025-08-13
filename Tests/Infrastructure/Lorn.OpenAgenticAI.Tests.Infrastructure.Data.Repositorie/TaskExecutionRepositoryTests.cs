using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class TaskExecutionRepositoryTests : EfSqliteTestBase
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
    public async Task Add_And_QuerySteps_Should_Work()
    {
        var sp = BuildProvider();
        var ctx = sp.GetRequiredService<Lorn.OpenAgenticAI.Infrastructure.Data.OpenAgenticAIDbContext>();
        var userId = Guid.NewGuid();

        // 种子：插入一个执行记录和两条步骤
        // 先插入关联的用户，满足外键
        var profile = new UserProfile(userId, "tester", "tester@test.local", new SecuritySettings("pwd", 30, false, DateTime.UtcNow));
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var exec = new TaskExecutionHistory(userId, "req-1", "ask", "unit-test");
        ctx.TaskExecutionHistories.Add(exec);
        await ctx.SaveChangesAsync();

        ctx.ExecutionStepRecords.Add(new ExecutionStepRecord(exec.ExecutionId, "s1", 0, "d1", "agent", "act"));
        ctx.ExecutionStepRecords.Add(new ExecutionStepRecord(exec.ExecutionId, "s2", 1, "d2", "agent", "act"));
        await ctx.SaveChangesAsync();

        var repo = sp.GetRequiredService<Lorn.OpenAgenticAI.Domain.Contracts.Repositories.ITaskExecutionRepository>();
        var byReq = await repo.GetByRequestIdAsync("req-1");
        byReq.Should().NotBeNull();

        var steps = await repo.ListStepsAsync(exec.ExecutionId);
        steps.Should().HaveCount(2);
        steps.Select(s => s.StepId).Should().ContainInOrder(new[] { "s1", "s2" });
    }
}
