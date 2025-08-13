using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_UniqueConstraints : EfSqliteTestBase
{
    [Fact]
    public async Task ExecutionStepRecord_Unique_ExecutionId_StepOrder()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u-uniq",
            "u-uniq@x.com",
            new SecuritySettings("Pwd", 30, false, DateTime.UtcNow, new System.Collections.Generic.Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "input", "General");

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            await ctx.SaveChangesAsync();

            ctx.Add(new ExecutionStepRecord(hist.ExecutionId, "sid", 1, "desc", "agent", "act"));
            await ctx.SaveChangesAsync();

            ctx.Add(new ExecutionStepRecord(hist.ExecutionId, "sid2", 1, "desc2", "agent", "act"));
            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        }
    }
}
