using System;
using System.Collections.Generic;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户档案实体
/// </summary>
public class UserProfile
{
    public Guid UserId { get; private set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedTime { get; private set; }
    public DateTime LastLoginTime { get; private set; }
    public bool IsActive { get; private set; }
    public int ProfileVersion { get; private set; }
    public ValueObjects.SecuritySettings SecuritySettings { get; private set; } = null!;
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // 导航属性
    public virtual ICollection<UserPreferences> UserPreferences { get; private set; } = new List<UserPreferences>();
    public virtual ICollection<Execution.TaskExecutionHistory> ExecutionHistories { get; private set; } = new List<Execution.TaskExecutionHistory>();
    public virtual ICollection<Workflow.WorkflowTemplate> WorkflowTemplates { get; private set; } = new List<Workflow.WorkflowTemplate>();

    // 私有构造函数用于EF Core
    private UserProfile()
    {
        UserId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        IsActive = true;
        ProfileVersion = 1;
    }

    public UserProfile(
        Guid userId,
        string username,
        string email,
        ValueObjects.SecuritySettings securitySettings,
        Dictionary<string, object>? metadata = null)
    {
        UserId = userId == Guid.Empty ? Guid.NewGuid() : userId;
        Username = !string.IsNullOrWhiteSpace(username) ? username : throw new ArgumentException("Username cannot be empty", nameof(username));
        Email = email ?? string.Empty;
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        IsActive = true;
        ProfileVersion = 1;
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// 验证用户档案
    /// </summary>
    public bool ValidateProfile()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               SecuritySettings?.IsValid() == true &&
               IsActive;
    }

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 增加版本号
    /// </summary>
    public void IncrementVersion()
    {
        ProfileVersion++;
    }

    /// <summary>
    /// 更新邮箱
    /// </summary>
    public void UpdateEmail(string? email)
    {
        Email = email ?? string.Empty;
        IncrementVersion();
    }

    /// <summary>
    /// 更新安全设置
    /// </summary>
    public void UpdateSecuritySettings(ValueObjects.SecuritySettings securitySettings)
    {
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        IncrementVersion();
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        IncrementVersion();
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }
}