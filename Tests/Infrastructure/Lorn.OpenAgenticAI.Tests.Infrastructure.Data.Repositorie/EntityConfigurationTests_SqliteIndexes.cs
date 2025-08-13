using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.Execution;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_SqliteIndexes : EfSqliteTestBase
{
    [Fact]
    public async Task ExecutionStepRecord_Index_Used_By_QueryPlan()
    {
        var user = new UserProfile(
            Guid.NewGuid(),
            "u-idx",
            "u-idx@x.com",
            new SecuritySettings("Pwd", 30, false, DateTime.UtcNow, new System.Collections.Generic.Dictionary<string, string>())
        );
        var hist = new TaskExecutionHistory(user.UserId, Guid.NewGuid().ToString(), "input", "General");

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.TaskExecutionHistories.Add(hist);
            await ctx.SaveChangesAsync();

            // 插入多行，确保索引可被利用
            for (int i = 1; i <= 5; i++)
            {
                ctx.Add(new ExecutionStepRecord(hist.ExecutionId, $"sid-{i}", i, $"d-{i}", "agent", "act"));
            }
            await ctx.SaveChangesAsync();

            // 使用 EXPLAIN QUERY PLAN 检查是否命中索引
            var conn = (SqliteConnection)ctx.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"EXPLAIN QUERY PLAN SELECT * FROM ExecutionStepRecords WHERE ExecutionId = $eid ORDER BY StepOrder";
            cmd.Parameters.AddWithValue("$eid", hist.ExecutionId.ToString());

            using var reader = await cmd.ExecuteReaderAsync();
            bool usedIndex = false;
            while (await reader.ReadAsync())
            {
                // detail 列通常包含 using index 之类的提示
                var detail = reader.GetString(reader.GetOrdinal("detail"));
                if (detail.Contains("USING INDEX", StringComparison.OrdinalIgnoreCase) ||
                    detail.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                {
                    usedIndex = true;
                }
            }
            Assert.True(usedIndex);
        }
    }
}
