using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Net.Http;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// API配置值对象
/// </summary>
public class ApiConfiguration : ValueObject
{
    public string BaseUrl { get; private set; } = string.Empty;
    public EncryptedString ApiKey { get; private set; } = null!;
    public Enumerations.AuthenticationMethod AuthMethod { get; private set; } = null!;

    /// <summary>
    /// 自定义头部条目（导航属性）
    /// </summary>
    [NotMapped]
    public virtual ICollection<ApiHeaderEntry> CustomHeaderEntries { get; set; } = new List<ApiHeaderEntry>();

    public int TimeoutSeconds { get; private set; }
    public RetryPolicy RetryPolicy { get; private set; } = null!;
    public RateLimit RateLimit { get; private set; } = null!;
    public ProxySettings ProxySettings { get; private set; } = null!;

    // EF Core 需要的无参数构造函数
    private ApiConfiguration()
    {
        BaseUrl = string.Empty;
        ApiKey = EncryptedString.FromPlainText(string.Empty);
        AuthMethod = Enumerations.AuthenticationMethod.None;
        CustomHeaderEntries = new List<ApiHeaderEntry>();
        TimeoutSeconds = 30;
        RetryPolicy = new RetryPolicy(3, 1000, 2.0);
        RateLimit = new RateLimit(60, 10, 100, TimeSpan.FromMinutes(1));
        ProxySettings = new ProxySettings();
    }

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
        CustomHeaderEntries = new List<ApiHeaderEntry>();
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
        RetryPolicy = retryPolicy ?? new RetryPolicy(3, 1000, 2.0);
        RateLimit = rateLimit ?? new RateLimit(60, 10, 100, TimeSpan.FromMinutes(1));
        ProxySettings = proxySettings ?? new ProxySettings();

        // 如果提供了自定义头部字典，转换为实体对象
        if (customHeaders != null)
        {
            foreach (var kvp in customHeaders)
            {
                CustomHeaderEntries.Add(new ApiHeaderEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 获取自定义头部字典（向后兼容）
    /// </summary>
    public Dictionary<string, string> GetCustomHeaders()
    {
        return CustomHeaderEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.HeaderName, e => e.HeaderValue);
    }

    /// <summary>
    /// 验证配置
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
    /// 构建HTTP客户端
    /// </summary>
    public HttpClient BuildHttpClient()
    {
        var handler = new HttpClientHandler();

        // 配置代理
        if (ProxySettings.IsEnabled)
        {
            ProxySettings.ConfigureProxy(handler);
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        // 配置认证头
        switch (AuthMethod.Name)
        {
            case "ApiKey":
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetDecryptedApiKey()}");
                break;
            case "BearerToken":
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetDecryptedApiKey()}");
                break;
        }

        // 添加自定义头
        foreach (var headerEntry in CustomHeaderEntries.Where(e => e.IsEnabled))
        {
            client.DefaultRequestHeaders.Add(headerEntry.HeaderName, headerEntry.HeaderValue);
        }

        return client;
    }

    /// <summary>
    /// 获取解密的API密钥
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

        foreach (var headerEntry in CustomHeaderEntries.Where(e => e.IsEnabled).OrderBy(e => e.HeaderName))
        {
            yield return headerEntry.HeaderName;
            yield return headerEntry.HeaderValue;
        }
    }
}

/// <summary>
/// 重试策略值对象
/// </summary>
public class RetryPolicy : ValueObject
{
    public int MaxRetries { get; private set; }
    public int RetryDelayMs { get; private set; }
    public double BackoffMultiplier { get; private set; }
    public List<HttpStatusCode> RetryableStatusCodes { get; private set; } = new();

    // EF Core 需要的无参数构造函数
    private RetryPolicy()
    {
        MaxRetries = 3;
        RetryDelayMs = 1000;
        BackoffMultiplier = 2.0;
        RetryableStatusCodes = new List<HttpStatusCode>
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };
    }

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
    /// 检查是否应该重试
    /// </summary>
    public bool ShouldRetry(int attemptNumber, Exception exception)
    {
        if (attemptNumber >= MaxRetries)
            return false;

        if (exception is HttpRequestException httpEx)
        {
            // 检查HTTP状态码
            return true; // 简化处理，实际应该解析状态码
        }

        if (exception is TaskCanceledException)
        {
            return true; // 超时异常可以重试
        }

        return false;
    }

    /// <summary>
    /// 计算重试延迟
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
/// 速率限制值对象
/// </summary>
public class RateLimit : ValueObject
{
    public int RequestsPerMinute { get; private set; }
    public int ConcurrentRequests { get; private set; }
    public int BurstLimit { get; private set; }
    public TimeSpan WindowSize { get; private set; }

    // EF Core 需要的无参数构造函数
    private RateLimit()
    {
        RequestsPerMinute = 60;
        ConcurrentRequests = 10;
        BurstLimit = 100;
        WindowSize = TimeSpan.FromMinutes(1);
    }

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
    /// 检查是否在限制范围内
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
/// 代理设置值对象
/// </summary>
public class ProxySettings : ValueObject
{
    public string ProxyUrl { get; private set; } = string.Empty;
    public EncryptedString ProxyAuth { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public List<string> BypassList { get; private set; } = new();

    // EF Core 需要的无参数构造函数
    private ProxySettings()
    {
        ProxyUrl = string.Empty;
        ProxyAuth = EncryptedString.FromPlainText(string.Empty);
        IsEnabled = false;
        BypassList = new List<string>();
    }

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
    /// 配置代理
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