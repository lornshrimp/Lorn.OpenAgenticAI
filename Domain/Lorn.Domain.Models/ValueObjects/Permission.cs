using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Permission value object
/// </summary>
public class Permission : ValueObject
{
    /// <summary>
    /// Gets the permission type
    /// </summary>
    public string PermissionType { get; }

    /// <summary>
    /// Gets the resource
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Gets the allowed actions
    /// </summary>
    public List<string> Actions { get; }

    /// <summary>
    /// Gets the constraints
    /// </summary>
    public Dictionary<string, object> Constraints { get; }

    /// <summary>
    /// Initializes a new instance of the Permission class
    /// </summary>
    /// <param name="permissionType">The permission type</param>
    /// <param name="resource">The resource</param>
    /// <param name="actions">The allowed actions</param>
    /// <param name="constraints">The constraints</param>
    public Permission(
        string permissionType,
        string resource,
        List<string>? actions = null,
        Dictionary<string, object>? constraints = null)
    {
        PermissionType = permissionType ?? throw new ArgumentNullException(nameof(permissionType));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Actions = actions ?? new List<string>();
        Constraints = constraints ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Checks if the permission is granted for the specified action and resource
    /// </summary>
    /// <param name="action">The action to check</param>
    /// <param name="resource">The resource to check</param>
    /// <returns>True if granted, false otherwise</returns>
    public bool IsGranted(string action, string resource)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(resource))
            return false;

        // Check if resource matches (exact match or wildcard)
        if (!ResourceMatches(resource))
            return false;

        // Check if action is allowed
        if (!Actions.Contains(action) && !Actions.Contains("*"))
            return false;

        // Check constraints
        return CheckConstraints(action, resource);
    }

    /// <summary>
    /// Checks if the resource matches this permission
    /// </summary>
    /// <param name="resource">The resource to check</param>
    /// <returns>True if matches, false otherwise</returns>
    private bool ResourceMatches(string resource)
    {
        if (Resource == "*") return true; // Wildcard matches all
        if (Resource == resource) return true; // Exact match

        // Pattern matching (simple wildcard support)
        if (Resource.EndsWith("*"))
        {
            var prefix = Resource[..^1];
            return resource.StartsWith(prefix);
        }

        return false;
    }

    /// <summary>
    /// Checks constraints for the permission
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="resource">The resource</param>
    /// <returns>True if constraints are satisfied, false otherwise</returns>
    private bool CheckConstraints(string action, string resource)
    {
        if (Constraints.Count == 0)
            return true;

        // Time-based constraints
        if (Constraints.ContainsKey("timeWindow"))
        {
            var timeWindow = Constraints["timeWindow"].ToString();
            if (!IsWithinTimeWindow(timeWindow))
                return false;
        }

        // Rate limiting constraints
        if (Constraints.ContainsKey("maxRequests"))
        {
            var maxRequests = Convert.ToInt32(Constraints["maxRequests"]);
            if (!IsWithinRateLimit(maxRequests))
                return false;
        }

        // IP-based constraints
        if (Constraints.ContainsKey("allowedIPs"))
        {
            var allowedIPs = (List<string>)Constraints["allowedIPs"];
            if (!IsFromAllowedIP(allowedIPs))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if current time is within allowed time window
    /// </summary>
    /// <param name="timeWindow">The time window specification</param>
    /// <returns>True if within window, false otherwise</returns>
    private bool IsWithinTimeWindow(string? timeWindow)
    {
        if (string.IsNullOrEmpty(timeWindow))
            return true;

        // Simple implementation - could be extended for complex time windows
        var parts = timeWindow.Split('-');
        if (parts.Length == 2)
        {
            if (TimeSpan.TryParse(parts[0], out var start) && TimeSpan.TryParse(parts[1], out var end))
            {
                var now = DateTime.Now.TimeOfDay;
                return now >= start && now <= end;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if request is within rate limit
    /// </summary>
    /// <param name="maxRequests">The maximum requests allowed</param>
    /// <returns>True if within limit, false otherwise</returns>
    private bool IsWithinRateLimit(int maxRequests)
    {
        // This would typically check against a rate limiting service
        // For now, return true as a placeholder
        return true;
    }

    /// <summary>
    /// Checks if request is from allowed IP
    /// </summary>
    /// <param name="allowedIPs">The allowed IP addresses</param>
    /// <returns>True if from allowed IP, false otherwise</returns>
    private bool IsFromAllowedIP(List<string> allowedIPs)
    {
        // This would typically check the current request IP
        // For now, return true as a placeholder
        return true;
    }

    /// <summary>
    /// Creates a permission for file system access
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="actions">The allowed actions</param>
    /// <returns>A file system permission</returns>
    public static Permission CreateFileSystemPermission(string path, params string[] actions)
    {
        return new Permission("FileSystem", path, actions.ToList());
    }

    /// <summary>
    /// Creates a permission for application access
    /// </summary>
    /// <param name="applicationName">The application name</param>
    /// <param name="actions">The allowed actions</param>
    /// <returns>An application permission</returns>
    public static Permission CreateApplicationPermission(string applicationName, params string[] actions)
    {
        return new Permission("Application", applicationName, actions.ToList());
    }

    /// <summary>
    /// Creates a permission for network access
    /// </summary>
    /// <param name="endpoint">The network endpoint</param>
    /// <param name="actions">The allowed actions</param>
    /// <returns>A network permission</returns>
    public static Permission CreateNetworkPermission(string endpoint, params string[] actions)
    {
        return new Permission("Network", endpoint, actions.ToList());
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return PermissionType;
        yield return Resource;
        
        foreach (var action in Actions.OrderBy(a => a))
        {
            yield return action;
        }
        
        foreach (var constraint in Constraints.OrderBy(c => c.Key))
        {
            yield return constraint.Key;
            yield return constraint.Value;
        }
    }
}