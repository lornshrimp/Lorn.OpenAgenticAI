using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Encrypted string value object for secure storage of sensitive data
/// </summary>
public class EncryptedString : ValueObject
{
    /// <summary>
    /// Gets the encrypted value
    /// </summary>
    public string EncryptedValue { get; }

    /// <summary>
    /// Gets whether the string is empty
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(EncryptedValue);

    /// <summary>
    /// Initializes a new instance of the EncryptedString class
    /// </summary>
    /// <param name="encryptedValue">The encrypted value</param>
    private EncryptedString(string encryptedValue)
    {
        EncryptedValue = encryptedValue ?? string.Empty;
    }

    /// <summary>
    /// Creates an encrypted string from a plain text value
    /// </summary>
    /// <param name="plainValue">The plain text value</param>
    /// <returns>An encrypted string</returns>
    public static EncryptedString Encrypt(string plainValue)
    {
        if (string.IsNullOrEmpty(plainValue))
            return new EncryptedString(string.Empty);

        // In a real implementation, this would use proper encryption
        // For now, we'll use a simple base64 encoding as placeholder
        var encrypted = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainValue));
        return new EncryptedString(encrypted);
    }

    /// <summary>
    /// Creates an encrypted string from an already encrypted value
    /// </summary>
    /// <param name="encryptedValue">The already encrypted value</param>
    /// <returns>An encrypted string</returns>
    public static EncryptedString FromEncrypted(string encryptedValue)
    {
        return new EncryptedString(encryptedValue);
    }

    /// <summary>
    /// Decrypts the string value
    /// </summary>
    /// <returns>The decrypted plain text value</returns>
    public string Decrypt()
    {
        if (IsEmpty)
            return string.Empty;

        try
        {
            // In a real implementation, this would use proper decryption
            // For now, we'll use base64 decoding as placeholder
            var bytes = Convert.FromBase64String(EncryptedValue);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // If decryption fails, return empty string for security
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates an empty encrypted string
    /// </summary>
    /// <returns>An empty encrypted string</returns>
    public static EncryptedString Empty()
    {
        return new EncryptedString(string.Empty);
    }

    /// <summary>
    /// Implicitly converts a plain string to an encrypted string
    /// </summary>
    /// <param name="plainValue">The plain string</param>
    /// <returns>An encrypted string</returns>
    public static implicit operator EncryptedString(string plainValue)
    {
        return Encrypt(plainValue);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return EncryptedValue;
    }

    /// <summary>
    /// Returns a string representation that doesn't expose the encrypted value
    /// </summary>
    /// <returns>A safe string representation</returns>
    public override string ToString()
    {
        return IsEmpty ? "[Empty]" : "[Encrypted]";
    }
}