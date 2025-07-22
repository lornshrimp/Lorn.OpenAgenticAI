using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

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
        IsActive = true;
        ProfileVersion = 1;
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
        IsActive = true;
        ProfileVersion = 1;
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        // Metadata ����ͨ�� UserMetadataEntry ʵ�����
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
}