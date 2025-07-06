using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using System.Net;
using System.Net.Sockets;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// API configuration value object
/// </summary>
public class ApiConfiguration : ValueObject
{
    /// <summary>
    /// Gets the base URL
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Gets the encrypted API key
    /// </summary>
    public EncryptedString ApiKey { get; }

    /// <summary>
    /// Gets the authentication method
    /// </summary>
    public AuthenticationMethod AuthMethod { get; }

    /// <summary>
    /// Gets the custom headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; }

    /// <summary>
    /// Gets the timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Gets the retry policy
    /// </summary>
    public RetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Gets the rate limit settings
    /// </summary>
    public RateLimit RateLimit { get; }

    /// <summary>
    /// Gets the proxy settings
    /// </summary>
    public ProxySettings? ProxySettings { get; }

    /// <summary>
    /// Initializes a new instance of the ApiConfiguration class
    /// </summary>
    /// <param name="baseUrl">The base URL</param>
    /// <param name="apiKey">The API key</param>
    /// <param name="authMethod">The authentication method</param>
    /// <param name="timeoutSeconds">The timeout in seconds</param>
    /// <param name="retryPolicy">The retry policy</param>
    /// <param name="rateLimit">The rate limit</param>
    /// <param name="customHeaders">The custom headers</param>
    /// <param name="proxySettings">The proxy settings</param>
    public ApiConfiguration(
        string baseUrl,
        EncryptedString apiKey,
        AuthenticationMethod authMethod,
        int timeoutSeconds = 30,
        RetryPolicy? retryPolicy = null,
        RateLimit? rateLimit = null,
        Dictionary<string, string>? customHeaders = null,
        ProxySettings? proxySettings = null)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        AuthMethod = authMethod ?? throw new ArgumentNullException(nameof(authMethod));
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
        RetryPolicy = retryPolicy ?? RetryPolicy.Default();
        RateLimit = rateLimit ?? RateLimit.Default();
        CustomHeaders = customHeaders ?? new Dictionary<string, string>();
        ProxySettings = proxySettings;
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>A validation result</returns>
    public ApiValidationResult ValidateConfiguration()
    {
        var result = new ApiValidationResult();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            result.AddError("BaseUrl", "Base URL is required");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            result.AddError("BaseUrl", "Base URL must be a valid HTTP or HTTPS URL");
        }

        if (AuthMethod.RequiresCredentials() && ApiKey.IsEmpty)
        {
            result.AddError("ApiKey", "API key is required for the selected authentication method");
        }

        if (TimeoutSeconds <= 0 || TimeoutSeconds > 300)
        {
            result.AddError("TimeoutSeconds", "Timeout must be between 1 and 300 seconds");
        }

        return result;
    }

    /// <summary>
    /// Gets the decrypted API key
    /// </summary>
    /// <returns>The decrypted API key</returns>
    public string GetDecryptedApiKey()
    {
        return ApiKey.Decrypt();
    }

    /// <summary>
    /// Builds an HTTP client with this configuration
    /// </summary>
    /// <returns>A configured HTTP client</returns>
    public HttpClient BuildHttpClient()
    {
        var handler = new HttpClientHandler();

        // Configure proxy if specified
        ProxySettings?.ConfigureProxy(handler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        // Add authentication headers
        if (AuthMethod.RequiresCredentials())
        {
            var apiKeyValue = GetDecryptedApiKey();
            client.DefaultRequestHeaders.Add("Authorization", AuthMethod.Name switch
            {
                "ApiKey" => $"Bearer {apiKeyValue}",
                "BearerToken" => $"Bearer {apiKeyValue}",
                "OAuth2" => $"Bearer {apiKeyValue}",
                _ => apiKeyValue
            });
        }

        // Add custom headers
        foreach (var header in CustomHeaders)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return BaseUrl;
        yield return ApiKey;
        yield return AuthMethod;
        yield return TimeoutSeconds;
        yield return RetryPolicy;
        yield return RateLimit;

        foreach (var header in CustomHeaders.OrderBy(x => x.Key))
        {
            yield return header.Key;
            yield return header.Value;
        }

        if (ProxySettings != null)
            yield return ProxySettings;
    }
}

