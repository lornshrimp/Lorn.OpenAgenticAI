namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 活跃状态 - 用户可以正常使用系统
    /// </summary>
    Active = 1,

    /// <summary>
    /// 非活跃状态 - 用户长时间未使用系统
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// 暂停状态 - 用户账户被暂时停用
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// 已删除状态 - 用户账户被标记为删除
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// 初始化状态 - 用户账户刚创建，正在初始化
    /// </summary>
    Initializing = 5
}