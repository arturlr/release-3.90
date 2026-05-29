using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching during an HTTP request (short term caching).
    /// Uses HttpContext.Items which is scoped to the current request.
    /// </summary>
    public partial class PerRequestCacheManager : ICacheManager
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        public PerRequestCacheManager(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the items dictionary from the current HttpContext
        /// </summary>
        protected virtual IDictionary<object, object> GetItems()
        {
            return _httpContextAccessor?.HttpContext?.Items;
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
            var items = GetItems();
            if (items == null)
                return default(T);

            if (items.TryGetValue(key, out var value))
                return (T)value;

            return default(T);
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time (ignored for per-request cache)</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            var items = GetItems();
            if (items == null)
                return;

            if (data != null)
            {
                items[key] = data;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public virtual bool IsSet(string key)
        {
            var items = GetItems();
            if (items == null)
                return false;

            return items.ContainsKey(key) && items[key] != null;
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">key</param>
        public virtual void Remove(string key)
        {
            var items = GetItems();
            items?.Remove(key);
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public virtual void RemoveByPattern(string pattern)
        {
            var items = GetItems();
            if (items == null)
                return;

            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = items.Keys
                .Select(k => k.ToString())
                .Where(k => regex.IsMatch(k))
                .ToList();

            foreach (var key in keysToRemove)
                items.Remove(key);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            var items = GetItems();
            items?.Clear();
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
