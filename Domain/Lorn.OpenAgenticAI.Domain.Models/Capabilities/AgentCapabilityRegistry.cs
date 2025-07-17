using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Capabilities;

/// <summary>
/// Agent能力注册实体（聚合根）
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

    // 导航属性
    public virtual ICollection<AgentActionDefinition> ActionDefinitions { get; private set; } = new List<AgentActionDefinition>();

    // 私有构造函数供EF Core
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
    /// 注册新动作
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
    /// 更新健康状态
    /// </summary>
    public void UpdateHealthStatus(HealthStatus status)
    {
        HealthStatus = status ?? throw new ArgumentNullException(nameof(status));
        LastHealthCheck = DateTime.UtcNow;
        LastUpdatedTime = DateTime.UtcNow;

        // 如果健康状态为Critical或Offline，设置为不活跃
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
    /// 检查健康状态
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

        // 检查是否超过了检查间隔
        var timeSinceLastCheck = DateTime.UtcNow - LastHealthCheck;
        if (timeSinceLastCheck.TotalMinutes > 5) // 5分钟无检查视为异常情况
        {
            result.Status = HealthStatus.Warning;
            result.ErrorMessage = "Health check interval exceeded";
        }

        return result;
    }

    /// <summary>
    /// 检查是否支持指定能力
    /// </summary>
    public bool IsCapabilitySupported(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return false;

        return ActionDefinitions.Any(a => a.ActionName.Equals(capability, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 更新性能指标
    /// </summary>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加支持的应用程序
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
    /// 添加所需权限
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
    /// 激活Agent
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 停用Agent
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新配置文件路径
    /// </summary>
    public void UpdateConfigurationFile(string? configurationFile)
    {
        ConfigurationFile = configurationFile ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新安装路径
    /// </summary>
    public void UpdateInstallationPath(string? installationPath)
    {
        InstallationPath = installationPath ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 健康检查结果
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