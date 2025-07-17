using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Capabilities;

/// <summary>
/// Agent����ע��ʵ�壨�ۺϸ���
/// </summary>
public class AgentCapabilityRegistry
{
    public string AgentId { get; private set; } = string.Empty;
    public string AgentName { get; private set; } = string.Empty;
    public AgentType AgentType { get; private set; } = null!;
    public string Version { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsSystemAgent { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public List<string> SupportedApplications { get; private set; } = new();
    public List<Permission> RequiredPermissions { get; private set; } = new();
    public string InstallationPath { get; private set; } = string.Empty;
    public string ConfigurationFile { get; private set; } = string.Empty;
    public DateTime LastHealthCheck { get; private set; }
    public HealthStatus HealthStatus { get; private set; } = null!;
    public PerformanceMetrics PerformanceMetrics { get; private set; } = null!;
    public DateTime RegistrationTime { get; private set; }
    public DateTime LastUpdatedTime { get; private set; }

    // ��������
    public virtual ICollection<AgentActionDefinition> ActionDefinitions { get; private set; } = new List<AgentActionDefinition>();

    // ˽�й��캯����EF Core
    private AgentCapabilityRegistry() { }

    public AgentCapabilityRegistry(
        string agentId,
        string agentName,
        AgentType agentType,
        string version,
        string? description = null,
        bool isSystemAgent = false,
        List<string>? supportedApplications = null,
        List<Permission>? requiredPermissions = null,
        string? installationPath = null,
        string? configurationFile = null)
    {
        AgentId = !string.IsNullOrWhiteSpace(agentId) ? agentId : throw new ArgumentException("AgentId cannot be empty", nameof(agentId));
        AgentName = !string.IsNullOrWhiteSpace(agentName) ? agentName : throw new ArgumentException("AgentName cannot be empty", nameof(agentName));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Version = !string.IsNullOrWhiteSpace(version) ? version : "1.0.0";
        Description = description ?? string.Empty;
        IsActive = true;
        IsSystemAgent = isSystemAgent;
        SupportedApplications = supportedApplications ?? new List<string>();
        RequiredPermissions = requiredPermissions ?? new List<Permission>();
        InstallationPath = installationPath ?? string.Empty;
        ConfigurationFile = configurationFile ?? string.Empty;
        LastHealthCheck = DateTime.UtcNow;
        HealthStatus = HealthStatus.Unknown;
        PerformanceMetrics = new PerformanceMetrics();
        RegistrationTime = DateTime.UtcNow;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ע���¶���
    /// </summary>
    public void RegisterAction(AgentActionDefinition action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (action.AgentId != AgentId)
            throw new ArgumentException("Action must belong to this agent", nameof(action));

        var existingAction = ActionDefinitions.FirstOrDefault(a => a.ActionName == action.ActionName);
        if (existingAction != null)
        {
            ActionDefinitions.Remove(existingAction);
        }

        ActionDefinitions.Add(action);
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���½���״̬
    /// </summary>
    public void UpdateHealthStatus(HealthStatus status)
    {
        HealthStatus = status ?? throw new ArgumentNullException(nameof(status));
        LastHealthCheck = DateTime.UtcNow;
        LastUpdatedTime = DateTime.UtcNow;

        // �������״̬ΪCritical��Offline������Ϊ����Ծ
        if (status == HealthStatus.Critical || status == HealthStatus.Offline)
        {
            IsActive = false;
        }
        else if (status == HealthStatus.Healthy)
        {
            IsActive = true;
        }
    }

    /// <summary>
    /// ��齡��״̬
    /// </summary>
    public HealthCheckResult CheckHealth()
    {
        var result = new HealthCheckResult
        {
            AgentId = AgentId,
            Status = HealthStatus,
            CheckTime = DateTime.UtcNow,
            ResponseTime = 0,
            IsResponsive = IsActive,
            ErrorMessage = null
        };

        // ����Ƿ񳬹��˼����
        var timeSinceLastCheck = DateTime.UtcNow - LastHealthCheck;
        if (timeSinceLastCheck.TotalMinutes > 5) // 5�����޼����Ϊ�쳣���
        {
            result.Status = HealthStatus.Warning;
            result.ErrorMessage = "Health check interval exceeded";
        }

        return result;
    }

    /// <summary>
    /// ����Ƿ�֧��ָ������
    /// </summary>
    public bool IsCapabilitySupported(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return false;

        return ActionDefinitions.Any(a => a.ActionName.Equals(capability, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// ��������ָ��
    /// </summary>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���֧�ֵ�Ӧ�ó���
    /// </summary>
    public void AddSupportedApplication(string application)
    {
        if (!string.IsNullOrWhiteSpace(application) && !SupportedApplications.Contains(application))
        {
            SupportedApplications.Add(application);
            LastUpdatedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// �������Ȩ��
    /// </summary>
    public void AddRequiredPermission(Permission permission)
    {
        if (permission != null && !RequiredPermissions.Any(p => 
            p.PermissionType == permission.PermissionType && 
            p.Resource == permission.Resource))
        {
            RequiredPermissions.Add(permission);
            LastUpdatedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// ����Agent
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ͣ��Agent
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���������ļ�·��
    /// </summary>
    public void UpdateConfigurationFile(string? configurationFile)
    {
        ConfigurationFile = configurationFile ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���°�װ·��
    /// </summary>
    public void UpdateInstallationPath(string? installationPath)
    {
        InstallationPath = installationPath ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// ���������
/// </summary>
public class HealthCheckResult
{
    public string AgentId { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = null!;
    public DateTime CheckTime { get; set; }
    public double ResponseTime { get; set; }
    public bool IsResponsive { get; set; }
    public string? ErrorMessage { get; set; }
}