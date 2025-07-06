using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.UserManagement;

/// <summary>
/// User preferences entity
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the preference category
    /// </summary>
    public string PreferenceCategory { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the preference key
    /// </summary>
    public string PreferenceKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the preference value
    /// </summary>
    public string PreferenceValue { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the value type
    /// </summary>
    public string ValueType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the last updated time
    /// </summary>
    public DateTime LastUpdatedTime { get; private set; }

    /// <summary>
    /// Gets whether this is a system default preference
    /// </summary>
    public bool IsSystemDefault { get; private set; }

    /// <summary>
    /// Gets the description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the user profile
    /// </summary>
    public UserProfile User { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the UserPreferences class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="preferenceCategory">The preference category</param>
    /// <param name="preferenceKey">The preference key</param>
    /// <param name="preferenceValue">The preference value</param>
    /// <param name="valueType">The value type</param>
    /// <param name="description">The description</param>
    /// <param name="isSystemDefault">Whether this is a system default</param>
    public UserPreferences(
        Guid userId,
        string preferenceCategory,
        string preferenceKey,
        string preferenceValue,
        string valueType,
        string? description = null,
        bool isSystemDefault = false)
    {
        UserId = userId;
        PreferenceCategory = preferenceCategory ?? throw new ArgumentNullException(nameof(preferenceCategory));
        PreferenceKey = preferenceKey ?? throw new ArgumentNullException(nameof(preferenceKey));
        PreferenceValue = preferenceValue ?? throw new ArgumentNullException(nameof(preferenceValue));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Description = description;
        IsSystemDefault = isSystemDefault;
        LastUpdatedTime = DateTime.UtcNow;

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private UserPreferences() { }

    /// <summary>
    /// Updates the preference value
    /// </summary>
    /// <param name="preferenceValue">The new preference value</param>
    /// <param name="valueType">The new value type</param>
    public void UpdateValue(string preferenceValue, string? valueType = null)
    {
        PreferenceValue = preferenceValue ?? throw new ArgumentNullException(nameof(preferenceValue));
        if (!string.IsNullOrEmpty(valueType))
            ValueType = valueType;

        LastUpdatedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Gets the typed value of the preference
    /// </summary>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>The typed value</returns>
    public T GetTypedValue<T>()
    {
        return ValueType.ToLower() switch
        {
            "string" => (T)(object)PreferenceValue,
            "int" => (T)(object)int.Parse(PreferenceValue),
            "double" => (T)(object)double.Parse(PreferenceValue),
            "bool" => (T)(object)bool.Parse(PreferenceValue),
            "datetime" => (T)(object)DateTime.Parse(PreferenceValue),
            "json" => System.Text.Json.JsonSerializer.Deserialize<T>(PreferenceValue)!,
            _ => throw new InvalidOperationException($"Unsupported value type: {ValueType}")
        };
    }

    /// <summary>
    /// Sets the typed value of the preference
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to set</param>
    public void SetTypedValue<T>(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var typeString = typeof(T).Name.ToLower();
        var valueString = typeString switch
        {
            "string" => value.ToString()!,
            "int32" => value.ToString()!,
            "double" => value.ToString()!,
            "boolean" => value.ToString()!,
            "datetime" => ((DateTime)(object)value).ToString("O"),
            _ => System.Text.Json.JsonSerializer.Serialize(value)
        };

        UpdateValue(valueString, typeString);
    }

    /// <summary>
    /// Validates the preference
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(PreferenceCategory) &&
               !string.IsNullOrWhiteSpace(PreferenceKey) &&
               !string.IsNullOrWhiteSpace(PreferenceValue) &&
               !string.IsNullOrWhiteSpace(ValueType) &&
               UserId != Guid.Empty;
    }

    /// <summary>
    /// Gets the full preference identifier
    /// </summary>
    /// <returns>The full preference identifier</returns>
    public string GetFullKey()
    {
        return $"{PreferenceCategory}.{PreferenceKey}";
    }
}