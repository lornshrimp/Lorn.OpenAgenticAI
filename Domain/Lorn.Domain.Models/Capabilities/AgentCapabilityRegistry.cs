using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Capabilities;

/// <summary>
/// Agent capability registry aggregate root
/// </summary>
public class AgentCapabilityRegistry : AggregateRoot
{
    private readonly List<AgentActionDefinition> _actionDefinitions = new();

    /// <summary>
    /// Gets the agent identifier
    /// </summary>
    public string AgentId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the agent name
    /// </summary>
    public string AgentName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the agent type
    /// </summary>
    public AgentType AgentType { get; private set; } = AgentType.Custom;

    /// <summary>
    /// Gets the agent version
    /// </summary>
    public new string Version { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether this is a system agent
    /// </summary>
    public bool IsSystemAgent { get; private set; }

    /// <summary>
    /// Gets the description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the supported applications
    /// </summary>
    public List<string> SupportedApplications { get; private set; } = new();

    /// <summary>
    /// Gets the required permissions
    /// </summary>
    public List<Permission> RequiredPermissions { get; private set; } = new();

    /// <summary>
    /// Gets the installation path
    /// </summary>
    public string? InstallationPath { get; private set; }

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string? ConfigurationFile { get; private set; }

    /// <summary>
    /// Gets the last health check time
    /// </summary>
    public DateTime? LastHealthCheck { get; private set; }

    /// <summary>
    /// Gets the health status
    /// </summary>
    public HealthStatus HealthStatus { get; private set; } = HealthStatus.Unknown;

    /// <summary>
    /// Gets the performance metrics
    /// </summary>
    public PerformanceMetrics PerformanceMetrics { get; private set; }

    /// <summary>
    /// Gets the registration time
    /// </summary>
    public DateTime RegistrationTime { get; private set; }

    /// <summary>
    /// Gets the last updated time
    /// </summary>
    public DateTime LastUpdatedTime { get; private set; }

    /// <summary>
    /// Gets the action definitions
    /// </summary>
    public IReadOnlyList<AgentActionDefinition> ActionDefinitions => _actionDefinitions.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the AgentCapabilityRegistry class
    /// </summary>
    /// <param name="agentId">The agent identifier</param>
    /// <param name="agentName">The agent name</param>
    /// <param name="agentType">The agent type</param>
    /// <param name="version">The version</param>
    /// <param name="description">The description</param>
    /// <param name="isSystemAgent">Whether this is a system agent</param>
    public AgentCapabilityRegistry(
        string agentId,
        string agentName,
        AgentType agentType,
        string version,
        string description,
        bool isSystemAgent = false)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsSystemAgent = isSystemAgent;
        PerformanceMetrics = PerformanceMetrics.Default();
        RegistrationTime = DateTime.UtcNow;
        LastUpdatedTime = DateTime.UtcNow;

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AgentRegisteredEvent(agentId, agentName, agentType.Name));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private AgentCapabilityRegistry()
    {
        PerformanceMetrics = PerformanceMetrics.Default();
        AgentType = AgentType.Custom;
        HealthStatus = HealthStatus.Unknown;
    }

    /// <summary>
    /// Updates the agent information
    /// </summary>
    /// <param name="agentName">The new agent name</param>
    /// <param name="description">The new description</param>
    /// <param name="version">The new version</param>
    public void UpdateAgentInfo(string agentName, string description, string version)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new ArgumentException("Agent name cannot be empty", nameof(agentName));

        AgentName = agentName;
        Description = description ?? string.Empty; // Ensure description is not null
        Version = version ?? string.Empty; // Ensure version is not null
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();

