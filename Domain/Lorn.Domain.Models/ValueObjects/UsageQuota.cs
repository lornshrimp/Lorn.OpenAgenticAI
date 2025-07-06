using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Usage quota value object
/// </summary>
public class UsageQuota : ValueObject
{
    /// <summary>
    /// Gets the daily token limit
    /// </summary>
    public int? DailyLimit { get; }

    /// <summary>
    /// Gets the monthly token limit
    /// </summary>
    public int? MonthlyLimit { get; }

    /// <summary>
    /// Gets the cost limit
    /// </summary>
    public decimal? CostLimit { get; }

    /// <summary>
    /// Gets the alert threshold percentage
    /// </summary>
    public decimal AlertThreshold { get; }

    /// <summary>
    /// Gets custom limits
    /// </summary>
    public Dictionary<string, int> CustomLimits { get; }

    /// <summary>
    /// Gets the remaining requests (for backward compatibility)
    /// </summary>
    public int RemainingRequests { get; private set; }

    /// <summary>
    /// Gets the remaining tokens (for backward compatibility)
    /// </summary>
    public int RemainingTokens { get; private set; }

    /// <summary>
    /// Gets the reset time (for backward compatibility)
    /// </summary>
    public DateTime ResetTime { get; private set; }

    /// <summary>
    /// Initializes a new instance of the UsageQuota class
    /// </summary>
    /// <param name="dailyLimit">The daily token limit</param>
    /// <param name="monthlyLimit">The monthly token limit</param>
    /// <param name="costLimit">The cost limit</param>
    /// <param name="alertThreshold">The alert threshold percentage (0-1)</param>
    /// <param name="customLimits">Custom limits</param>
    /// <param name="remainingRequests">The remaining requests (optional, for backward compatibility)</param>
    /// <param name="remainingTokens">The remaining tokens (optional, for backward compatibility)</param>
    /// <param name="resetTime">The reset time (optional, for backward compatibility)</param>
    public UsageQuota(
        int? dailyLimit = null,
        int? monthlyLimit = null,
        decimal? costLimit = null,
        decimal alertThreshold = 0.8m,
        Dictionary<string, int>? customLimits = null,
        int remainingRequests = 0,
        int remainingTokens = 0,
        DateTime? resetTime = null)
    {
        DailyLimit = dailyLimit.HasValue ? Math.Max(0, dailyLimit.Value) : null;
        MonthlyLimit = monthlyLimit.HasValue ? Math.Max(0, monthlyLimit.Value) : null;
        CostLimit = costLimit.HasValue ? Math.Max(0, costLimit.Value) : null;
        AlertThreshold = Math.Max(0, Math.Min(1, alertThreshold));
        CustomLimits = customLimits ?? new Dictionary<string, int>();
        RemainingRequests = remainingRequests;
        RemainingTokens = remainingTokens;
        ResetTime = resetTime ?? DateTime.UtcNow.AddHours(1);
    }

    /// <summary>
    /// Checks if usage is within limits
    /// </summary>
    /// <param name="currentUsage">Current token usage</param>
    /// <param name="currentCost">Current cost</param>
    /// <returns>True if within limits, false otherwise</returns>
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
    /// Gets the remaining quota
    /// </summary>
    /// <param name="usedTokens">Used tokens</param>
    /// <param name="usedCost">Used cost</param>
    /// <returns>Quota status</returns>
    public QuotaStatus GetRemainingQuota(int usedTokens, decimal usedCost)
    {
        var dailyRemaining = DailyLimit.HasValue ? Math.Max(0, DailyLimit.Value - usedTokens) : int.MaxValue;
        var monthlyRemaining = MonthlyLimit.HasValue ? Math.Max(0, MonthlyLimit.Value - usedTokens) : int.MaxValue;
        var costRemaining = CostLimit.HasValue ? Math.Max(0, CostLimit.Value - usedCost) : decimal.MaxValue;

        var shouldAlert = false;
        if (DailyLimit.HasValue)
            shouldAlert |= (decimal)usedTokens / DailyLimit.Value >= AlertThreshold;
        if (MonthlyLimit.HasValue)
            shouldAlert |= (decimal)usedTokens / MonthlyLimit.Value >= AlertThreshold;
        if (CostLimit.HasValue)
            shouldAlert |= usedCost / CostLimit.Value >= AlertThreshold;

        return new QuotaStatus(
            Math.Min(dailyRemaining, monthlyRemaining),
            costRemaining,
            shouldAlert);
    }

    /// <summary>
    /// Creates unlimited quota
    /// </summary>
    /// <returns>Unlimited usage quota</returns>
    public static UsageQuota Unlimited()
    {
        return new UsageQuota();
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        if (DailyLimit.HasValue)
            yield return DailyLimit.Value;
        
        if (MonthlyLimit.HasValue)
            yield return MonthlyLimit.Value;
        
        if (CostLimit.HasValue)
            yield return CostLimit.Value;
        
        yield return AlertThreshold;
        yield return RemainingRequests;
        yield return RemainingTokens;
        yield return ResetTime;
        
        foreach (var limit in CustomLimits.OrderBy(x => x.Key))
        {
            yield return limit.Key;
            yield return limit.Value;
        }
    }
}