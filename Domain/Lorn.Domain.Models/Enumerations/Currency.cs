using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Currency enumeration
/// </summary>
public sealed class Currency : Enumeration
{
    /// <summary>
    /// US Dollar
    /// </summary>
    public static readonly Currency USD = new(1, nameof(USD), "United States Dollar", "$");

    /// <summary>
    /// Chinese Yuan
    /// </summary>
    public static readonly Currency CNY = new(2, nameof(CNY), "Chinese Yuan", "?");

    /// <summary>
    /// Euro
    /// </summary>
    public static readonly Currency EUR = new(3, nameof(EUR), "Euro", "€");

    /// <summary>
    /// British Pound
    /// </summary>
    public static readonly Currency GBP = new(4, nameof(GBP), "British Pound Sterling", "?");

    /// <summary>
    /// Japanese Yen
    /// </summary>
    public static readonly Currency JPY = new(5, nameof(JPY), "Japanese Yen", "?");

    /// <summary>
    /// Gets the full name of the currency
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the currency symbol
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Initializes a new instance of the Currency class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The currency code</param>
    /// <param name="fullName">The full name</param>
    /// <param name="symbol">The currency symbol</param>
    private Currency(int id, string name, string fullName, string symbol) : base(id, name)
    {
        FullName = fullName;
        Symbol = symbol;
    }

    /// <summary>
    /// Formats an amount with the currency symbol
    /// </summary>
    /// <param name="amount">The amount to format</param>
    /// <returns>The formatted amount</returns>
    public string FormatAmount(decimal amount)
    {
        return this switch
        {
            var c when c == USD => $"${amount:F2}",
            var c when c == EUR => $"€{amount:F2}",
            var c when c == GBP => $"?{amount:F2}",
            var c when c == CNY => $"?{amount:F2}",
            var c when c == JPY => $"?{amount:F0}",
            _ => $"{Symbol}{amount:F2}"
        };
    }

    /// <summary>
    /// Gets the decimal places typically used for this currency
    /// </summary>
    /// <returns>The number of decimal places</returns>
    public int GetDecimalPlaces()
    {
        return this == JPY ? 0 : 2;
    }

    /// <summary>
    /// Checks if this is a major global currency
    /// </summary>
    /// <returns>True if it's a major currency, false otherwise</returns>
    public bool IsMajorCurrency()
    {
        return this == USD || this == EUR || this == GBP || this == JPY;
    }
}