using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// Ȩ��ֵ����
/// </summary>
public class Permission : ValueObject
{
    public string PermissionType { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public List<string> Actions { get; private set; } = new();
    public Dictionary<string, object> Constraints { get; private set; } = new();

    public Permission(
        string permissionType,
        string resource,
        List<string>? actions = null,
        Dictionary<string, object>? constraints = null)
    {
        PermissionType = !string.IsNullOrWhiteSpace(permissionType) ? permissionType : throw new ArgumentException("PermissionType cannot be empty", nameof(permissionType));
        Resource = !string.IsNullOrWhiteSpace(resource) ? resource : throw new ArgumentException("Resource cannot be empty", nameof(resource));
        Actions = actions ?? new List<string>();
        Constraints = constraints ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// ����Ƿ�ӵ��ָ��������Ȩ��
    /// </summary>
    public bool IsGranted(string action, string resource)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(resource))
            return false;

        // ��Դƥ����
        if (!IsResourceMatch(resource))
            return false;

        // ����Ȩ�޼��
        return Actions.Contains(action) || Actions.Contains("*");
    }

    private bool IsResourceMatch(string resource)
    {
        if (Resource == "*")
            return true;

        if (Resource.EndsWith("*"))
        {
            var prefix = Resource.Substring(0, Resource.Length - 1);
            return resource.StartsWith(prefix);
        }

        return Resource.Equals(resource, StringComparison.OrdinalIgnoreCase);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return PermissionType;
        yield return Resource;
        
        foreach (var action in Actions)
        {
            yield return action;
        }
        
        foreach (var constraint in Constraints)
        {
            yield return constraint.Key;
            yield return constraint.Value ?? "null";
        }
    }
}