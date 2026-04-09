namespace Nop.Core.Caching;

/// <summary>
/// Per-request cache — scoped dictionary that prevents duplicate lookups within a single HTTP request.
/// Register as Scoped in DI. Replaces legacy PerRequestCacheManager (HttpContext.Items).
/// </summary>
public class NopRequestCache
{
    private readonly Dictionary<string, object?> _items = [];

    public T? Get<T>(string key)
    {
        return _items.TryGetValue(key, out var value) ? (T?)value : default;
    }

    public void Set<T>(string key, T? data)
    {
        _items[key] = data;
    }

    public bool IsSet(string key) => _items.ContainsKey(key);

    public void Remove(string key) => _items.Remove(key);

    public void Clear() => _items.Clear();
}
