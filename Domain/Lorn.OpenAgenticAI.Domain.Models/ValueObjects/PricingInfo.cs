using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

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
    public Dictionary<string, decimal> SpecialPricing { get; private set; } = new();

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
        SpecialPricing = specialPricing ?? new Dictionary<string, decimal>();
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
                if (SpecialPricing.TryGetValue(usage.Key, out var price))
                {
                    cost += usage.Value * price;
                }
            }
        }

        return cost;
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
            SpecialPricing
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
        
        foreach (var kvp in SpecialPricing)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}

/// <summary>
/// 使用配额值对象
/// </summary>
public class UsageQuota : ValueObject
{
    public int? DailyLimit { get; private set; }
    public int? MonthlyLimit { get; private set; }
    public decimal? CostLimit { get; private set; }
    public decimal AlertThreshold { get; private set; }
    public Dictionary<string, int> CustomLimits { get; private set; } = new();

    public UsageQuota(
        int? dailyLimit = null,
        int? monthlyLimit = null,
        decimal? costLimit = null,
        decimal alertThreshold = 0.8m,
        Dictionary<string, int>? customLimits = null)
    {
        DailyLimit = dailyLimit >= 0 ? dailyLimit : null;
        MonthlyLimit = monthlyLimit >= 0 ? monthlyLimit : null;
        CostLimit = costLimit >= 0 ? costLimit : null;
        AlertThreshold = alertThreshold >= 0 && alertThreshold <= 1 ? alertThreshold : 0.8m;
        CustomLimits = customLimits ?? new Dictionary<string, int>();
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
        yield return DailyLimit ?? (object)"null";
        yield return MonthlyLimit ?? (object)"null";
        yield return CostLimit ?? (object)"null";
        yield return AlertThreshold;
        
        foreach (var kvp in CustomLimits)
        {
            yield return kvp.Key;
            yield return kvp.Value;
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