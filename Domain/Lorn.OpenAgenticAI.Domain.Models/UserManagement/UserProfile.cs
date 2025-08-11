using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户档案实体
/// </summary>
public class UserProfile : IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedTime { get; private set; }
    public DateTime LastLoginTime { get; private set; }
    public bool IsActive { get; private set; }
    public int ProfileVersion { get; private set; }
    public ValueObjects.SecuritySettings SecuritySettings { get; private set; } = null!;

    // 静默认证相关属性
    public string MachineId { get; private set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime LastActiveTime { get; private set; }
    public string Avatar { get; set; } = string.Empty;

    // 注意：Metadata 不再直接存储在这里
    // 它将通过 UserMetadataEntry 实体在数据库中单独存储

    // IAggregateRoot 接口实现
    public Guid Id => UserId;

    // 导航属性
    public virtual ICollection<UserPreferences> UserPreferences { get; private set; } = new List<UserPreferences>();
    public virtual ICollection<Execution.TaskExecutionHistory> ExecutionHistories { get; private set; } = new List<Execution.TaskExecutionHistory>();
    public virtual ICollection<Workflow.WorkflowTemplate> WorkflowTemplates { get; private set; } = new List<Workflow.WorkflowTemplate>();
    public virtual ICollection<UserMetadataEntry> MetadataEntries { get; private set; } = new List<UserMetadataEntry>();

    // 私有构造函数用于EF Core
    private UserProfile()
    {
        UserId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IsActive = true;
        ProfileVersion = 1;
        Status = UserStatus.Initializing;
    }

    public UserProfile(
        Guid userId,
        string username,
        string email,
        ValueObjects.SecuritySettings securitySettings)
    {
        UserId = userId == Guid.Empty ? Guid.NewGuid() : userId;
        Username = !string.IsNullOrWhiteSpace(username) ? username : throw new ArgumentException("Username cannot be empty", nameof(username));
        Email = email ?? string.Empty;
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IsActive = true;
        ProfileVersion = 1;
        Status = UserStatus.Active;
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        // Metadata 现在通过 UserMetadataEntry 实体管理
    }

    /// <summary>
    /// 静默认证专用构造函数 - 基于机器ID创建用户
    /// </summary>
    public UserProfile(
        string machineId,
        string? displayName = null,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            throw new ArgumentException("MachineId cannot be empty", nameof(machineId));

        UserId = Guid.NewGuid();
        MachineId = machineId;
        Username = GenerateUsernameFromMachineId(machineId);
        DisplayName = displayName ?? GenerateDefaultDisplayName();
        Email = string.Empty;
        Description = "通过静默认证自动创建的用户";
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IsActive = true;
        IsDefault = isDefault;
        ProfileVersion = 1;
        Status = UserStatus.Active;
        SecuritySettings = CreateDefaultSecuritySettings();

        // 自动生成智能默认偏好设置
        GenerateDefaultPreferences();
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

    #region 静默认证相关业务方法

    /// <summary>
    /// 验证机器ID是否匹配
    /// </summary>
    public bool ValidateMachineId(string machineId)
    {
        return !string.IsNullOrWhiteSpace(MachineId) &&
               MachineId.Equals(machineId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 更新机器ID（用于设备迁移场景）
    /// </summary>
    public void UpdateMachineId(string newMachineId)
    {
        if (string.IsNullOrWhiteSpace(newMachineId))
            throw new ArgumentException("MachineId cannot be empty", nameof(newMachineId));

        MachineId = newMachineId;
        IncrementVersion();
    }

    /// <summary>
    /// 静默登录 - 更新最后登录时间和活跃时间
    /// </summary>
    public void PerformSilentLogin()
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"无法对状态为 {Status} 的用户执行静默登录");

        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// 更新用户活跃时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActiveTime = DateTime.UtcNow;

        // 如果用户之前是非活跃状态，自动激活
        if (Status == UserStatus.Inactive)
        {
            Status = UserStatus.Active;
            IsActive = true;
        }

        IncrementVersion();
    }

    /// <summary>
    /// 设置为默认用户
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        IncrementVersion();
    }

    /// <summary>
    /// 取消默认用户设置
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        IncrementVersion();
    }

    #endregion

    #region 用户状态转换业务规则验证

    /// <summary>
    /// 验证状态转换是否合法
    /// </summary>
    public bool CanTransitionTo(UserStatus newStatus)
    {
        return newStatus switch
        {
            UserStatus.Active => Status is UserStatus.Inactive or UserStatus.Suspended or UserStatus.Initializing,
            UserStatus.Inactive => Status is UserStatus.Active,
            UserStatus.Suspended => Status is UserStatus.Active or UserStatus.Inactive,
            UserStatus.Deleted => Status is not UserStatus.Deleted,
            UserStatus.Initializing => Status is UserStatus.Deleted, // 只有删除状态可以重新初始化
            _ => false
        };
    }

    /// <summary>
    /// 转换用户状态
    /// </summary>
    public void TransitionTo(UserStatus newStatus, string? reason = null)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"无法从状态 {Status} 转换到 {newStatus}");

        var oldStatus = Status;
        Status = newStatus;

        // 根据新状态更新相关属性
        switch (newStatus)
        {
            case UserStatus.Active:
                IsActive = true;
                LastActiveTime = DateTime.UtcNow;
                break;
            case UserStatus.Inactive:
                IsActive = false;
                break;
            case UserStatus.Suspended:
                IsActive = false;
                break;
            case UserStatus.Deleted:
                IsActive = false;
                break;
            case UserStatus.Initializing:
                IsActive = false;
                break;
        }

        IncrementVersion();
    }

    /// <summary>
    /// 检查用户是否长时间未活跃，自动转换为非活跃状态
    /// </summary>
    public bool CheckAndUpdateInactiveStatus(int inactiveDays = 30)
    {
        if (Status != UserStatus.Active) return false;

        var daysSinceLastActive = (DateTime.UtcNow - LastActiveTime).TotalDays;
        if (daysSinceLastActive > inactiveDays)
        {
            TransitionTo(UserStatus.Inactive, $"用户超过 {inactiveDays} 天未活跃");
            return true;
        }

        return false;
    }

    #endregion

    #region 用户数据完整性检查和自动修复

    /// <summary>
    /// 执行数据完整性检查
    /// </summary>
    public List<string> PerformIntegrityCheck()
    {
        var issues = new List<string>();

        // 检查必填字段
        if (string.IsNullOrWhiteSpace(Username))
            issues.Add("用户名不能为空");

        if (UserId == Guid.Empty)
            issues.Add("用户ID无效");

        if (SecuritySettings == null)
            issues.Add("安全设置不能为空");
        else if (!SecuritySettings.IsValid())
            issues.Add("安全设置无效");

        // 检查时间逻辑
        if (LastLoginTime < CreatedTime)
            issues.Add("最后登录时间不能早于创建时间");

        if (LastActiveTime < CreatedTime)
            issues.Add("最后活跃时间不能早于创建时间");

        // 检查状态一致性
        if (IsActive && Status is UserStatus.Inactive or UserStatus.Suspended or UserStatus.Deleted)
            issues.Add("用户活跃标志与状态不一致");

        // 检查默认用户逻辑（如果有多个默认用户，这需要在聚合层面检查）
        if (IsDefault && Status == UserStatus.Deleted)
            issues.Add("已删除的用户不能是默认用户");

        return issues;
    }

    /// <summary>
    /// 自动修复数据完整性问题
    /// </summary>
    public bool AutoRepairIntegrityIssues()
    {
        bool hasRepairs = false;

        // 修复用户名
        if (string.IsNullOrWhiteSpace(Username))
        {
            Username = !string.IsNullOrWhiteSpace(MachineId)
                ? GenerateUsernameFromMachineId(MachineId)
                : $"User_{UserId:N}";
            hasRepairs = true;
        }

        // 修复显示名称
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = GenerateDefaultDisplayName();
            hasRepairs = true;
        }

        // 修复安全设置
        if (SecuritySettings == null || !SecuritySettings.IsValid())
        {
            SecuritySettings = CreateDefaultSecuritySettings();
            hasRepairs = true;
        }

        // 修复时间逻辑
        if (LastLoginTime < CreatedTime)
        {
            LastLoginTime = CreatedTime;
            hasRepairs = true;
        }

        if (LastActiveTime < CreatedTime)
        {
            LastActiveTime = CreatedTime;
            hasRepairs = true;
        }

        // 修复状态一致性
        if (IsActive && Status is UserStatus.Inactive or UserStatus.Suspended or UserStatus.Deleted)
        {
            IsActive = false;
            hasRepairs = true;
        }
        else if (!IsActive && Status == UserStatus.Active)
        {
            Status = UserStatus.Inactive;
            hasRepairs = true;
        }

        // 修复默认用户逻辑
        if (IsDefault && Status == UserStatus.Deleted)
        {
            IsDefault = false;
            hasRepairs = true;
        }

        if (hasRepairs)
        {
            IncrementVersion();
        }

        return hasRepairs;
    }

    #endregion

    #region 用户偏好智能默认值生成

    /// <summary>
    /// 生成默认偏好设置
    /// </summary>
    private void GenerateDefaultPreferences()
    {
        // 这个方法在构造函数中调用，实际的偏好设置会通过 UserPreferences 实体管理
        // 这里只是标记需要生成默认偏好设置
    }

    /// <summary>
    /// 获取智能默认偏好设置配置
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetIntelligentDefaultPreferences()
    {
        var preferences = new Dictionary<string, Dictionary<string, object>>();

        // 界面偏好设置
        preferences["Interface"] = new Dictionary<string, object>
        {
            ["Theme"] = DetectSystemTheme(),
            ["FontSize"] = DetectOptimalFontSize(),
            ["Language"] = DetectSystemLanguage(),
            ["Layout"] = "Standard",
            ["ShowTips"] = true,
            ["AutoSave"] = true
        };

        // 操作偏好设置
        preferences["Operation"] = new Dictionary<string, object>
        {
            ["DefaultLLMModel"] = "gpt-3.5-turbo",
            ["TaskTimeout"] = 300, // 5分钟
            ["MaxConcurrentTasks"] = Environment.ProcessorCount,
            ["AutoRetry"] = true,
            ["RetryCount"] = 3,
            ["EnableNotifications"] = true
        };

        // 安全偏好设置
        preferences["Security"] = new Dictionary<string, object>
        {
            ["SessionTimeout"] = 30, // 30分钟
            ["AutoLock"] = false,
            ["AuditLogging"] = true,
            ["DataEncryption"] = true,
            ["BackupEnabled"] = true,
            ["BackupFrequency"] = "Daily"
        };

        return preferences;
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 从机器ID生成用户名
    /// </summary>
    private static string GenerateUsernameFromMachineId(string machineId)
    {
        // 取机器ID的前8位作为用户名后缀
        var suffix = machineId.Length > 8 ? machineId[..8] : machineId;
        return $"User_{suffix}";
    }

    /// <summary>
    /// 生成默认显示名称
    /// </summary>
    private string GenerateDefaultDisplayName()
    {
        var computerName = Environment.MachineName;
        var userName = Environment.UserName;

        return !string.IsNullOrWhiteSpace(userName)
            ? $"{userName}@{computerName}"
            : $"用户@{computerName}";
    }

    /// <summary>
    /// 创建默认安全设置
    /// </summary>
    private static ValueObjects.SecuritySettings CreateDefaultSecuritySettings()
    {
        return new ValueObjects.SecuritySettings(
            authenticationMethod: "Silent",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow,
            additionalSettings: new Dictionary<string, string>
            {
                ["AutoLogin"] = "true",
                ["RememberDevice"] = "true"
            }
        );
    }

    /// <summary>
    /// 检测系统主题
    /// </summary>
    private static string DetectSystemTheme()
    {
        try
        {
            // 尝试检测系统主题设置
            // 这里简化处理，实际可以通过注册表或系统API检测
            return "Light"; // 默认浅色主题
        }
        catch
        {
            return "Light";
        }
    }

    /// <summary>
    /// 检测最佳字体大小
    /// </summary>
    private static int DetectOptimalFontSize()
    {
        try
        {
            // 根据系统DPI设置推荐字体大小
            // 这里简化处理，实际可以通过系统API获取DPI信息
            return 14; // 默认14px
        }
        catch
        {
            return 14;
        }
    }

    /// <summary>
    /// 检测系统语言
    /// </summary>
    private static string DetectSystemLanguage()
    {
        try
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            return culture.Name switch
            {
                var name when name.StartsWith("zh") => "zh-CN",
                var name when name.StartsWith("en") => "en-US",
                _ => "zh-CN" // 默认中文
            };
        }
        catch
        {
            return "zh-CN";
        }
    }

    #endregion
}