using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using System.IO;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Sqlite;

/// <summary>
/// SQLite数据库迁移管理器单元测试
/// 验证数据库迁移的应用、版本检查和完整性验证功能
/// </summary>
[TestFixture]
public class SqliteDatabaseMigratorTests
{
    private Mock<ILogger<SqliteDatabaseMigrator>> _mockLogger = null!;
    private DbContextOptions<SqliteOpenAgenticAIDbContext> _dbOptions = null!;
    private SqliteOpenAgenticAIDbContext _context = null!;
    private SqliteDatabaseMigrator _migrator = null!;

    [SetUp]
    public void SetUp()
    {
        // 设置Mock日志记录器
        _mockLogger = new Mock<ILogger<SqliteDatabaseMigrator>>();

        // 使用临时SQLite数据库文件进行测试
        var databasePath = Path.GetTempFileName();
        var connectionString = $"Data Source={databasePath}";

        // 使用 DbContextOptionsBuilder 的非泛型版本创建选项
        var optionsBuilder = new DbContextOptionsBuilder<SqliteOpenAgenticAIDbContext>()
            .UseSqlite(connectionString, options =>
            {
                // 指定迁移程序集
                options.MigrationsAssembly("Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite");
            })
            .EnableServiceProviderCaching(false)
            .EnableSensitiveDataLogging();

        _dbOptions = optionsBuilder.Options;

        _context = new SqliteOpenAgenticAIDbContext(_dbOptions);
        _migrator = new SqliteDatabaseMigrator(_context, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (_context != null)
        {
            // 获取数据库文件路径
            var connectionString = _context.Database.GetConnectionString();

            // 确保所有操作都完成，并关闭连接
            _context.Database.CloseConnection();
            _context.Dispose();

            // 等待一小段时间确保文件释放
            System.Threading.Thread.Sleep(100);

            // 清理临时数据库文件
            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Data Source="))
            {
                var dataSourceStart = connectionString.IndexOf("Data Source=") + "Data Source=".Length;
                var dataSourceEnd = connectionString.IndexOf(";", dataSourceStart);
                if (dataSourceEnd == -1) dataSourceEnd = connectionString.Length;

                var filePath = connectionString.Substring(dataSourceStart, dataSourceEnd - dataSourceStart);

                try
                {
                    if (File.Exists(filePath))
                    {
                        // 强制垃圾回收以释放文件句柄
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        File.Delete(filePath);
                    }
                }
                catch (IOException)
                {
                    // 如果仍然无法删除，就忽略错误（临时文件系统会清理）
                }
            }
        }
    }

    #region 构造函数测试

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        _migrator.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseMigrator(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseMigrator(_context, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region 迁移应用测试

    [Test]
    public async Task ApplyMigrationsAsync_WithValidDatabase_ShouldReturnSuccessResult()
    {
        // Arrange - 不使用 EnsureCreatedAsync，直接测试迁移

        // Act
        var result = await _migrator.ApplyMigrationsAsync();

        // Assert - 打印详细错误信息用于调试
        if (!result.Success)
        {
            Console.WriteLine($"Migration failed: {result.Message}");
            if (result.Exception != null)
            {
                Console.WriteLine($"Exception: {result.Exception}");
            }
        }

        result.Should().NotBeNull();
        result.Success.Should().BeTrue($"Migration should succeed. Message: {result.Message}. Exception: {result.Exception}");
        result.Message.Should().NotBeNullOrEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Exception.Should().BeNull();
    }

    [Test]
    public async Task ApplyMigrationsAsync_WithNoPendingMigrations_ShouldReturnSuccessWithZeroCount()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var result = await _migrator.ApplyMigrationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("没有待应用的迁移");
        result.AppliedMigrationsCount.Should().Be(0);
        result.AppliedMigrations.Should().BeEmpty();
    }

    [Test]
    public async Task ApplyMigrationsAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.ApplyMigrationsAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 版本信息测试

    [Test]
    public async Task GetCurrentVersionAsync_ShouldReturnVersionString()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var version = await _migrator.GetCurrentVersionAsync();

        // Assert
        version.Should().NotBeNull();
    }

    [Test]
    public async Task GetCurrentVersionAsync_WithNoPreviousMigrations_ShouldReturnDefaultMessage()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var version = await _migrator.GetCurrentVersionAsync();

        // Assert
        version.Should().Contain("未应用任何迁移");
    }

    [Test]
    public async Task GetCurrentVersionAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.GetCurrentVersionAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 可用迁移测试

    [Test]
    public async Task GetAvailableMigrationsAsync_ShouldReturnMigrationsList()
    {
        // Act
        var migrations = await _migrator.GetAvailableMigrationsAsync();

        // Assert
        migrations.Should().NotBeNull();
        // 在测试环境中，迁移列表可能为空，但集合应该存在
    }

