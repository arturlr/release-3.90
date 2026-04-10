using System.Collections.Concurrent;
using Nop.Core.Caching;

namespace Nop.Tests;

/// <summary>
/// In-memory IStaticCacheManager for unit tests.
/// Simple dictionary-based cache — no expiration, no distributed cache.
/// </summary>
public class FakeCacheManager : IStaticCacheManager
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();

    public async Task<T?> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
    {
        if (_cache.TryGetValue(key.Key, out var cached))
            return (T?)cached;

        var value = await acquire();
        _cache[key.Key] = value;
        return value;
    }

    public T? Get<T>(CacheKey key, Func<T> acquire)
    {
        if (_cache.TryGetValue(key.Key, out var cached))
            return (T?)cached;

        var value = acquire();
        _cache[key.Key] = value;
        return value;
    }

    public Task SetAsync<T>(CacheKey key, T data)
    {
        _cache[key.Key] = data;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(CacheKey key)
    {
        _cache.TryRemove(key.Key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        foreach (var k in _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            _cache.TryRemove(k, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }
}