/// <summary>
/// Retry policy value object
/// </summary>
public class RetryPolicy : ValueObject
{
    /// <summary>
    /// Gets the maximum number of retries
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; }

    /// <summary>
    /// Gets the backoff multiplier
    /// </summary>
    public double BackoffMultiplier { get; }

    /// <summary>
    /// Gets the HTTP status codes that should trigger a retry
    /// </summary>
    public List<HttpStatusCode> RetryableStatusCodes { get; }

    /// <summary>
    /// Initializes a new instance of the RetryPolicy class
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries</param>
    /// <param name="retryDelayMs">The retry delay in milliseconds</param>
    /// <param name="backoffMultiplier">The backoff multiplier</param>
    /// <param name="retryableStatusCodes">The HTTP status codes that should trigger a retry</param>
    public RetryPolicy(
        int maxRetries,
        int retryDelayMs,
        double backoffMultiplier = 2.0,
        List<HttpStatusCode>? retryableStatusCodes = null)
    {
        MaxRetries = Math.Max(0, maxRetries);
        RetryDelayMs = Math.Max(100, retryDelayMs);
        BackoffMultiplier = Math.Max(1.0, backoffMultiplier);
        RetryableStatusCodes = retryableStatusCodes ?? GetDefaultRetryableStatusCodes();
    }

    /// <summary>
    /// Determines whether a retry should be attempted
    /// </summary>
    /// <param name="attemptNumber">The current attempt number</param>
    /// <param name="exception">The exception that occurred</param>
    /// <returns>True if retry should be attempted, false otherwise</returns>
    public bool ShouldRetry(int attemptNumber, Exception exception)
    {
        if (attemptNumber >= MaxRetries)
            return false;

        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            SocketException => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the delay for the specified attempt
    /// </summary>
    /// <param name="attemptNumber">The attempt number</param>
    /// <returns>The delay in milliseconds</returns>
    public int GetDelay(int attemptNumber)
    {
        return (int)(RetryDelayMs * Math.Pow(BackoffMultiplier, attemptNumber));
    }

    /// <summary>
    /// Creates a default retry policy
    /// </summary>
    /// <returns>A default retry policy</returns>
    public static RetryPolicy Default()
    {
        return new RetryPolicy(3, 1000, 2.0);
    }

    /// <summary>
    /// Gets the default retryable HTTP status codes
    /// </summary>
    /// <returns>List of default retryable status codes</returns>
    private static List<HttpStatusCode> GetDefaultRetryableStatusCodes()
    {
        return new List<HttpStatusCode>
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.TooManyRequests
        };
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxRetries;
        yield return RetryDelayMs;
        yield return BackoffMultiplier;

        foreach (var statusCode in RetryableStatusCodes.OrderBy(x => (int)x))
        {
            yield return statusCode;
        }
    }
}

/// <summary>
/// Rate limit value object
/// </summary>
public class RateLimit : ValueObject
{
    /// <summary>
    /// Gets the requests per minute limit
    /// </summary>
    public int RequestsPerMinute { get; }

    /// <summary>
    /// Gets the concurrent requests limit
    /// </summary>
    public int ConcurrentRequests { get; }

    /// <summary>
    /// Gets the burst limit
    /// </summary>
    public int BurstLimit { get; }

    /// <summary>
    /// Gets the time window size
    /// </summary>
    public TimeSpan WindowSize { get; }

