using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

    /// <summary>
    /// 自定义设置条目（导航属性）
    /// </summary>
    public virtual ICollection<ProviderCustomSettingEntry> CustomSettingEntries { get; set; } = new List<ProviderCustomSettingEntry>();

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
        CustomSettingEntries = new List<ProviderCustomSettingEntry>();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;

        // 如果提供了自定义设置字典，转换为实体对象
        if (customSettings != null)
        {
            foreach (var kvp in customSettings)
            {
                CustomSettingEntries.Add(new ProviderCustomSettingEntry(ConfigurationId, kvp.Key, kvp.Value));
            }
        }
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
        {
            // 清除现有设置并添加新设置
            CustomSettingEntries.Clear();
            foreach (var kvp in customSettings)
            {
                CustomSettingEntries.Add(new ProviderCustomSettingEntry(ConfigurationId, kvp.Key, kvp.Value));
            }
        }

        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取自定义设置字典（向后兼容）
    /// </summary>
    public Dictionary<string, object> GetCustomSettings()
    {
        return CustomSettingEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.SettingKey, e => e.GetObjectValue() ?? string.Empty);
    }

    /// <summary>
    /// 设置自定义设置值
    /// </summary>
    public void SetCustomSetting(string key, object value)
    {
        var existingEntry = CustomSettingEntries.FirstOrDefault(e => e.SettingKey == key);
        if (existingEntry != null)
        {
            existingEntry.UpdateValue(value);
        }
        else
        {
            CustomSettingEntries.Add(new ProviderCustomSettingEntry(ConfigurationId, key, value));
        }
        UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取自定义设置值
    /// </summary>
    public T? GetCustomSetting<T>(string key)
    {
        var entry = CustomSettingEntries.FirstOrDefault(e => e.SettingKey == key && e.IsEnabled);
        return entry != null ? entry.GetValue<T>() : default;
    }

    /// <summary>
    /// 移除自定义设置
    /// </summary>
    public void RemoveCustomSetting(string key)
    {
        var entry = CustomSettingEntries.FirstOrDefault(e => e.SettingKey == key);
        if (entry != null)
        {
            CustomSettingEntries.Remove(entry);
            UpdatedTime = DateTime.UtcNow;
        }
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