        AddDomainEvent(new AgentUpdatedEvent(AgentId, agentName, Description)); // Use the non-null Description
    }

    /// <summary>
    /// Sets the installation path
    /// </summary>
    /// <param name="installationPath">The installation path</param>
    public void SetInstallationPath(string installationPath)
    {
        InstallationPath = installationPath;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the configuration file path
    /// </summary>
    /// <param name="configurationFile">The configuration file path</param>
    public void SetConfigurationFile(string configurationFile)
    {
        ConfigurationFile = configurationFile;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a supported application
    /// </summary>
    /// <param name="applicationName">The application name</param>
    public void AddSupportedApplication(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name cannot be empty", nameof(applicationName));

        if (!SupportedApplications.Contains(applicationName))
        {
            SupportedApplications.Add(applicationName);
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a supported application
    /// </summary>
    /// <param name="applicationName">The application name</param>
    public void RemoveSupportedApplication(string applicationName)
    {
        if (SupportedApplications.Remove(applicationName))
        {
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Adds a required permission
    /// </summary>
    /// <param name="permission">The permission</param>
    public void AddRequiredPermission(Permission permission)
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        if (!RequiredPermissions.Contains(permission))
        {
            RequiredPermissions.Add(permission);
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a required permission
    /// </summary>
    /// <param name="permission">The permission</param>
    public void RemoveRequiredPermission(Permission permission)
    {
        if (RequiredPermissions.Remove(permission))
        {
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Registers an action definition
    /// </summary>
    /// <param name="action">The action definition</param>
    public void RegisterAction(AgentActionDefinition action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // Remove existing action with same name
        var existingAction = _actionDefinitions.FirstOrDefault(a => a.ActionName == action.ActionName);
        if (existingAction != null)
            _actionDefinitions.Remove(existingAction);

        _actionDefinitions.Add(action);
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();

        AddDomainEvent(new AgentActionRegisteredEvent(AgentId, action.ActionName, action.ActionDescription));
    }

    /// <summary>
    /// Removes an action definition
    /// </summary>
    /// <param name="actionName">The action name</param>
    public void RemoveAction(string actionName)
    {
        var action = _actionDefinitions.FirstOrDefault(a => a.ActionName == actionName);
        if (action != null)
        {
            _actionDefinitions.Remove(action);
            LastUpdatedTime = DateTime.UtcNow;
            UpdateVersion();

            AddDomainEvent(new AgentActionRemovedEvent(AgentId, actionName));
        }
    }

    /// <summary>
    /// Updates the health status
    /// </summary>
    /// <param name="status">The new health status</param>
    public void UpdateHealthStatus(HealthStatus status)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));

        var previousStatus = HealthStatus;
        HealthStatus = status;
        LastHealthCheck = DateTime.UtcNow;
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();

        if (previousStatus != status)
        {
            AddDomainEvent(new AgentHealthStatusChangedEvent(AgentId, previousStatus.Name, status.Name));
        }
    }

    /// <summary>
    /// Updates the performance metrics
    /// </summary>
    /// <param name="metrics">The new performance metrics</param>
    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        PerformanceMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Performs a health check
    /// </summary>
    /// <returns>The health check result</returns>
    public HealthCheckResult CheckHealth()
    {
        var result = new HealthCheckResult();

        try
        {
            // Check if agent is responsive
            if (LastHealthCheck.HasValue)
            {
                var timeSinceLastCheck = DateTime.UtcNow - LastHealthCheck.Value;
                if (timeSinceLastCheck.TotalMinutes > 30)
                {
                    result.AddIssue("Agent has not reported health status in over 30 minutes");
                    UpdateHealthStatus(HealthStatus.Warning);
                }
            }

            // Check performance metrics
            if (!PerformanceMetrics.IsWithinThresholds(PerformanceThresholds.Default()))
            {
                result.AddIssue("Agent performance metrics are outside normal thresholds");
                if (HealthStatus != HealthStatus.Critical)
                    UpdateHealthStatus(HealthStatus.Warning);
            }

            // If no issues found and not already healthy, mark as healthy
            if (result.IsHealthy && HealthStatus != HealthStatus.Healthy)
            {
                UpdateHealthStatus(HealthStatus.Healthy);
            }
        }
        catch (Exception ex)
        {
            result.AddIssue($"Health check failed: {ex.Message}");
            UpdateHealthStatus(HealthStatus.Critical);
        }

        return result;
    }

    /// <summary>
    /// Checks if the agent supports a specific capability
    /// </summary>
    /// <param name="capability">The capability to check</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool IsCapabilitySupported(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return false;

        // Check if any action provides this capability
        return _actionDefinitions.Any(a => a.ActionName.Equals(capability, StringComparison.OrdinalIgnoreCase) ||
                                          a.ActionDescription.Contains(capability, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets actions by capability
    /// </summary>
    /// <param name="capability">The capability to search for</param>
    /// <returns>List of actions that provide the capability</returns>
    public List<AgentActionDefinition> GetActionsByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return new List<AgentActionDefinition>();

        return _actionDefinitions.Where(a =>
            a.ActionName.Contains(capability, StringComparison.OrdinalIgnoreCase) ||
            a.ActionDescription.Contains(capability, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Activates the agent
    /// </summary>
    public new void Activate()
    {
        base.Activate();
        UpdateHealthStatus(HealthStatus.Healthy);
        AddDomainEvent(new AgentActivatedEvent(AgentId, AgentName));
    }

    /// <summary>
    /// Deactivates the agent
    /// </summary>
    public new void Deactivate()
    {
        base.Deactivate();
        UpdateHealthStatus(HealthStatus.Offline);
        AddDomainEvent(new AgentDeactivatedEvent(AgentId, AgentName));
    }
}

/// <summary>
/// Health check result class
/// </summary>
public class HealthCheckResult
{
    private readonly List<string> _issues = new();

    /// <summary>
    /// Gets whether the health check passed
    /// </summary>
    public bool IsHealthy => _issues.Count == 0;

    /// <summary>
    /// Gets the list of issues found
    /// </summary>
    public IReadOnlyList<string> Issues => _issues.AsReadOnly();

    /// <summary>
    /// Adds an issue to the health check result
    /// </summary>
    /// <param name="issue">The issue description</param>
    public void AddIssue(string issue)
    {
        if (!string.IsNullOrWhiteSpace(issue))
            _issues.Add(issue);
    }

    /// <summary>
    /// Gets a summary of the health check result
    /// </summary>
    /// <returns>The summary</returns>
    public string GetSummary()
    {
        return IsHealthy ? "Healthy" : $"Issues found: {string.Join(", ", _issues)}";
    }
}

/// <summary>
/// Domain event raised when an agent is registered
/// </summary>
public class AgentRegisteredEvent : DomainEvent
{
    public string AgentId { get; }
    public string AgentName { get; }
    public string AgentType { get; }

    public AgentRegisteredEvent(string agentId, string agentName, string agentType)
    {
        AgentId = agentId;
        AgentName = agentName;
        AgentType = agentType;
    }
}

/// <summary>
/// Domain event raised when an agent is updated
/// </summary>
public class AgentUpdatedEvent : DomainEvent
{
    public string AgentId { get; }
    public string AgentName { get; }
    public string Description { get; }

    public AgentUpdatedEvent(string agentId, string agentName, string description)
    {
        AgentId = agentId;
        AgentName = agentName;
        Description = description;
    }
}

/// <summary>
/// Domain event raised when an agent action is registered
/// </summary>
public class AgentActionRegisteredEvent : DomainEvent
{
    public string AgentId { get; }
    public string ActionName { get; }
    public string ActionDescription { get; }

    public AgentActionRegisteredEvent(string agentId, string actionName, string actionDescription)
    {
        AgentId = agentId;
        ActionName = actionName;
        ActionDescription = actionDescription;
    }
}

/// <summary>
/// Domain event raised when an agent action is removed
/// </summary>
public class AgentActionRemovedEvent : DomainEvent
{
    public string AgentId { get; }
    public string ActionName { get; }

    public AgentActionRemovedEvent(string agentId, string actionName)
    {
        AgentId = agentId;
        ActionName = actionName;
    }
}

/// <summary>
/// Domain event raised when an agent health status changes
/// </summary>
public class AgentHealthStatusChangedEvent : DomainEvent
{
    public string AgentId { get; }
    public string PreviousStatus { get; }
    public string NewStatus { get; }

    public AgentHealthStatusChangedEvent(string agentId, string previousStatus, string newStatus)
    {
        AgentId = agentId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }
}

/// <summary>
/// Domain event raised when an agent is activated
/// </summary>
public class AgentActivatedEvent : DomainEvent
{
    public string AgentId { get; }
    public string AgentName { get; }

    public AgentActivatedEvent(string agentId, string agentName)
    {
        AgentId = agentId;
        AgentName = agentName;
    }
}

/// <summary>
/// Domain event raised when an agent is deactivated
/// </summary>
public class AgentDeactivatedEvent : DomainEvent
{
    public string AgentId { get; }
    public string AgentName { get; }

    public AgentDeactivatedEvent(string agentId, string agentName)
    {
        AgentId = agentId;
        AgentName = agentName;
    }
}