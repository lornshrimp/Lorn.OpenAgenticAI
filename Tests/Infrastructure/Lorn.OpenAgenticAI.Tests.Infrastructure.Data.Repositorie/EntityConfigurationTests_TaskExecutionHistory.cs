using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_TaskExecutionHistory : EfSqliteTestBase
{
    [Fact]
    public async Task TaskExecutionHistory_JsonCollections_Roundtrip()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u3",
            "u3@x.com",
            new SecuritySettings(
                authenticationMethod: "Pwd",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow,
                additionalSettings: new Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "hello", "General",
            tags: new List<string> { "t1", "t2" },
            metadata: new Dictionary<string, object> { { "k1", 123 }, { "k2", "v2" } });

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var loaded = await ctx.TaskExecutionHistories.FirstAsync();
            Assert.Equal(2, loaded.Tags.Count);
            Assert.Equal("v2", loaded.Metadata["k2"].ToString());
        }
    }

    [Fact]
    public async Task TaskExecutionHistory_Relation_WithUser()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u4",
            "u4@x.com",
            new SecuritySettings(
                authenticationMethod: "Pwd",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow,
                additionalSettings: new Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "hi", "General");

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var reloaded = await ctx.TaskExecutionHistories.Include(h => h.User).FirstAsync();
            Assert.Equal(user.UserId, reloaded.UserId);
            Assert.NotNull(reloaded.User);
        }
    }
}