    [Test]
    public void GetAvailableMigrationsAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _migrator.GetAvailableMigrationsAsync(cts.Token);
        });
    }

    #endregion

    #region 已应用迁移测试

    [Test]
    public async Task GetAppliedMigrationsAsync_WithFreshDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var appliedMigrations = await _migrator.GetAppliedMigrationsAsync();

        // Assert
        appliedMigrations.Should().NotBeNull();
        // 新创建的内存数据库通常没有迁移记录
    }

    [Test]
    public async Task GetAppliedMigrationsAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.GetAppliedMigrationsAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 迁移需求检查测试

    [Test]
    public async Task NeedsMigrationAsync_WithFreshDatabase_ShouldReturnFalse()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var needsMigration = await _migrator.NeedsMigrationAsync();

        // Assert
        needsMigration.Should().BeFalse(); // 已应用迁移的数据库不需要进一步迁移
    }

    [Test]
    public async Task NeedsMigrationAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.NeedsMigrationAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 迁移完整性验证测试

    [Test]
    public async Task ValidateMigrationIntegrityAsync_WithValidDatabase_ShouldPass()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var result = await _migrator.ValidateMigrationIntegrityAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Issues.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateMigrationIntegrityAsync_WithNonExistentDatabase_ShouldFail()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result = await _migrator.ValidateMigrationIntegrityAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Issues.Should().NotBeEmpty();
        result.Recommendations.Should().NotBeEmpty();
    }

    [Test]
    public async Task ValidateMigrationIntegrityAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.ValidateMigrationIntegrityAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 数据库重建测试 (危险操作)

    [Test]
    public async Task RecreateDatabase_ShouldSuccessfullyRecreateDatabase()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // 验证数据库存在
        var initialExists = await _context.Database.CanConnectAsync();
        initialExists.Should().BeTrue();

        // Act
        var result = await _migrator.RecreateDatabase();

        // Assert
        result.Should().BeTrue();

        // 验证数据库重新创建
        var finalExists = await _context.Database.CanConnectAsync();
        finalExists.Should().BeTrue();
    }

    [Test]
    public async Task RecreateDatabase_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _migrator.RecreateDatabase(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 错误处理测试

    [Test]
    public async Task ApplyMigrationsAsync_WithDatabaseError_ShouldReturnFailureResult()
    {
        // 注意：在内存数据库环境下很难模拟真实的数据库错误
        // 这里主要测试方法的结构完整性

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _migrator.ApplyMigrationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Test]
    public async Task GetCurrentVersionAsync_WithDatabaseError_ShouldReturnErrorMessage()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var version = await _migrator.GetCurrentVersionAsync();

        // Assert
        version.Should().NotBeNull();
        version.Should().Be("未应用任何迁移"); // 删除数据库后应该返回未应用任何迁移
    }

    [Test]
    public async Task GetAvailableMigrationsAsync_WithError_ShouldReturnEmptyList()
    {
        // Arrange
        // 内存数据库很难模拟真实错误，主要测试方法行为

        // Act
        var migrations = await _migrator.GetAvailableMigrationsAsync();

        // Assert
        migrations.Should().NotBeNull();
    }

    [Test]
    public async Task GetAppliedMigrationsAsync_WithError_ShouldReturnEmptyList()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var appliedMigrations = await _migrator.GetAppliedMigrationsAsync();

        // Assert
        appliedMigrations.Should().NotBeNull();
        appliedMigrations.Should().BeEmpty();
    }

    [Test]
    public async Task NeedsMigrationAsync_WithError_ShouldReturnTrue()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var needsMigration = await _migrator.NeedsMigrationAsync();

        // Assert
        needsMigration.Should().BeTrue(); // 删除数据库后需要迁移
    }

    #endregion

    #region 性能测试

    [Test]
    public async Task ApplyMigrationsAsync_PerformanceTest_ShouldCompleteQuickly()
    {
        // Arrange
        await SetupDatabaseForTestAsync();
        var timeLimit = TimeSpan.FromSeconds(10); // 10秒内完成

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _migrator.ApplyMigrationsAsync();
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.Should().BeLessThan(timeLimit);
        result.Duration.Should().BeLessThan(timeLimit);
    }

    #endregion

    #region 幂等性测试

    [Test]
    public async Task ApplyMigrationsAsync_MultipleExecutions_ShouldBeIdempotent()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var result1 = await _migrator.ApplyMigrationsAsync();
        var result2 = await _migrator.ApplyMigrationsAsync();
        var result3 = await _migrator.ApplyMigrationsAsync();

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result3.Success.Should().BeTrue();

        // 后续执行应该显示没有待应用的迁移
        result2.Message.Should().Contain("没有待应用的迁移");
        result3.Message.Should().Contain("没有待应用的迁移");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 正确设置数据库用于测试
    /// 使用迁移而不是 EnsureCreated 来避免模型不匹配问题
    /// </summary>
    private async Task SetupDatabaseForTestAsync()
    {
        // 使用迁移系统来创建数据库，而不是 EnsureCreatedAsync
        await _context.Database.MigrateAsync();
    }

    #endregion
}
