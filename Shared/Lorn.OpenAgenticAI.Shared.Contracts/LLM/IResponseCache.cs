namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 响应缓存接口
/// 提供多级缓存策略，最大化响应性能
/// </summary>
public interface IResponseCache
{
    /// <summary>
    /// 获取缓存的响应
    /// </summary>
    /// <typeparam name="T">响应类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存的响应，如果不存在则返回null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 设置缓存的响应
    /// </summary>
    /// <typeparam name="T">响应类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">要缓存的值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空缓存
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <param name="request">LLM请求</param>
    /// <returns>缓存键</returns>
    string GenerateCacheKey(LLMRequest request);
}
