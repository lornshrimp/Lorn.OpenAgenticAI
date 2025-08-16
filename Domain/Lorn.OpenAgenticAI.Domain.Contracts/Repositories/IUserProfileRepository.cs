using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Shared.Contracts.Repositories;

namespace Lorn.OpenAgenticAI.Domain.Contracts.Repositories;

/// <summary>
/// 用户档案仓储接口（领域层契约）
/// 整合了所有UserProfile相关的数据访问操作，包括基础CRUD、业务查询、验证方法等
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile>, IAsyncRepository<UserProfile>
{
    #region 基础查询方法

    /// <summary>
    /// 根据用户名获取用户档案（同步版本）
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    UserProfile? GetByUserName(string userName);

    /// <summary>
    /// 根据用户名获取用户档案
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户档案
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据机器ID获取用户档案
    /// </summary>
    /// <param name="machineId">机器ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案，如果不存在则返回null</returns>
    Task<UserProfile?> GetByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    #endregion

    #region 业务查询方法

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
    /// 获取默认用户列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>默认用户列表</returns>
    Task<IEnumerable<UserProfile>> GetDefaultUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近登录的用户
    /// </summary>
    /// <param name="since">起始时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近登录的用户列表</returns>
    Task<IEnumerable<UserProfile>> GetRecentlyActiveUsersAsync(DateTime since, CancellationToken cancellationToken = default);

    #endregion

    #region 业务验证方法

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
    /// 检查机器ID是否已存在
    /// </summary>
    /// <param name="machineId">机器ID</param>
    /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果机器ID已存在则返回true</returns>
    Task<bool> IsMachineIdExistsAsync(string machineId, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    #endregion

    #region 分页和统计方法

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

    #endregion

    #region 特殊操作方法

    /// <summary>
    /// 软删除用户（设置为非活跃状态）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否操作成功</returns>
    Task<bool> SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新用户安全设置
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    /// <param name="settings">安全设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功的用户数量</returns>
    Task<int> BulkUpdateSecuritySettingsAsync(List<Guid> userIds, Domain.Models.ValueObjects.SecuritySettings settings, CancellationToken cancellationToken = default);

    #endregion
}
