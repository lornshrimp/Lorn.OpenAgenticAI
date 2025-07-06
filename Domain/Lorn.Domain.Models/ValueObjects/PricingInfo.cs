using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Pricing information value object
/// </summary>
public class PricingInfo : ValueObject
{
    /// <summary>
    /// Gets the currency
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Gets the input token price per token
    /// </summary>
    public decimal InputPrice { get; }

    /// <summary>
    /// Gets the output token price per token
    /// </summary>
    public decimal OutputPrice { get; }

    /// <summary>
    /// Gets the image processing price
    /// </summary>
    public decimal? ImagePrice { get; }

    /// <summary>
    /// Gets the audio processing price
    /// </summary>
    public decimal? AudioPrice { get; }

    /// <summary>
    /// Gets the free quota in tokens
    /// </summary>
    public int? FreeQuota { get; }

    /// <summary>
    /// Gets the last update time
    /// </summary>
    public DateTime UpdateTime { get; }

    /// <summary>
    /// Gets special pricing for specific features
    /// </summary>
    public Dictionary<string, decimal> SpecialPricing { get; }

    /// <summary>
    /// Initializes a new instance of the PricingInfo class
    /// </summary>
    /// <param name="currency">The currency</param>
    /// <param name="inputPrice">The input token price</param>
    /// <param name="outputPrice">The output token price</param>
    /// <param name="imagePrice">The image processing price</param>
    /// <param name="audioPrice">The audio processing price</param>
    /// <param name="freeQuota">The free quota</param>
    /// <param name="specialPricing">Special pricing</param>
    /// <param name="updateTime">The update time</param>
    public PricingInfo(
        Currency currency,
        decimal inputPrice,
        decimal outputPrice,
        decimal? imagePrice = null,
        decimal? audioPrice = null,
        int? freeQuota = null,
        Dictionary<string, decimal>? specialPricing = null,
        DateTime? updateTime = null)
    {
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        InputPrice = Math.Max(0, inputPrice);
        OutputPrice = Math.Max(0, outputPrice);
        ImagePrice = imagePrice.HasValue ? Math.Max(0, imagePrice.Value) : null;
        AudioPrice = audioPrice.HasValue ? Math.Max(0, audioPrice.Value) : null;
        FreeQuota = freeQuota.HasValue ? Math.Max(0, freeQuota.Value) : null;
        UpdateTime = updateTime ?? DateTime.UtcNow;
        SpecialPricing = specialPricing ?? new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Calculates the cost for token usage
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="specialUsage">Special usage amounts</param>
    /// <returns>The total cost</returns>
    public decimal CalculateCost(int inputTokens, int outputTokens, Dictionary<string, int>? specialUsage = null)
    {
        var totalCost = (inputTokens * InputPrice) + (outputTokens * OutputPrice);

        // Apply special pricing
        if (specialUsage != null)
        {
            foreach (var usage in specialUsage)
            {
                if (SpecialPricing.ContainsKey(usage.Key))
                {
                    totalCost += usage.Value * SpecialPricing[usage.Key];
                }
            }
        }

        // Apply free quota
        if (FreeQuota.HasValue)
        {
            var totalTokens = inputTokens + outputTokens;
            if (totalTokens <= FreeQuota.Value)
            {
                return 0;
            }
            
            // Calculate cost only for tokens beyond free quota
            var chargableTokens = totalTokens - FreeQuota.Value;
            var ratio = (decimal)chargableTokens / totalTokens;
            totalCost *= ratio;
        }

        return Math.Round(totalCost, Currency.GetDecimalPlaces());
    }

    /// <summary>
    /// Checks if the usage is within free quota
    /// </summary>
    /// <param name="totalTokens">Total token count</param>
    /// <returns>True if within free quota, false otherwise</returns>
    public bool IsWithinFreeQuota(int totalTokens)
    {
        return FreeQuota.HasValue && totalTokens <= FreeQuota.Value;
    }

    /// <summary>
    /// Gets the formatted cost string
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="specialUsage">Special usage amounts</param>
    /// <returns>Formatted cost string</returns>
    public string GetFormattedCost(int inputTokens, int outputTokens, Dictionary<string, int>? specialUsage = null)
    {
        var cost = CalculateCost(inputTokens, outputTokens, specialUsage);
        return Currency.FormatAmount(cost);
    }

    /// <summary>
    /// Creates pricing info with zero cost (free model)
    /// </summary>
    /// <param name="currency">The currency</param>
    /// <returns>Free pricing info</returns>
    public static PricingInfo Free(Currency currency)
    {
        return new PricingInfo(currency, 0, 0);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Currency;
        yield return InputPrice;
        yield return OutputPrice;
        
        if (ImagePrice.HasValue)
            yield return ImagePrice.Value;
        
        if (AudioPrice.HasValue)
            yield return AudioPrice.Value;
        
        if (FreeQuota.HasValue)
            yield return FreeQuota.Value;
        
        yield return UpdateTime;
        
        foreach (var pricing in SpecialPricing.OrderBy(x => x.Key))
        {
            yield return pricing.Key;
            yield return pricing.Value;
        }
    }
}

/// <summary>
/// Quota status class
/// </summary>
public class QuotaStatus
{
    /// <summary>
    /// Gets the remaining tokens
    /// </summary>
    public int RemainingTokens { get; }

    /// <summary>
    /// Gets the remaining cost
    /// </summary>
    public decimal RemainingCost { get; }

    /// <summary>
    /// Gets whether an alert should be shown
    /// </summary>
    public bool ShouldAlert { get; }

    /// <summary>
    /// Initializes a new instance of the QuotaStatus class
    /// </summary>
    /// <param name="remainingTokens">The remaining tokens</param>
    /// <param name="remainingCost">The remaining cost</param>
    /// <param name="shouldAlert">Whether an alert should be shown</param>
    public QuotaStatus(int remainingTokens, decimal remainingCost, bool shouldAlert)
    {
        RemainingTokens = remainingTokens;
        RemainingCost = remainingCost;
        ShouldAlert = shouldAlert;
    }
}