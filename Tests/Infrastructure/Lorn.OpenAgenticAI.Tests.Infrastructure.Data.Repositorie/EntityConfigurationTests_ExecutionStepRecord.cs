using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_ExecutionStepRecord : EfSqliteTestBase
{
    [Fact]
    public async Task ExecutionStepRecord_CascadeDelete_FromHistory()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u5",
            "u5@x.com",
            new SecuritySettings(
                authenticationMethod: "Pwd",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow,
                additionalSettings: new System.Collections.Generic.Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "input", "General");
        var step = new ExecutionStepRecord(hist.ExecutionId, "s1", 1, "desc", "agent-a", "act-a");

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            await ctx.SaveChangesAsync();
            ctx.Add(step);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var count = await ctx.Set<ExecutionStepRecord>().CountAsync();
            Assert.Equal(1, count);
            var histLoaded = await ctx.TaskExecutionHistories.FirstAsync();
            ctx.TaskExecutionHistories.Remove(histLoaded);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var count = await ctx.Set<ExecutionStepRecord>().CountAsync();
            Assert.Equal(0, count);
        }
    }

    [Fact]
    public async Task ExecutionStepRecord_OwnedResourceUsage_Mapped()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u6",
            "u6@x.com",
            new SecuritySettings(
                authenticationMethod: "Pwd",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow,
                additionalSettings: new System.Collections.Generic.Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "input", "General");
        var step = new ExecutionStepRecord(hist.ExecutionId, "s2", 2, "desc2", "agent-b", "act-b");
        step.UpdateResourceUsage(new ResourceUsage(12.5, 1024, 256, 128, new System.Collections.Generic.Dictionary<string, double> { { "gpu", 0.8 } }));

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            ctx.Add(step);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var loaded = await ctx.Set<ExecutionStepRecord>().FirstAsync(s => s.StepId == "s2");
            Assert.True(loaded.ResourceUsage.MemoryUsageBytes >= 1024);
            Assert.True(loaded.ResourceUsage.CustomMetrics.ContainsKey("gpu"));
        }
    }
}
