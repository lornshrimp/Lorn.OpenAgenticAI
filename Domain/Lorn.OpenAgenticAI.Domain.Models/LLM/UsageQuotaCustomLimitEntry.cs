using System;
using System.Text.Json;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 使用配额自定义限制条目实体 - 用于存储UsageQuota中的CustomLimits Dictionary
/// </summary>
public class UsageQuotaCustomLimitEntry : IEntity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// 关联的使用配额ID
    /// </summary>
    public Guid UsageQuotaId { get; private set; }

    /// <summary>
    /// 限制名称
    /// </summary>
    public string LimitName { get; private set; } = string.Empty;

    /// <summary>
    /// 限制值
    /// </summary>
    public int LimitValue { get; private set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; private set; }

    // EF Core 需要的无参数构造函数
    private UsageQuotaCustomLimitEntry()
    {
        Id = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public UsageQuotaCustomLimitEntry(
        Guid usageQuotaId,
        string limitName,
        int limitValue,
        string? description = null)
    {
        Id = Guid.NewGuid();
        UsageQuotaId = usageQuotaId;
        LimitName = limitName ?? throw new ArgumentNullException(nameof(limitName));
        LimitValue = limitValue;
        Description = description;
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新限制值
    /// </summary>
    public void UpdateLimitValue(int newValue)
    {
        LimitValue = newValue;
    }

    /// <summary>
    /// 更新描述
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}
