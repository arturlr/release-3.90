using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching between HTTP requests (long term caching)
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 4): re-based from System.Runtime.Caching
    /// (<c>ObjectCache</c>/<c>MemoryCache.Default</c>) onto
    /// <see cref="IMemoryCache"/>. The <see cref="ICacheManager"/> contract is unchanged.
    ///
    /// Two capabilities of ObjectCache have no direct IMemoryCache counterpart and are
    /// reconstructed here:
    ///   * key enumeration (needed by <see cref="RemoveByPattern"/>) - IMemoryCache does not
    ///     expose its keys, so they are tracked in <see cref="_allKeys"/> and kept in sync
    ///     through a post-eviction callback registered on every entry;
    ///   * bulk clear (<see cref="Clear"/>) - implemented with a
    ///     <see cref="CancellationChangeToken"/> that is linked as an expiration token to
    ///     every entry, so cancelling it evicts the whole set at once.
    /// </remarks>
    public partial class MemoryCacheManager : ICacheManager
    {
        #region Fields

        private readonly IMemoryCache _cache;

        /// <summary>
        /// Holds the keys known to be (or to have been) in the cache.
        /// </summary>
        /// <remarks>
        /// The dictionary value indicates whether the key is still considered present:
        /// <c>true</c> while the entry is live, <c>false</c> once eviction has been observed
        /// but the bookkeeping entry has not been cleaned up yet.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, bool> _allKeys =
            new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Cancellation token source whose token is linked to every cache entry; cancelling
        /// it is what gives <see cref="Clear"/> its "remove everything" behaviour.
        /// </summary>
        private CancellationTokenSource _clearToken = new CancellationTokenSource();

        private readonly object _clearTokenLock = new object();

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cache">Memory cache</param>
        public MemoryCacheManager(IMemoryCache cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");

            this._cache = cache;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Cache object
        /// </summary>
        protected IMemoryCache Cache
        {
            get { return _cache; }
        }

        /// <summary>
        /// Add the key to the known-keys bookkeeping and return it
        /// </summary>
        protected virtual string AddKey(string key)
        {
            _allKeys.AddOrUpdate(key, true, (k, v) => true);
            return key;
        }

        /// <summary>
        /// Drop the key from the known-keys bookkeeping and return it
        /// </summary>
        protected virtual string RemoveKey(string key)
        {
            TryRemoveKey(key);
            return key;
        }

        /// <summary>
        /// Try to remove a key from the bookkeeping; if it is currently in use, mark it as
        /// stale instead so it is dropped on the next sweep
        /// </summary>
        protected virtual void TryRemoveKey(string key)
        {
            bool removed;
            if (!_allKeys.TryRemove(key, out removed))
                _allKeys.TryUpdate(key, false, true);
        }

        /// <summary>
        /// Remove the keys previously marked as stale
        /// </summary>
        protected virtual void ClearKeys()
        {
            foreach (var key in _allKeys.Where(p => !p.Value).Select(p => p.Key).ToList())
                RemoveKey(key);
        }

        /// <summary>
        /// Build the entry options for the supplied cache time
        /// </summary>
        /// <param name="cacheTime">Cache time in minutes</param>
        /// <returns>Memory cache entry options</returns>
        protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(TimeSpan cacheTime)
        {
            var options = new MemoryCacheEntryOptions
            {
                //the legacy implementation used CacheItemPolicy.AbsoluteExpiration =
                //DateTime.Now + TimeSpan.FromMinutes(cacheTime), which is exactly this
                AbsoluteExpirationRelativeToNow = cacheTime
            };

            //link the entry to the "clear all" token (see Clear)
            options.AddExpirationToken(new CancellationChangeToken(GetClearToken()));

            //keep the key bookkeeping in sync with actual evictions
            options.RegisterPostEvictionCallback(PostEviction);

            return options;
        }

        /// <summary>
        /// Get the current "clear all" cancellation token
        /// </summary>
        protected virtual CancellationToken GetClearToken()
        {
            lock (_clearTokenLock)
            {
                return _clearToken.Token;
            }
        }

        /// <summary>
        /// Post eviction callback keeping <see cref="_allKeys"/> in sync
        /// </summary>
        protected virtual void PostEviction(object key, object value, EvictionReason reason, object state)
        {
            //a replaced entry is immediately re-added under the same key, so the key stays valid
            if (reason == EvictionReason.Replaced)
                return;

            ClearKeys();

            if (key != null)
                TryRemoveKey(key.ToString());
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
            return _cache.Get<T>(key);
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

            //IMemoryCache rejects a non-positive AbsoluteExpirationRelativeToNow, whereas the
            //legacy ObjectCache silently accepted an already-elapsed absolute expiration and
            //discarded the item. Not caching at all is the equivalent observable behaviour.
            if (cacheTime <= 0)
                return;

            _cache.Set(AddKey(key), data, GetMemoryCacheEntryOptions(TimeSpan.FromMinutes(cacheTime)));
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public virtual bool IsSet(string key)
        {
            object value;
            return _cache.TryGetValue(key, out value);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">/key</param>
        public virtual void Remove(string key)
        {
            _cache.Remove(RemoveKey(key));
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public virtual void RemoveByPattern(string pattern)
        {
            this.RemoveByPattern(pattern, _allKeys.Where(p => p.Value).Select(p => p.Key).ToList());
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            //cancel the token every entry is linked to - this evicts them all in one go
            CancellationTokenSource previous;
            lock (_clearTokenLock)
            {
                previous = _clearToken;
                _clearToken = new CancellationTokenSource();
            }

            previous.Cancel();
            previous.Dispose();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
        }

        #endregion
    }
}
