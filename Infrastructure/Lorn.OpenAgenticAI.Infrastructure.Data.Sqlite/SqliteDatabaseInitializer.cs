using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations;
using Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.SeedData;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;
using System.Diagnostics;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite;

/// <summary>
/// SQLite数据库初始化服务实现
/// 负责数据库创建、迁移、版本检查和种子数据初始化
/// </summary>
public class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly SqliteDatabaseMigrator _migrator;
    private readonly SqliteSeedDataService _seedDataService;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;

    public SqliteDatabaseInitializer(
        OpenAgenticAIDbContext context,
        SqliteDatabaseMigrator migrator,
        SqliteSeedDataService seedDataService,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
        _seedDataService = seedDataService ?? throw new ArgumentNullException(nameof(seedDataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 初始化数据库，包括创建数据库、应用迁移和初始化种子数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化结果</returns>
    public async Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("开始初始化SQLite数据库...");
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        var databaseCreated = false;
        MigrationResult migrationResult = new();
        SeedDataResult seedDataResult = new();

        try
        {
            // 1. 检查并创建数据库
            if (!await DatabaseExistsAsync(cancellationToken))
            {
                _logger.LogInformation("数据库不存在，正在创建...");
                databaseCreated = await CreateDatabaseAsync(cancellationToken);

                if (!databaseCreated)
                {
                    return new DatabaseInitializationResult
                    {
                        Success = false,
                        Message = "数据库创建失败",
                        DatabaseCreated = false,
                        Duration = stopwatch.Elapsed
                    };
                }
            }

            // 2. 应用数据库迁移
            _logger.LogInformation("检查并应用数据库迁移...");
            migrationResult = await ApplyMigrationsAsync(cancellationToken);

            if (!migrationResult.Success)
            {
                return new DatabaseInitializationResult
                {
                    Success = false,
                    Message = $"数据库迁移失败: {migrationResult.Message}",
                    DatabaseCreated = databaseCreated,
                    MigrationResult = migrationResult,
                    Duration = stopwatch.Elapsed,
                    Exception = migrationResult.Exception
                };
            }

            // 3. 初始化种子数据
            _logger.LogInformation("初始化种子数据...");
            seedDataResult = await InitializeSeedDataAsync(force: false, cancellationToken);

            if (!seedDataResult.Success)
            {
                warnings.Add($"种子数据初始化部分失败: {seedDataResult.Message}");
                _logger.LogWarning("种子数据初始化失败，但不影响数据库正常使用");
            }

            // 4. 验证数据库完整性
            _logger.LogInformation("验证数据库完整性...");
            var validationResult = await ValidateDatabaseIntegrityAsync(cancellationToken);

            if (!validationResult.IsValid)
            {
                warnings.AddRange(validationResult.Issues);
                _logger.LogWarning("数据库完整性验证发现问题: {Issues}",
                    string.Join(", ", validationResult.Issues));
            }

            _logger.LogInformation("SQLite数据库初始化完成，耗时 {Duration}ms", stopwatch.ElapsedMilliseconds);

            return new DatabaseInitializationResult
            {
                Success = true,
                Message = "数据库初始化成功",
                DatabaseCreated = databaseCreated,
                MigrationResult = migrationResult,
                SeedDataResult = seedDataResult,
                Duration = stopwatch.Elapsed,
                Warnings = warnings
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("数据库初始化操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化过程中发生错误");
            return new DatabaseInitializationResult
            {
                Success = false,
                Message = $"数据库初始化失败: {ex.Message}",
                DatabaseCreated = databaseCreated,
                MigrationResult = migrationResult,
                SeedDataResult = seedDataResult,
                Duration = stopwatch.Elapsed,
                Warnings = warnings,
                Exception = ex
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 检查数据库是否存在
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据库是否存在</returns>
    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("检查数据库是否存在操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查数据库是否存在时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 创建数据库
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否创建成功</returns>
    public async Task<bool> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogInformation("正在创建SQLite数据库...");

            // 确保数据库文件所在目录存在
            await EnsureDatabaseDirectoryExistsAsync();

            // 检查数据库是否已存在
            var exists = await _context.Database.CanConnectAsync(cancellationToken);
            if (exists)
            {
                _logger.LogInformation("SQLite数据库已存在，无需创建");
                return true;
            }

            // 使用 MigrateAsync 创建数据库并应用迁移，而不是 EnsureCreatedAsync
            // EnsureCreatedAsync 会阻止迁移系统正常工作
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("SQLite数据库创建成功并应用了迁移");

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("创建SQLite数据库操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建SQLite数据库时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 应用数据库迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>迁移结果</returns>
    public async Task<MigrationResult> ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        return await _migrator.ApplyMigrationsAsync(cancellationToken);
    }

    /// <summary>
    /// 获取当前数据库版本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前数据库版本</returns>
    public async Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        return await _migrator.GetCurrentVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 获取可用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用迁移列表</returns>
    public async Task<IReadOnlyList<string>> GetAvailableMigrationsAsync(CancellationToken cancellationToken = default)
    {
        return await _migrator.GetAvailableMigrationsAsync(cancellationToken);
    }

    /// <summary>
    /// 获取已应用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已应用迁移列表</returns>
    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        return await _migrator.GetAppliedMigrationsAsync(cancellationToken);
    }

    /// <summary>
    /// 检查是否需要迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否需要迁移</returns>
    public async Task<bool> NeedsMigrationAsync(CancellationToken cancellationToken = default)
    {
        return await _migrator.NeedsMigrationAsync(cancellationToken);
    }

    /// <summary>
    /// 初始化种子数据
    /// </summary>
    /// <param name="force">是否强制重新初始化</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种子数据初始化结果</returns>
    public async Task<SeedDataResult> InitializeSeedDataAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        return await _seedDataService.InitializeSeedDataAsync(force, cancellationToken);
    }

    /// <summary>
    /// 验证数据库完整性
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    public async Task<DatabaseValidationResult> ValidateDatabaseIntegrityAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始验证SQLite数据库完整性...");
        var stopwatch = Stopwatch.StartNew();
        var issues = new List<string>();
        var recommendations = new List<string>();

        try
        {
            // 1. 基本连接测试
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                issues.Add("无法连接到数据库");
                recommendations.Add("检查数据库文件路径和权限");

                return new DatabaseValidationResult
                {
                    IsValid = false,
                    Message = "数据库连接失败",
                    Issues = issues,
                    Recommendations = recommendations,
                    Duration = stopwatch.Elapsed
                };
            }

            // 2. 迁移完整性检查
            var migrationValidation = await _migrator.ValidateMigrationIntegrityAsync(cancellationToken);
            if (!migrationValidation.IsValid)
            {
                issues.AddRange(migrationValidation.Issues);
                recommendations.AddRange(migrationValidation.Recommendations);
            }

            // 3. 数据库文件完整性检查
            await ValidateDatabaseFileIntegrityAsync(issues, recommendations, cancellationToken);

            // 4. 关键表存在性检查
            await ValidateEssentialTablesAsync(issues, recommendations, cancellationToken);

            var isValid = !issues.Any();
            var message = isValid ? "数据库完整性验证通过" : $"发现 {issues.Count} 个问题";

            _logger.LogInformation("SQLite数据库完整性验证完成，结果: {Result}，耗时: {Duration}ms",
                isValid ? "通过" : "失败",
                stopwatch.ElapsedMilliseconds);

            return new DatabaseValidationResult
            {
                IsValid = isValid,
                Message = message,
                Issues = issues,
                Recommendations = recommendations,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证数据库完整性时发生错误");
            return new DatabaseValidationResult
            {
                IsValid = false,
                Message = $"验证失败: {ex.Message}",
                Issues = new List<string> { ex.Message },
                Recommendations = new List<string> { "检查数据库连接和权限" },
                Duration = stopwatch.Elapsed
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 确保数据库文件目录存在
    /// </summary>
    /// <returns></returns>
    private Task EnsureDatabaseDirectoryExistsAsync()
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("数据库连接字符串为空");
                return Task.CompletedTask;
            }

            // 从连接字符串中提取数据库文件路径
            var dbFilePath = ExtractDatabaseFilePathFromConnectionString(connectionString);
            if (string.IsNullOrEmpty(dbFilePath))
            {
                _logger.LogWarning("无法从连接字符串中提取数据库文件路径");
                return Task.CompletedTask;
            }

            var directory = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("创建数据库目录: {Directory}", directory);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据库目录时发生错误");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 从连接字符串中提取数据库文件路径
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <returns>数据库文件路径</returns>
    private static string? ExtractDatabaseFilePathFromConnectionString(string connectionString)
    {
        // 简单的SQLite连接字符串解析
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLowerInvariant();
                if (key == "data source" || key == "datasource")
                {
                    return keyValue[1].Trim();
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 验证数据库文件完整性
    /// </summary>
    /// <param name="issues">问题列表</param>
    /// <param name="recommendations">建议列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ValidateDatabaseFileIntegrityAsync(
        List<string> issues,
        List<string> recommendations,
        CancellationToken cancellationToken)
    {
        try
        {
            // 执行SQLite的PRAGMA integrity_check命令
            var integrityCheckResults = await _context.Database
                .SqlQueryRaw<string>("PRAGMA integrity_check")
                .ToListAsync(cancellationToken);

            var integrityCheckResult = integrityCheckResults.FirstOrDefault();
            if (integrityCheckResult != "ok")
            {
                issues.Add($"数据库文件完整性检查失败: {integrityCheckResult}");
                recommendations.Add("考虑重建数据库或从备份恢复");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"无法执行数据库完整性检查: {ex.Message}");
            recommendations.Add("检查数据库文件权限和磁盘空间");
        }
    }

    /// <summary>
    /// 验证关键表的存在性
    /// </summary>
    /// <param name="issues">问题列表</param>
    /// <param name="recommendations">建议列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ValidateEssentialTablesAsync(
        List<string> issues,
        List<string> recommendations,
        CancellationToken cancellationToken)
    {
        var essentialTables = new[]
        {
            "UserProfiles",
            "UserPreferences",
            "TaskExecutionHistories",
            "ExecutionStepRecords"
        };

        foreach (var tableName in essentialTables)
        {
            try
            {
                var tableCountResults = await _context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name={0}", tableName)
                    .ToListAsync(cancellationToken);

                var tableExists = tableCountResults.FirstOrDefault();
                if (tableExists == 0)
                {
                    issues.Add($"关键表 {tableName} 不存在");
                    recommendations.Add("执行数据库迁移以创建缺失的表");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"检查表 {tableName} 时发生错误: {ex.Message}");
                recommendations.Add("检查数据库权限和结构");
            }
        }
    }
}
