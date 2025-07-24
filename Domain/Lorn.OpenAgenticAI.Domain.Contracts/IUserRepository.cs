using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Contracts;

/// <summary>
/// 用户仓储接口，定义用户数据访问契约
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据用户ID获取用户档案
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户名获取用户档案
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户档案
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有活跃用户
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃用户列表</returns>
    Task<IEnumerable<UserProfile>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有用户（包括非活跃用户）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有用户列表</returns>
    Task<IEnumerable<UserProfile>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果用户名已存在则返回true</returns>
    Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否已存在
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果邮箱已存在则返回true</returns>
    Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加新用户
    /// </summary>
    /// <param name="userProfile">用户档案</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的用户档案</returns>
    Task<UserProfile> AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户档案
    /// </summary>
    /// <param name="userProfile">用户档案</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的用户档案</returns>
    Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除用户（设置为非活跃状态）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否操作成功</returns>
    Task<bool> SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数量
    /// </summary>
    /// <param name="activeOnly">是否只统计活跃用户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数量</returns>
    Task<int> GetUserCountAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取用户列表
    /// </summary>
    /// <param name="pageIndex">页索引（从0开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="activeOnly">是否只获取活跃用户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页用户列表</returns>
    Task<(IEnumerable<UserProfile> Users, int TotalCount)> GetUsersPagedAsync(
        int pageIndex,
        int pageSize,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}