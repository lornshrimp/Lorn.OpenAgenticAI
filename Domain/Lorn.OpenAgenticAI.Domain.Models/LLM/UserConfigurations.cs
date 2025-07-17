using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 提供商用户配置实体
/// </summary>
public class ProviderUserConfiguration
{
    public Guid ConfigurationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public ApiConfiguration UserApiConfiguration { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public int Priority { get; private set; }
    public UsageQuota UsageQuota { get; private set; } = null!;
    public Dictionary<string, object> CustomSettings { get; private set; } = new();
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }
    public DateTime? LastUsedTime { get; private set; }

    // 导航属性
    public virtual UserManagement.UserProfile User { get; private set; } = null!;
    public virtual ModelProvider Provider { get; private set; } = null!;
    public virtual ICollection<ModelUserConfiguration> ModelConfigurations { get; private set; } = new List<ModelUserConfiguration>();

    // 私有构造函数供EF Core
    private ProviderUserConfiguration() { }

    public ProviderUserConfiguration(
        Guid userId,
        Guid providerId,
        ApiConfiguration userApiConfiguration,
        bool isEnabled = true,
        int priority = 1,
        UsageQuota? usageQuota = null,
        Dictionary<string, object>? customSettings = null)
    {
        ConfigurationId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        ProviderId = providerId != Guid.Empty ? providerId : throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));
        UserApiConfiguration = userApiConfiguration ?? throw new ArgumentNullException(nameof(userApiConfiguration));
        IsEnabled = isEnabled;
        Priority = priority > 0 ? priority : 1;
        UsageQuota = usageQuota ?? new UsageQuota();
        CustomSettings = customSettings ?? new Dictionary<string, object>();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();

        if (UserId == Guid.Empty)
        {
            result.AddError("UserId", "User ID is required");
        }

        if (ProviderId == Guid.Empty)
        {
            result.AddError("ProviderId", "Provider ID is required");
        }

        if (UserApiConfiguration == null)
        {
            result.AddError("UserApiConfiguration", "API configuration is required");
        }
        else
        {
            var configValidation = UserApiConfiguration.ValidateConfiguration();
            if (!configValidation.IsValid)
            {
                foreach (var error in configValidation.Errors)
                {
                    result.AddError($"UserApiConfiguration.{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 检查是否在配额内
    /// </summary>
    public bool IsWithinQuota(int tokenUsage)
    {
        return UsageQuota.IsWithinLimits(tokenUsage, 0);
    }

    /// <summary>
    /// 更新最后使用时间
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public void UpdateConfiguration(
        ApiConfiguration? userApiConfiguration = null,
        bool? isEnabled = null,
        int? priority = null,
        UsageQuota? usageQuota = null,
        Dictionary<string, object>? customSettings = null)
    {
        if (userApiConfiguration != null)
            UserApiConfiguration = userApiConfiguration;

        if (isEnabled.HasValue)
            IsEnabled = isEnabled.Value;

        if (priority.HasValue && priority.Value > 0)
            Priority = priority.Value;

        if (usageQuota != null)
            UsageQuota = usageQuota;

        if (customSettings != null)
            CustomSettings = customSettings;

        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用配置
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用配置
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 模型用户配置实体
/// </summary>
public class ModelUserConfiguration
{
    public Guid ConfigurationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ModelId { get; private set; }
    public Guid ProviderId { get; private set; }
    public bool IsEnabled { get; private set; }
    public int Priority { get; private set; }
    public ModelParameters DefaultParameters { get; private set; } = null!;
    public QualitySettings QualitySettings { get; private set; } = null!;
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; private set; }
    public DateTime? LastUsedTime { get; private set; }

    // 导航属性
    public virtual UserManagement.UserProfile User { get; private set; } = null!;
    public virtual Model Model { get; private set; } = null!;
    public virtual ModelProvider Provider { get; private set; } = null!;

    // 私有构造函数供EF Core
    private ModelUserConfiguration() { }

    public ModelUserConfiguration(
        Guid userId,
        Guid modelId,
        Guid providerId,
        bool isEnabled = true,
        int priority = 1,
        ModelParameters? defaultParameters = null,
        QualitySettings? qualitySettings = null)
    {
        ConfigurationId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        ModelId = modelId != Guid.Empty ? modelId : throw new ArgumentException("ModelId cannot be empty", nameof(modelId));
        ProviderId = providerId != Guid.Empty ? providerId : throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));
        IsEnabled = isEnabled;
        Priority = priority > 0 ? priority : 1;
        DefaultParameters = defaultParameters ?? ModelParameters.CreateBalanced();
        QualitySettings = qualitySettings ?? new QualitySettings();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证参数
    /// </summary>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        if (DefaultParameters == null)
        {
            result.AddError("DefaultParameters", "Default parameters are required");
        }
        else
        {
            var paramValidation = DefaultParameters.ValidateParameters();
            if (!paramValidation.IsValid)
            {
                foreach (var error in paramValidation.Errors)
                {
                    result.AddError($"DefaultParameters.{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 构建请求参数
    /// </summary>
    public ModelParameters BuildRequestParameters(Dictionary<string, object>? overrides = null)
    {
        if (overrides == null || overrides.Count == 0)
            return DefaultParameters;

        // 这里应该将overrides转换为ModelParameters并合并
        // 简化实现，直接返回默认参数
        return DefaultParameters;
    }

    /// <summary>
    /// 更新默认参数
    /// </summary>
    public void UpdateDefaultParameters(ModelParameters parameters)
    {
        DefaultParameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新质量设置
    /// </summary>
    public void UpdateQualitySettings(QualitySettings settings)
    {
        QualitySettings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录使用
    /// </summary>
    public void RecordUsage()
    {
        LastUsedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用配置
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用配置
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedTime = DateTime.UtcNow;
    }
}