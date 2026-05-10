using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching during an HTTP request (short term caching)
    /// </summary>
    public partial class PerRequestCacheManager : ICacheManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PerRequestCacheManager(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        protected virtual IDictionary GetItems()
        {
            return _httpContextAccessor?.HttpContext?.Items as IDictionary;
        }

        public virtual T Get<T>(string key)
        {
            var items = GetItems();
            if (items == null)
                return default(T);
            return (T)items[key];
        }

        public virtual void Set(string key, object data, int cacheTime)
        {
            var items = GetItems();
            if (items == null)
                return;
            if (data != null)
            {
                if (items.Contains(key))
                    items[key] = data;
                else
                    items.Add(key, data);
            }
        }

        public virtual bool IsSet(string key)
        {
            var items = GetItems();
            if (items == null)
                return false;
            return (items[key] != null);
        }

        public virtual void Remove(string key)
        {
            var items = GetItems();
            if (items == null)
                return;
            items.Remove(key);
        }

        public virtual void RemoveByPattern(string pattern)
        {
            var items = GetItems();
            if (items == null)
                return;
            this.RemoveByPattern(pattern, items.Keys.Cast<object>().Select(p => p.ToString()));
        }

        public virtual void Clear()
        {
            var items = GetItems();
            if (items == null)
                return;
            items.Clear();
        }

        public virtual void Dispose()
        {
        }
    }
}
