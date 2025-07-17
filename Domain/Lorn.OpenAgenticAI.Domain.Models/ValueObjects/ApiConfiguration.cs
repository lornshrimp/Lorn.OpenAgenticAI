using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// API����ֵ����
/// </summary>
public class ApiConfiguration : ValueObject
{
    public string BaseUrl { get; private set; } = string.Empty;
    public EncryptedString ApiKey { get; private set; } = null!;
    public Enumerations.AuthenticationMethod AuthMethod { get; private set; } = null!;
    public Dictionary<string, string> CustomHeaders { get; private set; } = new();
    public int TimeoutSeconds { get; private set; }
    public RetryPolicy RetryPolicy { get; private set; } = null!;
    public RateLimit RateLimit { get; private set; } = null!;
    public ProxySettings ProxySettings { get; private set; } = null!;

    public ApiConfiguration(
        string baseUrl,
        EncryptedString? apiKey,
        Enumerations.AuthenticationMethod authMethod,
        Dictionary<string, string>? customHeaders = null,
        int timeoutSeconds = 30,
        RetryPolicy? retryPolicy = null,
        RateLimit? rateLimit = null,
        ProxySettings? proxySettings = null)
    {
        BaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl : throw new ArgumentException("BaseUrl cannot be empty", nameof(baseUrl));
        ApiKey = apiKey ?? EncryptedString.FromPlainText(string.Empty);
        AuthMethod = authMethod ?? throw new ArgumentNullException(nameof(authMethod));
        CustomHeaders = customHeaders ?? new Dictionary<string, string>();
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
        RetryPolicy = retryPolicy ?? new RetryPolicy(3, 1000, 2.0);
        RateLimit = rateLimit ?? new RateLimit(60, 10, 100, TimeSpan.FromMinutes(1));
        ProxySettings = proxySettings ?? new ProxySettings();
    }

    /// <summary>
    /// ��֤����
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            result.AddError("BaseUrl", "Base URL is required");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            result.AddError("BaseUrl", "Base URL must be a valid absolute URI");
        }

        if (AuthMethod != Enumerations.AuthenticationMethod.None && ApiKey.IsEmpty())
        {
            result.AddError("ApiKey", "API key is required for the selected authentication method");
        }

        if (TimeoutSeconds <= 0)
        {
            result.AddError("TimeoutSeconds", "Timeout must be positive");
        }

        return result;
    }

    /// <summary>
    /// ����HTTP�ͻ���
    /// </summary>
    public HttpClient BuildHttpClient()
    {
        var handler = new HttpClientHandler();
        
        // ���ô���
        if (ProxySettings.IsEnabled)
        {
            ProxySettings.ConfigureProxy(handler);
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        // ������֤ͷ
        switch (AuthMethod.Name)
        {
            case "ApiKey":
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetDecryptedApiKey()}");
                break;
            case "BearerToken":
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetDecryptedApiKey()}");
                break;
        }

        // ����Զ���ͷ
        foreach (var header in CustomHeaders)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }

    /// <summary>
    /// ��ȡ���ܵ�API��Կ
    /// </summary>
    public string GetDecryptedApiKey()
    {
        return ApiKey.Decrypt();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return BaseUrl;
        yield return ApiKey.EncryptedValue;
        yield return AuthMethod.Id;
        yield return TimeoutSeconds;
        
        foreach (var header in CustomHeaders)
        {
            yield return header.Key;
            yield return header.Value;
        }
    }
}

/// <summary>
/// ���Բ���ֵ����
/// </summary>
public class RetryPolicy : ValueObject
{
    public int MaxRetries { get; private set; }
    public int RetryDelayMs { get; private set; }
    public double BackoffMultiplier { get; private set; }
    public List<HttpStatusCode> RetryableStatusCodes { get; private set; } = new();

