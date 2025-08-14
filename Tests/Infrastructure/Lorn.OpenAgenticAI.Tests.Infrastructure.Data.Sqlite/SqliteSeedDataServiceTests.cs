using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.SeedData;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Sqlite;

/// <summary>
/// SqliteSeedDataService单元测试
/// </summary>
[TestFixture]
public class SqliteSeedDataServiceTests
{
    private SqliteOpenAgenticAIDbContext _context = null!;
    private SqliteSeedDataService _seedDataService = null!;
    private Mock<ILogger<SqliteSeedDataService>> _mockLogger = null!;
    private DbContextOptions<SqliteOpenAgenticAIDbContext> _dbOptions = null!;

    [SetUp]
    public void SetUp()
    {
        // 设置Mock日志记录器
        _mockLogger = new Mock<ILogger<SqliteSeedDataService>>();

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
        _seedDataService = new SqliteSeedDataService(_context, _mockLogger.Object);
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
        _seedDataService.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteSeedDataService(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteSeedDataService(_context, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region 种子数据初始化测试

    [Test]
    public async Task InitializeSeedDataAsync_WithEmptyDatabase_ShouldSucceed()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _seedDataService.InitializeSeedDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ClearSeedDataAsync_ShouldSucceed()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _seedDataService.ClearSeedDataAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
