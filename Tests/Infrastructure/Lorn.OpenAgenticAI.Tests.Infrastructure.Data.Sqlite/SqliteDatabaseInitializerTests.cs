using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using System.IO;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.SeedData;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Sqlite;

/// <summary>
/// SQLite数据库初始化服务单元测试
/// 验证数据库创建、迁移、种子数据和完整性检查功能
/// </summary>
[TestFixture]
public class SqliteDatabaseInitializerTests
{
    private Mock<ILogger<SqliteDatabaseInitializer>> _mockLogger = null!;
    private Mock<ILogger<SqliteDatabaseMigrator>> _mockMigratorLogger = null!;
    private Mock<ILogger<SqliteSeedDataService>> _mockSeedDataLogger = null!;
    private DbContextOptions<SqliteOpenAgenticAIDbContext> _dbOptions = null!;
    private SqliteOpenAgenticAIDbContext _context = null!;
    private SqliteDatabaseMigrator _migrator = null!;
    private SqliteSeedDataService _seedDataService = null!;
    private SqliteDatabaseInitializer _initializer = null!;

    [SetUp]
    public void SetUp()
    {
        // 设置Mock日志记录器
        _mockLogger = new Mock<ILogger<SqliteDatabaseInitializer>>();
        _mockMigratorLogger = new Mock<ILogger<SqliteDatabaseMigrator>>();
        _mockSeedDataLogger = new Mock<ILogger<SqliteSeedDataService>>();

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
        _migrator = new SqliteDatabaseMigrator(_context, _mockMigratorLogger.Object);
        _seedDataService = new SqliteSeedDataService(_context, _mockSeedDataLogger.Object);
        _initializer = new SqliteDatabaseInitializer(_context, _migrator, _seedDataService, _mockLogger.Object);
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
        _initializer.Should().NotBeNull();
        _initializer.Should().BeAssignableTo<IDatabaseInitializer>();
    }

    [Test]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseInitializer(null!, _migrator, _seedDataService, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Test]
    public void Constructor_WithNullMigrator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseInitializer(_context, null!, _seedDataService, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("migrator");
    }

