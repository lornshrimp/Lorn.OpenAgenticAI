using Lorn.Domain.Models.Common;

namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Authentication method enumeration
/// </summary>
public sealed class AuthenticationMethod : Enumeration
{
    /// <summary>
    /// API key authentication
    /// </summary>
    public static readonly AuthenticationMethod ApiKey = new(1, nameof(ApiKey), "Authentication using API key");

    /// <summary>
    /// OAuth2 authentication
    /// </summary>
    public static readonly AuthenticationMethod OAuth2 = new(2, nameof(OAuth2), "OAuth 2.0 authentication flow");

    /// <summary>
    /// Bearer token authentication
    /// </summary>
    public static readonly AuthenticationMethod BearerToken = new(3, nameof(BearerToken), "Bearer token authentication");

    /// <summary>
    /// Custom authentication
    /// </summary>
    public static readonly AuthenticationMethod CustomAuth = new(4, nameof(CustomAuth), "Custom authentication method");

    /// <summary>
    /// No authentication required
    /// </summary>
    public static readonly AuthenticationMethod None = new(5, nameof(None), "No authentication required");

    /// <summary>
    /// Gets the description of the authentication method
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the AuthenticationMethod class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private AuthenticationMethod(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Checks if this authentication method requires credentials
    /// </summary>
    /// <returns>True if credentials are required, false otherwise</returns>
    public bool RequiresCredentials()
    {
        return this != None;
    }

    /// <summary>
    /// Checks if this authentication method supports automatic refresh
    /// </summary>
    /// <returns>True if automatic refresh is supported, false otherwise</returns>
    public bool SupportsAutoRefresh()
    {
        return this == OAuth2 || this == BearerToken;
    }

    /// <summary>
    /// Gets the security level of this authentication method
    /// </summary>
    /// <returns>The security level (1-5, where 5 is most secure)</returns>
    public int GetSecurityLevel()
    {
        return this switch
        {
            var m when m == None => 1,
            var m when m == ApiKey => 2,
            var m when m == BearerToken => 3,
            var m when m == CustomAuth => 3,
            var m when m == OAuth2 => 5,
            _ => 1
        };
    }
}