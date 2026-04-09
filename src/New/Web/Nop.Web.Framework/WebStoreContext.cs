using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Services.Stores;

namespace Nop.Web.Framework;

/// <summary>
/// Store context — resolves current store from request host header.
/// </summary>
public class WebStoreContext : IStoreContext
{
    private readonly IStoreService _storeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Store? _cachedStore;

    public WebStoreContext(IStoreService storeService, IHttpContextAccessor httpContextAccessor)
    {
        _storeService = storeService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Store CurrentStore
    {
        get
        {
            if (_cachedStore != null)
                return _cachedStore;

            var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
            var allStores = _storeService.GetAllStoresAsync().GetAwaiter().GetResult();

            var store = !string.IsNullOrEmpty(host)
                ? allStores.FirstOrDefault(s => ContainsHost(s, host))
                : null;

            store ??= allStores.FirstOrDefault();

            _cachedStore = store ?? throw new InvalidOperationException("No store could be loaded");
            return _cachedStore;
        }
    }

    private static bool ContainsHost(Store store, string host)
    {
        if (string.IsNullOrEmpty(store.Hosts))
            return false;

        return store.Hosts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(h => h.Equals(host, StringComparison.OrdinalIgnoreCase));
    }
}
