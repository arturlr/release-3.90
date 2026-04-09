namespace Nop.Core.Caching;

/// <summary>
/// Static cache manager — replaces legacy ICacheManager.
/// Wraps IMemoryCache + IDistributedCache with pattern-based invalidation.
/// </summary>
public interface IStaticCacheManager
{
    /// <summary>
    /// Get a cached item. If it's not in the cache yet, load and cache it.
    /// </summary>
    Task<T?> GetAsync<T>(CacheKey key, Func<Task<T>> acquire);

    /// <summary>
    /// Get a cached item synchronously.
    /// </summary>
    T? Get<T>(CacheKey key, Func<T> acquire);

    /// <summary>
    /// Set the specified key/value pair.
    /// </summary>
    Task SetAsync<T>(CacheKey key, T data);

    /// <summary>
    /// Remove the value with the specified key from the cache.
    /// </summary>
    Task RemoveAsync(CacheKey key);

    /// <summary>
    /// Remove items by cache key prefix.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix);

    /// <summary>
    /// Clear all cache data.
    /// </summary>
    Task ClearAsync();
}
