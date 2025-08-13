using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_SqliteJsonQuery : EfSqliteTestBase
{
    [Fact]
    public async Task ExecutionStepRecord_Json_CustomMetrics_Query()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u-json",
            "u-json@x.com",
            new SecuritySettings("Pwd", 30, false, DateTime.UtcNow, new System.Collections.Generic.Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "input", "General");
        var step = new ExecutionStepRecord(hist.ExecutionId, "sid-json", 1, "desc", "agent", "act");
        step.UpdateResourceUsage(new ResourceUsage(12.5, 1024, 256, 128, new System.Collections.Generic.Dictionary<string, double> { { "gpu", 0.8 } }));

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            ctx.Add(step);
            await ctx.SaveChangesAsync();

            var conn = (SqliteConnection)ctx.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(1) FROM ExecutionStepRecords WHERE json_extract(ResourceUsage_CustomMetrics, '$.gpu') IS NOT NULL";
            var count = (long?)await cmd.ExecuteScalarAsync();
            Assert.True(count.HasValue && count.Value == 1);
        }
    }
}
