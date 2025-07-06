using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Execution;
using Lorn.Domain.Models.ValueObjects;
using Lorn.Domain.Models.Workflow;

namespace Lorn.Domain.Models.UserManagement;

/// <summary>
/// User profile entity representing a user in the system
/// </summary>
public class UserProfile : AggregateRoot
{
    private readonly List<UserPreferences> _userPreferences = new();
    private readonly List<TaskExecutionHistory> _executionHistories = new();
    private readonly List<WorkflowTemplate> _workflowTemplates = new();

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the username
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the email address
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Gets the last login time
    /// </summary>
    public DateTime? LastLoginTime { get; private set; }

    /// <summary>
    /// Gets the profile version
    /// </summary>
    public int ProfileVersion { get; private set; } = 1;

    /// <summary>
    /// Gets the security settings
    /// </summary>
    public SecuritySettings SecuritySettings { get; private set; }

    /// <summary>
    /// Gets the metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    /// <summary>
    /// Gets the user preferences
    /// </summary>
    public IReadOnlyList<UserPreferences> UserPreferences => _userPreferences.AsReadOnly();

    /// <summary>
    /// Gets the execution histories
    /// </summary>
    public IReadOnlyList<TaskExecutionHistory> ExecutionHistories => _executionHistories.AsReadOnly();

    /// <summary>
    /// Gets the workflow templates
    /// </summary>
    public IReadOnlyList<WorkflowTemplate> WorkflowTemplates => _workflowTemplates.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the UserProfile class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="username">The username</param>
    /// <param name="email">The email address</param>
    /// <param name="securitySettings">The security settings</param>
    public UserProfile(
        Guid userId,
        string username,
        string? email,
        SecuritySettings securitySettings)
    {
        UserId = userId;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email;
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));

        Id = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserProfileCreatedEvent(userId, username));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private UserProfile()
    {
        SecuritySettings = new SecuritySettings(
            authenticationMethod: "DefaultMethod",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow,
            additionalSettings: new Dictionary<string, string>()
        ); // Ensure SecuritySettings is initialized
        Metadata = new Dictionary<string, object>(); // Ensure Metadata is initialized
    }

    /// <summary>
    /// Validates the user profile
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateProfile()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               Username.Length >= 3 &&
               Username.Length <= 50 &&
               (string.IsNullOrEmpty(Email) || IsValidEmail(Email)) &&
               SecuritySettings.IsValid();
    }

    /// <summary>
    /// Updates the last login time
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginTime = DateTime.UtcNow;
        UpdateVersion();

        AddDomainEvent(new UserLoginEvent(UserId, LastLoginTime.Value));
    }

    /// <summary>
    /// Increments the profile version
    /// </summary>
    public void IncrementProfileVersion()
    {
        ProfileVersion++;
        UpdateVersion();
    }

    /// <summary>
    /// Updates the user profile information
    /// </summary>
    /// <param name="username">The new username</param>
    /// <param name="email">The new email</param>
    public void UpdateProfile(string username, string? email)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        Username = username;
        Email = email;
        IncrementProfileVersion();

        AddDomainEvent(new UserProfileUpdatedEvent(UserId, username, email));
    }

    /// <summary>
    /// Updates the security settings
    /// </summary>
    /// <param name="securitySettings">The new security settings</param>
    public void UpdateSecuritySettings(SecuritySettings securitySettings)
    {
        SecuritySettings = securitySettings ?? throw new ArgumentNullException(nameof(securitySettings));
        IncrementProfileVersion();

        AddDomainEvent(new SecuritySettingsUpdatedEvent(UserId, securitySettings));
    }

    /// <summary>
    /// Adds a user preference
    /// </summary>
    /// <param name="preference">The user preference to add</param>
    public void AddUserPreference(UserPreferences preference)
    {
        if (preference == null)
            throw new ArgumentNullException(nameof(preference));

        // Remove existing preference with same category and key
        var existingPreference = _userPreferences.FirstOrDefault(p =>
            p.PreferenceCategory == preference.PreferenceCategory &&
            p.PreferenceKey == preference.PreferenceKey);

        if (existingPreference != null)
            _userPreferences.Remove(existingPreference);

        _userPreferences.Add(preference);
        UpdateVersion();
    }

    /// <summary>
    /// Removes a user preference
    /// </summary>
    /// <param name="category">The preference category</param>
    /// <param name="key">The preference key</param>
    public void RemoveUserPreference(string category, string key)
    {
        var preference = _userPreferences.FirstOrDefault(p =>
            p.PreferenceCategory == category &&
            p.PreferenceKey == key);

        if (preference != null)
        {
            _userPreferences.Remove(preference);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Gets a user preference value
    /// </summary>
    /// <param name="category">The preference category</param>
    /// <param name="key">The preference key</param>
    /// <returns>The preference value or null if not found</returns>
    public string? GetUserPreference(string category, string key)
    {
        return _userPreferences.FirstOrDefault(p =>
            p.PreferenceCategory == category &&
            p.PreferenceKey == key)?.PreferenceValue;
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Domain event raised when a user profile is created
/// </summary>
public class UserProfileCreatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the username
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Initializes a new instance of the UserProfileCreatedEvent class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="username">The username</param>
    public UserProfileCreatedEvent(Guid userId, string username)
    {
        UserId = userId;
        Username = username;
    }
}

/// <summary>
/// Domain event raised when a user logs in
/// </summary>
public class UserLoginEvent : DomainEvent
{
    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the login time
    /// </summary>
    public DateTime LoginTime { get; }

    /// <summary>
    /// Initializes a new instance of the UserLoginEvent class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="loginTime">The login time</param>
    public UserLoginEvent(Guid userId, DateTime loginTime)
    {
        UserId = userId;
        LoginTime = loginTime;
    }
}

/// <summary>
/// Domain event raised when a user profile is updated
/// </summary>
public class UserProfileUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the username
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Initializes a new instance of the UserProfileUpdatedEvent class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="username">The username</param>
    /// <param name="email">The email</param>
    public UserProfileUpdatedEvent(Guid userId, string username, string? email)
    {
        UserId = userId;
        Username = username;
        Email = email;
    }
}

/// <summary>
/// Domain event raised when security settings are updated
/// </summary>
public class SecuritySettingsUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the security settings
    /// </summary>
    public SecuritySettings SecuritySettings { get; }

    /// <summary>
    /// Initializes a new instance of the SecuritySettingsUpdatedEvent class
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="securitySettings">The security settings</param>
    public SecuritySettingsUpdatedEvent(Guid userId, SecuritySettings securitySettings)
    {
        UserId = userId;
        SecuritySettings = securitySettings;
    }
}