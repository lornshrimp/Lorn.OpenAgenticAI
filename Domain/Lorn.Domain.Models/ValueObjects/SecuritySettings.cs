using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Security settings value object
/// </summary>
public class SecuritySettings : ValueObject
{
    /// <summary>
    /// Gets the authentication method
    /// </summary>
    public string AuthenticationMethod { get; }

    /// <summary>
    /// Gets the session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; }

    /// <summary>
    /// Gets whether two-factor authentication is required
    /// </summary>
    public bool RequireTwoFactor { get; }

    /// <summary>
    /// Gets the password last changed date
    /// </summary>
    public DateTime PasswordLastChanged { get; }

    /// <summary>
    /// Gets additional security settings
    /// </summary>
    public Dictionary<string, string> AdditionalSettings { get; }

    /// <summary>
    /// Initializes a new instance of the SecuritySettings class
    /// </summary>
    /// <param name="authenticationMethod">The authentication method</param>
    /// <param name="sessionTimeoutMinutes">The session timeout in minutes</param>
    /// <param name="requireTwoFactor">Whether two-factor authentication is required</param>
    /// <param name="passwordLastChanged">The password last changed date</param>
    /// <param name="additionalSettings">Additional security settings</param>
    public SecuritySettings(
        string authenticationMethod,
        int sessionTimeoutMinutes,
        bool requireTwoFactor,
        DateTime passwordLastChanged,
        Dictionary<string, string>? additionalSettings = null)
    {
        AuthenticationMethod = authenticationMethod ?? throw new ArgumentNullException(nameof(authenticationMethod));
        SessionTimeoutMinutes = sessionTimeoutMinutes;
        RequireTwoFactor = requireTwoFactor;
        PasswordLastChanged = passwordLastChanged;
        AdditionalSettings = additionalSettings ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Validates the security settings
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AuthenticationMethod) &&
               SessionTimeoutMinutes > 0 &&
               SessionTimeoutMinutes <= 1440 && // Max 24 hours
               PasswordLastChanged <= DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if password change is required
    /// </summary>
    /// <param name="passwordExpirationDays">The password expiration period in days</param>
    /// <returns>True if password change is required, false otherwise</returns>
    public bool RequiresPasswordChange(int passwordExpirationDays = 90)
    {
        return DateTime.UtcNow.Subtract(PasswordLastChanged).TotalDays >= passwordExpirationDays;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AuthenticationMethod;
        yield return SessionTimeoutMinutes;
        yield return RequireTwoFactor;
        yield return PasswordLastChanged;

        foreach (var setting in AdditionalSettings.OrderBy(x => x.Key))
        {
            yield return setting.Key;
            yield return setting.Value;
        }
    }
}