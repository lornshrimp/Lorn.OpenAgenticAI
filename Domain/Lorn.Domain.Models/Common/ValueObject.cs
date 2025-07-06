namespace Lorn.Domain.Models.Common;

/// <summary>
/// Base class for value objects that implement equality comparison
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected abstract IEnumerable<object> GetAtomicValues();

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object
    /// </summary>
    /// <param name="other">The value object to compare with the current value object</param>
    /// <returns>true if the specified value object is equal to the current value object; otherwise, false</returns>
    public bool Equals(ValueObject? other)
    {
        if (other == null)
            return false;

        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    /// <summary>
    /// Returns the hash code for this value object
    /// </summary>
    /// <returns>A hash code for this value object</returns>
    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>true if equal; otherwise, false</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>true if not equal; otherwise, false</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}