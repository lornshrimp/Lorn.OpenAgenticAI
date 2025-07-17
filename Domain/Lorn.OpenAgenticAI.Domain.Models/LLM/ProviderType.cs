using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 模型服务提供商类型实体
/// </summary>
public class ProviderType
{
    public Guid TypeId { get; private set; }
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AdapterClassName { get; set; } = string.Empty;
    public List<AuthenticationMethod> SupportedAuthMethods { get; set; } = new();
    public Dictionary<string, object> DefaultSettings { get; set; } = new();
    public bool IsBuiltIn { get; private set; }
    public DateTime CreatedTime { get; private set; }

    // 导航属性
    public virtual ICollection<ModelProvider> Providers { get; private set; } = new List<ModelProvider>();

    // 私有构造函数用于EF Core
    private ProviderType()
    {
        TypeId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public ProviderType(
        string typeName,
        string description,
        string adapterClassName,
        List<AuthenticationMethod>? supportedAuthMethods = null,
        Dictionary<string, object>? defaultSettings = null,
        bool isBuiltIn = false)
    {
        TypeId = Guid.NewGuid();
        TypeName = !string.IsNullOrWhiteSpace(typeName) ? typeName : throw new ArgumentException("TypeName cannot be empty", nameof(typeName));
        Description = description ?? string.Empty;
        AdapterClassName = !string.IsNullOrWhiteSpace(adapterClassName) ? adapterClassName : throw new ArgumentException("AdapterClassName cannot be empty", nameof(adapterClassName));
        SupportedAuthMethods = supportedAuthMethods ?? new List<AuthenticationMethod> { AuthenticationMethod.ApiKey };
        DefaultSettings = defaultSettings ?? new Dictionary<string, object>();
        IsBuiltIn = isBuiltIn;
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否支持指定认证方法
    /// </summary>
    public bool SupportsAuthMethod(AuthenticationMethod method)
    {
        return SupportedAuthMethods.Contains(method);
    }

    /// <summary>
    /// 添加支持的认证方法
    /// </summary>
    public void AddSupportedAuthMethod(AuthenticationMethod method)
    {
        if (!SupportedAuthMethods.Contains(method))
        {
            SupportedAuthMethods.Add(method);
        }
    }

    /// <summary>
    /// 更新默认设置
    /// </summary>
    public void UpdateDefaultSettings(Dictionary<string, object>? settings)
    {
        if (settings != null)
        {
            foreach (var kvp in settings)
            {
                DefaultSettings[kvp.Key] = kvp.Value ?? string.Empty;
            }
        }
    }
}