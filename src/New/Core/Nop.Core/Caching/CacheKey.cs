namespace Nop.Core.Caching;

/// <summary>
/// Cache key descriptor with pattern-based invalidation support
/// </summary>
public class CacheKey
{
    public CacheKey(string key, params string[] prefixes)
    {
        Key = key;
        Prefixes = prefixes;
    }

    public string Key { get; }

    /// <summary>
    /// Prefixes used for pattern-based cache invalidation
    /// </summary>
    public string[] Prefixes { get; }

    /// <summary>
    /// Cache time in minutes (0 = use default)
    /// </summary>
    public int CacheTime { get; init; }
}
