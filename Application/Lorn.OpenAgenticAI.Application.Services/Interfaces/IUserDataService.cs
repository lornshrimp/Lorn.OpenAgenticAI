using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Interfaces;

/// <summary>
/// 用户数据服务接口 - 纯数据访问层
/// 仅封装对仓储的读写，不包含业务流程/验证/上下文逻辑。
/// 职责边界：
/// 1. 不做输入合法性或唯一性检查（由 <see cref="IUserManagementService"/> 负责）。
/// 2. 不缓存当前用户会话（由 <see cref="IUserContextService"/> 负责）。
/// 3. 允许被业务层批量调用；保持幂等、最小副作用；避免抛出与业务策略相关的异常信息。
/// 4. 提供最细粒度的数据操作，避免组合式业务语义（组合交由业务管理层 orchestrate）。
/// </summary>
public interface IUserDataService
{
    #region 用户档案数据操作

    /// <summary>
    /// 按用户主键获取档案。
    /// 不存在则返回 <c>null</c>；不抛出业务语义异常。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户档案或 null</returns>
    Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过设备/机器标识获取用户档案（若系统支持“绑定本机”概念）。
    /// </summary>
    Task<UserProfile?> GetUserProfileByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过用户名获取档案；大小写匹配策略由仓储实现层决定。
    /// </summary>
    Task<UserProfile?> GetUserProfileByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过邮箱获取档案。
    /// </summary>
    Task<UserProfile?> GetUserProfileByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建用户档案（不做唯一性验证，调用前由业务层保证）。
    /// </summary>
    /// <param name="userProfile">已构建的领域对象（应满足领域不变量）。</param>
    /// <returns>持久化后的领域对象（通常为同一引用）。</returns>
    Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户档案（全量或增量由仓储策略决定）。
    /// </summary>
    Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除用户（内部实现调用领域模型 <c>Deactivate()</c>）。
    /// 返回 false 表示未找到；不抛出未找到异常。
    /// </summary>
    Task<bool> DeleteUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部用户（可选包含已停用）。返回只读集合，调用方勿修改。
    /// </summary>
    Task<IReadOnlyList<UserProfile>> GetAllUserProfilesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认用户（若存在多个，返回第一个；整理逻辑在业务层保证唯一）。
    /// </summary>
    Task<UserProfile?> GetDefaultUserProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户名是否已存在（可排除指定用户，用于更新场景）。
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断邮箱是否已存在（可排除指定用户，用于更新场景）。
    /// </summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    #endregion

    #region 偏好设置数据操作

    /// <summary>
    /// 获取单个偏好项；未找到返回 null。
    /// </summary>
    Task<UserPreferences?> GetUserPreferenceAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户全部或某类别的偏好集合；返回只读集合。
    /// </summary>
    Task<IReadOnlyList<UserPreferences>> GetUserPreferencesAsync(Guid userId, string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存（新增或更新）单个偏好项；不存在则追加，存在则覆盖值与非空描述。
    /// </summary>
    Task<UserPreferences> SaveUserPreferenceAsync(UserPreferences preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除偏好（可指定类别+键；不指定键表示删除该类别所有项）。返回删除条数。
    /// </summary>
    Task<int> DeleteUserPreferencesAsync(Guid userId, string category, string? key = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量保存偏好：同一 (UserId, Category, Key) 以最后一次出现为准；
    /// 若某条目的 <c>PreferenceValue</c> 为空或空白字符串，表示删除该键；
    /// 返回受影响的（新增 + 更新 + 删除）条目数。调用前业务层应已完成策略校验。
    /// </summary>
    Task<int> SaveUserPreferencesBatchAsync(IEnumerable<UserPreferences> preferences, CancellationToken cancellationToken = default);

    #endregion
}
