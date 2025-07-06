using System.Reflection;

namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Base class for enumeration types
/// </summary>
public abstract class Enumeration : IComparable<Enumeration>
{
    /// <summary>
    /// Gets the unique identifier of the enumeration
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the name of the enumeration
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Initializes a new instance of the Enumeration class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Returns the string representation of the enumeration
    /// </summary>
    /// <returns>The name of the enumeration</returns>
    public override string ToString() => Name;

    /// <summary>
    /// Gets all enumeration values of the specified type
    /// </summary>
    /// <typeparam name="T">The enumeration type</typeparam>
    /// <returns>All enumeration values</returns>
    public static IEnumerable<T> GetAll<T>() where T : Enumeration
    {
        var type = typeof(T);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null))
                    .Cast<T>();
    }

    /// <summary>
    /// Gets the enumeration value by its identifier
    /// </summary>
    /// <typeparam name="T">The enumeration type</typeparam>
    /// <param name="value">The identifier</param>
    /// <returns>The enumeration value</returns>
    public static T FromValue<T>(int value) where T : Enumeration
    {
        var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
        return matchingItem;
    }

    /// <summary>
    /// Gets the enumeration value by its name
    /// </summary>
    /// <typeparam name="T">The enumeration type</typeparam>
    /// <param name="displayName">The name</param>
    /// <returns>The enumeration value</returns>
    public static T FromDisplayName<T>(string displayName) where T : Enumeration
    {
        var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
        return matchingItem;
    }

    /// <summary>
    /// Parses the enumeration value based on a predicate
    /// </summary>
    /// <typeparam name="T">The enumeration type</typeparam>
    /// <typeparam name="K">The key type</typeparam>
    /// <param name="value">The value to parse</param>
    /// <param name="description">The description of the value</param>
    /// <param name="predicate">The predicate to match</param>
    /// <returns>The enumeration value</returns>
    private static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

        return matchingItem;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue)
            return false;

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    /// <summary>
    /// Returns the hash code for this enumeration
    /// </summary>
    /// <returns>A hash code for this enumeration</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Compares the current enumeration with another enumeration
    /// </summary>
    /// <param name="other">The enumeration to compare with</param>
    /// <returns>A value that indicates the relative order of the objects being compared</returns>
    public int CompareTo(Enumeration? other) => Id.CompareTo(other?.Id);
}