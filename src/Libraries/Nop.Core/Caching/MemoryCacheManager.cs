using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching between HTTP requests (long term caching)
    /// Uses Microsoft.Extensions.Caching.Memory (IMemoryCache)
    /// </summary>
    public partial class MemoryCacheManager : ICacheManager
    {
        #region Fields

        private readonly IMemoryCache _memoryCache;

        // Track all keys for pattern-based removal and clear operations
        private readonly ConcurrentDictionary<string, bool> _allKeys;
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        #endregion

        #region Ctor

        public MemoryCacheManager(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _allKeys = new ConcurrentDictionary<string, bool>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public virtual T Get<T>(string key)
        {
            return _memoryCache.Get<T>(key);
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time in minutes</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            if (data == null)
                return;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheTime))
                .RegisterPostEvictionCallback(PostEviction);

            _memoryCache.Set(key, data, cacheEntryOptions);
            _allKeys.TryAdd(key, true);
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public virtual bool IsSet(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">key</param>
        public virtual void Remove(string key)
        {
            _memoryCache.Remove(key);
            _allKeys.TryRemove(key, out _);
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public virtual void RemoveByPattern(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _allKeys.Keys.Where(k => regex.IsMatch(k)).ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            foreach (var key in _allKeys.Keys.ToList())
            {
                Remove(key);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            // IMemoryCache is managed by DI container, don't dispose here
        }

        #endregion

        #region Utilities

        private void PostEviction(object key, object value, EvictionReason reason, object state)
        {
            // If entry was evicted (expired, capacity), remove from our tracking dictionary
            if (reason != EvictionReason.Removed && reason != EvictionReason.Replaced)
            {
                _allKeys.TryRemove(key.ToString(), out _);
            }
        }

        #endregion
    }
}
