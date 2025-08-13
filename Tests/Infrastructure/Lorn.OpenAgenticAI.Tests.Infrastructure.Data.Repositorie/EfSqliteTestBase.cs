using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public abstract class EfSqliteTestBase : IDisposable
{
    protected readonly DbConnection _connection;
    protected readonly DbContextOptions<SqliteOpenAgenticAIDbContext> _options;

    protected EfSqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqliteOpenAgenticAIDbContext>()
                .UseSqlite(_connection)
                .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    protected SqliteOpenAgenticAIDbContext CreateContext()
        => new SqliteOpenAgenticAIDbContext(_options);

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
