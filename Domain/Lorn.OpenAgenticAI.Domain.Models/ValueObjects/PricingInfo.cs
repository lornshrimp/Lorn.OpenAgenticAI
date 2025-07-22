using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.LLM;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 定价信息值对象
/// </summary>
public class PricingInfo : ValueObject
{
    public Enumerations.Currency Currency { get; private set; } = null!;
    public decimal InputPrice { get; private set; }
    public decimal OutputPrice { get; private set; }
    public decimal? ImagePrice { get; private set; }
    public decimal? AudioPrice { get; private set; }
    public int? FreeQuota { get; private set; }
    public DateTime UpdateTime { get; private set; }

    /// <summary>
    /// 特殊定价条目（导航属性）
    /// </summary>
    [NotMapped]
    public virtual ICollection<PricingSpecialEntry> SpecialPricingEntries { get; set; } = new List<PricingSpecialEntry>();

    public PricingInfo(
        Enumerations.Currency currency,
        decimal inputPrice,
        decimal outputPrice,
        decimal? imagePrice = null,
        decimal? audioPrice = null,
        int? freeQuota = null,
        Dictionary<string, decimal>? specialPricing = null)
    {
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        InputPrice = inputPrice >= 0 ? inputPrice : throw new ArgumentException("InputPrice must be non-negative", nameof(inputPrice));
        OutputPrice = outputPrice >= 0 ? outputPrice : throw new ArgumentException("OutputPrice must be non-negative", nameof(outputPrice));
        ImagePrice = imagePrice >= 0 ? imagePrice : null;
        AudioPrice = audioPrice >= 0 ? audioPrice : null;
        FreeQuota = freeQuota >= 0 ? freeQuota : null;
        UpdateTime = DateTime.UtcNow;
        SpecialPricingEntries = new List<PricingSpecialEntry>();

        // 如果提供了特殊定价字典，转换为实体对象
        if (specialPricing != null)
        {
            foreach (var kvp in specialPricing)
            {
                SpecialPricingEntries.Add(new PricingSpecialEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 计算使用成本
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens, Dictionary<string, int>? specialUsage = null)
    {
        var cost = (inputTokens * InputPrice) + (outputTokens * OutputPrice);

        // 计算特殊使用的成本
        if (specialUsage != null)
        {
            foreach (var usage in specialUsage)
            {
                var specialEntry = SpecialPricingEntries.FirstOrDefault(e => e.PricingKey == usage.Key && e.IsEnabled);
                if (specialEntry != null)
                {
                    cost += usage.Value * specialEntry.Price;
                }
            }
        }

        return cost;
    }

    /// <summary>
    /// 获取特殊定价字典（向后兼容）
    /// </summary>
    public Dictionary<string, decimal> GetSpecialPricing()
    {
        return SpecialPricingEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.PricingKey, e => e.Price);
    }

    /// <summary>
    /// 设置特殊定价（向后兼容）
    /// </summary>
    public void SetSpecialPricing(string key, decimal price)
    {
        var existing = SpecialPricingEntries.FirstOrDefault(e => e.PricingKey == key);
        if (existing != null)
        {
            existing.UpdatePrice(price);
            existing.SetEnabled(true);
        }
        else
        {
            SpecialPricingEntries.Add(new PricingSpecialEntry(key, price));
        }
    }

    /// <summary>
    /// 检查是否在免费额度内
    /// </summary>
    public bool IsWithinFreeQuota(int totalTokens)
    {
        return FreeQuota.HasValue && totalTokens <= FreeQuota.Value;
    }

    /// <summary>
    /// 更新定价信息
    /// </summary>
    public PricingInfo UpdatePricing(
        decimal? inputPrice = null,
        decimal? outputPrice = null,
        decimal? imagePrice = null,
        decimal? audioPrice = null,
        int? freeQuota = null)
    {
        return new PricingInfo(
            Currency,
            inputPrice ?? InputPrice,
            outputPrice ?? OutputPrice,
            imagePrice ?? ImagePrice,
            audioPrice ?? AudioPrice,
            freeQuota ?? FreeQuota,
            GetSpecialPricing()
        );
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Currency.Id;
        yield return InputPrice;
        yield return OutputPrice;
        yield return ImagePrice ?? (object)"null";
        yield return AudioPrice ?? (object)"null";
        yield return FreeQuota ?? (object)"null";
        yield return UpdateTime.Date; // 只比较日期部分

        foreach (var entry in SpecialPricingEntries.Where(e => e.IsEnabled).OrderBy(e => e.PricingKey))
        {
            yield return entry.PricingKey;
            yield return entry.Price;
        }
    }
}

/// <summary>
/// 使用配额值对象
/// </summary>
[ValueObject]
public class UsageQuota : ValueObject
{
    public Guid Id { get; private set; }
    public int? DailyLimit { get; private set; }
    public int? MonthlyLimit { get; private set; }
    public decimal? CostLimit { get; private set; }
    public decimal AlertThreshold { get; private set; }

    // 导航属性 - 指向自定义限制条目
    public virtual ICollection<UsageQuotaCustomLimitEntry> CustomLimitEntries { get; private set; } = new List<UsageQuotaCustomLimitEntry>();

    // EF Core 需要的无参数构造函数
    private UsageQuota()
    {
        Id = Guid.NewGuid();
        AlertThreshold = 0.8m;
        CustomLimitEntries = new List<UsageQuotaCustomLimitEntry>();
    }

    public UsageQuota(
        int? dailyLimit = null,
        int? monthlyLimit = null,
        decimal? costLimit = null,
        decimal alertThreshold = 0.8m,
        Dictionary<string, int>? customLimits = null)
    {
        Id = Guid.NewGuid();
        DailyLimit = dailyLimit >= 0 ? dailyLimit : null;
        MonthlyLimit = monthlyLimit >= 0 ? monthlyLimit : null;
        CostLimit = costLimit >= 0 ? costLimit : null;
        AlertThreshold = alertThreshold >= 0 && alertThreshold <= 1 ? alertThreshold : 0.8m;
        CustomLimitEntries = new List<UsageQuotaCustomLimitEntry>();

        // 将Dictionary转换为Entry实体
        if (customLimits != null)
        {
            foreach (var kvp in customLimits)
            {
                CustomLimitEntries.Add(new UsageQuotaCustomLimitEntry(Id, kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 检查是否在限制范围内
    /// </summary>
    public bool IsWithinLimits(int currentUsage, decimal currentCost)
    {
        if (DailyLimit.HasValue && currentUsage > DailyLimit.Value)
            return false;

        if (MonthlyLimit.HasValue && currentUsage > MonthlyLimit.Value)
            return false;

        if (CostLimit.HasValue && currentCost > CostLimit.Value)
            return false;

        return true;
    }

    /// <summary>
    /// 获取剩余配额
    /// </summary>
    public QuotaStatus GetRemainingQuota(int usedTokens, decimal usedCost)
    {
        var status = new QuotaStatus
        {
            IsWithinLimits = IsWithinLimits(usedTokens, usedCost),
            UsedTokens = usedTokens,
            UsedCost = usedCost
        };

        if (DailyLimit.HasValue)
        {
            status.RemainingDailyTokens = Math.Max(0, DailyLimit.Value - usedTokens);
            status.DailyUsagePercentage = (double)usedTokens / DailyLimit.Value;
        }

        if (MonthlyLimit.HasValue)
        {
            status.RemainingMonthlyTokens = Math.Max(0, MonthlyLimit.Value - usedTokens);
            status.MonthlyUsagePercentage = (double)usedTokens / MonthlyLimit.Value;
        }

        if (CostLimit.HasValue)
        {
            status.RemainingCostLimit = Math.Max(0, CostLimit.Value - usedCost);
            status.CostUsagePercentage = (double)(usedCost / CostLimit.Value);
        }

        // 检查是否达到警告阈值
        status.IsNearLimit = status.DailyUsagePercentage >= (double)AlertThreshold ||
                            status.MonthlyUsagePercentage >= (double)AlertThreshold ||
                            status.CostUsagePercentage >= (double)AlertThreshold;

        return status;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;
        yield return DailyLimit ?? (object)"null";
        yield return MonthlyLimit ?? (object)"null";
        yield return CostLimit ?? (object)"null";
        yield return AlertThreshold;

        foreach (var entry in CustomLimitEntries.OrderBy(e => e.LimitName))
        {
            yield return entry.LimitName;
            yield return entry.LimitValue;
        }
    }

    /// <summary>
    /// 获取自定义限制字典（用于向后兼容）
    /// </summary>
    public Dictionary<string, int> GetCustomLimits()
    {
        return CustomLimitEntries.ToDictionary(e => e.LimitName, e => e.LimitValue);
    }

    /// <summary>
    /// 添加或更新自定义限制
    /// </summary>
    public void SetCustomLimit(string limitName, int limitValue)
    {
        var existingEntry = CustomLimitEntries.FirstOrDefault(e => e.LimitName == limitName);
        if (existingEntry != null)
        {
            existingEntry.UpdateLimitValue(limitValue);
        }
        else
        {
            CustomLimitEntries.Add(new UsageQuotaCustomLimitEntry(Id, limitName, limitValue));
        }
    }
}

/// <summary>
/// 配额状态
/// </summary>
public class QuotaStatus
{
    public bool IsWithinLimits { get; set; }
    public bool IsNearLimit { get; set; }
    public int UsedTokens { get; set; }
    public decimal UsedCost { get; set; }
    public int? RemainingDailyTokens { get; set; }
    public int? RemainingMonthlyTokens { get; set; }
    public decimal? RemainingCostLimit { get; set; }
    public double DailyUsagePercentage { get; set; }
    public double MonthlyUsagePercentage { get; set; }
    public double CostUsagePercentage { get; set; }
}