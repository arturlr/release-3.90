using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Nop.Core.Caching;

/// <summary>
/// In-memory cache manager implementing IStaticCacheManager.
/// Wraps IMemoryCache with prefix-based key tracking for pattern invalidation.
/// </summary>
public class MemoryCacheManager : IStaticCacheManager, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheManager(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
    {
        return _cache.GetOrCreateAsync(key.Key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                key.CacheTime > 0 ? key.CacheTime : CachingDefaults.DefaultCacheTime);
            _keys.TryAdd(key.Key, 0);
            entry.RegisterPostEvictionCallback(PostEviction);
            return await acquire();
        });
    }

    public T? Get<T>(CacheKey key, Func<T> acquire)
    {
        return _cache.GetOrCreate(key.Key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                key.CacheTime > 0 ? key.CacheTime : CachingDefaults.DefaultCacheTime);
            _keys.TryAdd(key.Key, 0);
            entry.RegisterPostEvictionCallback(PostEviction);
            return acquire();
        });
    }

    public Task SetAsync<T>(CacheKey key, T data)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                key.CacheTime > 0 ? key.CacheTime : CachingDefaults.DefaultCacheTime)
        };
        options.RegisterPostEvictionCallback(PostEviction);

        _cache.Set(key.Key, data, options);
        _keys.TryAdd(key.Key, 0);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(CacheKey key)
    {
        _cache.Remove(key.Key);
        // PostEviction callback handles _keys cleanup
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        foreach (var key in _keys.Keys)
            _cache.Remove(key);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void PostEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is string keyStr)
            _keys.TryRemove(keyStr, out _);
    }
}
