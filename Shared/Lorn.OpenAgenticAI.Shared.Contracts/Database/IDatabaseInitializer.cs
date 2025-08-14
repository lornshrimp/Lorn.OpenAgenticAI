using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lorn.OpenAgenticAI.Shared.Contracts.Database;

/// <summary>
/// 数据库初始化服务接口
/// 提供数据库创建、迁移、版本检查和种子数据初始化功能
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// 初始化数据库，包括创建数据库、应用迁移和初始化种子数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化结果</returns>
    Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查数据库是否存在
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据库是否存在</returns>
    Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建数据库
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否创建成功</returns>
    Task<bool> CreateDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 应用数据库迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>迁移结果</returns>
    Task<MigrationResult> ApplyMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前数据库版本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前数据库版本</returns>
    Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用迁移列表</returns>
    Task<IReadOnlyList<string>> GetAvailableMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已应用的迁移列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已应用迁移列表</returns>
    Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否需要迁移
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否需要迁移</returns>
    Task<bool> NeedsMigrationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化种子数据
    /// </summary>
    /// <param name="force">是否强制重新初始化</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种子数据初始化结果</returns>
    Task<SeedDataResult> InitializeSeedDataAsync(bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证数据库完整性
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<DatabaseValidationResult> ValidateDatabaseIntegrityAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据库初始化结果
/// </summary>
public record DatabaseInitializationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool DatabaseCreated { get; init; }
    public MigrationResult MigrationResult { get; init; } = new();
    public SeedDataResult SeedDataResult { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public List<string> Warnings { get; init; } = new();
    public Exception? Exception { get; init; }
}

/// <summary>
/// 迁移结果
/// </summary>
public record MigrationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int AppliedMigrationsCount { get; init; }
    public List<string> AppliedMigrations { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public Exception? Exception { get; init; }
}

/// <summary>
/// 种子数据结果
/// </summary>
public record SeedDataResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int InitializedTablesCount { get; init; }
    public List<string> InitializedTables { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public Exception? Exception { get; init; }
}

/// <summary>
/// 数据库验证结果
/// </summary>
public record DatabaseValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public TimeSpan Duration { get; init; }
}
