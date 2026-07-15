using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching between HTTP requests (long term caching)
    /// </summary>
    public partial class MemoryCacheManager : ICacheManager
    {
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cache">Memory cache instance</param>
        public MemoryCacheManager(IMemoryCache cache)
        {
            this._cache = cache;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public virtual T Get<T>(string key)
        {
            _cache.TryGetValue(key, out T value);
            return value;
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            if (data == null)
                return;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheTime));

            _cache.Set(key, data, cacheEntryOptions);
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public virtual bool IsSet(string key)
        {
            return _cache.TryGetValue(key, out _);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">/key</param>
        public virtual void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public virtual void RemoveByPattern(string pattern)
        {
            //IMemoryCache does not expose Keys directly, so we use reflection to access the entries collection
            //This is a known limitation of IMemoryCache; in production consider maintaining a separate key list
            var allKeys = GetAllKeys();
            this.RemoveByPattern(pattern, allKeys);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            var allKeys = GetAllKeys();
            foreach (var key in allKeys)
                Remove(key);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets all keys from the memory cache using reflection.
        /// IMemoryCache does not expose keys publicly, so we access internal structures.
        /// </summary>
        /// <returns>Collection of cache keys as strings</returns>
        private IEnumerable<string> GetAllKeys()
        {
            // MemoryCache stores entries in a ConcurrentDictionary called _coherentState._entries (or _entries in older versions)
            // We use reflection to get the keys
            var coherentStateField = _cache.GetType().GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (coherentStateField != null)
            {
                var coherentState = coherentStateField.GetValue(_cache);
                if (coherentState != null)
                {
                    var entriesField = coherentState.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (entriesField != null)
                    {
                        var entries = entriesField.GetValue(coherentState);
                        if (entries != null)
                        {
                            var keysProperty = entries.GetType().GetProperty("Keys");
                            if (keysProperty != null)
                            {
                                var keys = keysProperty.GetValue(entries) as IEnumerable<object>;
                                if (keys != null)
                                    return keys.Select(k => k.ToString());
                            }
                        }
                    }
                }
            }

            // Fallback: try _entries directly (older Microsoft.Extensions.Caching.Memory versions)
            var entriesDirectField = _cache.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            if (entriesDirectField != null)
            {
                var entries = entriesDirectField.GetValue(_cache);
                if (entries != null)
                {
                    var keysProperty = entries.GetType().GetProperty("Keys");
                    if (keysProperty != null)
                    {
                        var keys = keysProperty.GetValue(entries) as IEnumerable<object>;
                        if (keys != null)
                            return keys.Select(k => k.ToString());
                    }
                }
            }

            return Enumerable.Empty<string>();
        }
    }
}
