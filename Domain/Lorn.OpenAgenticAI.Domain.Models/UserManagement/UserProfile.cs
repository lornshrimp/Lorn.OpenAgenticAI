using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// �û�����ʵ��
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

    // ��Ĭ��֤�������
    public string MachineId { get; private set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime LastActiveTime { get; private set; }
    public string Avatar { get; set; } = string.Empty;

    // ע�⣺Metadata ����ֱ�Ӵ洢������
    // ����ͨ�� UserMetadataEntry ʵ�������ݿ��е����洢

    // IAggregateRoot �ӿ�ʵ��
    public Guid Id => UserId;

    // ��������
    public virtual ICollection<UserPreferences> UserPreferences { get; private set; } = new List<UserPreferences>();
    public virtual ICollection<Execution.TaskExecutionHistory> ExecutionHistories { get; private set; } = new List<Execution.TaskExecutionHistory>();
    public virtual ICollection<Workflow.WorkflowTemplate> WorkflowTemplates { get; private set; } = new List<Workflow.WorkflowTemplate>();
    public virtual ICollection<UserMetadataEntry> MetadataEntries { get; private set; } = new List<UserMetadataEntry>();

    // ˽�й��캯������EF Core
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
        // Metadata ����ͨ�� UserMetadataEntry ʵ�����
    }

    /// <summary>
    /// ��Ĭ��֤ר�ù��캯�� - ���ڻ���ID�����û�
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
        Description = "ͨ����Ĭ��֤�Զ��������û�";
        CreatedTime = DateTime.UtcNow;
        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IsActive = true;
        IsDefault = isDefault;
        ProfileVersion = 1;
        Status = UserStatus.Active;
        SecuritySettings = CreateDefaultSecuritySettings();

        // �Զ���������Ĭ��ƫ������
        GenerateDefaultPreferences();
    }

    /// <summary>
    /// ��֤�û�����
    /// </summary>
    public bool ValidateProfile()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               SecuritySettings?.IsValid() == true &&
               IsActive;
    }

    /// <summary>
    /// ��������¼ʱ��
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���Ӱ汾��
    /// </summary>
    public void IncrementVersion()
    {
        ProfileVersion++;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void UpdateEmail(string? email)
    {
        Email = email ?? string.Empty;
        IncrementVersion();
    }

    /// <summary>
    /// ���°�ȫ����
    /// </summary>
    public void UpdateSecuritySettings(ValueObjects.SecuritySettings securitySettings)
    {
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        IncrementVersion();
    }

    /// <summary>
    /// �����û�
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        IncrementVersion();
    }

    /// <summary>
    /// ͣ���û�
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }

    #region ��Ĭ��֤���ҵ�񷽷�

    /// <summary>
    /// ��֤����ID�Ƿ�ƥ��
    /// </summary>
    public bool ValidateMachineId(string machineId)
    {
        return !string.IsNullOrWhiteSpace(MachineId) &&
               MachineId.Equals(machineId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ���»���ID�������豸Ǩ�Ƴ�����
    /// </summary>
    public void UpdateMachineId(string newMachineId)
    {
        if (string.IsNullOrWhiteSpace(newMachineId))
            throw new ArgumentException("MachineId cannot be empty", nameof(newMachineId));

        MachineId = newMachineId;
        IncrementVersion();
    }

    /// <summary>
    /// ��Ĭ��¼ - ��������¼ʱ��ͻ�Ծʱ��
    /// </summary>
    public void PerformSilentLogin()
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"�޷���״̬Ϊ {Status} ���û�ִ�о�Ĭ��¼");

        LastLoginTime = DateTime.UtcNow;
        LastActiveTime = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// �����û���Ծʱ��
    /// </summary>
    public void UpdateActivity()
    {
        LastActiveTime = DateTime.UtcNow;

        // ����û�֮ǰ�Ƿǻ�Ծ״̬���Զ�����
        if (Status == UserStatus.Inactive)
        {
            Status = UserStatus.Active;
            IsActive = true;
        }

        IncrementVersion();
    }

    /// <summary>
    /// ����ΪĬ���û�
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        IncrementVersion();
    }

    /// <summary>
    /// ȡ��Ĭ���û�����
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        IncrementVersion();
    }

    #endregion

    #region �û�״̬ת��ҵ�������֤

    /// <summary>
    /// ��֤״̬ת���Ƿ�Ϸ�
    /// </summary>
    public bool CanTransitionTo(UserStatus newStatus)
    {
        return newStatus switch
        {
            UserStatus.Active => Status is UserStatus.Inactive or UserStatus.Suspended or UserStatus.Initializing,
            UserStatus.Inactive => Status is UserStatus.Active,
            UserStatus.Suspended => Status is UserStatus.Active or UserStatus.Inactive,
            UserStatus.Deleted => Status is not UserStatus.Deleted,
            UserStatus.Initializing => Status is UserStatus.Deleted, // ֻ��ɾ��״̬�������³�ʼ��
            _ => false
        };
    }

    /// <summary>
    /// ת���û�״̬
    /// </summary>
    public void TransitionTo(UserStatus newStatus, string? reason = null)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"�޷���״̬ {Status} ת���� {newStatus}");

        var oldStatus = Status;
        Status = newStatus;

        // ������״̬�����������
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
    /// ����û��Ƿ�ʱ��δ��Ծ���Զ�ת��Ϊ�ǻ�Ծ״̬
    /// </summary>
    public bool CheckAndUpdateInactiveStatus(int inactiveDays = 30)
    {
        if (Status != UserStatus.Active) return false;

        var daysSinceLastActive = (DateTime.UtcNow - LastActiveTime).TotalDays;
        if (daysSinceLastActive > inactiveDays)
        {
            TransitionTo(UserStatus.Inactive, $"�û����� {inactiveDays} ��δ��Ծ");
            return true;
        }

        return false;
    }

    #endregion

    #region �û����������Լ����Զ��޸�

    /// <summary>
    /// ִ�����������Լ��
    /// </summary>
    public List<string> PerformIntegrityCheck()
    {
        var issues = new List<string>();

        // �������ֶ�
        if (string.IsNullOrWhiteSpace(Username))
            issues.Add("�û�������Ϊ��");

        if (UserId == Guid.Empty)
            issues.Add("�û�ID��Ч");

        if (SecuritySettings == null)
            issues.Add("��ȫ���ò���Ϊ��");
        else if (!SecuritySettings.IsValid())
            issues.Add("��ȫ������Ч");

        // ���ʱ���߼�
        if (LastLoginTime < CreatedTime)
            issues.Add("����¼ʱ�䲻�����ڴ���ʱ��");

        if (LastActiveTime < CreatedTime)
            issues.Add("����Ծʱ�䲻�����ڴ���ʱ��");

        // ���״̬һ����
        if (IsActive && Status is UserStatus.Inactive or UserStatus.Suspended or UserStatus.Deleted)
            issues.Add("�û���Ծ��־��״̬��һ��");

        // ���Ĭ���û��߼�������ж��Ĭ���û�������Ҫ�ھۺϲ����飩
        if (IsDefault && Status == UserStatus.Deleted)
            issues.Add("��ɾ�����û�������Ĭ���û�");

        return issues;
    }

    /// <summary>
    /// �Զ��޸���������������
    /// </summary>
    public bool AutoRepairIntegrityIssues()
    {
        bool hasRepairs = false;

        // �޸��û���
        if (string.IsNullOrWhiteSpace(Username))
        {
            Username = !string.IsNullOrWhiteSpace(MachineId)
                ? GenerateUsernameFromMachineId(MachineId)
                : $"User_{UserId:N}";
            hasRepairs = true;
        }

        // �޸���ʾ����
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = GenerateDefaultDisplayName();
            hasRepairs = true;
        }

        // �޸���ȫ����
        if (SecuritySettings == null || !SecuritySettings.IsValid())
        {
            SecuritySettings = CreateDefaultSecuritySettings();
            hasRepairs = true;
        }

        // �޸�ʱ���߼�
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

        // �޸�״̬һ����
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

        // �޸�Ĭ���û��߼�
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

    #region �û�ƫ������Ĭ��ֵ����

    /// <summary>
    /// ����Ĭ��ƫ������
    /// </summary>
    private void GenerateDefaultPreferences()
    {
        // ��������ڹ��캯���е��ã�ʵ�ʵ�ƫ�����û�ͨ�� UserPreferences ʵ�����
        // ����ֻ�Ǳ����Ҫ����Ĭ��ƫ������
    }

    /// <summary>
    /// ��ȡ����Ĭ��ƫ����������
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetIntelligentDefaultPreferences()
    {
        var preferences = new Dictionary<string, Dictionary<string, object>>();

        // ����ƫ������
        preferences["Interface"] = new Dictionary<string, object>
        {
            ["Theme"] = DetectSystemTheme(),
            ["FontSize"] = DetectOptimalFontSize(),
            ["Language"] = DetectSystemLanguage(),
            ["Layout"] = "Standard",
            ["ShowTips"] = true,
            ["AutoSave"] = true
        };

        // ����ƫ������
        preferences["Operation"] = new Dictionary<string, object>
        {
            ["DefaultLLMModel"] = "gpt-3.5-turbo",
            ["TaskTimeout"] = 300, // 5����
            ["MaxConcurrentTasks"] = Environment.ProcessorCount,
            ["AutoRetry"] = true,
            ["RetryCount"] = 3,
            ["EnableNotifications"] = true
        };

        // ��ȫƫ������
        preferences["Security"] = new Dictionary<string, object>
        {
            ["SessionTimeout"] = 30, // 30����
            ["AutoLock"] = false,
            ["AuditLogging"] = true,
            ["DataEncryption"] = true,
            ["BackupEnabled"] = true,
            ["BackupFrequency"] = "Daily"
        };

        return preferences;
    }

    #endregion

    #region ˽�и�������

    /// <summary>
    /// �ӻ���ID�����û���
    /// </summary>
    private static string GenerateUsernameFromMachineId(string machineId)
    {
        // ȡ����ID��ǰ8λ��Ϊ�û�����׺
        var suffix = machineId.Length > 8 ? machineId[..8] : machineId;
        return $"User_{suffix}";
    }

    /// <summary>
    /// ����Ĭ����ʾ����
    /// </summary>
    private string GenerateDefaultDisplayName()
    {
        var computerName = Environment.MachineName;
        var userName = Environment.UserName;

        return !string.IsNullOrWhiteSpace(userName)
            ? $"{userName}@{computerName}"
            : $"�û�@{computerName}";
    }

    /// <summary>
    /// ����Ĭ�ϰ�ȫ����
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
    /// ���ϵͳ����
    /// </summary>
    private static string DetectSystemTheme()
    {
        try
        {
            // ���Լ��ϵͳ��������
            // ����򻯴���ʵ�ʿ���ͨ��ע����ϵͳAPI���
            return "Light"; // Ĭ��ǳɫ����
        }
        catch
        {
            return "Light";
        }
    }

    /// <summary>
    /// �����������С
    /// </summary>
    private static int DetectOptimalFontSize()
    {
        try
        {
            // ����ϵͳDPI�����Ƽ������С
            // ����򻯴���ʵ�ʿ���ͨ��ϵͳAPI��ȡDPI��Ϣ
            return 14; // Ĭ��14px
        }
        catch
        {
            return 14;
        }
    }

    /// <summary>
    /// ���ϵͳ����
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
                _ => "zh-CN" // Ĭ������
            };
        }
        catch
        {
            return "zh-CN";
        }
    }

    #endregion
}