    /// <summary>
    /// Initializes a new instance of the RateLimit class
    /// </summary>
    /// <param name="requestsPerMinute">The requests per minute limit</param>
    /// <param name="concurrentRequests">The concurrent requests limit</param>
    /// <param name="burstLimit">The burst limit</param>
    /// <param name="windowSize">The time window size</param>
    public RateLimit(
        int requestsPerMinute,
        int concurrentRequests,
        int burstLimit,
        TimeSpan? windowSize = null)
    {
        RequestsPerMinute = Math.Max(1, requestsPerMinute);
        ConcurrentRequests = Math.Max(1, concurrentRequests);
        BurstLimit = Math.Max(requestsPerMinute, burstLimit);
        WindowSize = windowSize ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Checks if the current request count is within limits
    /// </summary>
    /// <param name="currentRequests">The current request count</param>
    /// <returns>True if within limits, false otherwise</returns>
    public bool IsWithinLimit(int currentRequests)
    {
        return currentRequests <= RequestsPerMinute;
    }

    /// <summary>
    /// Creates a default rate limit
    /// </summary>
    /// <returns>A default rate limit</returns>
    public static RateLimit Default()
    {
        return new RateLimit(60, 10, 100);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return RequestsPerMinute;
        yield return ConcurrentRequests;
        yield return BurstLimit;
        yield return WindowSize;
    }
}

/// <summary>
/// Proxy settings value object
/// </summary>
public class ProxySettings : ValueObject
{
    /// <summary>
    /// Gets the proxy URL
    /// </summary>
    public string ProxyUrl { get; }

    /// <summary>
    /// Gets the encrypted proxy authentication
    /// </summary>
    public EncryptedString ProxyAuth { get; }

    /// <summary>
    /// Gets whether the proxy is enabled
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the bypass list
    /// </summary>
    public List<string> BypassList { get; }

    /// <summary>
    /// Initializes a new instance of the ProxySettings class
    /// </summary>
    /// <param name="proxyUrl">The proxy URL</param>
    /// <param name="proxyAuth">The proxy authentication</param>
    /// <param name="isEnabled">Whether the proxy is enabled</param>
    /// <param name="bypassList">The bypass list</param>
    public ProxySettings(
        string proxyUrl,
        EncryptedString? proxyAuth = null,
        bool isEnabled = true,
        List<string>? bypassList = null)
    {
        ProxyUrl = proxyUrl ?? throw new ArgumentNullException(nameof(proxyUrl));
        ProxyAuth = proxyAuth ?? EncryptedString.Empty();
        IsEnabled = isEnabled;
        BypassList = bypassList ?? new List<string>();
    }

    /// <summary>
    /// Configures a proxy for the HTTP client handler
    /// </summary>
    /// <param name="handler">The HTTP client handler</param>
    public void ConfigureProxy(HttpClientHandler handler)
    {
        if (!IsEnabled || string.IsNullOrEmpty(ProxyUrl))
            return;

        var proxy = new WebProxy(ProxyUrl);

        if (!ProxyAuth.IsEmpty)
        {
            var auth = ProxyAuth.Decrypt();
            if (!string.IsNullOrEmpty(auth))
            {
                var parts = auth.Split(':');
                if (parts.Length == 2)
                {
                    proxy.Credentials = new NetworkCredential(parts[0], parts[1]);
                }
            }
        }

        if (BypassList.Any())
        {
            proxy.BypassList = BypassList.ToArray();
        }

        handler.Proxy = proxy;
        handler.UseProxy = true;
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ProxyUrl;
        yield return ProxyAuth;
        yield return IsEnabled;

        foreach (var bypass in BypassList.OrderBy(x => x))
        {
            yield return bypass;
        }
    }
}

/// <summary>
/// API validation result class
/// </summary>
public class ApiValidationResult
{
    private readonly List<ApiValidationError> _errors = new();

    /// <summary>
    /// Gets whether the validation is successful
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the validation errors
    /// </summary>
    public IReadOnlyList<ApiValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Adds a validation error
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="errorMessage">The error message</param>
    public void AddError(string propertyName, string errorMessage)
    {
        _errors.Add(new ApiValidationError(propertyName, errorMessage));
    }

    /// <summary>
    /// Gets a summary of all errors
    /// </summary>
    /// <returns>Error summary</returns>
    public string GetErrorSummary()
    {
        return string.Join("; ", _errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

/// <summary>
/// API validation error class
/// </summary>
public class ApiValidationError
{
    /// <summary>
    /// Gets the property name
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the ApiValidationError class
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="errorMessage">The error message</param>
    public ApiValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}