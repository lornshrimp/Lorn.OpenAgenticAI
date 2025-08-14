using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;
using System.Diagnostics;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations;

/// <summary>
/// SQLite数据库迁移管理器
/// 负责数据库迁移的检查、应用和版本管理
/// </summary>
public class SqliteDatabaseMigrator
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<SqliteDatabaseMigrator> _logger;

    public SqliteDatabaseMigrator(OpenAgenticAIDbContext context, ILogger<SqliteDatabaseMigrator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 应用数据库迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>迁移结果</returns>
    public async Task<MigrationResult> ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("开始应用数据库迁移...");
        var stopwatch = Stopwatch.StartNew();
        var appliedMigrations = new List<string>();

        try
        {
            // 获取待应用的迁移
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingMigrationsList = pendingMigrations.ToList();

            if (!pendingMigrationsList.Any())
            {
                _logger.LogInformation("没有待应用的迁移");
                return new MigrationResult
                {
                    Success = true,
                    Message = "没有待应用的迁移",
                    AppliedMigrationsCount = 0,
                    AppliedMigrations = new List<string>(),
                    Duration = stopwatch.Elapsed
                };
            }

            _logger.LogInformation("发现 {Count} 个待应用的迁移: {Migrations}",
                pendingMigrationsList.Count,
                string.Join(", ", pendingMigrationsList));

            // 应用迁移
            await _context.Database.MigrateAsync(cancellationToken);
            appliedMigrations.AddRange(pendingMigrationsList);

            _logger.LogInformation("成功应用 {Count} 个迁移，耗时 {Duration}ms",
                appliedMigrations.Count,
                stopwatch.ElapsedMilliseconds);

            return new MigrationResult
            {
                Success = true,
                Message = $"成功应用 {appliedMigrations.Count} 个迁移",
                AppliedMigrationsCount = appliedMigrations.Count,
                AppliedMigrations = appliedMigrations,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("数据库迁移操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用数据库迁移时发生错误");
            return new MigrationResult
            {
                Success = false,
                Message = $"迁移失败: {ex.Message}",
                AppliedMigrationsCount = appliedMigrations.Count,
                AppliedMigrations = appliedMigrations,
                Duration = stopwatch.Elapsed,
                Exception = ex
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 获取当前数据库版本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前数据库版本</returns>
    public async Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
            return appliedMigrations.LastOrDefault() ?? "未应用任何迁移";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("获取当前数据库版本操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前数据库版本时发生错误");
            return "版本获取失败";
        }
    }

    /// <summary>
    /// 获取可用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用迁移列表</returns>
    public Task<IReadOnlyList<string>> GetAvailableMigrationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var allMigrations = _context.Database.GetMigrations();
            return Task.FromResult<IReadOnlyList<string>>(allMigrations.ToList());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("获取可用迁移列表操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用迁移列表时发生错误");
            return Task.FromResult<IReadOnlyList<string>>(new List<string>());
        }
    }

    /// <summary>
    /// 获取已应用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已应用迁移列表</returns>
    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            return appliedMigrations.ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("获取已应用迁移列表操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已应用迁移列表时发生错误");
            return new List<string>();
        }
    }

    /// <summary>
    /// 检查是否需要迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否需要迁移</returns>
    public async Task<bool> NeedsMigrationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            return pendingMigrations.Any();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("检查是否需要迁移操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查是否需要迁移时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 验证数据库迁移完整性
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    public async Task<DatabaseValidationResult> ValidateMigrationIntegrityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("开始验证数据库迁移完整性...");
        var stopwatch = Stopwatch.StartNew();
        var issues = new List<string>();
        var recommendations = new List<string>();

        try
        {
            // 检查是否存在待应用的迁移
            var needsMigration = await NeedsMigrationAsync(cancellationToken);
            if (needsMigration)
            {
                issues.Add("存在待应用的迁移");
                recommendations.Add("执行数据库迁移");
            }

            // 检查数据库是否可以连接
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                issues.Add("无法连接到数据库");
                recommendations.Add("检查数据库连接字符串和数据库服务状态");
            }

            // 检查数据库表是否存在
            if (canConnect)
            {
                try
                {
                    var tableCountResults = await _context.Database.SqlQueryRaw<int>(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
                        .ToListAsync(cancellationToken);

                    var tableCount = tableCountResults.FirstOrDefault();
                    if (tableCount == 0)
                    {
                        issues.Add("数据库中没有应用表");
                        recommendations.Add("执行数据库迁移以创建必要的表");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"检查数据库表时发生错误: {ex.Message}");
                    recommendations.Add("检查数据库权限和完整性");
                }
            }

            var isValid = !issues.Any();
            var message = isValid ? "数据库迁移完整性验证通过" : $"发现 {issues.Count} 个问题";

            _logger.LogInformation("数据库迁移完整性验证完成，结果: {Result}，耗时: {Duration}ms",
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
        catch (OperationCanceledException)
        {
            _logger.LogWarning("验证数据库迁移完整性操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证数据库迁移完整性时发生错误");
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
    /// 强制重建数据库（危险操作，仅用于开发环境）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> RecreateDatabase(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogWarning("正在执行数据库重建操作（危险操作）...");

        try
        {
            // 删除数据库
            await _context.Database.EnsureDeletedAsync(cancellationToken);
            _logger.LogInformation("数据库已删除");

            // 创建数据库并应用迁移
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("数据库已重新创建并应用迁移");

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("重建数据库操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重建数据库时发生错误");
            return false;
        }
    }
}
