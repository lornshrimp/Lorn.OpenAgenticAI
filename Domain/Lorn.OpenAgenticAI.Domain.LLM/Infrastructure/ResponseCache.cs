using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Infrastructure;

/// <summary>
/// 响应缓存实现
/// 提供多级缓存策略，最大化响应性能
/// </summary>
public class ResponseCache : IResponseCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ICacheSerializer _serializer;
    private readonly IOptions<ResponseCacheOptions> _options;
    private readonly ILogger<ResponseCache> _logger;

    public ResponseCache(
        IMemoryCache memoryCache,
        ICacheSerializer serializer,
        IOptions<ResponseCacheOptions> options,
        ILogger<ResponseCache> logger,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _distributedCache = distributedCache;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            // L1: 内存缓存
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                _logger.LogDebug("内存缓存命中: {Key}", key);
                return memoryValue;
            }

            // L2: 分布式缓存
            if (_distributedCache != null)
            {
                var distributedData = await _distributedCache.GetAsync(key, cancellationToken);
                if (distributedData != null)
                {
                    var distributedValue = _serializer.Deserialize<T>(distributedData);
                    if (distributedValue != null)
                    {
                        // 回填内存缓存
                        var memoryExpiry = TimeSpan.FromMinutes(_options.Value.MemoryCacheExpiryMinutes);
                        _memoryCache.Set(key, distributedValue, memoryExpiry);

                        _logger.LogDebug("分布式缓存命中: {Key}", key);
                        return distributedValue;
                    }
                }
            }

            _logger.LogDebug("缓存未命中: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存失败: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
            return;

        try
        {
            var memoryExpiry = expiration ?? TimeSpan.FromMinutes(_options.Value.MemoryCacheExpiryMinutes);
            var distributedExpiry = expiration ?? TimeSpan.FromMinutes(_options.Value.DistributedCacheExpiryMinutes);

            // L1: 设置内存缓存
            _memoryCache.Set(key, value, memoryExpiry);

            // L2: 设置分布式缓存
            if (_distributedCache != null)
            {
                var serializedData = _serializer.Serialize(value);
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = distributedExpiry
                };

                await _distributedCache.SetAsync(key, serializedData, distributedOptions, cancellationToken);
            }

            _logger.LogDebug("设置缓存: {Key}, Expiry: {Expiry}", key, memoryExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            // 移除内存缓存
            _memoryCache.Remove(key);

            // 移除分布式缓存
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }

            _logger.LogDebug("移除缓存: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除缓存失败: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 清空内存缓存（需要反射或其他方式）
            if (_memoryCache is MemoryCache memCache)
            {
                memCache.Compact(1.0); // 压缩并移除所有过期项
            }

            _logger.LogDebug("清空缓存完成");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空缓存失败");
        }
    }

    /// <inheritdoc />
    public string GenerateCacheKey(LLMRequest request)
    {
        try
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(_options.Value.CacheKeyPrefix);
            keyBuilder.Append(request.ModelId);
            keyBuilder.Append(":");

            // 添加系统提示词哈希
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                keyBuilder.Append(ComputeHash(request.SystemPrompt));
                keyBuilder.Append(":");
            }

            // 添加用户提示词哈希
            keyBuilder.Append(ComputeHash(request.UserPrompt));

            // 添加对话历史哈希（如果有）
            if (request.ConversationHistory != null && request.ConversationHistory.Count > 0)
            {
                keyBuilder.Append(":");
                var historyText = string.Join("|", request.ConversationHistory.Select(m => m.Content));
                keyBuilder.Append(ComputeHash(historyText));
            }

            // 添加执行设置哈希（如果有）
            if (request.ExecutionSettings != null)
            {
                keyBuilder.Append(":");
                var settingsJson = JsonSerializer.Serialize(request.ExecutionSettings);
                keyBuilder.Append(ComputeHash(settingsJson));
            }

            var cacheKey = keyBuilder.ToString();
            _logger.LogDebug("生成缓存键: {CacheKey}", cacheKey);

            return cacheKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成缓存键失败");
            return $"{_options.Value.CacheKeyPrefix}fallback:{Guid.NewGuid()}";
        }
    }

    /// <summary>
    /// 计算字符串的哈希值
    /// </summary>
    private static string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16]; // 取前16个字符
    }
}
