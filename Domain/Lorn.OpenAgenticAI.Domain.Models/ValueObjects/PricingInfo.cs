using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.LLM;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// ������Ϣֵ����
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
    /// ���ⶨ����Ŀ���������ԣ�
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

        // ����ṩ�����ⶨ���ֵ䣬ת��Ϊʵ�����
        if (specialPricing != null)
        {
            foreach (var kvp in specialPricing)
            {
                SpecialPricingEntries.Add(new PricingSpecialEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// ����ʹ�óɱ�
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens, Dictionary<string, int>? specialUsage = null)
    {
        var cost = (inputTokens * InputPrice) + (outputTokens * OutputPrice);

        // ��������ʹ�õĳɱ�
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
    /// ��ȡ���ⶨ���ֵ䣨�����ݣ�
    /// </summary>
    public Dictionary<string, decimal> GetSpecialPricing()
    {
        return SpecialPricingEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.PricingKey, e => e.Price);
    }

    /// <summary>
    /// �������ⶨ�ۣ������ݣ�
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
    /// ����Ƿ�����Ѷ����
    /// </summary>
    public bool IsWithinFreeQuota(int totalTokens)
    {
        return FreeQuota.HasValue && totalTokens <= FreeQuota.Value;
    }

    /// <summary>
    /// ���¶�����Ϣ
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
        yield return UpdateTime.Date; // ֻ�Ƚ����ڲ���

        foreach (var entry in SpecialPricingEntries.Where(e => e.IsEnabled).OrderBy(e => e.PricingKey))
        {
            yield return entry.PricingKey;
            yield return entry.Price;
        }
    }
}

/// <summary>
/// ʹ�����ֵ����
/// </summary>
[ValueObject]
public class UsageQuota : ValueObject
{
    public Guid Id { get; private set; }
    public int? DailyLimit { get; private set; }
    public int? MonthlyLimit { get; private set; }
    public decimal? CostLimit { get; private set; }
    public decimal AlertThreshold { get; private set; }

    // �������� - ָ���Զ���������Ŀ
    public virtual ICollection<UsageQuotaCustomLimitEntry> CustomLimitEntries { get; private set; } = new List<UsageQuotaCustomLimitEntry>();

    // EF Core ��Ҫ���޲������캯��
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

        // ��Dictionaryת��ΪEntryʵ��
        if (customLimits != null)
        {
            foreach (var kvp in customLimits)
            {
                CustomLimitEntries.Add(new UsageQuotaCustomLimitEntry(Id, kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// ����Ƿ������Ʒ�Χ��
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
    /// ��ȡʣ�����
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

        // ����Ƿ�ﵽ������ֵ
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
    /// ��ȡ�Զ��������ֵ䣨���������ݣ�
    /// </summary>
    public Dictionary<string, int> GetCustomLimits()
    {
        return CustomLimitEntries.ToDictionary(e => e.LimitName, e => e.LimitValue);
    }

    /// <summary>
    /// ��ӻ�����Զ�������
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
/// ���״̬
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