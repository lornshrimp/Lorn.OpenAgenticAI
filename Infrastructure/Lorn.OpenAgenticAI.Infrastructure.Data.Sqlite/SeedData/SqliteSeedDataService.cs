using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Infrastructure.Data;
using Lorn.OpenAgenticAI.Shared.Contracts.Database;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using System.Diagnostics;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.SeedData;

/// <summary>
/// SQLite种子数据服务
/// 负责初始化系统默认数据和配置
/// </summary>
public class SqliteSeedDataService
{
    private readonly OpenAgenticAIDbContext _context;
    private readonly ILogger<SqliteSeedDataService> _logger;

    public SqliteSeedDataService(OpenAgenticAIDbContext context, ILogger<SqliteSeedDataService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 初始化种子数据
    /// </summary>
    /// <param name="force">是否强制重新初始化</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种子数据初始化结果</returns>
    public async Task<SeedDataResult> InitializeSeedDataAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("开始初始化种子数据，强制模式: {Force}", force);
        var stopwatch = Stopwatch.StartNew();
        var initializedTables = new List<string>();

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 初始化默认用户档案
                if (await ShouldInitializeTable("UserProfiles", force, cancellationToken))
                {
                    await InitializeDefaultUserProfilesAsync(cancellationToken);
                    initializedTables.Add("UserProfiles");
                }

                // 初始化默认用户偏好
                if (await ShouldInitializeTable("UserPreferences", force, cancellationToken))
                {
                    await InitializeDefaultUserPreferencesAsync(cancellationToken);
                    initializedTables.Add("UserPreferences");
                }

                // 提交事务
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("种子数据初始化完成，初始化了 {Count} 个表，耗时 {Duration}ms",
                    initializedTables.Count,
                    stopwatch.ElapsedMilliseconds);

                return new SeedDataResult
                {
                    Success = true,
                    Message = $"成功初始化 {initializedTables.Count} 个表的种子数据",
                    InitializedTablesCount = initializedTables.Count,
                    InitializedTables = initializedTables,
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("种子数据初始化操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化种子数据时发生错误");
            return new SeedDataResult
            {
                Success = false,
                Message = $"种子数据初始化失败: {ex.Message}",
                InitializedTablesCount = initializedTables.Count,
                InitializedTables = initializedTables,
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
    /// 检查是否应该初始化表
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="force">是否强制初始化</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否应该初始化</returns>
    private async Task<bool> ShouldInitializeTable(string tableName, bool force, CancellationToken cancellationToken)
    {
        if (force)
        {
            _logger.LogInformation("强制模式，将初始化表: {TableName}（如果数据已存在，将跳过）", tableName);
            // 强制模式仍然检查数据是否存在，但不强制删除现有数据以避免复杂的级联删除问题
            // 在实际应用中，如果真的需要清除数据，应该通过数据库管理工具进行

            try
            {
                var hasData = tableName switch
                {
                    "UserProfiles" => await _context.UserProfiles.AnyAsync(cancellationToken),
                    "UserPreferences" => await _context.UserPreferences.AnyAsync(cancellationToken),
                    _ => false
                };

                if (hasData)
                {
                    _logger.LogInformation("表 {TableName} 已有数据，即使在强制模式下也跳过初始化以避免数据冲突", tableName);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查表 {TableName} 是否有数据时发生错误", tableName);
                return true; // 发生错误时默认进行初始化
            }
        }

        try
        {
            var hasData = tableName switch
            {
                "UserProfiles" => await _context.UserProfiles.AnyAsync(cancellationToken),
                "UserPreferences" => await _context.UserPreferences.AnyAsync(cancellationToken),
                _ => false
            };

            if (hasData)
            {
                _logger.LogInformation("表 {TableName} 已有数据，跳过初始化", tableName);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查表 {TableName} 是否有数据时发生错误", tableName);
            return true; // 发生错误时默认进行初始化
        }
    }

    /// <summary>
    /// 初始化默认用户档案
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task InitializeDefaultUserProfilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("初始化默认用户档案...");

        var defaultUser = new UserProfile(
            userId: Guid.NewGuid(),
            username: "admin",
            email: "admin@localhost",
            securitySettings: new SecuritySettings(
                authenticationMethod: "Silent",
                sessionTimeoutMinutes: 30,
                requireTwoFactor: false,
                passwordLastChanged: DateTime.UtcNow
            )
        );

        // 设置显示名称
        defaultUser.DisplayName = "系统管理员";

        _context.UserProfiles.Add(defaultUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("默认用户档案初始化完成");
    }

    /// <summary>
    /// 初始化默认用户偏好
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task InitializeDefaultUserPreferencesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("初始化默认用户偏好...");

        var adminUser = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);
        if (adminUser == null)
        {
            _logger.LogWarning("未找到管理员用户，跳过用户偏好初始化");
            return;
        }

        // 创建主题偏好
        var themePreference = new UserPreferences(
            userId: adminUser.UserId,
            preferenceCategory: "UI",
            preferenceKey: "Theme",
            preferenceValue: "Light",
            valueType: "String",
            description: "用户界面主题"
        );

        // 创建语言偏好
        var languagePreference = new UserPreferences(
            userId: adminUser.UserId,
            preferenceCategory: "UI",
            preferenceKey: "Language",
            preferenceValue: "zh-CN",
            valueType: "String",
            description: "用户界面语言"
        );

        // 创建自动保存偏好
        var autoSavePreference = new UserPreferences(
            userId: adminUser.UserId,
            preferenceCategory: "System",
            preferenceKey: "AutoSave",
            preferenceValue: "true",
            valueType: "Boolean",
            description: "自动保存设置"
        );

        _context.UserPreferences.AddRange(themePreference, languagePreference, autoSavePreference);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("默认用户偏好初始化完成");
    }

    /// <summary>
    /// 清理种子数据（危险操作，仅用于开发环境）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> ClearSeedDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogWarning("正在执行种子数据清理操作（危险操作）...");

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 清理默认用户偏好
                var defaultPreferences = await _context.UserPreferences
                    .Where(p => p.User.Username == "admin")
                    .ToListAsync(cancellationToken);
                _context.UserPreferences.RemoveRange(defaultPreferences);

                // 清理默认用户档案
                var defaultUser = await _context.UserProfiles
                    .FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);
                if (defaultUser != null)
                {
                    _context.UserProfiles.Remove(defaultUser);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("种子数据清理完成");
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("种子数据清理操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理种子数据时发生错误");
            return false;
        }
    }
}
