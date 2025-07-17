using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 模型服务提供者实体（聚合根）
/// </summary>
public class ModelProvider
{
    public Guid ProviderId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public Guid ProviderTypeId { get; private set; }
    public string? IconUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? ApiKeyUrl { get; private set; }
    public string? DocsUrl { get; private set; }
    public string? ModelsUrl { get; private set; }
    public ApiConfiguration DefaultApiConfiguration { get; private set; } = null!;
    public bool IsPrebuilt { get; private set; }
    public ServiceStatus Status { get; private set; } = null!;
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }
    public Guid? CreatedBy { get; private set; }

    // 导航属性
    public virtual ProviderType ProviderType { get; private set; } = null!;
    public virtual ICollection<Model> Models { get; private set; } = new List<Model>();
    public virtual ICollection<ProviderUserConfiguration> UserConfigurations { get; private set; } = new List<ProviderUserConfiguration>();

    // 私有构造函数供EF Core
    private ModelProvider() { }

    public ModelProvider(
        string providerName,
        Guid providerTypeId,
        ApiConfiguration defaultApiConfiguration,
        bool isPrebuilt = false,
        string? iconUrl = null,
        string? websiteUrl = null,
        string? apiKeyUrl = null,
        string? docsUrl = null,
        string? modelsUrl = null,
        Guid? createdBy = null)
    {
        ProviderId = Guid.NewGuid();
        ProviderName = !string.IsNullOrWhiteSpace(providerName) ? providerName : throw new ArgumentException("ProviderName cannot be empty", nameof(providerName));
        ProviderTypeId = providerTypeId != Guid.Empty ? providerTypeId : throw new ArgumentException("ProviderTypeId cannot be empty", nameof(providerTypeId));
        DefaultApiConfiguration = defaultApiConfiguration ?? throw new ArgumentNullException(nameof(defaultApiConfiguration));
        IsPrebuilt = isPrebuilt;
        Status = ServiceStatus.Available;
        IconUrl = iconUrl;
        WebsiteUrl = websiteUrl;
        ApiKeyUrl = apiKeyUrl;
        DocsUrl = docsUrl;
        ModelsUrl = modelsUrl;
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// 验证提供者配置
    /// </summary>
    public ValidationResult ValidateProvider()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ProviderName))
        {
            result.AddError("ProviderName", "Provider name is required");
        }

        if (ProviderTypeId == Guid.Empty)
        {
            result.AddError("ProviderTypeId", "Provider type is required");
        }

        if (DefaultApiConfiguration == null)
        {
            result.AddError("DefaultApiConfiguration", "Default API configuration is required");
        }
        else
        {
            var configValidation = DefaultApiConfiguration.ValidateConfiguration();
            if (!configValidation.IsValid)
            {
                foreach (var error in configValidation.Errors)
                {
                    result.AddError($"DefaultApiConfiguration.{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public void UpdateStatus(ServiceStatus newStatus)
    {
        Status = newStatus ?? throw new ArgumentNullException(nameof(newStatus));
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加模型
    /// </summary>
    public void AddModel(Model model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        if (model.ProviderId != ProviderId)
            throw new ArgumentException("Model must belong to this provider", nameof(model));

        // 检查是否已存在同名模型
        var existingModel = Models.FirstOrDefault(m => m.ModelName == model.ModelName);
        if (existingModel != null)
        {
            throw new InvalidOperationException($"Model '{model.ModelName}' already exists in this provider");
        }

        Models.Add(model);
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 移除模型
    /// </summary>
    public void RemoveModel(Guid modelId)
    {
        var model = Models.FirstOrDefault(m => m.ModelId == modelId);
        if (model != null)
        {
            Models.Remove(model);
            UpdatedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 检查是否与指定类型兼容
    /// </summary>
    public bool IsCompatibleWith(ProviderType type)
    {
        return ProviderTypeId == type.TypeId;
    }

    /// <summary>
    /// 更新提供者信息
    /// </summary>
    public void UpdateProvider(
        string? providerName = null,
        string? iconUrl = null,
        string? websiteUrl = null,
        string? apiKeyUrl = null,
        string? docsUrl = null,
        string? modelsUrl = null,
        ApiConfiguration? defaultApiConfiguration = null)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
            ProviderName = providerName;

        IconUrl = iconUrl;
        WebsiteUrl = websiteUrl;
        ApiKeyUrl = apiKeyUrl;
        DocsUrl = docsUrl;
        ModelsUrl = modelsUrl;

        if (defaultApiConfiguration != null)
            DefaultApiConfiguration = defaultApiConfiguration;

        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取活跃模型数量
    /// </summary>
    public int GetActiveModelCount()
    {
        return Models.Count(m => m.IsActive());
    }

    /// <summary>
    /// 获取模型按组分类
    /// </summary>
    public Dictionary<string, List<Model>> GetModelsByGroup()
    {
        return Models
            .Where(m => m.IsActive())
            .GroupBy(m => m.ModelGroup ?? "Default")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// 检查提供者是否可用
    /// </summary>
    public bool IsAvailable()
    {
        return Status == ServiceStatus.Available && GetActiveModelCount() > 0;
    }
}