    public RetryPolicy(
        int maxRetries = 3,
        int retryDelayMs = 1000,
        double backoffMultiplier = 2.0,
        List<HttpStatusCode>? retryableStatusCodes = null)
    {
        MaxRetries = maxRetries >= 0 ? maxRetries : 3;
        RetryDelayMs = retryDelayMs > 0 ? retryDelayMs : 1000;
        BackoffMultiplier = backoffMultiplier > 0 ? backoffMultiplier : 2.0;
        RetryableStatusCodes = retryableStatusCodes ?? new List<HttpStatusCode>
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };
    }

    /// <summary>
    /// ����Ƿ�Ӧ������
    /// </summary>
    public bool ShouldRetry(int attemptNumber, Exception exception)
    {
        if (attemptNumber >= MaxRetries)
            return false;

        if (exception is HttpRequestException httpEx)
        {
            // ���HTTP״̬��
            return true; // �򻯴���ʵ��Ӧ�ý���״̬��
        }

        if (exception is TaskCanceledException)
        {
            return true; // ��ʱ�쳣��������
        }

        return false;
    }

    /// <summary>
    /// ���������ӳ�
    /// </summary>
    public TimeSpan GetRetryDelay(int attemptNumber)
    {
        var delay = RetryDelayMs * Math.Pow(BackoffMultiplier, attemptNumber);
        return TimeSpan.FromMilliseconds(delay);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MaxRetries;
        yield return RetryDelayMs;
        yield return BackoffMultiplier;
        
        foreach (var statusCode in RetryableStatusCodes)
        {
            yield return statusCode;
        }
    }
}

/// <summary>
/// ��������ֵ����
/// </summary>
public class RateLimit : ValueObject
{
    public int RequestsPerMinute { get; private set; }
    public int ConcurrentRequests { get; private set; }
    public int BurstLimit { get; private set; }
    public TimeSpan WindowSize { get; private set; }

    public RateLimit(
        int requestsPerMinute = 60,
        int concurrentRequests = 10,
        int burstLimit = 100,
        TimeSpan windowSize = default)
    {
        RequestsPerMinute = requestsPerMinute > 0 ? requestsPerMinute : 60;
        ConcurrentRequests = concurrentRequests > 0 ? concurrentRequests : 10;
        BurstLimit = burstLimit > 0 ? burstLimit : 100;
        WindowSize = windowSize == default ? TimeSpan.FromMinutes(1) : windowSize;
    }

    /// <summary>
    /// ����Ƿ������Ʒ�Χ��
    /// </summary>
    public bool IsWithinLimit(int currentRequests)
    {
        return currentRequests <= RequestsPerMinute;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return RequestsPerMinute;
        yield return ConcurrentRequests;
        yield return BurstLimit;
        yield return WindowSize.TotalMilliseconds;
    }
}

/// <summary>
/// ��������ֵ����
/// </summary>
public class ProxySettings : ValueObject
{
    public string ProxyUrl { get; private set; } = string.Empty;
    public EncryptedString ProxyAuth { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public List<string> BypassList { get; private set; } = new();

    public ProxySettings(
        string? proxyUrl = null,
        EncryptedString? proxyAuth = null,
        bool isEnabled = false,
        List<string>? bypassList = null)
    {
        ProxyUrl = proxyUrl ?? string.Empty;
        ProxyAuth = proxyAuth ?? EncryptedString.FromPlainText(string.Empty);
        IsEnabled = isEnabled && !string.IsNullOrWhiteSpace(proxyUrl);
        BypassList = bypassList ?? new List<string>();
    }

    /// <summary>
    /// ���ô���
    /// </summary>
    public void ConfigureProxy(HttpClientHandler handler)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(ProxyUrl))
            return;

        handler.UseProxy = true;
        handler.Proxy = new WebProxy(ProxyUrl);

        if (!ProxyAuth.IsEmpty())
        {
            var auth = ProxyAuth.Decrypt();
            if (!string.IsNullOrWhiteSpace(auth))
            {
                var parts = auth.Split(':');
                if (parts.Length == 2)
                {
                    handler.Proxy.Credentials = new NetworkCredential(parts[0], parts[1]);
                }
            }
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ProxyUrl;
        yield return ProxyAuth.EncryptedValue;
        yield return IsEnabled;
        
        foreach (var bypass in BypassList)
        {
            yield return bypass;
        }
    }
}