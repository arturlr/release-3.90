using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching during an HTTP request (short term caching)
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 4): the per-request store moved from
    /// <c>System.Web.HttpContextBase.Items</c> to
    /// <see cref="HttpContext.Items"/> reached through <see cref="IHttpContextAccessor"/>.
    /// Outside a request (scheduled tasks, installation, unit tests) the accessor's
    /// HttpContext is null; every operation then degrades to a no-op exactly as the legacy
    /// implementation did when the context was null.
    /// </remarks>
    public partial class PerRequestCacheManager : ICacheManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        public PerRequestCacheManager(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the per-request item dictionary, or null when there is no current request
        /// </summary>
        protected virtual IDictionary<object, object> GetItems()
        {
            if (_httpContextAccessor == null)
                return null;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            return httpContext.Items;
        }

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

            object value;
            if (!items.TryGetValue(key, out value) || value == null)
                return default(T);

            return (T)value;
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            var items = GetItems();
            if (items == null)
                return;

            if (data != null)
            {
                //HttpContext.Items is an IDictionary<object, object>; the indexer both adds
                //and replaces, so no Contains/Add split is needed
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

            object value;
            return items.TryGetValue(key, out value) && value != null;
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">/key</param>
        public virtual void Remove(string key)
        {
            var items = GetItems();
            if (items == null)
                return;

            items.Remove(key);
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

            this.RemoveByPattern(pattern, items.Keys
                .Where(p => p != null)
                .Select(p => p.ToString())
                .ToList());
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            var items = GetItems();
            if (items == null)
                return;

            items.Clear();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