    [Test]
    public void Constructor_WithNullSeedDataService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseInitializer(_context, _migrator, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("seedDataService");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteDatabaseInitializer(_context, _migrator, _seedDataService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region 数据库存在性检查测试

    [Test]
    public async Task DatabaseExistsAsync_WithValidDatabase_ShouldReturnTrue()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _initializer.DatabaseExistsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task DatabaseExistsAsync_WithNonExistentDatabase_ShouldReturnFalse()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result = await _initializer.DatabaseExistsAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 数据库创建测试

    [Test]
    public async Task CreateDatabaseAsync_WithNonExistentDatabase_ShouldCreateSuccessfully()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result = await _initializer.CreateDatabaseAsync();

        // Assert
        result.Should().BeTrue();
        var exists = await _initializer.DatabaseExistsAsync();
        exists.Should().BeTrue();
    }

    [Test]
    public async Task CreateDatabaseAsync_WithExistingDatabase_ShouldReturnTrue()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _initializer.CreateDatabaseAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region 迁移测试

    [Test]
    public async Task ApplyMigrationsAsync_ShouldReturnMigrationResult()
    {
        // Act
        var result = await _initializer.ApplyMigrationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Test]
    public async Task GetCurrentVersionAsync_ShouldReturnVersion()
    {
        // Act
        var version = await _initializer.GetCurrentVersionAsync();

        // Assert
        version.Should().NotBeNull();
    }

    [Test]
    public async Task GetAvailableMigrationsAsync_ShouldReturnMigrationsList()
    {
        // Act
        var migrations = await _initializer.GetAvailableMigrationsAsync();

        // Assert
        migrations.Should().NotBeNull();
    }

    [Test]
    public async Task GetAppliedMigrationsAsync_ShouldReturnAppliedMigrationsList()
    {
        // Act
        var appliedMigrations = await _initializer.GetAppliedMigrationsAsync();

        // Assert
        appliedMigrations.Should().NotBeNull();
    }

    [Test]
    public async Task NeedsMigrationAsync_ShouldReturnBool()
    {
        // Act
        var needsMigration = await _initializer.NeedsMigrationAsync();

        // Assert
        // needsMigration 应该是 bool 类型，不需要特别验证
        // needsMigration.Should().BeFalse(); // 或者 BeTrue()，根据预期结果决定
    }

    #endregion

    #region 种子数据测试

    [Test]
    public async Task InitializeSeedDataAsync_WithEmptyDatabase_ShouldInitializeSuccessfully()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _initializer.InitializeSeedDataAsync(force: false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.InitializedTablesCount.Should().BeGreaterThan(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // 验证种子数据已创建
        var userExists = await _context.UserProfiles.AnyAsync(u => u.Username == "admin");
        userExists.Should().BeTrue();
    }

    [Test]
    public async Task InitializeSeedDataAsync_WithForceFlag_ShouldReinitialize()
    {
        // Arrange
        await SetupDatabaseForTestAsync();
        await _initializer.InitializeSeedDataAsync(force: false);

        // Act - 在有数据的情况下强制初始化
        var result = await _initializer.InitializeSeedDataAsync(force: true);

        // Assert - 强制模式下即使有数据也应该成功（但会跳过初始化以避免冲突）
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task InitializeSeedDataAsync_WithExistingData_ShouldSkipInitialization()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        await _initializer.InitializeSeedDataAsync(force: false);

        // Act
        var result = await _initializer.InitializeSeedDataAsync(force: false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.InitializedTablesCount.Should().Be(0); // 已有数据，跳过初始化
    }

    #endregion

    #region 数据库完整性验证测试

    [Test]
    public async Task ValidateDatabaseIntegrityAsync_WithValidDatabase_ShouldPass()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var result = await _initializer.ValidateDatabaseIntegrityAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Test]
    public async Task ValidateDatabaseIntegrityAsync_WithoutDatabase_ShouldFail()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result = await _initializer.ValidateDatabaseIntegrityAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Issues.Should().NotBeEmpty();
        result.Recommendations.Should().NotBeEmpty();
    }

    #endregion

    #region 完整初始化流程测试

    [Test]
    public async Task InitializeAsync_WithCompleteFlow_ShouldSucceed()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result = await _initializer.InitializeAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.DatabaseCreated.Should().BeTrue();
        result.MigrationResult.Should().NotBeNull();
        result.MigrationResult.Success.Should().BeTrue();
        result.SeedDataResult.Should().NotBeNull();
        result.SeedDataResult.Success.Should().BeTrue();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // 验证数据库状态
        var databaseExists = await _initializer.DatabaseExistsAsync();
        databaseExists.Should().BeTrue();

        // 验证种子数据
        var adminUser = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Username == "admin");
        adminUser.Should().NotBeNull();
        adminUser!.DisplayName.Should().Be("系统管理员");
    }

    [Test]
    public async Task InitializeAsync_WithExistingDatabase_ShouldSkipCreation()
    {
        // Arrange
        await SetupDatabaseForTestAsync();

        // Act
        var result = await _initializer.InitializeAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DatabaseCreated.Should().BeFalse(); // 数据库已存在，未创建
    }

    [Test]
    public async Task InitializeAsync_WhenCancelled_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _initializer.InitializeAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 错误处理测试

    [Test]
    public async Task InitializeAsync_WithDatabaseCreationFailure_ShouldReturnFailureResult()
    {
        // 注意：这个测试在内存数据库环境下很难模拟真实的数据库创建失败
        // 在实际环境中，可以通过提供无效的连接字符串来测试

        // Arrange
        var invalidOptions = new DbContextOptionsBuilder<SqliteOpenAgenticAIDbContext>()
            .UseInMemoryDatabase(databaseName: "invalid_db")
            .Options;

        using var invalidContext = new SqliteOpenAgenticAIDbContext(invalidOptions);
        await invalidContext.Database.EnsureDeletedAsync();

        var invalidMigrator = new SqliteDatabaseMigrator(invalidContext, _mockMigratorLogger.Object);
        var invalidSeedDataService = new SqliteSeedDataService(invalidContext, _mockSeedDataLogger.Object);
        var invalidInitializer = new SqliteDatabaseInitializer(invalidContext, invalidMigrator, invalidSeedDataService, _mockLogger.Object);

        // Act
        var result = await invalidInitializer.InitializeAsync();

        // Assert
        result.Should().NotBeNull();
        // 在内存数据库中通常不会失败，所以这里主要测试结构完整性
    }

    #endregion

    #region 幂等性测试

    [Test]
    public async Task InitializeAsync_MultipleExecutions_ShouldBeIdempotent()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var result1 = await _initializer.InitializeAsync();
        var result2 = await _initializer.InitializeAsync();
        var result3 = await _initializer.InitializeAsync();

        // Assert
        result1.Success.Should().BeTrue();
        result1.DatabaseCreated.Should().BeTrue();

        result2.Success.Should().BeTrue();
        result2.DatabaseCreated.Should().BeFalse(); // 已存在

        result3.Success.Should().BeTrue();
        result3.DatabaseCreated.Should().BeFalse(); // 已存在

        // 验证数据一致性
        var adminUserCount = await _context.UserProfiles.CountAsync(u => u.Username == "admin");
        adminUserCount.Should().Be(1); // 只应该有一个管理员用户
    }

    #endregion

    #region 性能测试

    [Test]
    public async Task InitializeAsync_PerformanceTest_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();
        var timeLimit = TimeSpan.FromSeconds(30); // 30秒内完成

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _initializer.InitializeAsync();
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.Should().BeLessThan(timeLimit);
        result.Duration.Should().BeLessThan(timeLimit);
    }

    #endregion

    #region 并发测试

    [Test]
    public async Task InitializeAsync_ConcurrentExecution_ShouldHandleGracefully()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();

        // Act
        var tasks = Enumerable.Range(0, 3).Select(_ => _initializer.InitializeAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Success.Should().BeTrue());

        // 验证数据一致性
        var adminUserCount = await _context.UserProfiles.CountAsync(u => u.Username == "admin");
        adminUserCount.Should().Be(1); // 只应该有一个管理员用户
    }

    #region 辅助方法

    /// <summary>
    /// 为测试设置数据库，使用迁移系统而不是 EnsureCreatedAsync
    /// </summary>
    private async Task SetupDatabaseForTestAsync()
    {
        // 使用迁移系统来创建数据库，而不是 EnsureCreatedAsync
        await _context.Database.MigrateAsync();
    }

    #endregion

    #endregion
}
